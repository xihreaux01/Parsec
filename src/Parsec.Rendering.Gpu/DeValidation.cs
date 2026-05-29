using System.Numerics;
using Parsec.Core.Ifs;
using Parsec.Rendering.Raymarching;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Phase-2a validation: build the GPU DE for a known IFS, query a set of
/// representative points, and compare to the CPU DE values. If they agree
/// (to within float precision tolerance), the GPU port is correct and we
/// proceed to wire up the raymarcher.
/// </summary>
public static class DeValidation
{
    public static int Run()
    {
        Console.WriteLine("Parsec GPU DE validation");
        Console.WriteLine("========================");
        Console.WriteLine();

        using var ctx = new HeadlessGLContext();
        Console.WriteLine("GL context ready.");
        Console.WriteLine();

        Console.WriteLine("Available embedded shader resources:");
        foreach (var name in ShaderLoader.AvailableResources())
            Console.WriteLine($"  {name}");
        Console.WriteLine();

        var gl = ctx.Gl;
        int failures = 0;
        failures += ValidateIFS(gl,
            "Sierpiński tetrahedron",
            CanonicalIFS3D.SierpinskiTetrahedron(),
            TetrahedronTestPoints(),
            maxDepth: 10, detailEpsilon: 1e-2f);

        failures += ValidateIFS(gl,
            "Trefoil knot (N=24, s=0.20)",
            KnotIFS.TrefoilKnot(24, 0.20f),
            TrefoilTestPoints(),
            maxDepth: 10, detailEpsilon: 1e-2f);

        Console.WriteLine();
        if (failures == 0)
        {
            Console.WriteLine("PASS — GPU DE matches CPU DE on all test points.");
            return 0;
        }
        else
        {
            Console.WriteLine($"FAIL — {failures} mismatch(es). GPU port has a bug.");
            return 1;
        }
    }

    private static int ValidateIFS(Gl gl,
        
        string name, IFS3D ifs, Vector3[] points,
        int maxDepth, float detailEpsilon)
    {
        Console.WriteLine($"--- {name} ({ifs.Nodes.Length} maps) ---");

        // CPU reference.
        var cpu = new IFS3DDistanceEstimator(ifs, new IFS3DDistanceEstimatorConfig(maxDepth, detailEpsilon));
        var cpuTimer = System.Diagnostics.Stopwatch.StartNew();
        var cpuResults = new float[points.Length];
        for (int i = 0; i < points.Length; i++)
            cpuResults[i] = cpu.Estimate(points[i]);
        cpuTimer.Stop();

        // GPU.
        using var gpu = new GpuIFS3DDistanceEstimator(gl, ifs, maxDepth, detailEpsilon);
        // Warm up — first dispatch includes shader-binding and buffer-allocation overhead.
        gpu.Estimate(points[..1]);
        var gpuTimer = System.Diagnostics.Stopwatch.StartNew();
        var gpuResults = gpu.Estimate(points);
        gpuTimer.Stop();

        // Compare.
        int mismatches = 0;
        const float absTol = 1e-3f;
        const float relTol = 1e-2f;
        for (int i = 0; i < points.Length; i++)
        {
            float c = cpuResults[i];
            float g = gpuResults[i];
            float diff = MathF.Abs(c - g);
            float tol = absTol + relTol * MathF.Max(MathF.Abs(c), MathF.Abs(g));
            bool ok = diff < tol;
            if (!ok)
            {
                mismatches++;
                Console.WriteLine($"  MISMATCH at {points[i]}: cpu={c:F6} gpu={g:F6} diff={diff:F6} tol={tol:F6}");
            }
            else if (i < 6)
            {
                Console.WriteLine($"  {points[i]}: cpu={c:F6} gpu={g:F6} OK");
            }
        }

        Console.WriteLine($"  Timing: CPU {cpuTimer.ElapsedMilliseconds}ms, " +
                          $"GPU {gpuTimer.ElapsedMilliseconds}ms " +
                          $"({(float)cpuTimer.ElapsedMilliseconds / Math.Max(1, gpuTimer.ElapsedMilliseconds):F1}× speedup) " +
                          $"over {points.Length} points");
        if (mismatches > 0)
            Console.WriteLine($"  {mismatches}/{points.Length} mismatches");
        Console.WriteLine();
        return mismatches;
    }

    private static Vector3[] TetrahedronTestPoints()
    {
        return new Vector3[]
        {
            // Vertices (on attractor; distance should be ~0).
            new(0, 0, 0),
            new(1, 1, 0),
            new(1, 0, 1),
            new(0, 1, 1),
            // Edge midpoints (on attractor at depth 1).
            new(0.5f, 0.5f, 0f),
            new(0.5f, 0f, 0.5f),
            new(0f, 0.5f, 0.5f),
            // Centroid of attractor's bounding sphere (in a hole, off-attractor).
            new(0.5f, 0.5f, 0.5f),
            // Off-attractor (far).
            new(2f, 2f, 2f),
            new(-1f, 0f, 0f),
            new(5f, 5f, 5f),
            // Random interior points.
            new(0.25f, 0.25f, 0.25f),
            new(0.75f, 0.25f, 0.25f),
            new(0.3f, 0.7f, 0.1f),
            new(0.1f, 0.1f, 0.9f),
        };
    }

    private static Vector3[] TrefoilTestPoints()
    {
        // The trefoil's bounding sphere is centered at origin with radius ~3.
        var trefoil = (float t) => new Vector3(
            MathF.Sin(t) + 2f * MathF.Sin(2f * t),
            MathF.Cos(t) - 2f * MathF.Cos(2f * t),
            -MathF.Sin(3f * t));

        var points = new List<Vector3>();
        // Sample points: on the attractor (fixed points of each T_i).
        for (int i = 0; i < 24; i += 6)
            points.Add(trefoil(MathF.PI * 2f * i / 24f));

        // Off-attractor points.
        points.Add(new Vector3(0, 0, 0));      // empty interior
        points.Add(new Vector3(0, 0, 2));      // above plane
        points.Add(new Vector3(5, 5, 5));      // far away
        points.Add(new Vector3(2.5f, 0, 0));   // near surface
        points.Add(new Vector3(-2f, -2f, 0));  // off to side
        points.Add(new Vector3(0, 3, 0));      // edge of bound

        return points.ToArray();
    }
}
