using System.Numerics;
using Parsec.Rendering.Raymarching;

namespace Parsec.App;

/// <summary>
/// A free-fly (first-person) camera: a position plus yaw/pitch look angles.
/// Movement is along the camera's own basis (WASD + vertical), with speed
/// scaled externally by proximity to the surface so it feels right at any
/// scale. Builds a <see cref="Camera3D"/> for rendering.
/// </summary>
public sealed class FlyCamera
{
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }    // radians; rotation about world up (Y)
    public float Pitch { get; set; }  // radians; clamped to avoid flipping
    public float FovRadians { get; set; } = MathF.PI / 5f;

    private const float PitchLimit = 1.55f; // just under pi/2

    public FlyCamera(Vector3 position, float yaw, float pitch)
    {
        Position = position;
        Yaw = yaw;
        Pitch = pitch;
    }

    /// <summary>Forward unit vector from yaw/pitch (right-handed, Y up).</summary>
    public Vector3 Forward
    {
        get
        {
            float cp = MathF.Cos(Pitch), sp = MathF.Sin(Pitch);
            float cy = MathF.Cos(Yaw), sy = MathF.Sin(Yaw);
            return Vector3.Normalize(new Vector3(cp * sy, sp, -cp * cy));
        }
    }

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
    public Vector3 UpLocal => Vector3.Cross(Right, Forward);

    /// <summary>Apply a mouse-look delta (in radians).</summary>
    public void Look(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch = Math.Clamp(Pitch + deltaPitch, -PitchLimit, PitchLimit);
    }

    /// <summary>
    /// Move along the camera basis. <paramref name="local"/> components are
    /// (strafe, vertical, forward); <paramref name="distance"/> scales all.
    /// </summary>
    public void Move(Vector3 local, float distance)
    {
        Position += (Right * local.X + Vector3.UnitY * local.Y + Forward * local.Z) * distance;
    }

    /// <summary>
    /// Orient the camera (set Yaw/Pitch) to look from its current Position toward
    /// <paramref name="target"/>. Inverts the Forward(yaw,pitch) formula:
    /// f = (cp*sy, sp, -cp*cy)  =>  pitch = asin(f.y), yaw = atan2(f.x, -f.z).
    /// </summary>
    public void LookAt(Vector3 target)
    {
        var f = target - Position;
        if (f.LengthSquared() < 1e-12f) return;
        f = Vector3.Normalize(f);
        Pitch = Math.Clamp(MathF.Asin(f.Y), -PitchLimit, PitchLimit);
        Yaw = MathF.Atan2(f.X, -f.Z);
    }

    public Camera3D ToCamera(int width, int height) => new(
        position: Position,
        lookAt: Position + Forward,
        up: Vector3.UnitY,
        verticalFovRadians: FovRadians,
        aspectRatio: (float)width / height);
}
