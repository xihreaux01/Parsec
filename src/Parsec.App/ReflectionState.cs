using Parsec.Rendering.Gpu;

namespace Parsec.App;

/// <summary>
/// Shared glossy-reflection controls, applied across all fractals (like the
/// palette). Reflectivity is a property of the LOOK you're going for, not of a
/// particular fractal's math -- the same fractal wants matte for showing raw
/// geometry and glossy for a ceramic/glass treatment. So these live in one
/// shared state and are exposed for every fractal.
///
/// "Bounces = 0" means reflections are off, and is the default: with bounces at
/// zero the renderer takes the original single-bounce path byte-for-byte, so the
/// default look of every fractal (and the default preview cost) is unchanged.
/// Turn bounces up to opt in.
///
/// Found regimes worth knowing (from prototyping):
///   - Smooth fractals (QJulia) + gloss -> fired-ceramic / glazed look
///   - Detailed fractals (Mandelbulb) + gloss -> cast-glass / resin paperweight
///   F0 ~0.05 reads as ceramic/glass; pushing toward ~0.8 reads as metal.
/// </summary>
public sealed class ReflectionState
{
    /// <summary>Reflection bounce depth. 0 = off (single-bounce original path).
    /// 1 = surfaces reflect; 2 = reflections-of-reflections; 3 = hero polish.</summary>
    public int Bounces = 0;

    /// <summary>Overall reflection strength in [0,1]. Scales the fresnel weight.</summary>
    public float Gloss = 0.6f;

    /// <summary>Schlick fresnel base reflectivity. ~0.05 = dielectric (ceramic /
    /// glass, edge-weighted); ~0.8 = metallic (strong even face-on).</summary>
    public float F0 = 0.05f;

    public ParamSchema BuildSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor {
                Label = "Reflection bounces (0=off)", Group = "Reflections",
                Min = 0, Max = 3, Step = 1, Decimals = 0,
                Get = () => Bounces, Set = v => Bounces = (int)Math.Round(v) },
            new ParamDescriptor {
                Label = "Gloss", Group = "Reflections",
                Min = 0, Max = 1, Decimals = 2,
                Get = () => Gloss, Set = v => Gloss = (float)v },
            new ParamDescriptor {
                Label = "Fresnel F0 (0.05 ceramic … 0.8 metal)", Group = "Reflections",
                Min = 0.02, Max = 0.9, Decimals = 2,
                Get = () => F0, Set = v => F0 = (float)v },
        },
    };
}
