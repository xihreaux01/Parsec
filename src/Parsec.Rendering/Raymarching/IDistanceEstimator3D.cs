using System.Numerics;
using Parsec.Core.Geometry;

namespace Parsec.Rendering.Raymarching;

/// <summary>
/// A 3D distance estimator: a function returning a lower bound on the
/// unsigned distance from a query point to some implicit surface.
/// </summary>
/// <remarks>
/// <para>
/// For sphere-tracing raymarching to be correct, the value returned must be a
/// <em>true lower bound</em> on the actual distance: never larger than the
/// real distance. A DE that overestimates will cause rays to skip past
/// surfaces, producing artifacts and missed hits.
/// </para>
/// <para>
/// Implementations include <see cref="IFS3DDistanceEstimator"/> (Hart-style
/// branch-and-bound for multi-map affine IFSes), and future Mandelbox/
/// Mandelbulb-style fold estimators.
/// </para>
/// </remarks>
public interface IDistanceEstimator3D
{
    /// <summary>
    /// Returns a lower bound on the distance from <paramref name="point"/> to
    /// the implicit surface.
    /// </summary>
    float Estimate(Vector3 point);

    /// <summary>
    /// A bounding sphere outside of which the implicit surface lies. Used by
    /// the raymarcher to skip the empty region around the surface efficiently.
    /// </summary>
    BoundingSphere BoundingSphere { get; }
}
