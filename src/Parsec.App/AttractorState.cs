using Parsec.Core.Attractors;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the Thomas strange attractor. Unlike the fold/inversion
/// fractals, the attractor's shape comes from an expensive GENERATE step
/// (integrate trajectory + build spatial hash), so its parameters split in two:
///
///   - Generation params (b, dt, steps, perturbations, seeds, drift phase):
///     changing them does NOT re-render live; they take effect on "Generate".
///   - Live params (tube radius, fudge): affect only the raymarch and update
///     immediately, no regeneration.
///
/// The default is CANONICAL Thomas (all perturbations off) -- the clean logo.
/// Perturbations are exposed as strength sliders that naturally zero out, except
/// multi-seed which is a structural mode (a 0/1 stepped slider acting as toggle).
/// Drift phase is the reproducible replacement for Unity's Time.time randomizer.
/// </summary>
public sealed class AttractorState
{
    // --- Generation params (require Generate) ---
    public float B = 0.19f;
    public float Dt = 0.05f;
    public int NumSteps = 200_000;

    public float ParameterVariation = 0.0f;   // 0 = parameter drift off
    public float AmplitudeModulation = 1.0f;   // 1 = neutral
    public float PhaseModAmount = 0.0f;        // 0 = phase modulation off
    public float CouplingStrength = 0.0f;      // 0 = nonlinear coupling off
    public float DriftPhase = 0.0f;            // reproducible "explore" knob
    public int MultiSeed = 0;                  // 0/1 structural toggle
    public int SeedCount = 5;

    // --- Live params (no regeneration) ---
    public float TubeRadius = 0.06f;
    public float Fudge = 0.45f;

    /// <summary>Generation parameters for the integrator + hash build.</summary>
    public AttractorParams ToParams() => new()
    {
        B = B,
        Dt = Dt,
        NumSteps = NumSteps,
        // A perturbation channel is "on" when its strength is non-zero. Amplitude
        // modulation is always structurally present (neutral at 1.0), so it rides
        // with phase modulation here.
        UseParameterDrift = ParameterVariation > 1e-6f,
        UsePhaseModulation = PhaseModAmount > 1e-6f,
        UseNonlinearCoupling = CouplingStrength > 1e-6f,
        UseMultiSeed = MultiSeed >= 1,
        ParameterVariation = ParameterVariation,
        AmplitudeModulation = AmplitudeModulation,
        CouplingStrength = CouplingStrength,
        SeedCount = SeedCount,
        DriftPhase = DriftPhase,
    };

    /// <summary>Live render parameters (tube look), applied without regenerating.</summary>
    public AttractorRenderParams ToRenderParams() => new()
    {
        TubeRadius = TubeRadius,
        Fudge = Fudge,
    };

    /// <summary>
    /// Generation schema -- these controls take effect on Generate, not live.
    /// The panel marks them so the UI can route their changes to "needs
    /// regenerate" rather than a live re-render.
    /// </summary>
    public ParamSchema BuildGenerateSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Damping b", Group = "Attractor (Generate)", Min = 0.05, Max = 0.30, Decimals = 3,
                Get = () => B, Set = v => B = (float)v },
            new ParamDescriptor {
                Label = "Steps (x1000)", Group = "Attractor (Generate)", Min = 20, Max = 400, Step = 10, Decimals = 0,
                Get = () => NumSteps / 1000.0, Set = v => NumSteps = (int)(v * 1000) },

            new ParamDescriptor {
                Label = "Param variation", Group = "Perturbations (Generate)", Min = 0, Max = 0.3, Decimals = 3,
                Get = () => ParameterVariation, Set = v => ParameterVariation = (float)v },
            new ParamDescriptor {
                Label = "Phase mod", Group = "Perturbations (Generate)", Min = 0, Max = 1.0, Decimals = 2,
                Get = () => PhaseModAmount, Set = v => PhaseModAmount = (float)v },
            new ParamDescriptor {
                Label = "Coupling", Group = "Perturbations (Generate)", Min = 0, Max = 0.05, Decimals = 3,
                Get = () => CouplingStrength, Set = v => CouplingStrength = (float)v },
            new ParamDescriptor {
                Label = "Drift phase", Group = "Perturbations (Generate)", Min = 0, Max = 6.283, Decimals = 2,
                Get = () => DriftPhase, Set = v => DriftPhase = (float)v },

            new ParamDescriptor {
                Label = "Multi-seed (0/1)", Group = "Seeds (Generate)", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => MultiSeed, Set = v => MultiSeed = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Seed count", Group = "Seeds (Generate)", Min = 1, Max = 10, Step = 1, Decimals = 0,
                Get = () => SeedCount, Set = v => SeedCount = (int)Math.Round(v) },
        },
    };

    /// <summary>Live schema -- tube look, applied immediately without regenerating.</summary>
    public ParamSchema BuildLiveSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Tube radius", Group = "Tube (live)", Min = 0.01, Max = 0.2, Decimals = 3,
                Get = () => TubeRadius, Set = v => TubeRadius = (float)v },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Tube (live)", Min = 0.2, Max = 0.8, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
        },
    };

    /// <summary>Combined schema for display (generation groups first, then live).</summary>
    public ParamSchema BuildSchema() => new()
    {
        Parameters = BuildGenerateSchema().Parameters
            .Concat(BuildLiveSchema().Parameters).ToArray(),
    };
    public void Reset()
    {
        B = 0.19f;
        Dt = 0.05f;
        NumSteps = 200_000;
        ParameterVariation = 0.0f;
        AmplitudeModulation = 1.0f;
        PhaseModAmount = 0.0f;
        CouplingStrength = 0.0f;
        DriftPhase = 0.0f;
        MultiSeed = 0;
        SeedCount = 5;
        TubeRadius = 0.06f;
        Fudge = 0.45f;
    }}