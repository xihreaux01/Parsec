using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the quaternion Julia DE (quaternion_julia_core.glsl), used only
/// to scale fly-camera speed by proximity to the surface. Includes the half-cut
/// so the camera glides correctly along the cut face too. Not used for rendering.
/// </summary>
public static class QuaternionJuliaDE
{
    private static Vector4 QMul(Vector4 p, Vector4 q) => new(
        p.X * q.X - p.Y * q.Y - p.Z * q.Z - p.W * q.W,
        p.X * q.Y + p.Y * q.X + p.Z * q.W - p.W * q.Z,
        p.X * q.Z - p.Y * q.W + p.Z * q.X + p.W * q.Y,
        p.X * q.W + p.Y * q.Z - p.Z * q.Y + p.W * q.X);

    private static Vector4 QSq(Vector4 q) => new(
        q.X * q.X - q.Y * q.Y - q.Z * q.Z - q.W * q.W,
        2f * q.X * q.Y, 2f * q.X * q.Z, 2f * q.X * q.W);

    public static float Estimate(Vector3 p, QuaternionJuliaParams qj)
    {
        var z = new Vector4(p, qj.WSlice);
        var zp = new Vector4(1, 0, 0, 0);
        float r = 0f;
        for (int i = 0; i < qj.Iterations; i++)
        {
            r = z.Length();
            if (r > qj.Bailout) break;
            zp = 2f * QMul(z, zp);
            z = QSq(z) + qj.C;
        }
        r = z.Length();
        float dz = zp.Length();
        float de = dz < 1e-12f ? 0f : 0.5f * MathF.Log(MathF.Max(r, 1e-12f)) * r / dz;

        if (qj.Cut)
        {
            var n = Vector3.Normalize(qj.PlaneNormal);
            float plane = Vector3.Dot(p, n) - qj.PlaneOffset;
            de = MathF.Max(de, plane);
        }
        return de;
    }
}
