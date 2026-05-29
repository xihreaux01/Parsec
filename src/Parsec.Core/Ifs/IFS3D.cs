using System.Collections.Immutable;
using System.Numerics;
using Parsec.Core.Geometry;

namespace Parsec.Core.Ifs;

/// <summary>
/// A 3D iterated function system: a set of contractive (or near-contractive)
/// affine maps whose Hutchinson union has a unique compact attractor.
/// </summary>
/// <remarks>
/// <para>
/// Structurally parallel to <see cref="IFS2D"/>. Distance estimation and
/// raymarching need a bounding sphere for the attractor; this type exposes
/// <see cref="ComputeBoundingSphere"/> to derive one, but does not cache the
/// result itself — caching would interfere with record value-equality.
/// Renderers that hold an <see cref="IFS3D"/> are expected to compute and
/// hold the bounding sphere alongside it.
/// </para>
/// </remarks>
public sealed record IFS3D(ImmutableArray<IFSNode3D> Nodes)
{
    public static IFS3D FromNodes(params IFSNode3D[] nodes) =>
        new(nodes.ToImmutableArray());

    public static IFS3D Union(params IFS3D[] systems)
    {
        if (systems.Length == 0)
            return new IFS3D(ImmutableArray<IFSNode3D>.Empty);
        if (systems.Length == 1)
            return systems[0];

        var builder = ImmutableArray.CreateBuilder<IFSNode3D>();
        foreach (var s in systems) builder.AddRange(s.Nodes);
        return new IFS3D(builder.ToImmutable());
    }

    /// <summary>
    /// <c>a | b</c> is <see cref="Union"/>.
    /// </summary>
    public static IFS3D operator |(IFS3D a, IFS3D b) => Union(a, b);

    public float TotalWeight
    {
        get
        {
            float sum = 0f;
            foreach (var n in Nodes) sum += n.Weight;
            return sum;
        }
    }

    public bool IsContractive
    {
        get
        {
            foreach (var n in Nodes)
                if (!n.Transform.IsContractive) return false;
            return true;
        }
    }

    /// <summary>
    /// The maximum spectral norm across all nodes' primary transforms.
    /// For a contractive IFS this is in [0, 1) and bounds the geometric
    /// convergence rate of bounding-sphere iteration.
    /// </summary>
    public float MaxContractionRatio
    {
        get
        {
            float m = 0f;
            foreach (var n in Nodes)
            {
                float s = n.Transform.SpectralNorm;
                if (s > m) m = s;
            }
            return m;
        }
    }

    /// <summary>
    /// Compute a bounding sphere for the attractor by fixed-point iteration
    /// of the Hutchinson operator on spheres.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a contractive IFS this converges geometrically at rate
    /// <see cref="MaxContractionRatio"/>. For non-contractive IFSes the
    /// computation may not converge; in that case the result after
    /// <paramref name="maxIterations"/> is returned (and may be loose).
    /// </para>
    /// <para>
    /// The bound is correct (contains the attractor under contractive maps)
    /// but not necessarily tight — it overestimates when maps are anisotropic,
    /// because each map's image is bounded by a sphere rather than an
    /// ellipsoid.
    /// </para>
    /// <para>
    /// This is an O(N * iterations) computation where N is the number of
    /// nodes. Typical convergence is &lt; 30 iterations for IFSes with
    /// contraction ratio &lt; 0.7. Renderers should compute this once and
    /// cache the result.
    /// </para>
    /// </remarks>
    public BoundingSphere ComputeBoundingSphere(
        int maxIterations = 200,
        float convergenceEpsilon = 1e-5f)
    {
        if (Nodes.IsDefaultOrEmpty)
            return BoundingSphere.Empty;

        // Initial sphere: centered at the centroid of map translations.
        // The translations are good starting hints because each node's fixed
        // point is near its translation when the linear part is small.
        Vector3 centroid = Vector3.Zero;
        foreach (var node in Nodes)
            centroid += new Vector3(node.Transform.Tx, node.Transform.Ty, node.Transform.Tz);
        centroid /= Nodes.Length;

        float maxDistSq = 0f;
        foreach (var node in Nodes)
        {
            var t = new Vector3(node.Transform.Tx, node.Transform.Ty, node.Transform.Tz);
            float d = (t - centroid).LengthSquared();
            if (d > maxDistSq) maxDistSq = d;
        }
        float r0 = MathF.Sqrt(maxDistSq) + 1f;
        var sphere = new BoundingSphere(centroid, MathF.Max(r0, 1f));

        // Inflate the initial sphere until it's invariant-safe: it must
        // contain its own Hutchinson image, or the iteration could shrink
        // below the true attractor.
        for (int safety = 0; safety < 32; safety++)
        {
            var hutchinson = HutchinsonOfSphere(sphere);
            if (SphereContains(sphere, hutchinson)) break;
            sphere = new BoundingSphere(sphere.Center, sphere.Radius * 2f);
        }

        // Iterate to convergence.
        for (int i = 0; i < maxIterations; i++)
        {
            var next = HutchinsonOfSphere(sphere);
            bool radiusConverged = MathF.Abs(next.Radius - sphere.Radius)
                                   < convergenceEpsilon * MathF.Max(sphere.Radius, 1f);
            bool centerConverged = Vector3.Distance(next.Center, sphere.Center)
                                   < convergenceEpsilon * MathF.Max(sphere.Radius, 1f);
            sphere = next;
            if (radiusConverged && centerConverged) return sphere;
        }
        return sphere;
    }

    /// <summary>
    /// Smallest sphere containing the union of <c>T_i(input)</c> over all nodes.
    /// </summary>
    private BoundingSphere HutchinsonOfSphere(BoundingSphere input)
    {
        Span<BoundingSphere> images = Nodes.Length <= 32
            ? stackalloc BoundingSphere[Nodes.Length]
            : new BoundingSphere[Nodes.Length];
        for (int i = 0; i < Nodes.Length; i++)
            images[i] = input.TransformedBy(Nodes[i].Transform);
        return BoundingSphere.Enclose(images);
    }

    private static bool SphereContains(BoundingSphere outer, BoundingSphere inner)
    {
        float d = Vector3.Distance(outer.Center, inner.Center);
        return d + inner.Radius <= outer.Radius + 1e-6f;
    }
}
