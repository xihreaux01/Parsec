using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the hybrid Mandelbox+Mandelbulb DE (hybrid_core.glsl), used
/// only for fly-camera speed. The hybrid DE is heuristic (overestimating in some
/// regions); for camera speed it's fine, but it would not be safe for rendering.
/// </summary>
public static class HybridDE
{
    private static Matrix4x4 Euler(float ax, float ay, float az) =>
        Matrix4x4.CreateRotationX(ax) *
        Matrix4x4.CreateRotationY(ay) *
        Matrix4x4.CreateRotationZ(az);

    public static float Estimate(Vector3 p, HybridParams hp)
    {
        var R = Euler(hp.Rotation.X, hp.Rotation.Y, hp.Rotation.Z);
        float minR2 = hp.MinRadius * hp.MinRadius;
        float fixedR2 = hp.FixedRadius * hp.FixedRadius;

        Vector3 z = p;
        float dr = 1.0f;
        float r = 0f;
        for (int i = 0; i < hp.Iterations; i++)
        {
            z = Vector3.Transform(z, R);
            z = Vector3.Clamp(z, new Vector3(-hp.FoldLimit), new Vector3(hp.FoldLimit)) * 2f - z;
            float r2 = Vector3.Dot(z, z);
            if (r2 < minR2) { float f = fixedR2 / minR2; z *= f; dr *= f; }
            else if (r2 < fixedR2) { float f = fixedR2 / r2; z *= f; dr *= f; }
            z = z * hp.Scale + p;
            dr = dr * MathF.Abs(hp.Scale) + 1f;

            r = z.Length();
            if (r > 8f) break;
            if (r > 1e-12f)
            {
                float theta = MathF.Acos(Math.Clamp(z.Z / r, -1f, 1f));
                float phi = MathF.Atan2(z.Y, z.X);
                dr = hp.Power * MathF.Pow(r, hp.Power - 1f) * dr + 1f;
                float zr = MathF.Pow(r, hp.Power);
                theta *= hp.Power; phi *= hp.Power;
                z = zr * new Vector3(
                    MathF.Sin(theta) * MathF.Cos(phi),
                    MathF.Sin(theta) * MathF.Sin(phi),
                    MathF.Cos(theta)) + p;
            }
        }
        r = z.Length();
        return 0.25f * MathF.Log(MathF.Max(r, 1e-12f)) * r / MathF.Max(dr, 1e-12f);
    }
}
