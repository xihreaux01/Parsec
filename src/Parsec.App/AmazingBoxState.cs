using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the AmazingBox fractal, plus a <see cref="ParamSchema"/>
/// describing its tunable parameters. The rendering side consumes an immutable
/// <see cref="MandelboxParams"/> snapshot via <see cref="ToParams"/>; this class
/// is what the slider panel mutates.
/// </summary>
public sealed class AmazingBoxState
{
    public int Iterations = 14;
    public float Scale = -1.5f;
    public float FoldingLimit = 1.0f;
    public float MinRadius = 0.5f;
    public float FixedRadius = 1.0f;
    public float RotX = 13f * MathF.PI / 180f;
    public float RotY = 9f * MathF.PI / 180f;
    public float RotZ = -20f * MathF.PI / 180f;
    public float Fudge = 0.8f;

    public MandelboxParams ToParams() => new()
    {
        Iterations = Iterations,
        Mode = 1,                 // Amazing (abs) fold
        Scale = Scale,
        FoldingLimit = FoldingLimit,
        MinRadius = MinRadius,
        FixedRadius = FixedRadius,
        RotationRadians = new Vector3(RotX, RotY, RotZ),
        Fudge = Fudge,
        BoundRadius = 5.0f,
    };

    private const float Deg = MathF.PI / 180f;

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Fold shape — the parameters that define the attractor itself.
            new ParamDescriptor {
                Label = "Scale", Group = "Fold", Min = -3.0, Max = -1.0, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Min radius", Group = "Fold", Min = 0.05, Max = 1.0, Decimals = 2,
                Get = () => MinRadius, Set = v => MinRadius = (float)v },
            new ParamDescriptor {
                Label = "Fixed radius", Group = "Fold", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FixedRadius, Set = v => FixedRadius = (float)v },
            new ParamDescriptor {
                Label = "Folding limit", Group = "Fold", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FoldingLimit, Set = v => FoldingLimit = (float)v },

            // Rotation between folds — degrees in the UI, radians under the hood.
            new ParamDescriptor {
                Label = "Rotate X", Group = "Rotation", Min = -45, Max = 45, Decimals = 0,
                Get = () => RotX / Deg, Set = v => RotX = (float)v * Deg },
            new ParamDescriptor {
                Label = "Rotate Y", Group = "Rotation", Min = -45, Max = 45, Decimals = 0,
                Get = () => RotY / Deg, Set = v => RotY = (float)v * Deg },
            new ParamDescriptor {
                Label = "Rotate Z", Group = "Rotation", Min = -45, Max = 45, Decimals = 0,
                Get = () => RotZ / Deg, Set = v => RotZ = (float)v * Deg },

            // Quality / iteration.
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 24, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
