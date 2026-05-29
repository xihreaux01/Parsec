using System.Numerics;

namespace Parsec.Rendering.Gpu;

/// <summary>
/// Orbit-trap palette parameters, shared across the fold fractal family. Drives
/// the cosine palette (Inigo Quilez form) in raymarch_main.glsl:
/// <c>col = Base + Amp * cos(2pi (Frequency * t + Phase))</c>, where t is a
/// weighted blend of the orbit traps. Packed into the RenderParams buffer.
/// </summary>
public sealed record PaletteParams
{
    public Vector3 Base { get; init; } = new(0.62f, 0.54f, 0.44f);
    public Vector3 Amp { get; init; } = new(0.38f, 0.34f, 0.28f);
    public float Frequency { get; init; } = 1.0f;
    public Vector3 Phase { get; init; } = new(0.0f, 0.12f, 0.24f);

    /// <summary>Master scale on the trap blend before the palette cycles it.</summary>
    public float TrapScale { get; init; } = 0.6f;

    /// <summary>Weights of (origin, axis, plane) traps into the palette input.</summary>
    public Vector3 TrapMix { get; init; } = new(0.6f, 0.5f, 0.15f);

    /// <summary>How strongly the unit-shell trap brightens fold creases (glaze).</summary>
    public float ShellMix { get; init; } = 0.35f;

    public static PaletteParams Default { get; } = new();
}
