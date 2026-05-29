using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the Kaleidoscopic IFS ("Amazing IFS") fractal, plus a
/// <see cref="ParamSchema"/> describing its tunable parameters. Mirrors
/// <see cref="AmazingBoxState"/>; the rendering side consumes an immutable
/// <see cref="KifsParams"/> snapshot via <see cref="ToParams"/>.
/// </summary>
public sealed class KifsState
{
    public int Iterations = 16;
    public float Scale = 2.0f;
    public float MinRadius = 0.5f;
    public float FixedRadius = 1.0f;

    public float PreX = 0f, PreY = 0f, PreZ = 0f;
    public float PostX = 20f * MathF.PI / 180f;
    public float PostY = 15f * MathF.PI / 180f;
    public float PostZ = 10f * MathF.PI / 180f;

    public float PivotX = 1f, PivotY = 1f, PivotZ = 1f;
    public float Fudge = 0.7f;

    public KifsParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale,
        MinRadius = MinRadius,
        FixedRadius = FixedRadius,
        PreRotationRadians = new Vector3(PreX, PreY, PreZ),
        PostRotationRadians = new Vector3(PostX, PostY, PostZ),
        Pivot = new Vector3(PivotX, PivotY, PivotZ),
        Fudge = Fudge,
        BoundRadius = 6.0f,
    };

    private const float Deg = MathF.PI / 180f;

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Scale", Group = "Form", Min = 1.2, Max = 3.0, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Min radius", Group = "Form", Min = 0.05, Max = 1.0, Decimals = 2,
                Get = () => MinRadius, Set = v => MinRadius = (float)v },
            new ParamDescriptor {
                Label = "Fixed radius", Group = "Form", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FixedRadius, Set = v => FixedRadius = (float)v },

            // Post-fold rotation: the curl / spiral generator. Most expressive knobs.
            new ParamDescriptor {
                Label = "Post-rot X", Group = "Curl (post-fold rot)", Min = -45, Max = 45, Decimals = 0,
                Get = () => PostX / Deg, Set = v => PostX = (float)v * Deg },
            new ParamDescriptor {
                Label = "Post-rot Y", Group = "Curl (post-fold rot)", Min = -45, Max = 45, Decimals = 0,
                Get = () => PostY / Deg, Set = v => PostY = (float)v * Deg },
            new ParamDescriptor {
                Label = "Post-rot Z", Group = "Curl (post-fold rot)", Min = -45, Max = 45, Decimals = 0,
                Get = () => PostZ / Deg, Set = v => PostZ = (float)v * Deg },

            new ParamDescriptor {
                Label = "Pre-rot X", Group = "Pre-fold rot", Min = -45, Max = 45, Decimals = 0,
                Get = () => PreX / Deg, Set = v => PreX = (float)v * Deg },
            new ParamDescriptor {
                Label = "Pre-rot Y", Group = "Pre-fold rot", Min = -45, Max = 45, Decimals = 0,
                Get = () => PreY / Deg, Set = v => PreY = (float)v * Deg },
            new ParamDescriptor {
                Label = "Pre-rot Z", Group = "Pre-fold rot", Min = -45, Max = 45, Decimals = 0,
                Get = () => PreZ / Deg, Set = v => PreZ = (float)v * Deg },

            // Pivot: where the scale step contracts toward. Strong morphology knob.
            new ParamDescriptor {
                Label = "Pivot X", Group = "Pivot", Min = -2, Max = 2, Decimals = 2,
                Get = () => PivotX, Set = v => PivotX = (float)v },
            new ParamDescriptor {
                Label = "Pivot Y", Group = "Pivot", Min = -2, Max = 2, Decimals = 2,
                Get = () => PivotY, Set = v => PivotY = (float)v },
            new ParamDescriptor {
                Label = "Pivot Z", Group = "Pivot", Min = -2, Max = 2, Decimals = 2,
                Get = () => PivotZ, Set = v => PivotZ = (float)v },

            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 28, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
