using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// A CPU mirror of the Mandelbox / AmazingBox fold distance estimator
/// (<c>mandelbox_core.glsl</c>). This is NOT used for rendering — the GPU does
/// that. It exists so the interactive fly-camera can cheaply query the distance
/// to the surface at the camera position and scale movement speed by it, which
/// is what makes a fractal flycam feel right (slow near detail, fast in open
/// space). Called once per frame, so performance is irrelevant; correctness of
/// the falloff trend is what matters, and it mirrors the shader exactly.
/// </summary>
public static class MandelboxDE
{
    public static float Estimate(Vector3 p, MandelboxParams mb)
    {
        float minR2 = mb.MinRadius * mb.MinRadius;
        float fixedR2 = mb.FixedRadius * mb.FixedRadius;
        float scale = mb.Scale;
        float L = mb.FoldingLimit;

        Matrix4x4 rot = RotationFromEuler(mb.RotationRadians);
        bool useRot = mb.RotationRadians != Vector3.Zero;

        Vector3 offset = mb.JuliaMode == 1 ? mb.JuliaC : p;
        Vector3 z = p;
        float dr = 1f;
        const float bailout2 = 1000f;

        for (int i = 0; i < mb.Iterations; i++)
        {
            if (mb.Mode == 1)
            {
                z = Vector3.Abs(z);
                z = Clamp(z, -L, L) * 2f - z;
            }
            else
            {
                z = Clamp(z, -L, L) * 2f - z;
            }

            if (useRot) z = Vector3.Transform(z, rot);

            float r2 = Vector3.Dot(z, z);
            if (r2 < minR2) { float t = fixedR2 / minR2; z *= t; dr *= t; }
            else if (r2 < fixedR2) { float t = fixedR2 / r2; z *= t; dr *= t; }

            z = scale * z + offset;
            dr = dr * MathF.Abs(scale) + 1f;

            if (Vector3.Dot(z, z) > bailout2) break;
        }

        return z.Length() / MathF.Abs(dr);
    }

    private static Vector3 Clamp(Vector3 v, float lo, float hi) => new(
        Math.Clamp(v.X, lo, hi),
        Math.Clamp(v.Y, lo, hi),
        Math.Clamp(v.Z, lo, hi));

    private static Matrix4x4 RotationFromEuler(Vector3 r)
        => Matrix4x4.CreateRotationX(r.X) * Matrix4x4.CreateRotationY(r.Y) * Matrix4x4.CreateRotationZ(r.Z);
}
