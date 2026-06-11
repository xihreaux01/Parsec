using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the pseudo-Kleinian inversive limit set, plus a
/// <see cref="ParamSchema"/>. Mirrors the other fractal states; the render side
/// consumes an immutable <see cref="KleinianParams"/> via <see cref="ToParams"/>.
/// </summary>
public sealed class KleinianState
{
    public int Iterations = 9;
    public float Scale = 2.0f;
    public float Cell = 1.0f;
    public float MinRadius = 0.5f;
    public float FixedRadius = 1.0f;
    public float OffX = 0.5f, OffY = 0.5f, OffZ = 1.2f;
    public float Fudge = 0.7f;

    public KleinianParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale,
        Cell = Cell,
        MinRadius = MinRadius,
        FixedRadius = FixedRadius,
        Offset = new Vector3(OffX, OffY, OffZ),
        Fudge = Fudge,
        BoundRadius = 6.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Scale", Group = "Form", Min = 1.3, Max = 2.6, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Cell size", Group = "Form", Min = 0.4, Max = 2.0, Decimals = 2,
                Get = () => Cell, Set = v => Cell = (float)v },
            new ParamDescriptor {
                Label = "Min radius", Group = "Form", Min = 0.1, Max = 1.0, Decimals = 2,
                Get = () => MinRadius, Set = v => MinRadius = (float)v },
            new ParamDescriptor {
                Label = "Fixed radius", Group = "Form", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => FixedRadius, Set = v => FixedRadius = (float)v },

            // The offset generates the limit set: the strongest morphology knob.
            new ParamDescriptor {
                Label = "Offset X", Group = "Generator (offset)", Min = -2, Max = 2, Decimals = 2,
                Get = () => OffX, Set = v => OffX = (float)v },
            new ParamDescriptor {
                Label = "Offset Y", Group = "Generator (offset)", Min = -2, Max = 2, Decimals = 2,
                Get = () => OffY, Set = v => OffY = (float)v },
            new ParamDescriptor {
                Label = "Offset Z", Group = "Generator (offset)", Min = -2, Max = 2, Decimals = 2,
                Get = () => OffZ, Set = v => OffZ = (float)v },

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
        Iterations = 9;
        Scale = 2.0f;
        Cell = 1.0f;
        MinRadius = 0.5f;
        FixedRadius = 1.0f;
        OffX = 0.5f; OffY = 0.5f; OffZ = 1.2f;
        Fudge = 0.7f;
    }}