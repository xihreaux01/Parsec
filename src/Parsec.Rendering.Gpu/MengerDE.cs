using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the rotated folded Menger DE (menger_core.glsl), used only for
/// fly-camera speed.
/// </summary>
public static class MengerDE
{
    private static Matrix4x4 Euler(float ax, float ay, float az) =>
        Matrix4x4.CreateRotationX(ax) *
        Matrix4x4.CreateRotationY(ay) *
        Matrix4x4.CreateRotationZ(az);

    public static float Estimate(Vector3 p, MengerParams mg)
    {
        var R = Euler(mg.Rotation.X, mg.Rotation.Y, mg.Rotation.Z);
        var z = p;
        float dr = 1.0f;
        for (int i = 0; i < mg.Iterations; i++)
        {
            z = Vector3.Transform(z, R);
            z = Vector3.Abs(z);

            // Pairwise sort: largest -> z.
            if (z.X < z.Y) z = new Vector3(z.Y, z.X, z.Z);
            if (z.X < z.Z) z = new Vector3(z.Z, z.Y, z.X);
            if (z.Y < z.Z) z = new Vector3(z.X, z.Z, z.Y);

            z = z * mg.Scale - mg.Offset * (mg.Scale - 1f);
            if (z.Z < -mg.Offset.Z * (mg.Scale - 1f) * 0.5f)
                z = new Vector3(z.X, z.Y, z.Z + mg.Offset.Z * (mg.Scale - 1f));
            dr *= mg.Scale;
        }
        var d = Vector3.Max(Vector3.Abs(z) - Vector3.One, Vector3.Zero);
        return d.Length() / MathF.Abs(dr);
    }
}
