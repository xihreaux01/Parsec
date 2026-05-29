using System.Numerics;
using Parsec.Core.Ifs;
using Parsec.Core.Transforms;
using SkiaSharp;

namespace Parsec.Cli.Examples;

/// <summary>
/// A 3D math sanity check: builds the canonical 3D IFSes, computes their
/// bounding spheres, and prints diagnostics. No rendering — this exists to
/// validate the 3D math layer before the raymarcher is built on top of it.
/// </summary>
public sealed class Sanity3DExample : IExample
{
    public string Name => "sanity-3d";
    public string Description => "3D math sanity check (no render, prints to console)";

    public SKBitmap? Render()
    {
        Console.WriteLine();
        Console.WriteLine("=== 3D math sanity check ===");
        Console.WriteLine();

        // Verify AffineMap3D basics.
        TestAffineBasics();
        Console.WriteLine();

        // Verify the canonical IFSes.
        TestIfs("Sierpinski tetrahedron", CanonicalIFS3D.SierpinskiTetrahedron());
        TestIfs("Octree corners",         CanonicalIFS3D.OctreeCorners());
        TestIfs("Menger sponge",          CanonicalIFS3D.MengerSponge());

        Console.WriteLine();
        Console.WriteLine("(No image produced — this is a math-only sanity check.)");

        return null;
    }

    private static void TestAffineBasics()
    {
        Console.WriteLine("AffineMap3D basics:");

        // Identity application
        var id = AffineMap3D.Identity;
        var p = new Vector3(1.5f, -0.7f, 3.2f);
        var pId = id.Apply(p);
        Console.WriteLine($"  Identity * (1.5, -0.7, 3.2)             = {pId}  (expected same)");

        // Translation
        var t = AffineMap3D.Translation(1, 2, 3);
        Console.WriteLine($"  T(1,2,3) * (0,0,0)                       = {t.Apply(Vector3.Zero)}  (expected 1,2,3)");

        // Rotation about Z by 90deg sends +X to +Y
        var rz = AffineMap3D.RotationZ(MathF.PI / 2f);
        var rotX = rz.Apply(Vector3.UnitX);
        Console.WriteLine($"  Rz(90°) * (1,0,0)                        = {rotX}  (expected ~0,1,0)");

        // Composition: T(1,2,3) ∘ Rz(90°): apply rotation first, then translation
        var composed = AffineMap3D.Compose(rz, t);
        var composedResult = composed.Apply(Vector3.UnitX);
        Console.WriteLine($"  Compose(Rz, T) * (1,0,0)                 = {composedResult}  (expected ~1,3,3)");

        // Inverse
        var rot = AffineMap3D.RotationY(0.7f).Then(AffineMap3D.Scale(0.5f)).Then(AffineMap3D.Translation(1, 0, 0));
        if (rot.TryInvert(out var inv))
        {
            var roundTrip = rot.Then(inv).Apply(p);
            float err = (roundTrip - p).Length();
            Console.WriteLine($"  Inverse round-trip error                 = {err:E2}  (expected ~0)");
        }
        else
        {
            Console.WriteLine("  Inverse: FAILED (matrix reported singular)");
        }

        // Spectral norm: pure rotation = 1, scale(0.5) = 0.5
        Console.WriteLine($"  SpectralNorm(Identity)                   = {AffineMap3D.Identity.SpectralNorm:F4}  (expected 1.0)");
        Console.WriteLine($"  SpectralNorm(RotZ(0.7))                  = {AffineMap3D.RotationZ(0.7f).SpectralNorm:F4}  (expected 1.0)");
        Console.WriteLine($"  SpectralNorm(Scale(0.5))                 = {AffineMap3D.Scale(0.5f).SpectralNorm:F4}  (expected 0.5)");
        Console.WriteLine($"  SpectralNorm(Scale(2.0))                 = {AffineMap3D.Scale(2.0f).SpectralNorm:F4}  (expected 2.0)");
    }

    private static void TestIfs(string name, IFS3D ifs)
    {
        Console.WriteLine();
        Console.WriteLine($"{name}:");
        Console.WriteLine($"  Nodes:                   {ifs.Nodes.Length}");
        Console.WriteLine($"  Is contractive:          {ifs.IsContractive}");
        Console.WriteLine($"  Max contraction ratio:   {ifs.MaxContractionRatio:F4}");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var sphere = ifs.ComputeBoundingSphere();
        stopwatch.Stop();
        Console.WriteLine($"  Bounding sphere:         center={sphere.Center}, radius={sphere.Radius:F4}");
        Console.WriteLine($"  Bounding sphere time:    {stopwatch.Elapsed.TotalMilliseconds:F2} ms");
    }
}
