using System.Numerics;
using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Mutable live state for the Riemann Sphere (Msltoe) fractal, plus a
/// <see cref="ParamSchema"/> describing its tunable parameters. Mirrors
/// <see cref="PseudoKleinian4DState"/>; the rendering side consumes an immutable
/// <see cref="RiemannSphereParams"/> snapshot via <see cref="ToParams"/>.
///
/// Knob guide:
///   - Fold offset A/B shift the sine-fold pattern -- the most expressive knobs,
///     and the obvious animation targets (sweep them for a morphing cellular
///     surface). Zero gives the symmetric canonical form.
///   - Scale sets the sphere-projection radius; small changes restructure a lot.
///   - Bailout stays LOW by design (exponent 2p ~ 72 overflows past r~4).
///   - The DE is approximate (Mandelbulb-style); if the surface looks faceted,
///     lower DE fudge first, then raise iterations.
/// </summary>
public sealed class RiemannSphereState
{
    public int Iterations = 20;

    // 0 = Mandelbrot (c = position), 1 = Julia (c = JuliaC)
    public int JuliaMode = 0;

    public float Scale = 1.0f;
    public float OffsetA = 0.4f;
    public float OffsetB = 0.7f;

    public float Bailout = 2.0f;
    public float PowerClamp = 36.0f;

    public float JuliaCx = 0.0f, JuliaCy = 0.0f, JuliaCz = 0.0f;

    public float RotX = 0f, RotY = 0f, RotZ = 0f;

    public float Fudge = 0.6f;
    public float BoundRadius = 3.0f;

    public RiemannSphereParams ToParams() => new()
    {
        Iterations = Iterations,
        JuliaMode = JuliaMode,
        Scale = Scale,
        OffsetA = OffsetA,
        OffsetB = OffsetB,
        Bailout = Bailout,
        PowerClamp = PowerClamp,
        JuliaC = new Vector3(JuliaCx, JuliaCy, JuliaCz),
        RotationRadians = new Vector3(RotX, RotY, RotZ),
        Fudge = Fudge,
        BoundRadius = BoundRadius,
    };

    private const float Deg = MathF.PI / 180f;

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            // Form.
            new ParamDescriptor {
                Label = "Scale", Group = "Form", Min = 0.5, Max = 2.0, Decimals = 2,
                Get = () => Scale, Set = v => Scale = (float)v },

            // The sine-fold offsets -- the expressive / animatable knobs.
            new ParamDescriptor {
                Label = "Fold offset A", Group = "Sine fold", Min = -3.14, Max = 3.14, Decimals = 2,
                Get = () => OffsetA, Set = v => OffsetA = (float)v },
            new ParamDescriptor {
                Label = "Fold offset B", Group = "Sine fold", Min = -3.14, Max = 3.14, Decimals = 2,
                Get = () => OffsetB, Set = v => OffsetB = (float)v },

            // Mode + Julia constant.
            new ParamDescriptor {
                Label = "Julia (0/1)", Group = "Modes", Min = 0, Max = 1, Step = 1, Decimals = 0,
                Get = () => JuliaMode, Set = v => JuliaMode = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Julia C x", Group = "Julia constant", Min = -1.0, Max = 1.0, Decimals = 2,
                Get = () => JuliaCx, Set = v => JuliaCx = (float)v },
            new ParamDescriptor {
                Label = "Julia C y", Group = "Julia constant", Min = -1.0, Max = 1.0, Decimals = 2,
                Get = () => JuliaCy, Set = v => JuliaCy = (float)v },
            new ParamDescriptor {
                Label = "Julia C z", Group = "Julia constant", Min = -1.0, Max = 1.0, Decimals = 2,
                Get = () => JuliaCz, Set = v => JuliaCz = (float)v },

            // Optional rotation.
            new ParamDescriptor {
                Label = "Rot X", Group = "Rotation", Min = -45, Max = 45, Decimals = 0,
                Get = () => RotX / Deg, Set = v => RotX = (float)v * Deg },
            new ParamDescriptor {
                Label = "Rot Y", Group = "Rotation", Min = -45, Max = 45, Decimals = 0,
                Get = () => RotY / Deg, Set = v => RotY = (float)v * Deg },
            new ParamDescriptor {
                Label = "Rot Z", Group = "Rotation", Min = -45, Max = 45, Decimals = 0,
                Get = () => RotZ / Deg, Set = v => RotZ = (float)v * Deg },

            // Distance estimate / quality.
            new ParamDescriptor {
                Label = "Power clamp", Group = "Quality", Min = 2, Max = 36, Step = 1, Decimals = 0,
                Get = () => PowerClamp, Set = v => PowerClamp = (float)v },
            new ParamDescriptor {
                Label = "Iterations", Group = "Quality", Min = 4, Max = 40, Step = 1, Decimals = 0,
                Get = () => Iterations, Set = v => Iterations = (int)Math.Round(v) },
            // Kept low on purpose -- high bailout overflows the 2p exponent.
            new ParamDescriptor {
                Label = "Bailout", Group = "Quality", Min = 1.2, Max = 3.0, Decimals = 2,
                Get = () => Bailout, Set = v => Bailout = (float)v },
            new ParamDescriptor {
                Label = "DE fudge", Group = "Quality", Min = 0.3, Max = 1.0, Decimals = 2,
                Get = () => Fudge, Set = v => Fudge = (float)v },
            new ParamDescriptor {
                Label = "Bound radius", Group = "Quality", Min = 1.0, Max = 8.0, Decimals = 1,
                Get = () => BoundRadius, Set = v => BoundRadius = (float)v },
        },
    };
}
