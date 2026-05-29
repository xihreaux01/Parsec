using System.Numerics;

namespace Parsec.Core.Attractors;

/// <summary>
/// Configuration for the (enhanced) Thomas cyclically-symmetric attractor.
///
/// Canonical Thomas is <c>dx = sin(y) - b*x</c> (cyclic in x->y->z->x). The
/// enhancement layers on four perturbation channels (Meadow's recipe, ported
/// from the Unity ThomasAttractorSDF and validated in Python):
///   1. Parameter drift: per-axis damping b1/b2/b3 driven by sin(driftPhase + k)
///      with k = 0, 120, 240 degrees.
///   2. Phase modulation: per-axis amplitude driven by position, k = 0, 60, 120.
///   3. Seed phase shift: each multi-seed orbit shifts the sines by seed*2pi/N.
///   4. Nonlinear coupling: quadratic cross terms (Rossler/Lorenz-like).
///
/// driftPhase replaces Unity's Time.time randomizer: a fixed value is
/// reproducible; randomizing it explores the family (the old 'G' key).
/// Defaults are CANONICAL Thomas (all perturbations off) -- the clean logo shape.
/// </summary>
public sealed record AttractorParams
{
    public float B { get; init; } = 0.208186f;
    public float Dt { get; init; } = 0.05f;
    public int NumSteps { get; init; } = 200_000;

    public bool UseParameterDrift { get; init; } = false;
    public bool UsePhaseModulation { get; init; } = false;
    public bool UseNonlinearCoupling { get; init; } = false;
    public bool UseMultiSeed { get; init; } = false;

    public float ParameterVariation { get; init; } = 0.1f;
    public float AmplitudeModulation { get; init; } = 1.0f;
    public float CouplingStrength { get; init; } = 0.01f;
    public int SeedCount { get; init; } = 5;

    /// <summary>Reproducible replacement for Unity's Time.time drift randomizer.</summary>
    public float DriftPhase { get; init; } = 0.0f;
}

/// <summary>
/// Integrates the Thomas attractor trajectory (RK4, with transient burn-in),
/// producing points tagged with normalized progress along the orbit (for
/// progress-based coloring). Pure CPU; the result feeds the GPU renderer's
/// trajectory buffer and spatial hash. This is the "generate" step -- expensive
/// (hundreds of thousands of RK4 steps), run on demand, not per frame.
/// </summary>
public static class ThomasAttractor
{
    private const float TwoPi = 2.0f * MathF.PI;

    /// <summary>A trajectory point: position plus 0..1 progress along the orbit.</summary>
    public readonly record struct TrajectoryPoint(Vector3 Position, float Progress);

    public static IReadOnlyList<TrajectoryPoint> Generate(AttractorParams p)
    {
        var points = new List<TrajectoryPoint>(p.NumSteps);

        if (p.UseMultiSeed)
        {
            int stepsPerSeed = p.NumSteps / p.SeedCount;
            for (int seed = 0; seed < p.SeedCount; seed++)
            {
                Vector3 pos = VariedInitialPoint(seed, p.SeedCount);
                BurnIn(ref pos, p, seed);
                for (int i = 0; i < stepsPerSeed; i++)
                {
                    float progress = (float)(seed * stepsPerSeed + i) / p.NumSteps;
                    points.Add(new TrajectoryPoint(pos, progress));
                    pos = Rk4Step(pos, p, seed);
                }
            }
        }
        else
        {
            Vector3 pos = new(1.0f, 0.0f, 0.0f);
            BurnIn(ref pos, p, 0);
            for (int i = 0; i < p.NumSteps; i++)
            {
                points.Add(new TrajectoryPoint(pos, (float)i / p.NumSteps));
                pos = Rk4Step(pos, p, 0);
            }
        }

        return points;
    }

    private static void BurnIn(ref Vector3 pos, AttractorParams p, int seed)
    {
        // Skip the transient so we start on the attractor, not the approach to it.
        for (int i = 0; i < 1000; i++)
            pos += Derivative(pos, p, seed) * p.Dt;
    }

    private static Vector3 VariedInitialPoint(int seedIndex, int seedCount)
    {
        float angle = (seedIndex * TwoPi) / seedCount;
        float radius = 0.1f + seedIndex * 0.05f;
        return new Vector3(
            radius * MathF.Cos(angle),
            radius * MathF.Sin(angle),
            radius * MathF.Sin(angle * 2.0f) * 0.5f);
    }

    private static Vector3 Rk4Step(Vector3 pos, AttractorParams p, int seed)
    {
        Vector3 k1 = Derivative(pos, p, seed);
        Vector3 k2 = Derivative(pos + k1 * p.Dt * 0.5f, p, seed);
        Vector3 k3 = Derivative(pos + k2 * p.Dt * 0.5f, p, seed);
        Vector3 k4 = Derivative(pos + k3 * p.Dt, p, seed);
        return pos + (k1 + 2 * k2 + 2 * k3 + k4) * (p.Dt / 6.0f);
    }

    /// <summary>
    /// The (enhanced) Thomas derivative. With all perturbation flags off this is
    /// exactly canonical Thomas: (sin y - b x, sin z - b y, sin x - b z).
    /// </summary>
    private static Vector3 Derivative(Vector3 pos, AttractorParams p, int seedIndex)
    {
        float b1 = p.B, b2 = p.B, b3 = p.B;
        if (p.UseParameterDrift)
        {
            b1 += MathF.Sin(p.DriftPhase) * p.ParameterVariation;
            b2 += MathF.Sin(p.DriftPhase + 2.094f) * p.ParameterVariation;  // +120 deg
            b3 += MathF.Sin(p.DriftPhase + 4.188f) * p.ParameterVariation;  // +240 deg
        }

        float a1 = p.AmplitudeModulation, a2 = p.AmplitudeModulation, a3 = p.AmplitudeModulation;
        if (p.UsePhaseModulation)
        {
            float posFactor = (pos.X + pos.Y + pos.Z) * 0.01f;
            a1 += MathF.Sin(posFactor) * 0.2f;
            a2 += MathF.Sin(posFactor + 1.047f) * 0.2f;  // +60 deg
            a3 += MathF.Sin(posFactor + 2.094f) * 0.2f;  // +120 deg
        }

        float ps = p.UseMultiSeed ? (seedIndex * TwoPi) / p.SeedCount : 0.0f;

        var dp = new Vector3(
            a1 * MathF.Sin(pos.Y + ps) - b1 * pos.X,
            a2 * MathF.Sin(pos.Z + ps) - b2 * pos.Y,
            a3 * MathF.Sin(pos.X + ps) - b3 * pos.Z);

        if (p.UseNonlinearCoupling)
        {
            dp.X += p.CouplingStrength * pos.Y * pos.Z;
            dp.Y += p.CouplingStrength * pos.Z * pos.X;
            dp.Z += p.CouplingStrength * pos.X * pos.Y;
        }

        return dp;
    }
}
