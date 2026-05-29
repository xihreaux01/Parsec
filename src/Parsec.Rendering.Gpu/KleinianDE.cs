using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the pseudo-Kleinian numerical-gradient DE (kleinian_core.glsl),
/// used ONLY to scale fly-camera movement speed by proximity to the surface.
/// Not used for rendering.
/// </summary>
public static class KleinianDE
{
    private static float Potential(Vector3 p, KleinianParams kl)
    {
        float minR2 = kl.MinRadius * kl.MinRadius;
        float fixedR2 = kl.FixedRadius * kl.FixedRadius;
        Vector3 z = p;
        for (int i = 0; i < kl.Iterations; i++)
        {
            float r2 = Vector3.Dot(z, z);
            if (r2 < minR2) z *= fixedR2 / minR2;
            else if (r2 < fixedR2) z *= fixedR2 / r2;
            z = Vector3.Clamp(z, new Vector3(-kl.Cell), new Vector3(kl.Cell)) * 2f - z;
            z = z * kl.Scale + kl.Offset;
        }
        return MathF.Log(MathF.Max(z.Length(), 1e-12f));
    }

    public static float Estimate(Vector3 p, KleinianParams kl)
    {
        const float eps = 1e-4f;
        float v = Potential(p, kl);
        float vx = Potential(p + new Vector3(eps, 0, 0), kl) - Potential(p - new Vector3(eps, 0, 0), kl);
        float vy = Potential(p + new Vector3(0, eps, 0), kl) - Potential(p - new Vector3(0, eps, 0), kl);
        float vz = Potential(p + new Vector3(0, 0, eps), kl) - Potential(p - new Vector3(0, 0, eps), kl);
        var grad = new Vector3(vx, vy, vz) / (2f * eps);
        float g = grad.Length();
        if (g < 1e-12f) return 1e3f;
        return MathF.Abs(v) / g;
    }
}
