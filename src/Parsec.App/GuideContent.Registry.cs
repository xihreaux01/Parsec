using System.Collections.Generic;

namespace Parsec.App;

/// <summary>Hand-authored guide prose for every fractal and deep-zoom formula.
/// The settings list itself is auto-derived from the live schema; this only
/// supplies the per-setting NOTE text (keyed by the exact ParamDescriptor.Label)
/// plus the surrounding prose. Anchored to the committed research in
/// docs/superpowers/research/.</summary>
public static partial class FractalGuide
{
    public static GuideContent Resolve(FractalType type, int deepFormula)
    {
        if (type == FractalType.DeepZoom)
            return DeepFormulas[System.Math.Clamp(deepFormula, 0, DeepFormulas.Count - 1)];
        return Registry[type];
    }

    private static readonly IReadOnlyList<GuideContent> DeepFormulas = new[]
    {
        Mandelbrot2D, Prospector2D, Julia2D, BurningShip2D,
    };

    private static readonly IReadOnlyDictionary<FractalType, GuideContent> Registry =
        new Dictionary<FractalType, GuideContent>
        {
            [FractalType.Mandelbox] = Mandelbox,
            [FractalType.AmazingBox] = AmazingBox,
            [FractalType.RotBox] = RotBox,
            [FractalType.Menger] = Menger,
            [FractalType.Mandelbulb] = Mandelbulb,
            [FractalType.QuaternionJulia] = QuaternionJulia,
            [FractalType.Bicomplex] = Bicomplex,
            [FractalType.QJBox] = QJBox,
            [FractalType.Kifs] = Kifs,
            [FractalType.Kleinian] = Kleinian,
            [FractalType.PseudoKleinian4D] = PseudoKleinian4D,
            [FractalType.OrbitHybrid] = OrbitHybrid,
            [FractalType.Apollonian] = Apollonian,
            [FractalType.Phoenix] = Phoenix,
            [FractalType.Biomorph] = Biomorph,
            [FractalType.Mosely] = Mosely,
            [FractalType.RiemannSphere] = RiemannSphere,
            [FractalType.Mandalay] = Mandalay,
            [FractalType.Anisotropic] = Anisotropic,
            [FractalType.Hybrid] = Hybrid,
            [FractalType.Attractor] = Attractor,
            [FractalType.BurningShip] = BurningShip,
        };

    // ===================================================================
    // BOX family (Mandelbox, AmazingBox, RotBox, Menger)
    // Definitions live in GuideContent.Box.cs (same partial class).
    // ===================================================================

    // ===================================================================
    // BULB / JULIA family
    // (Mandelbulb, QuaternionJulia, Bicomplex, QJBox live in
    //  GuideContent.Bulb.cs as part of this same partial class.)
    // ===================================================================

    // ===================================================================
    // KIFS / KLEINIAN family (Kifs, Kleinian, PseudoKleinian4D, OrbitHybrid)
    // Definitions live in GuideContent.Kifs.cs (same partial class).
    // ===================================================================

    // ===================================================================
    // EXOTIC group A (Apollonian, Phoenix, Biomorph, Mosely)
    // Definitions live in GuideContent.ExoticA.cs (same partial class).
    // ===================================================================

    // ===================================================================
    // EXOTIC group B (RiemannSphere, Mandalay, Anisotropic, Hybrid, Attractor)
    // Definitions live in GuideContent.ExoticB.cs (same partial class).
    // ===================================================================

}

