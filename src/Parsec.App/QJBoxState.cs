using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the quaternion-Julia × Mandelbox hybrid with inherited
/// half-cut. The richest parameter set in the app: Mandelbox fold params, the
/// quaternion constant c (4 components), wslice, rotation angles, the half-cut
/// (axis + offset), and quality. Each is necessary -- the c components define
/// the Julia identity, the folds define the structure, the rotation is the
/// magic, and the cut is the killer feature.
/// </summary>
public sealed class QJBoxState
{
    public int Iterations = 10;

    // Mandelbox half.
    public float Scale = -1.8f;
    public float MinRadius = 0.5f;
    public float FixedRadius = 1.0f;
    public float FoldLimit = 1.0f;

    // Quaternion-Julia half.
    public float Cx = -0.2f, Cy = 0.6f, Cz = 0.1f, Cw = 0.0f;
    public float WSlice = 0.0f;

    // Rotation (degrees, converted to radians).
    public float RotXDeg = 5.7f;   // ~0.10 rad
    public float RotYDeg = 4.0f;   // ~0.07 rad
    public float RotZDeg = 2.3f;   // ~0.04 rad

    // Half-cut (inherited killer feature).
    public int Cut = 1;
    public int CutAxis = 0;          // 0=X, 1=Y, 2=Z
    public float PlaneOffset = 0.0f;

    public float Fudge = 0.6f;

    private const float Deg2Rad = MathF.PI / 180f;

    public QJBoxParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale, MinRadius = MinRadius, FixedRadius = FixedRadius, FoldLimit = FoldLimit,
        C = new Vector4(Cx, Cy, Cz, Cw),
        WSlice = WSlice,
        Rotation = new Vector3(RotXDeg * Deg2Rad, RotYDeg * Deg2Rad, RotZDeg * Deg2Rad),
        Cut = Cut >= 1, CutAxis = CutAxis, PlaneOffset = PlaneOffset,
        Fudge = Fudge,
        BoundRadius = 4.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Rotation first -- the morph stars, as in the other hybrids.
            new ParamDescriptor {
                Label = "Rotate X", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotXDeg, Set = v => RotXDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Y", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotYDeg, Set = v => RotYDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Z", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotZDeg, Set = v => RotZDeg = (float)v },

            // Half-cut second -- the inherited killer feature.
            new ParamDescriptor {
                Label = "Cut (0/1)", Group = "Half-cut", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => Cut, Set = v => Cut = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Cut axis (0X 1Y 2Z)", Group = "Half-cut", Min = 0, Max = 2, Step = 1, Decimals = 0,
                Get = () => CutAxis, Set = v => CutAxis = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Cut plane offset", Group = "Half-cut", Min = -1.5, Max = 1.5, Decimals = 3,
                Get = () => PlaneOffset, Set = v => PlaneOffset = (float)v },

            // Quaternion constant c.
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
                Label = "4D slice (w)", Group = "Constant c", Min = -1, Max = 1, Decimals = 3,
                Get = () => WSlice, Set = v => WSlice = (float)v },

            // Mandelbox fold params.
            new ParamDescriptor {
                Label = "Scale", Group = "Fold (box half)", Min = -2.5, Max = -1.2, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Min radius", Group = "Fold (box half)", Min = 0.1, Max = 1.0, Decimals = 2,
                Get = () => MinRadius, Set = v => MinRadius = (float)v },
            new ParamDescriptor {
                Label = "Fixed radius", Group = "Fold (box half)", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FixedRadius, Set = v => FixedRadius = (float)v },
            new ParamDescriptor {
                Label = "Fold limit", Group = "Fold (box half)", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FoldLimit, Set = v => FoldLimit = (float)v },

            // Quality.
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
    public void Reset()
    {
        Iterations = 10;
        Scale = -1.8f;
        MinRadius = 0.5f;
        FixedRadius = 1.0f;
        FoldLimit = 1.0f;
        Cx = -0.2f; Cy = 0.6f; Cz = 0.1f; Cw = 0.0f;
        WSlice = 0.0f;
        RotXDeg = 5.7f;
        RotYDeg = 4.0f;
        RotZDeg = 2.3f;
        Cut = 1;
        CutAxis = 0;
        PlaneOffset = 0.0f;
        Fudge = 0.6f;
    }}