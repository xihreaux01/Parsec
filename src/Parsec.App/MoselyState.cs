using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the Mosely-snowflake KIFS, plus a <see cref="ParamSchema"/>
/// describing its tunable parameters. Mirrors <see cref="KifsState"/>; the
/// rendering side consumes an immutable <see cref="MoselyParams"/> snapshot via
/// <see cref="ToParams"/>.
///
/// Knob notes:
///   Twist 0 deg + Wedge 360 deg (== OFF) recovers the exact pure snowflake.
///   Sweeping Twist toward ~118 deg is the chiral-pinwheel "reveal" (animatable).
///   Dialing Wedge down to ~120 deg turns it into a radial mandala.
///   Body 1.0 -> ~1.5 goes from sparse dust to fuller lace.
/// </summary>
public sealed class MoselyState
{
    public int Iterations = 16;
    public float Scale = 3.0f;
    public float Body = 1.4f;

    public float TwistDeg = 0f;
    public float WedgeDeg = 360f;   // 360 == OFF (full circle, no fold)

    public float Fudge = 0.9f;

    private const float Deg = MathF.PI / 180f;

    public MoselyParams ToParams() => new()
    {
        Iterations = Iterations,
        Scale = Scale,
        Body = Body,
        TwistRadians = TwistDeg * Deg,
        WedgeRadians = WedgeDeg * Deg,   // 360 deg -> 2*pi -> core treats as OFF
        Fudge = Fudge,
        BoundRadius = 2.0f,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Form: scale stays ~3 for the true snowflake; body fattens the lace.
            new ParamDescriptor {
                Label = "Scale", Group = "Form", Min = 2.6, Max = 3.4, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },
            new ParamDescriptor {
                Label = "Body", Group = "Form", Min = 1.0, Max = 2.0, Decimals = 2,
                Get = () => Body, Set = v => Body = (float)v },

            // The two deformation knobs. Twist is the chiral generator; wedge is
            // the kaleidoscope fold about the body axis. Most expressive knobs.
            new ParamDescriptor {
                Label = "Twist", Group = "Twist & Fold", Min = 0, Max = 240, Decimals = 0,
                Get = () => TwistDeg, Set = v => TwistDeg = (float)v },
            new ParamDescriptor {
                Label = "Wedge (360=off)", Group = "Twist & Fold", Min = 60, Max = 360, Decimals = 0,
                Get = () => WedgeDeg, Set = v => WedgeDeg = (float)v },

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
        Iterations = 16;
        Scale = 3.0f;
        Body = 1.4f;
        TwistDeg = 0f;
        WedgeDeg = 360f;
        Fudge = 0.9f;
    }}