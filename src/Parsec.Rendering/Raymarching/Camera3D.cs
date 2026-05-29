using System.Numerics;
using Parsec.Core.Geometry;

namespace Parsec.Rendering.Raymarching;

/// <summary>
/// A pinhole camera in 3D space, parameterized by position, look-at target,
/// up direction, and vertical field of view.
/// </summary>
/// <remarks>
/// The camera produces rays for normalized image-space coordinates u, v in
/// [0, 1] (with v=0 at the bottom of the image, v=1 at the top). Internally
/// it constructs a right-handed view frame and computes rays as
/// <c>direction = normalize(forward + (2u-1)*tanFovX*right + (2v-1)*tanFovY*up')</c>.
/// </remarks>
public sealed class Camera3D
{
    public Vector3 Position { get; }
    public Vector3 LookAt { get; }
    public Vector3 Up { get; }
    public float VerticalFovRadians { get; }
    public float AspectRatio { get; }

    // Precomputed view frame and frustum scale.
    private readonly Vector3 _forward;
    private readonly Vector3 _right;
    private readonly Vector3 _upPrime;
    private readonly float _tanFovX;
    private readonly float _tanFovY;

    public Camera3D(
        Vector3 position,
        Vector3 lookAt,
        Vector3 up,
        float verticalFovRadians,
        float aspectRatio)
    {
        Position = position;
        LookAt = lookAt;
        Up = up;
        VerticalFovRadians = verticalFovRadians;
        AspectRatio = aspectRatio;

        var forwardRaw = lookAt - position;
        float forwardLen = forwardRaw.Length();
        if (forwardLen < float.Epsilon)
            throw new ArgumentException("camera position coincides with lookAt target");
        _forward = forwardRaw / forwardLen;

        // Right-handed frame: right = forward × up, then re-orthogonalize up.
        _right = Vector3.Normalize(Vector3.Cross(_forward, up));
        _upPrime = Vector3.Cross(_right, _forward);

        _tanFovY = MathF.Tan(verticalFovRadians * 0.5f);
        _tanFovX = _tanFovY * aspectRatio;
    }

    /// <summary>
    /// Generate a ray for normalized image coordinates <paramref name="u"/>, <paramref name="v"/>
    /// (both in [0, 1], with v=0 at the bottom).
    /// </summary>
    public Ray RayForUV(float u, float v)
    {
        float x = (2f * u - 1f) * _tanFovX;
        float y = (2f * v - 1f) * _tanFovY;
        var dir = _forward + x * _right + y * _upPrime;
        return Ray.Normalized(Position, dir);
    }
}
