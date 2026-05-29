using System.Numerics;
using Parsec.Core.Transforms;

namespace Parsec.Core.Geometry;

/// <summary>
/// A sphere in 3D, used both as a bounding volume for an IFS attractor and as
/// the recursion primitive for distance estimation.
/// </summary>
public readonly record struct BoundingSphere(Vector3 Center, float Radius)
{
    /// <summary>
    /// Unit sphere centered at the origin.
    /// </summary>
    public static BoundingSphere Unit { get; } = new(Vector3.Zero, 1f);

    /// <summary>
    /// Unsigned distance from <paramref name="point"/> to the sphere boundary.
    /// </summary>
    /// <remarks>
    /// Returns 0 inside the sphere. For raymarching/DE purposes we typically
    /// want the *signed* distance (negative inside); use <see cref="SignedDistance"/>.
    /// </remarks>
    public float Distance(Vector3 point) =>
        MathF.Max(0f, Vector3.Distance(point, Center) - Radius);

    /// <summary>
    /// Signed distance from <paramref name="point"/> to the sphere boundary:
    /// negative inside, positive outside, zero on the surface.
    /// </summary>
    public float SignedDistance(Vector3 point) =>
        Vector3.Distance(point, Center) - Radius;

    /// <summary>
    /// The smallest sphere containing the image of this sphere under an affine map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Under an affine map <c>p' = M*p + t</c>, the image of a sphere is an
    /// ellipsoid (unless M is a similarity). The smallest enclosing sphere of
    /// that ellipsoid has center <c>M*Center + t</c> and radius
    /// <c>Radius * sigma_max(M)</c>, where sigma_max is the largest singular value.
    /// </para>
    /// <para>
    /// This is an overestimate when M is anisotropic, but it's a valid
    /// bounding sphere — that's what we need for the bounding hierarchy.
    /// </para>
    /// </remarks>
    public BoundingSphere TransformedBy(AffineMap3D map) =>
        new(map.Apply(Center), Radius * map.SpectralNorm);

    /// <summary>
    /// Smallest sphere centered at the centroid of input centers that contains
    /// every input sphere. Returns <see cref="Empty"/> if the input is empty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is <em>not</em> the true minimum enclosing sphere — a smaller
    /// off-center sphere may exist for asymmetric inputs. But it is:
    /// <list type="bullet">
    /// <item>Correct (always contains every input sphere).</item>
    /// <item>Symmetric (gives the symmetric answer for symmetric inputs).</item>
    /// <item>Order-independent (the answer doesn't depend on iteration order).</item>
    /// <item>Cheap (O(n)).</item>
    /// </list>
    /// The previous pairwise-fold implementation was none of (2) or (3): for
    /// a symmetric IFS like the Sierpiński tetrahedron, it converged to a
    /// drifted, ~30% over-large bound. For asymmetric IFSes this version is
    /// also loose by some constant factor, but never asymmetrically wrong.
    /// </para>
    /// <para>
    /// TODO: For tighter bounds on asymmetric IFSes, implement Welzl's
    /// minimum-enclosing-sphere on the input centers, then expand to cover
    /// each input's radius. The current implementation is sufficient for
    /// DE pruning in the canonical IFSes.
    /// </para>
    /// </remarks>
    public static BoundingSphere Enclose(ReadOnlySpan<BoundingSphere> spheres)
    {
        if (spheres.Length == 0) return Empty;
        if (spheres.Length == 1) return spheres[0];

        // Centroid of input centers.
        Vector3 centroid = Vector3.Zero;
        for (int i = 0; i < spheres.Length; i++) centroid += spheres[i].Center;
        centroid /= spheres.Length;

        // Radius: max distance from centroid to any input sphere's surface.
        float maxR = 0f;
        for (int i = 0; i < spheres.Length; i++)
        {
            float d = Vector3.Distance(centroid, spheres[i].Center) + spheres[i].Radius;
            if (d > maxR) maxR = d;
        }
        return new BoundingSphere(centroid, maxR);
    }

    /// <summary>
    /// Smallest sphere centered at the centroid containing both <paramref name="a"/>
    /// and <paramref name="b"/>. Provided for backwards convenience; prefer
    /// <see cref="Enclose(ReadOnlySpan{BoundingSphere})"/> for general use.
    /// </summary>
    public static BoundingSphere Enclose(BoundingSphere a, BoundingSphere b)
    {
        Span<BoundingSphere> pair = stackalloc BoundingSphere[2];
        pair[0] = a; pair[1] = b;
        return Enclose(pair);
    }

    /// <summary>A degenerate "empty" sphere — radius -1 marks it as invalid.</summary>
    public static BoundingSphere Empty { get; } = new(Vector3.Zero, -1f);

    public bool IsEmpty => Radius < 0f;
}
