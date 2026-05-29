using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the rotated Mandelbox + Mandelbulb hybrid. Same shape
/// as RotBox plus a Power slider for the bulb half. The DE is heuristic (with a
/// safety factor baked in), so some parameter regions will produce visible
/// artifacts; this is expected for hybrid formulas. Every knob is a smooth
/// scalar -- a phenomenal animation target.
/// </summary>
public sealed class HybridState
{
    public int Iterations = 8;
    public float Scale = -1.8f;
    public float MinRadius = 0.5f;
    public float FixedRadius = 1.0f;
    public float FoldLimit = 1.0f;
    public float Power = 2.0f;

    // Rotation angles in DEGREES (converted to radians).
    public float RotXDeg = 6.9f;   // ~0.12 rad
    public float RotYDeg = 4.6f;   // ~0.08 rad
    public float RotZDeg = 2.3f;   // ~0.04 rad

    public float Fudge = 0.6f;

    private const float Deg2Rad = MathF.PI / 180f;

    public HybridParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale,
        MinRadius = MinRadius,
        FixedRadius = FixedRadius,
        FoldLimit = FoldLimit,
        Power = Power,
        Rotation = new Vector3(RotXDeg * Deg2Rad, RotYDeg * Deg2Rad, RotZDeg * Deg2Rad),
        Fudge = Fudge,
        BoundRadius = 4.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Power", Group = "Hybrid (bulb half)", Min = 1.5, Max = 8.0, Decimals = 2,
                Get = () => Power, Set = v => Power = (float)v },

            new ParamDescriptor {
                Label = "Rotate X", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotXDeg, Set = v => RotXDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Y", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotYDeg, Set = v => RotYDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Z", Group = "Rotation (morph)", Min = -30, Max = 30, Decimals = 2,
                Get = () => RotZDeg, Set = v => RotZDeg = (float)v },

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

            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 16, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
