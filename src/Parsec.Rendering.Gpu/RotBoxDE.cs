using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the rotation-augmented Mandelbox DE (rotbox_core.glsl), used
/// only to scale fly-camera speed by proximity to the surface. Not used for
/// rendering.
/// </summary>
public static class RotBoxDE
{
    private static Matrix4x4 Euler(float ax, float ay, float az)
    {
        // Rz * Ry * Rx, matching the shader.
        return Matrix4x4.CreateRotationX(ax) *
               Matrix4x4.CreateRotationY(ay) *
               Matrix4x4.CreateRotationZ(az);
    }

    public static float Estimate(Vector3 p, RotBoxParams rb)
    {
        var R = Euler(rb.Rotation.X, rb.Rotation.Y, rb.Rotation.Z);
        float minR2 = rb.MinRadius * rb.MinRadius;
        float fixedR2 = rb.FixedRadius * rb.FixedRadius;

        Vector3 z = p;
        float dr = 1.0f;
        for (int i = 0; i < rb.Iterations; i++)
        {
            z = Vector3.Transform(z, R);
            z = Vector3.Clamp(z, new Vector3(-rb.FoldLimit), new Vector3(rb.FoldLimit)) * 2f - z;
            float r2 = Vector3.Dot(z, z);
            if (r2 < minR2) { float f = fixedR2 / minR2; z *= f; dr *= f; }
            else if (r2 < fixedR2) { float f = fixedR2 / r2; z *= f; dr *= f; }
            z = z * rb.Scale + p;
            dr = dr * MathF.Abs(rb.Scale) + 1f;
        }
        return z.Length() / MathF.Abs(dr);
    }
}
