using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the rotation-augmented Mandelbox ("rotated fold").
/// The headline knobs are the three rotation angles (exposed in DEGREES for
/// intuitive dialing): they compound across iterations, so small changes cascade
/// into wildly different forms -- the most generative morph controls in the app,
/// and superb animation targets. Plus the familiar Mandelbox fold params.
/// </summary>
public sealed class RotBoxState
{
    public int Iterations = 12;
    public float Scale = -2.0f;
    public float MinRadius = 0.5f;
    public float FixedRadius = 1.0f;
    public float FoldLimit = 1.0f;

    // Rotation angles in DEGREES (converted to radians for the renderer).
    public float RotXDeg = 8.6f;   // ~0.15 rad
    public float RotYDeg = 5.7f;   // ~0.10 rad
    public float RotZDeg = 2.9f;   // ~0.05 rad

    public float Fudge = 0.85f;

    private const float Deg2Rad = MathF.PI / 180f;

    public RotBoxParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale,
        MinRadius = MinRadius,
        FixedRadius = FixedRadius,
        FoldLimit = FoldLimit,
        Rotation = new Vector3(RotXDeg * Deg2Rad, RotYDeg * Deg2Rad, RotZDeg * Deg2Rad),
        Fudge = Fudge,
        BoundRadius = 8.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // The rotation angles first -- they're the stars of this fractal.
            new ParamDescriptor {
                Label = "Rotate X", Group = "Rotation (morph)", Min = -45, Max = 45, Decimals = 2,
                Get = () => RotXDeg, Set = v => RotXDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Y", Group = "Rotation (morph)", Min = -45, Max = 45, Decimals = 2,
                Get = () => RotYDeg, Set = v => RotYDeg = (float)v },
            new ParamDescriptor {
                Label = "Rotate Z", Group = "Rotation (morph)", Min = -45, Max = 45, Decimals = 2,
                Get = () => RotZDeg, Set = v => RotZDeg = (float)v },

            new ParamDescriptor {
                Label = "Scale", Group = "Fold", Min = -3.0, Max = -1.2, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Min radius", Group = "Fold", Min = 0.1, Max = 1.0, Decimals = 2,
                Get = () => MinRadius, Set = v => MinRadius = (float)v },
            new ParamDescriptor {
                Label = "Fixed radius", Group = "Fold", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FixedRadius, Set = v => FixedRadius = (float)v },
            new ParamDescriptor {
                Label = "Fold limit", Group = "Fold", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FoldLimit, Set = v => FoldLimit = (float)v },

            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.4, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
    public void Reset()
    {
        Iterations = 12;
        Scale = -2.0f;
        MinRadius = 0.5f;
        FixedRadius = 1.0f;
        FoldLimit = 1.0f;
        RotXDeg = 8.6f;
        RotYDeg = 5.7f;
        RotZDeg = 2.9f;
        Fudge = 0.85f;
    }}