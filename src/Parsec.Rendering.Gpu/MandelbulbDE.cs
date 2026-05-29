using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the Mandelbulb DE (mandelbulb_core.glsl), used only to scale
/// fly-camera speed by proximity to the surface. Not used for rendering.
/// </summary>
public static class MandelbulbDE
{
    public static float Estimate(Vector3 p, MandelbulbParams mb)
    {
        Vector3 z = p;
        float dr = 1.0f;
        float r = 0.0f;
        for (int i = 0; i < mb.Iterations; i++)
        {
            r = z.Length();
            if (r > mb.Bailout) break;
            float theta = MathF.Acos(Math.Clamp(z.Z / MathF.Max(r, 1e-12f), -1f, 1f));
            float phi = MathF.Atan2(z.Y, z.X);
            dr = mb.Power * MathF.Pow(r, mb.Power - 1f) * dr + 1f;
            float zr = MathF.Pow(r, mb.Power);
            theta *= mb.Power;
            phi *= mb.Power;
            z = zr * new Vector3(
                MathF.Sin(theta) * MathF.Cos(phi),
                MathF.Sin(theta) * MathF.Sin(phi),
                MathF.Cos(theta)) + p;
        }
        return 0.5f * MathF.Log(MathF.Max(r, 1e-12f)) * r / MathF.Max(dr, 1e-12f);
    }
}
