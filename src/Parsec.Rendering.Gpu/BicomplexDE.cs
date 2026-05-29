using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// CPU mirror of the bicomplex Julia DE for fly-cam speed. Uses a much-
/// simplified estimator (just |z|/iters), since fly-speed doesn't need the
/// expensive numerical-gradient.
/// </summary>
public static class BicomplexDE
{
    public static float Estimate(Vector3 p, BicomplexParams bp)
    {
        Vector4 z = new(p.X, p.Y, p.Z, bp.WSlice);
        float bailout = bp.Bailout;
        int n = 0;
        for (int i = 0; i < bp.Iterations; i++)
        {
            float r = z.Length();
            if (r > bailout) break;
            float x = z.X, y = z.Y, zc = z.Z, w = z.W;
            z = new Vector4(
                bp.XMul * (x*x - y*y - 2f*zc*w) + bp.C.X,
                bp.YMul * (2f*x*y + zc*zc - w*w) + bp.C.Y,
                bp.ZMul * (2f*x*zc - 2f*y*w) + bp.C.Z,
                bp.WMul * (2f*x*w + 2f*y*zc) + bp.WAdd + bp.C.W);
            n++;
        }
        // Rough speed estimate -- not the proper DE but adequate for fly-cam.
        float r0 = z.Length();
        float de = MathF.Max(0.01f, MathF.Log(MathF.Max(r0, 1.01f)) * 0.1f);

        if (bp.Cut)
        {
            float pn = bp.CutAxis switch { 1 => p.Y, 2 => p.Z, _ => p.X };
            de = MathF.Max(de, pn - bp.PlaneOffset);
        }
        return de;
    }
}
