using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the Mandelbulb, plus a <see cref="ParamSchema"/>.
/// Power is the headline morphology knob (default 8 = canonical); it is a smooth
/// scalar, so it also makes a lovely animation target.
/// </summary>
public sealed class MandelbulbState
{
    public int Iterations = 8;
    public float Power = 8.0f;
    public float Bailout = 4.0f;
    public float Fudge = 1.0f;

    public MandelbulbParams ToParams() => new()
    {
        Iterations = Iterations,
        Power = Power,
        Bailout = Bailout,
        Fudge = Fudge,
        BoundRadius = 1.3f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Power", Group = "Form", Min = 2, Max = 12, Decimals = 2,
                Get = () => Power, Set = v => Power = (float)v },
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Bailout", Group = "Quality", Min = 1.5, Max = 20.0, Decimals = 2,
                Get = () => Bailout, Set = v => Bailout = (float)v },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.4, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };
    public void Reset()
    {
        Iterations = 8;
        Power = 8.0f;
        Bailout = 4.0f;
        Fudge = 1.0f;
    }}