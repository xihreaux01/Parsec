using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the Mandalay Fold fractal, plus a <see cref="ParamSchema"/>
/// describing its tunable parameters. Mirrors <see cref="KifsState"/>; the
/// rendering side consumes an immutable <see cref="MandalayParams"/> snapshot via
/// <see cref="ToParams"/>.
///
/// Knob guide:
///   - Scale is the escape-time expansion; negative (~ -2) gives the rich set.
///   - Fold offset (fo) is the primary shape knob; g and h are secondary offsets
///     that open up the beam/cross structure (both default 0 for the canonical look).
///   - Sequential feeds each axis fold into the next (vs all from the original z).
///   - DE fudge defaults to 0.55 because the fold expands ~1.7x at its seams;
///     lower it if you see sparkle/dropout along the folds.
/// </summary>
public sealed class MandalayState
{
    public int Iterations = 12;

    // 0 = parallel, 1 = sequential (Sw)
    public int Mode = 0;
    // 0 = Mandelbrot (c = position), 1 = Julia (c = JuliaC)
    public int JuliaMode = 0;

    public float Scale = -2.0f;
    public float FoldOffset = 0.555f;
    public float OffsetG = 0.0f;
    public float OffsetH = 0.0f;

    public float Bailout = 8.0f;

    public float JuliaCx = 0.0f, JuliaCy = 0.0f, JuliaCz = 0.0f;

    public float Fudge = 0.55f;
    public float BoundRadius = 6.0f;

    public MandalayParams ToParams() => new()
    {
        Iterations = Iterations,
        Mode = Mode,
        JuliaMode = JuliaMode,
        Scale = Scale,
        FoldOffset = FoldOffset,
        OffsetG = OffsetG,
        OffsetH = OffsetH,
        Bailout = Bailout,
        JuliaC = new Vector3(JuliaCx, JuliaCy, JuliaCz),
        Fudge = Fudge,
        BoundRadius = BoundRadius,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Form.
            new ParamDescriptor {
                Label = "Scale", Group = "Form", Min = -3.0, Max = 3.0, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Fold offset", Group = "Form", Min = 0.1, Max = 1.5, Decimals = 3,
                Get = () => FoldOffset, Set = v => FoldOffset = (float)v },

            // Secondary beam offsets.
            new ParamDescriptor {
                Label = "Offset g", Group = "Beam offsets", Min = -1.0, Max = 1.0, Decimals = 2,
                Get = () => OffsetG, Set = v => OffsetG = (float)v },
            new ParamDescriptor {
                Label = "Offset h", Group = "Beam offsets", Min = -1.0, Max = 1.0, Decimals = 2,
                Get = () => OffsetH, Set = v => OffsetH = (float)v },

            // Modes.
            new ParamDescriptor {
                Label = "Sequential (0/1)", Group = "Modes", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => Mode, Set = v => Mode = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Julia (0/1)", Group = "Modes", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => JuliaMode, Set = v => JuliaMode = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Julia C x", Group = "Julia constant", Min = -2.0, Max = 2.0, Decimals = 2,
                Get = () => JuliaCx, Set = v => JuliaCx = (float)v },
            new ParamDescriptor {
                Label = "Julia C y", Group = "Julia constant", Min = -2.0, Max = 2.0, Decimals = 2,
                Get = () => JuliaCy, Set = v => JuliaCy = (float)v },
            new ParamDescriptor {
                Label = "Julia C z", Group = "Julia constant", Min = -2.0, Max = 2.0, Decimals = 2,
                Get = () => JuliaCz, Set = v => JuliaCz = (float)v },

            // Quality.
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Bailout", Group = "Quality", Min = 2, Max = 20, Decimals = 1,
                Get = () => Bailout, Set = v => Bailout = (float)v },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.2, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
            new ParamDescriptor {
                Label = "Bound radius", Group = "Quality", Min = 2.0, Max = 12.0, Decimals = 1,
                Get = () => BoundRadius, Set = v => BoundRadius = (float)v },
        },
    };
    public void Reset()
    {
        Iterations = 12;
        Mode = 0;
        JuliaMode = 0;
        Scale = -2.0f;
        FoldOffset = 0.555f;
        OffsetG = 0.0f;
        OffsetH = 0.0f;
        Bailout = 8.0f;
        JuliaCx = 0.0f; JuliaCy = 0.0f; JuliaCz = 0.0f;
        Fudge = 0.55f;
        BoundRadius = 6.0f;
    }}