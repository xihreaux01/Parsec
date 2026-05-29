using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable orbit-trap palette state, shared across all fold fractals, plus a
/// <see cref="ParamSchema"/> exposing the cosine-palette and trap controls. The
/// render side consumes an immutable <see cref="PaletteParams"/> via
/// <see cref="ToParams"/>.
/// </summary>
public sealed class PaletteState
{
    public float BaseR = 0.62f, BaseG = 0.54f, BaseB = 0.44f;
    public float AmpR = 0.38f, AmpG = 0.34f, AmpB = 0.28f;
    public float Frequency = 1.0f;
    public float PhaseR = 0.0f, PhaseG = 0.12f, PhaseB = 0.24f;
    public float TrapScale = 0.6f;
    public float MixOrigin = 0.6f, MixAxis = 0.5f, MixPlane = 0.15f;
    public float ShellMix = 0.35f;

    public PaletteParams ToParams() => new()
    {
        Base = new Vector3(BaseR, BaseG, BaseB),
        Amp = new Vector3(AmpR, AmpG, AmpB),
        Frequency = Frequency,
        Phase = new Vector3(PhaseR, PhaseG, PhaseB),
        TrapScale = TrapScale,
        TrapMix = new Vector3(MixOrigin, MixAxis, MixPlane),
        ShellMix = ShellMix,
    };

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // The two most expressive knobs for a cosine palette: where the
            // bands sit (phase) and how dense they are (frequency / scale).
            new ParamDescriptor {
                Label = "Frequency", Group = "Color: bands", Min = 0.0, Max = 4.0, Decimals = 2,
                Get = () => Frequency, Set = v => Frequency = (float)v },
            new ParamDescriptor {
                Label = "Trap scale", Group = "Color: bands", Min = 0.05, Max = 3.0, Decimals = 2,
                Get = () => TrapScale, Set = v => TrapScale = (float)v },
            new ParamDescriptor {
                Label = "Phase R", Group = "Color: phase", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => PhaseR, Set = v => PhaseR = (float)v },
            new ParamDescriptor {
                Label = "Phase G", Group = "Color: phase", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => PhaseG, Set = v => PhaseG = (float)v },
            new ParamDescriptor {
                Label = "Phase B", Group = "Color: phase", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => PhaseB, Set = v => PhaseB = (float)v },

            new ParamDescriptor {
                Label = "Base R", Group = "Color: base", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => BaseR, Set = v => BaseR = (float)v },
            new ParamDescriptor {
                Label = "Base G", Group = "Color: base", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => BaseG, Set = v => BaseG = (float)v },
            new ParamDescriptor {
                Label = "Base B", Group = "Color: base", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => BaseB, Set = v => BaseB = (float)v },
            new ParamDescriptor {
                Label = "Amp R", Group = "Color: amplitude", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => AmpR, Set = v => AmpR = (float)v },
            new ParamDescriptor {
                Label = "Amp G", Group = "Color: amplitude", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => AmpG, Set = v => AmpG = (float)v },
            new ParamDescriptor {
                Label = "Amp B", Group = "Color: amplitude", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => AmpB, Set = v => AmpB = (float)v },

            // Which traps drive the banding (the "structural vs arbitrary" knob).
            new ParamDescriptor {
                Label = "Mix origin", Group = "Color: trap mix", Min = 0.0, Max = 1.5, Decimals = 2,
                Get = () => MixOrigin, Set = v => MixOrigin = (float)v },
            new ParamDescriptor {
                Label = "Mix axis", Group = "Color: trap mix", Min = 0.0, Max = 1.5, Decimals = 2,
                Get = () => MixAxis, Set = v => MixAxis = (float)v },
            new ParamDescriptor {
                Label = "Mix plane", Group = "Color: trap mix", Min = 0.0, Max = 1.5, Decimals = 2,
                Get = () => MixPlane, Set = v => MixPlane = (float)v },
            new ParamDescriptor {
                Label = "Shell glaze", Group = "Color: trap mix", Min = 0.0, Max = 1.0, Decimals = 2,
                Get = () => ShellMix, Set = v => ShellMix = (float)v },
        },
    };
}
