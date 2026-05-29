using System.Numerics;
using Parsec.Core.Geometry;
using Parsec.Core.Ifs;
using Parsec.Core.Transforms;

namespace Parsec.Rendering.Raymarching;

/// <summary>
/// Configuration for <see cref="IFS3DDistanceEstimator"/>.
/// </summary>
/// <param name="MaxDepth">
/// Hard cap on recursion depth. Each level multiplies cost by N (the number
/// of IFS maps), so depth has dramatic compute implications. Default 12 is a
/// reasonable starting point for IFSes with contraction ratio ~0.5; lower for
/// higher branching factors, higher for ratios near 1.
/// </param>
/// <param name="DetailEpsilon">
/// Relative resolution at which to stop recursing. A child is not explored
/// further once its bounding sphere radius falls below
/// <c>DetailEpsilon * dist(p, sphere)</c>, i.e. when it subtends an angle
/// smaller than this from the query point. Tighter values give crisper
/// surfaces at the cost of more compute.
/// </param>
public sealed record IFS3DDistanceEstimatorConfig(
    int MaxDepth = 10,
    float DetailEpsilon = 1e-2f);

/// <summary>
/// Distance estimator for a multi-map affine IFS in 3D, via Hart-style
/// recursive descent over the IFS's self-similar bounding-sphere hierarchy
/// with branch-and-bound pruning.
/// </summary>
/// <remarks>
/// <para>
/// At each recursion level, the algorithm holds a bounding sphere of a
/// sub-attractor (a transformed copy of the full attractor). It computes
/// the distance from the query point to that sphere; if the distance is
/// already greater than the current best, the entire subtree is pruned. If
/// the sphere is small enough relative to its distance from the query point,
/// recursion terminates with the sphere distance as the estimate.
/// </para>
/// <para>
/// Children are explored nearest-first to maximize pruning effectiveness.
/// </para>
/// <para>
/// Correctness: the returned value is always a true lower bound on the
/// actual distance to the attractor. The bound is tightest when the IFS is
/// composed of similarities (uniform scale + rotation + translation); for
/// anisotropic affine maps it's loose by a factor of (largest singular
/// value / smallest singular value) per level.
/// </para>
/// </remarks>
public sealed class IFS3DDistanceEstimator : IDistanceEstimator3D
{
    private readonly AffineMap3D[] _transforms;
    private readonly float[] _spectralNorms;
    private readonly BoundingSphere _attractorBoundingSphere;
    private readonly IFS3DDistanceEstimatorConfig _config;

    public IFS3DDistanceEstimator(IFS3D ifs, IFS3DDistanceEstimatorConfig? config = null)
    {
        if (ifs.Nodes.IsDefaultOrEmpty)
            throw new ArgumentException("IFS must have at least one node", nameof(ifs));

        _config = config ?? new IFS3DDistanceEstimatorConfig();

        // Flatten to a transforms array, precomputing spectral norms. We use
        // these on every recursion step (the radius scaling factor for
        // transformed bounding spheres), so caching saves a power iteration
        // per call site.
        var nodes = ifs.Nodes;
        _transforms = new AffineMap3D[nodes.Length];
        _spectralNorms = new float[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            _transforms[i] = nodes[i].Transform;
            _spectralNorms[i] = nodes[i].Transform.SpectralNorm;
        }

        _attractorBoundingSphere = ifs.ComputeBoundingSphere();
    }

    public BoundingSphere BoundingSphere => _attractorBoundingSphere;

    public float Estimate(Vector3 point)
    {
        // Far outside the attractor's bounding sphere: the sphere distance is
        // itself a tight estimate, no recursion needed.
        float startDist = _attractorBoundingSphere.SignedDistance(point);
        if (startDist > _attractorBoundingSphere.Radius * 2f)
            return startDist;

        // Inside or near: recurse with no initial bound, letting the algorithm
        // build a true lower bound from the leaves. The recursion tracks the
        // accumulated transform T_w (where w is the word of indices). The
        // current "sphere" at any node is T_w(B), where B is the attractor's
        // bounding sphere. Children are obtained by composing on the INNER
        // (right) side: child i has transform T_w ∘ T_i, NOT T_i ∘ T_w.
        // This is because the Hutchinson decomposition gives
        // T_w(A) = ∪_i T_w(T_i(A)), where T_i is the innermost transform.
        float best = float.MaxValue;
        Recurse(point, AffineMap3D.Identity, _attractorBoundingSphere.Center,
                _attractorBoundingSphere.Radius, 0, ref best);
        return best;
    }

    /// <summary>
    /// Recurse with the accumulated outer transform <paramref name="accumulated"/>
    /// (so far applied to the base bounding sphere); the current sphere is
    /// described by <paramref name="sphereCenter"/> and <paramref name="sphereRadius"/>
    /// (already computed from <paramref name="accumulated"/> applied to the base).
    /// </summary>
    private void Recurse(
        Vector3 point,
        AffineMap3D accumulated,
        Vector3 sphereCenter,
        float sphereRadius,
        int depth,
        ref float best)
    {
        float distToSphere = MathF.Max(0f, Vector3.Distance(point, sphereCenter) - sphereRadius);
        if (distToSphere >= best) return;

        if (depth >= _config.MaxDepth ||
            sphereRadius < _config.DetailEpsilon * MathF.Max(distToSphere, sphereRadius))
        {
            if (distToSphere < best) best = distToSphere;
            return;
        }

        int n = _transforms.Length;
        Span<int> indices = n <= 32 ? stackalloc int[n] : new int[n];
        Span<float> dists = n <= 32 ? stackalloc float[n] : new float[n];
        Span<Vector3> centers = n <= 32 ? stackalloc Vector3[n] : new Vector3[n];
        Span<float> radii = n <= 32 ? stackalloc float[n] : new float[n];
        Span<AffineMap3D> childTransforms = n <= 32 ? stackalloc AffineMap3D[n] : new AffineMap3D[n];

        // Child i has accumulated transform = T_i.Then(accumulated), meaning
        // T_i is applied first (innermost), then `accumulated` is applied on top.
        // The resulting sphere center is childTransform.Apply(baseCenter), and
        // the radius is multiplied by the spectral norm of the new accumulated
        // transform — but we can compute that more efficiently: each level
        // multiplies the radius by the spectral norm of the latest T_i.
        for (int i = 0; i < n; i++)
        {
            var childTransform = _transforms[i].Then(accumulated);
            childTransforms[i] = childTransform;
            centers[i] = childTransform.Apply(_attractorBoundingSphere.Center);
            radii[i] = sphereRadius * _spectralNorms[i];
            dists[i] = MathF.Max(0f, Vector3.Distance(point, centers[i]) - radii[i]);
            indices[i] = i;
        }

        // Insertion sort by distance.
        for (int i = 1; i < n; i++)
        {
            int key = indices[i];
            float keyDist = dists[key];
            int j = i - 1;
            while (j >= 0 && dists[indices[j]] > keyDist)
            {
                indices[j + 1] = indices[j];
                j--;
            }
            indices[j + 1] = key;
        }

        for (int k = 0; k < n; k++)
        {
            int i = indices[k];
            if (dists[i] >= best) break;
            Recurse(point, childTransforms[i], centers[i], radii[i], depth + 1, ref best);
        }
    }
}
