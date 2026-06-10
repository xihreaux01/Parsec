using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the triball mined fold fractal. Scale is the headline
/// knob (morphs the whole solid, exactly like the Mandelbox's scale); bailout
/// and DE fudge are quality/safety knobs. The fold rule itself (box + three
/// nested ball folds) is fixed in triball_core.glsl. All smooth scalars, so they
/// make good keyframe/animation targets.
/// </summary>
public sealed class TriballState
{
    public int Iterations = 14;
    public float Scale = 1.82f;
    public float Bailout = 6.0f;
    public float Fudge = 0.9f;

    public TriballParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale,
        Bailout = Bailout,
        Fudge = Fudge,
        BoundRadius = 5.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Scale", Group = "Form", Min = 1.3, Max = 3.0, Decimals = 3,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Bailout", Group = "Form", Min = 3.0, Max = 12.0, Decimals = 1,
                Get = () => Bailout, Set = v => Bailout = (float)v },
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)System.Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.4, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
}
