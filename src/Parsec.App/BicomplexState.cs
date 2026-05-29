using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the bicomplex Julia (Fracmonk's formula). The mul/add
/// parameters are the artist's augmentation -- they symmetry-break the
/// otherwise-clean bicomplex square and give a rich exploration space.
/// </summary>
public sealed class BicomplexState
{
    public int Iterations = 12;

    // c constant.
    public float Cx = -0.5f, Cy = 0.0f, Cz = 0.0f, Cw = 0.0f;

    // Per-component muls (default 1 = clean bicomplex square).
    public float XMul = 1.0f;
    public float YMul = 1.0f;
    public float ZMul = 1.0f;
    public float WMul = 1.0f;
    public float WAdd = 0.0f;

    public float WSlice = 0.0f;
    public float Bailout = 4.0f;

    // Half-cut.
    public int Cut = 1;
    public int CutAxis = 0;
    public float PlaneOffset = 0.0f;

    public float Fudge = 0.85f;

    public BicomplexParams ToParams() => new()
    {
        Iterations = Iterations,
        C = new Vector4(Cx, Cy, Cz, Cw),
        XMul = XMul, YMul = YMul, ZMul = ZMul, WMul = WMul, WAdd = WAdd,
        WSlice = WSlice,
        Bailout = Bailout,
        Cut = Cut >= 1, CutAxis = CutAxis, PlaneOffset = PlaneOffset,
        Fudge = Fudge,
        BoundRadius = 4.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Constant c.
            new ParamDescriptor {
                Label = "c.x", Group = "Constant c", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => Cx, Set = v => Cx = (float)v },
            new ParamDescriptor {
                Label = "c.y", Group = "Constant c", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => Cy, Set = v => Cy = (float)v },
            new ParamDescriptor {
                Label = "c.z", Group = "Constant c", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => Cz, Set = v => Cz = (float)v },
            new ParamDescriptor {
                Label = "c.w", Group = "Constant c", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => Cw, Set = v => Cw = (float)v },
            new ParamDescriptor {
                Label = "4D slice (w)", Group = "Constant c", Min = -1, Max = 1, Decimals = 3,
                Get = () => WSlice, Set = v => WSlice = (float)v },

            // Muls -- the artist augmentation.
            new ParamDescriptor {
                Label = "X mul", Group = "Mul/Add (symmetry break)", Min = 0.3, Max = 1.5, Decimals = 3,
                Get = () => XMul, Set = v => XMul = (float)v },
            new ParamDescriptor {
                Label = "Y mul", Group = "Mul/Add (symmetry break)", Min = 0.3, Max = 1.5, Decimals = 3,
                Get = () => YMul, Set = v => YMul = (float)v },
            new ParamDescriptor {
                Label = "Z mul", Group = "Mul/Add (symmetry break)", Min = 0.3, Max = 1.5, Decimals = 3,
                Get = () => ZMul, Set = v => ZMul = (float)v },
            new ParamDescriptor {
                Label = "W mul", Group = "Mul/Add (symmetry break)", Min = 0.3, Max = 1.5, Decimals = 3,
                Get = () => WMul, Set = v => WMul = (float)v },
            new ParamDescriptor {
                Label = "W add", Group = "Mul/Add (symmetry break)", Min = -0.5, Max = 0.5, Decimals = 3,
                Get = () => WAdd, Set = v => WAdd = (float)v },

            // Half-cut.
            new ParamDescriptor {
                Label = "Cut (0/1)", Group = "Half-cut", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => Cut, Set = v => Cut = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Cut axis (0X 1Y 2Z)", Group = "Half-cut", Min = 0, Max = 2, Step = 1, Decimals = 0,
                Get = () => CutAxis, Set = v => CutAxis = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Cut plane offset", Group = "Half-cut", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => PlaneOffset, Set = v => PlaneOffset = (float)v },

            // Quality.
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 64, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Bailout", Group = "Quality", Min = 2.0, Max = 8.0, Decimals = 2,
                Get = () => Bailout, Set = v => Bailout = (float)v },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.4, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
