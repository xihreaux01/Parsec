using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the quaternion-Julia × Mandelbox hybrid DE (qjbox_core.glsl),
/// used only for fly-camera speed. The hybrid DE is heuristic, like the other
/// hybrids; for camera speed that's fine.
/// </summary>
public static class QJBoxDE
{
    private static Vector4 QMul(Vector4 p, Vector4 q) => new(
        p.X*q.X - p.Y*q.Y - p.Z*q.Z - p.W*q.W,
        p.X*q.Y + p.Y*q.X + p.Z*q.W - p.W*q.Z,
        p.X*q.Z - p.Y*q.W + p.Z*q.X + p.W*q.Y,
        p.X*q.W + p.Y*q.Z - p.Z*q.Y + p.W*q.X);

    private static Vector4 QSq(Vector4 q) => new(
        q.X*q.X - q.Y*q.Y - q.Z*q.Z - q.W*q.W,
        2f*q.X*q.Y, 2f*q.X*q.Z, 2f*q.X*q.W);

    private static Matrix4x4 Euler(float ax, float ay, float az) =>
        Matrix4x4.CreateRotationX(ax) *
        Matrix4x4.CreateRotationY(ay) *
        Matrix4x4.CreateRotationZ(az);

    public static float Estimate(Vector3 p, QJBoxParams qb)
    {
        var R = Euler(qb.Rotation.X, qb.Rotation.Y, qb.Rotation.Z);
        float minR2 = qb.MinRadius * qb.MinRadius;
        float fixedR2 = qb.FixedRadius * qb.FixedRadius;

        var z = new Vector4(p, qb.WSlice);
        var zp = new Vector4(1, 0, 0, 0);
        float r = 0f;
        for (int i = 0; i < qb.Iterations; i++)
        {
            r = z.Length();
            if (r > 4f) break;

            var z3 = Vector3.Transform(new Vector3(z.X, z.Y, z.Z), R);
            z = new Vector4(z3, z.W);

            z3 = Vector3.Clamp(new Vector3(z.X, z.Y, z.Z),
                new Vector3(-qb.FoldLimit), new Vector3(qb.FoldLimit)) * 2f
                 - new Vector3(z.X, z.Y, z.Z);
            float r2 = Vector3.Dot(z3, z3);
            if (r2 < minR2) { float f = fixedR2 / minR2; z3 *= f; zp *= f; }
            else if (r2 < fixedR2) { float f = fixedR2 / r2; z3 *= f; zp *= f; }
            z3 = z3 * qb.Scale + p;
            zp = zp * MathF.Abs(qb.Scale) + new Vector4(1, 0, 0, 0);
            z = new Vector4(z3, z.W);

            zp = 2f * QMul(z, zp);
            z = QSq(z) + qb.C;
        }

        r = z.Length();
        float dz = zp.Length();
        float de = dz < 1e-12f ? 0f : 0.25f * MathF.Log(MathF.Max(r, 1e-12f)) * r / dz;

        if (qb.Cut)
        {
            float pn = qb.CutAxis switch { 1 => p.Y, 2 => p.Z, _ => p.X };
            de = MathF.Max(de, pn - qb.PlaneOffset);
        }
        return de;
    }
}
