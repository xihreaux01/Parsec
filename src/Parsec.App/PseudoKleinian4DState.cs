using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the Pseudo-Kleinian 4D fractal, plus a
/// <see cref="ParamSchema"/> describing its tunable parameters. Mirrors
/// <see cref="KifsState"/>; the rendering side consumes an immutable
/// <see cref="PseudoKleinian4DParams"/> snapshot via <see cref="ToParams"/>.
///
/// Knob guide (which matter most for the Kleinian look):
///   - Box offset X/Y/Z is the symmetry break -- 0 is symmetric; turn it up for
///     the characteristic asymmetric Kleinian tiling.
///   - Box size shapes the cell lattice; sphere-fold scale sets inversion strength.
///   - W0 slices the 4th dimension (animatable).
///   - Inversion ON conformally bounds the half-space tiling into a ball; OFF
///     renders the raw tiling within the bound sphere (raise Bound radius then).
/// </summary>
public sealed class PseudoKleinian4DState
{
    public int Iterations = 12;

    // 0 = tube DE, 1 = quaternionic min-of-four DE
    public int Mode = 0;
    // 0 = raw (render within bound sphere), 1 = sphere-inversion bounding ON
    public int InversionMode = 1;

    public float BoxX = 1.0f, BoxY = 1.0f, BoxZ = 1.0f;
    public float SphereFoldScale = 1.0f;

    public float OffX = 0.0f, OffY = 0.0f, OffZ = 0.0f;
    public float W0 = 0.0f;

    public float TubeRadius = 0.0f;
    public float DeOffset = 0.0f;
    public float DeTweak = 0.05f;
    public float InversionScale = 1.0f;

    public float Fudge = 0.6f;
    public float BoundRadius = 8.0f;

    public PseudoKleinian4DParams ToParams() => new()
    {
        Iterations = Iterations,
        Mode = Mode,
        InversionMode = InversionMode,
        BoxSize = new Vector3(BoxX, BoxY, BoxZ),
        SphereFoldScale = SphereFoldScale,
        BoxOffset = new Vector3(OffX, OffY, OffZ),
        W0 = W0,
        TubeRadius = TubeRadius,
        DeOffset = DeOffset,
        DeTweak = DeTweak,
        InversionScale = InversionScale,
        Fudge = Fudge,
        BoundRadius = BoundRadius,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Box-fold cell lattice.
            new ParamDescriptor {
                Label = "Box size X", Group = "Form", Min = 0.2, Max = 2.0, Decimals = 2,
                Get = () => BoxX, Set = v => BoxX = (float)v },
            new ParamDescriptor {
                Label = "Box size Y", Group = "Form", Min = 0.2, Max = 2.0, Decimals = 2,
                Get = () => BoxY, Set = v => BoxY = (float)v },
            new ParamDescriptor {
                Label = "Box size Z", Group = "Form", Min = 0.2, Max = 2.0, Decimals = 2,
                Get = () => BoxZ, Set = v => BoxZ = (float)v },
            new ParamDescriptor {
                Label = "Sphere fold", Group = "Form", Min = 0.2, Max = 2.0, Decimals = 2,
                Get = () => SphereFoldScale, Set = v => SphereFoldScale = (float)v },

            // The Kleinian symmetry break -- the most expressive knobs.
            new ParamDescriptor {
                Label = "Offset X", Group = "Kleinian offset", Min = -1.5, Max = 1.5, Decimals = 2,
                Get = () => OffX, Set = v => OffX = (float)v },
            new ParamDescriptor {
                Label = "Offset Y", Group = "Kleinian offset", Min = -1.5, Max = 1.5, Decimals = 2,
                Get = () => OffY, Set = v => OffY = (float)v },
            new ParamDescriptor {
                Label = "Offset Z", Group = "Kleinian offset", Min = -1.5, Max = 1.5, Decimals = 2,
                Get = () => OffZ, Set = v => OffZ = (float)v },

            // 4D slice.
            new ParamDescriptor {
                Label = "W slice", Group = "4D slice", Min = -2.0, Max = 2.0, Decimals = 2,
                Get = () => W0, Set = v => W0 = (float)v },

            // Distance-estimate shaping.
            new ParamDescriptor {
                Label = "Tube radius", Group = "Distance estimate", Min = 0.0, Max = 1.5, Decimals = 2,
                Get = () => TubeRadius, Set = v => TubeRadius = (float)v },
            new ParamDescriptor {
                Label = "DE offset", Group = "Distance estimate", Min = -0.5, Max = 0.5, Decimals = 2,
                Get = () => DeOffset, Set = v => DeOffset = (float)v },
            new ParamDescriptor {
                Label = "DE tweak", Group = "Distance estimate", Min = 0.0, Max = 0.3, Decimals = 3,
                Get = () => DeTweak, Set = v => DeTweak = (float)v },
            new ParamDescriptor {
                Label = "Inversion scale", Group = "Distance estimate", Min = 0.2, Max = 2.0, Decimals = 2,
                Get = () => InversionScale, Set = v => InversionScale = (float)v },

            // Modes (0/1 toggles).
            new ParamDescriptor {
                Label = "Inversion (0/1)", Group = "Modes", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => InversionMode, Set = v => InversionMode = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE form (0/1)", Group = "Modes", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => Mode, Set = v => Mode = (int)Math.Round(v) },

            // Quality.
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 500, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 2.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
            new ParamDescriptor {
                Label = "Bound radius", Group = "Quality", Min = 2.0, Max = 16.0, Decimals = 1,
                Get = () => BoundRadius, Set = v => BoundRadius = (float)v },
        },
    };
    public void Reset()
    {
        Iterations = 12;
        Mode = 0;
        InversionMode = 1;
        BoxX = 1.0f; BoxY = 1.0f; BoxZ = 1.0f;
        SphereFoldScale = 1.0f;
        OffX = 0.0f; OffY = 0.0f; OffZ = 0.0f;
        W0 = 0.0f;
        TubeRadius = 0.0f;
        DeOffset = 0.0f;
        DeTweak = 0.05f;
        InversionScale = 1.0f;
        Fudge = 0.6f;
        BoundRadius = 8.0f;
    }}