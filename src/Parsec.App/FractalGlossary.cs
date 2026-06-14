using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Parsec.App;

/// <summary>One glossary entry: a display <see cref="Term"/>, a plain-language
/// <see cref="Definition"/>, and the <see cref="Tokens"/> to search for in guide
/// text (matched whole-word, case-insensitive).</summary>
public sealed record GlossaryEntry(string Term, string Definition, string[] Tokens);

/// <summary>
/// Shared glossary of the obscure fractal and rendering vocabulary used across the
/// guides. Each definition is written once here; a guide's Glossary section is the
/// subset of entries whose tokens actually appear in that guide's text (assembled by
/// <see cref="FractalGuide.BuildDocument"/>). This keeps definitions DRY and
/// consistent and means any obscure term that shows up is automatically explained.
/// </summary>
public static class FractalGlossary
{
    public static readonly IReadOnlyList<GlossaryEntry> Entries = new[]
    {
        new GlossaryEntry("Distance estimator (DE)",
            "A function that returns a safe lower bound on the distance from a point to the fractal surface, so the raymarcher can take large steps without overshooting.",
            new[] { "distance estimator", "distance estimate", "DE" }),
        new GlossaryEntry("DE fudge",
            "A safety factor below 1 applied to each marching step. Lower it to remove overstepping speckle, raise it toward 1 for speed.",
            new[] { "DE fudge", "fudge" }),
        new GlossaryEntry("Raymarching",
            "Rendering by stepping a ray forward by the distance-estimator value at each point until it reaches the surface.",
            new[] { "raymarch", "raymarching", "raymarched", "ray march" }),
        new GlossaryEntry("Orbit",
            "The sequence of points a starting value visits as the formula is applied to it over and over.",
            new[] { "orbit" }),
        new GlossaryEntry("Orbit trap",
            "A coloring method that records how close the iterated orbit passes to a chosen shape (a point, axis, or plane) and turns that closeness into color.",
            new[] { "orbit trap", "orbit-trap" }),
        new GlossaryEntry("Bailout / escape radius",
            "The magnitude threshold at which an orbit is treated as having escaped to infinity, ending the iteration for that point.",
            new[] { "bailout", "escape radius" }),
        new GlossaryEntry("Escape-time fractal",
            "A fractal (Mandelbrot, Julia, Burning Ship) colored by how many iterations a point survives before its orbit escapes the bailout radius.",
            new[] { "escape-time", "escape time" }),
        new GlossaryEntry("Iteration",
            "One application of the fractal's formula. Fractals are defined by repeating (iterating) a simple rule many times.",
            new[] { "iterate", "iterated", "iteration" }),
        new GlossaryEntry("Fold",
            "A space-warping step (such as a box fold or sphere fold) applied each iteration; the defining move of the Mandelbox family.",
            new[] { "fold", "folding", "folded" }),
        new GlossaryEntry("Box fold",
            "A Mandelbox operation that reflects any coordinate past a limit back toward the center, folding space into a boxed region.",
            new[] { "box fold" }),
        new GlossaryEntry("Sphere fold",
            "A Mandelbox operation that inverts points inside a small radius outward, inflating the inner region of the fractal.",
            new[] { "sphere fold" }),
        new GlossaryEntry("Folding limit",
            "The box-fold threshold: coordinates beyond it get reflected back. It controls how tightly space is boxed.",
            new[] { "folding limit" }),
        new GlossaryEntry("IFS (iterated function system)",
            "A fractal built by repeatedly applying a small set of contracting transforms (scale, rotate, translate) to a shape.",
            new[] { "iterated function system", "IFS" }),
        new GlossaryEntry("KIFS (kaleidoscopic IFS)",
            "An IFS with absolute-value and rotation folds added, producing kaleidoscopic, crystalline structures.",
            new[] { "KIFS", "kaleidoscopic" }),
        new GlossaryEntry("Perturbation theory",
            "A deep-zoom technique that computes one high-precision reference orbit, then iterates cheap low-precision differences from it for every pixel.",
            new[] { "perturbation" }),
        new GlossaryEntry("Reference orbit",
            "The single high-precision orbit, computed once at the view center, that perturbation deep zoom measures every pixel relative to.",
            new[] { "reference orbit" }),
        new GlossaryEntry("Delta iteration",
            "Iterating the small per-pixel difference from the reference orbit instead of the full value, the core trick of perturbation deep zoom.",
            new[] { "delta iteration", "per-pixel delta", "delta" }),
        new GlossaryEntry("floatexp",
            "An extended-range floating-point format (a double plus a separate large exponent) used for the deepest zooms, past where ordinary doubles underflow.",
            new[] { "floatexp" }),
        new GlossaryEntry("Mandelbrot set",
            "The set of complex numbers c for which z -> z^2 + c stays bounded starting from zero; the original escape-time fractal.",
            new[] { "Mandelbrot set" }),
        new GlossaryEntry("Julia set",
            "The shape obtained by fixing the added constant and varying the starting point; each constant yields a different Julia set.",
            new[] { "Julia set", "Julia" }),
        new GlossaryEntry("Julia constant (kappa)",
            "The fixed value (called kappa here) added on every iteration in Julia mode; sweeping it morphs the set in an animation.",
            new[] { "Julia constant", "kappa" }),
        new GlossaryEntry("Quaternion",
            "A four-component extension of complex numbers, used to build 3D and 4D Julia sets.",
            new[] { "quaternion" }),
        new GlossaryEntry("Bicomplex",
            "A four-dimensional number system (two complex units) whose Julia sets form the bicomplex 'Tetrabrot' family.",
            new[] { "bicomplex" }),
        new GlossaryEntry("Triplex",
            "The 3D number algebra behind the Mandelbulb's spherical power rule. It is not a true mathematical field, which is why its multiplication is custom.",
            new[] { "triplex" }),
        new GlossaryEntry("Stereographic projection",
            "A mapping between a flat plane and a sphere; used here to wrap a Julia slice onto a curved cut.",
            new[] { "stereographic" }),
        new GlossaryEntry("Conformal",
            "Angle-preserving: a transformation that keeps local angles and fine shape intact even while changing scale.",
            new[] { "conformal" }),
        new GlossaryEntry("Inversion",
            "Turning space inside-out through a sphere: points near the center map far away and vice versa. The basis of Kleinian and Apollonian fractals.",
            new[] { "inversion", "inversions", "inverting" }),
        new GlossaryEntry("Mobius transformation",
            "A conformal map of the form (az + b) / (cz + d); compositions of these generate Kleinian limit sets.",
            new[] { "Mobius", "Möbius" }),
        new GlossaryEntry("Kleinian group",
            "A group of Mobius and inversion transforms whose repeated application carves out an intricate limit set.",
            new[] { "Kleinian" }),
        new GlossaryEntry("Limit set",
            "The fractal residue that a group of transforms converges onto after infinitely many applications.",
            new[] { "limit set" }),
        new GlossaryEntry("Apollonian gasket",
            "A fractal packing of mutually tangent circles or spheres, where every gap is recursively filled by smaller ones.",
            new[] { "Apollonian", "gasket" }),
        new GlossaryEntry("Descartes circle theorem",
            "The relation between the curvatures of four mutually tangent circles that drives the Apollonian packing.",
            new[] { "Descartes circle", "Descartes" }),
        new GlossaryEntry("Menger sponge",
            "A cube recursively hollowed by removing its central cross in every sub-cube; a classic 3D fractal.",
            new[] { "Menger" }),
        new GlossaryEntry("Biomorph",
            "Clifford Pickover's biomorphs: organic, cell-like shapes produced by adding a trig-based test to an escape-time fractal.",
            new[] { "biomorph" }),
        new GlossaryEntry("Strange attractor",
            "A set that a chaotic system settles onto: trajectories never repeat yet stay within an intricate bounded shape.",
            new[] { "strange attractor", "attractor" }),
        new GlossaryEntry("Cosine palette",
            "A coloring scheme that builds smooth color bands from cosine waves with adjustable base, amplitude, frequency, and phase.",
            new[] { "cosine palette" }),
        new GlossaryEntry("Fresnel",
            "The optical effect where surfaces look more reflective at glancing angles; F0 sets the head-on reflectivity.",
            new[] { "fresnel" }),
        new GlossaryEntry("Supersampling (SSAA)",
            "Rendering at a higher resolution and averaging down to remove jagged edges (anti-aliasing).",
            new[] { "supersampling", "SSAA", "anti-alias", "anti-aliasing" }),
        new GlossaryEntry("Fractal dimension",
            "A non-integer measure of how completely a fractal fills space; higher means denser, more space-filling detail.",
            new[] { "fractal dimension" }),
    };

    /// <summary>
    /// Entries whose any token appears as a whole word/phrase in <paramref name="text"/>,
    /// case-insensitive, each entry at most once, sorted alphabetically by Term. Pure.
    /// </summary>
    public static IReadOnlyList<GlossaryEntry> Match(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<GlossaryEntry>();

        var hits = new List<GlossaryEntry>();
        foreach (var entry in Entries)
        {
            foreach (var token in entry.Tokens)
            {
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(token)}\b", RegexOptions.IgnoreCase))
                {
                    hits.Add(entry);
                    break;
                }
            }
        }
        return hits.OrderBy(e => e.Term, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
