using System.Numerics;

namespace Parsec.Core.Geometry;

/// <summary>
/// A ray with origin and unit direction.
/// </summary>
public readonly record struct Ray(Vector3 Origin, Vector3 Direction)
{
    /// <summary>
    /// Returns the point at distance <paramref name="t"/> along the ray.
    /// </summary>
    public Vector3 At(float t) => Origin + Direction * t;

    /// <summary>
    /// Returns a ray with the given origin and direction, with the direction
    /// normalized to unit length.
    /// </summary>
    public static Ray Normalized(Vector3 origin, Vector3 direction)
    {
        float len = direction.Length();
        if (len < float.Epsilon)
            throw new ArgumentException("zero-length ray direction", nameof(direction));
        return new Ray(origin, direction / len);
    }
}
