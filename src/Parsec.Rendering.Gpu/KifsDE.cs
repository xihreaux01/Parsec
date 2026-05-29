using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the KIFS distance estimator (kifs_core.glsl), used ONLY for
/// scaling fly-camera movement speed by proximity to the surface. Not used for
/// rendering. Mirrors the shader exactly: pre-rotate, abs fold, post-rotate,
/// sphere fold, scale-to-pivot, with derivative tracking.
/// </summary>
public static class KifsDE
{
    public static float Estimate(Vector3 p, KifsParams kf)
    {
        float minR2 = kf.MinRadius * kf.MinRadius;
        float fixedR2 = kf.FixedRadius * kf.FixedRadius;
        float scale = kf.Scale;

        Matrix4x4 preR = RotationFromEuler(kf.PreRotationRadians);
        Matrix4x4 postR = RotationFromEuler(kf.PostRotationRadians);
        bool usePre = kf.PreRotationRadians != Vector3.Zero;
        bool usePost = kf.PostRotationRadians != Vector3.Zero;
        Vector3 pivot = kf.Pivot;

        Vector3 z = p;
        float dr = 1.0f;
        const float bailout2 = 1000f;

        for (int i = 0; i < kf.Iterations; i++)
        {
            if (usePre) z = Vector3.Transform(z, preR);
            z = Vector3.Abs(z);
            if (usePost) z = Vector3.Transform(z, postR);

            float r2 = Vector3.Dot(z, z);
            if (r2 < minR2) { float t = fixedR2 / minR2; z *= t; dr *= t; }
            else if (r2 < fixedR2) { float t = fixedR2 / r2; z *= t; dr *= t; }

            z = scale * z - (scale - 1.0f) * pivot;
            dr = dr * MathF.Abs(scale) + 1.0f;

            if (Vector3.Dot(z, z) > bailout2) break;
        }

        return z.Length() / MathF.Abs(dr);
    }

    private static Matrix4x4 RotationFromEuler(Vector3 r)
        => Matrix4x4.CreateRotationX(r.X) * Matrix4x4.CreateRotationY(r.Y) * Matrix4x4.CreateRotationZ(r.Z);
}
