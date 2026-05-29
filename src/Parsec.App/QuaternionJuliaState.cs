using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the quaternion Julia set. The headline knobs are the
/// quaternion constant c (the shape's identity), the 4D slice 'wslice' (which 3D
/// shadow of the 4D object we see), and the half-cut plane offset (sweeping it
/// slices through the solid to reveal the nested interior). All are smooth
/// scalars -- great keyframe/animation targets.
/// </summary>
public sealed class QuaternionJuliaState
{
    public int Iterations = 10;
    public float Cx = -0.2f, Cy = 0.8f, Cz = 0.0f, Cw = 0.0f;
    public float WSlice = 0.0f;
    public int Cut = 1;                 // 0/1 toggle
    public int CutAxis = 0;             // 0=X, 1=Y, 2=Z
    public float PlaneOffset = 0.0f;    // sweep to slice through the solid
    public float Fudge = 0.9f;

    public QuaternionJuliaParams ToParams() => new()
    {
        Iterations = Iterations,
        C = new Vector4(Cx, Cy, Cz, Cw),
        WSlice = WSlice,
        Cut = Cut >= 1,
        CutAxis = CutAxis,
        PlaneOffset = PlaneOffset,
        Fudge = Fudge,
        BoundRadius = 2.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "c.x", Group = "Constant c", Min = -1, Max = 1, Decimals = 3,
                Get = () => Cx, Set = v => Cx = (float)v },
            new ParamDescriptor {
                Label = "c.y", Group = "Constant c", Min = -1, Max = 1, Decimals = 3,
                Get = () => Cy, Set = v => Cy = (float)v },
            new ParamDescriptor {
                Label = "c.z", Group = "Constant c", Min = -1, Max = 1, Decimals = 3,
                Get = () => Cz, Set = v => Cz = (float)v },
            new ParamDescriptor {
                Label = "c.w", Group = "Constant c", Min = -1, Max = 1, Decimals = 3,
                Get = () => Cw, Set = v => Cw = (float)v },

            new ParamDescriptor {
                Label = "4D slice (w)", Group = "Slice & Cut", Min = -1, Max = 1, Decimals = 3,
                Get = () => WSlice, Set = v => WSlice = (float)v },
            new ParamDescriptor {
                Label = "Cut (0/1)", Group = "Slice & Cut", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => Cut, Set = v => Cut = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Cut axis (0X 1Y 2Z)", Group = "Slice & Cut", Min = 0, Max = 2, Step = 1, Decimals = 0,
                Get = () => CutAxis, Set = v => CutAxis = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Cut plane offset", Group = "Slice & Cut", Min = -1.2, Max = 1.2, Decimals = 3,
                Get = () => PlaneOffset, Set = v => PlaneOffset = (float)v },

            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 64, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.4, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
