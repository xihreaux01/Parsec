using System;
using System.Collections.Generic;
using System.Globalization;

namespace Parsec.App;

/// <summary>
/// Builds a read-only <see cref="GuideDocument"/> for the active fractal. The
/// settings section is auto-derived from the live <see cref="ParamSchema"/> so it
/// never drifts from the real sliders; per-setting NOTE text and the prose come
/// from the hand-written <see cref="GuideContent"/> registry (see
/// GuideContent.Registry.cs).
/// </summary>
public static partial class FractalGuide
{
    /// <summary>Resolve content for the active fractal/formula and render it against
    /// the live schema.</summary>
    public static GuideDocument Build(FractalType type, int deepFormula, ParamSchema schema)
        => BuildDocument(Resolve(type, deepFormula), schema);

    /// <summary>
    /// A shared, beginner-level primer prepended to every guide. It teaches the
    /// universal machinery once (iteration and orbits, escape-time vs distance-
    /// estimated raymarching, complex and hypercomplex numbers, folds, coloring and
    /// lighting) so each per-fractal entry can assume these basics and go straight to
    /// extreme detail about its own object. Deliberately not scanned for the glossary,
    /// so each guide's glossary stays specific to that fractal.
    /// </summary>
    public static readonly IReadOnlyList<string> Primer = new[]
    {
        "A fractal is a shape with structure at every scale: zoom in and you keep finding more detail, often echoes of the whole. What makes that possible is that a fractal is not drawn, it is computed. You start with a simple rule, apply it over and over, and that endless repetition is what manufactures the endless detail. Nothing in this program stores a fractal's shape; every pixel is decided on the spot by running the rule.",
        "The core operation is iteration: take a value, push it through a formula, take the result, and push it back in again, hundreds or thousands of times. The running sequence of values you get is called the orbit of the starting point. Whether a point belongs to the fractal comes down to how its orbit behaves: does it stay contained forever, or does it eventually run off toward infinity?",
        "For the flat 2D sets (Mandelbrot, Julia, Burning Ship) this is the escape-time method. Each pixel on screen stands for one starting number. The program iterates that number's orbit and watches its size. If the magnitude shoots past a threshold (the bailout, or escape radius) the point has escaped and is outside the set; if it never escapes within the iteration budget it is treated as inside. The solid body of the Mandelbrot set is the never-escapes region, and the glowing colors around it encode how many iterations each outside point survived before escaping, which is why the edge looks like flames and filaments.",
        "Those 2D formulas run on complex numbers. A complex number is just a 2D coordinate (a, b) written a + b*i, where i is a unit with the rule i*i = -1. You add them coordinate by coordinate, and the reason they matter is multiplication: multiplying two complex numbers adds their angles and multiplies their lengths. So a rule like z -> z*z + c really means rotate-and-stretch the plane, then shift it, repeated forever. That geometric squeezing and folding of the plane is what carves out the Mandelbrot boundary.",
        "Going 3D is harder, because you cannot color a solid block of space by escape time alone; you would only ever see its outer shell. Instead the 3D fractals here are rendered with a distance estimator (DE): a formula that, given any point in space, returns a safe under-estimate of how far that point is from the fractal's surface. It does not give a direction, only a promise: you can move at least this far without hitting anything.",
        "That estimate powers raymarching. For each pixel the program shoots a ray out from the camera and walks along it in steps, each step as long as the distance estimator currently allows, like feeling through a dark room with a cane that reports the nearest wall is at least X away. When the remaining distance shrinks to almost nothing the ray has reached the surface, and that pixel is shaded. The same DE also yields the surface's facing direction (its normal), which is what lets the 3D fractals catch light, cast shading, and reflect.",
        "The 3D objects push the same iterate-and-test-for-escape idea into higher-dimensional number systems and geometric operations. Some use quaternions (4D cousins of complex numbers) or the triplex (a custom 3D multiplication that powers the Mandelbulb). Many others drop algebra in favor of folds: each iteration physically reflects, inverts, scales, and rotates space. A box fold reflects any coordinate that pokes past a wall back inside; a sphere fold turns the region near the origin inside out. Repeating those folds builds the boxes, spires, and shells of the Mandelbox, Menger, and Kleinian families. A few objects here are made differently again (a strange attractor, for example, traces the path of a chaotic motion instead of testing for escape), and their own sections say so.",
        "Finally, color and light. Surfaces are tinted with orbit traps, which record how close each point's orbit passed to chosen landmarks (the origin, an axis, a plane) and feed those distances into a smooth cosine color palette. For the 3D fractals the distance estimator's normal drives diffuse shading, optional reflections, and a key light, so the raw math ends up looking carved, glazed, or metallic. Every control in the panel on the right adjusts some part of this pipeline; the sections below explain what each one does for this particular fractal.",
    };

    /// <summary>Pure: turn explicit content + a schema into render-ready blocks.
    /// No Avalonia, no GL, so this is the unit-tested seam.</summary>
    public static GuideDocument BuildDocument(GuideContent content, ParamSchema schema)
    {
        var blocks = new List<GuideBlock>();

        blocks.Add(new GuideBlock.Heading("How these fractals are made (start here)"));
        foreach (var p in Primer) blocks.Add(new GuideBlock.Paragraph(p));

        blocks.Add(new GuideBlock.Heading("What this fractal is"));
        foreach (var p in content.WhatItIs) blocks.Add(new GuideBlock.Paragraph(p));

        blocks.Add(new GuideBlock.Heading("How Parsec computes it"));
        foreach (var p in content.HowComputed) blocks.Add(new GuideBlock.Paragraph(p));

        if (content.Math.Count > 0)
        {
            blocks.Add(new GuideBlock.Heading("The math"));
            foreach (var p in content.Math) blocks.Add(new GuideBlock.Paragraph(p));
        }

        blocks.Add(new GuideBlock.Heading("Settings"));
        foreach (var group in schema.Groups)
        {
            blocks.Add(new GuideBlock.SettingGroupHeading(group));
            foreach (var d in schema.InGroup(group))
                blocks.Add(new GuideBlock.SettingDefinition(d.Label, FormatRange(d), NoteFor(content, d.Label)));
        }

        blocks.Add(new GuideBlock.Heading("For best results"));
        foreach (var p in content.BestResults) blocks.Add(new GuideBlock.Paragraph(p));

        AppendGlossary(blocks, content);

        return new GuideDocument(content.Title, blocks);
    }

    /// <summary>Scan everything this guide says (prose plus the rendered setting names
    /// and notes) for glossary terms, and append a Glossary section defining each term
    /// that actually appears. Nothing is appended when no obscure term is present.</summary>
    private static void AppendGlossary(List<GuideBlock> blocks, GuideContent content)
    {
        var corpus = new System.Text.StringBuilder();
        void Add(IEnumerable<string> lines)
        {
            foreach (var line in lines) { corpus.Append(line); corpus.Append('\n'); }
        }
        Add(content.WhatItIs);
        Add(content.HowComputed);
        Add(content.Math);
        Add(content.BestResults);
        foreach (var b in blocks)
            if (b is GuideBlock.SettingDefinition d)
            {
                corpus.Append(d.Name); corpus.Append(' ');
                corpus.Append(d.Note); corpus.Append('\n');
            }

        var matched = FractalGlossary.Match(corpus.ToString());
        if (matched.Count == 0) return;

        blocks.Add(new GuideBlock.Heading("Glossary"));
        foreach (var e in matched)
            blocks.Add(new GuideBlock.GlossaryItem(e.Term, e.Definition));
    }

    internal static string FormatRange(ParamDescriptor d)
    {
        string fmt = "F" + d.Decimals;
        string lo = d.Min.ToString(fmt, CultureInfo.InvariantCulture);
        string hi = d.Max.ToString(fmt, CultureInfo.InvariantCulture);
        string range = $"range {lo} .. {hi}";
        if (d.Step > 0)
            range += $", step {d.Step.ToString(fmt, CultureInfo.InvariantCulture)}";
        return range;
    }

    internal static string NoteFor(GuideContent content, string label)
    {
        if (content.SettingNotes.TryGetValue(label, out var note)) return note;
        if (SharedSettingNotes.TryGetValue(label, out var shared)) return shared;
        return "";
    }

    /// <summary>Notes for the cross-fractal groups (Palette, Reflections, Light,
    /// Camera). Written once, reused for every fractal.</summary>
    public static readonly IReadOnlyDictionary<string, string> SharedSettingNotes =
        new Dictionary<string, string>
        {
            ["Frequency"] = "How many color bands the cosine palette packs across the orbit-trap value. Higher = tighter stripes.",
            ["Trap scale"] = "Scales the orbit-trap distance before coloring. Shifts where bands land on the surface.",
            ["Phase R"] = "Red channel phase offset of the cosine palette. Rotates the red band position.",
            ["Phase G"] = "Green channel phase offset. Rotates the green band position.",
            ["Phase B"] = "Blue channel phase offset. Rotates the blue band position.",
            ["Base R"] = "Red midpoint of the palette (the color when the cosine is zero).",
            ["Base G"] = "Green midpoint of the palette.",
            ["Base B"] = "Blue midpoint of the palette.",
            ["Amp R"] = "Red swing around the base. Higher = more saturated red contrast.",
            ["Amp G"] = "Green swing around the base.",
            ["Amp B"] = "Blue swing around the base.",
            ["Mix origin"] = "Weight of the origin orbit-trap in the color (structural banding toward the center).",
            ["Mix axis"] = "Weight of the axis orbit-trap (banding along the fold axes).",
            ["Mix plane"] = "Weight of the plane orbit-trap (banding across cut planes).",
            ["Shell glaze"] = "Blends a thin bright shell over the surface for a glazed look.",
            ["Reflection bounces (0=off)"] = "Reflection bounce depth. 0 keeps the fast single-bounce look; 1-3 add mirror reflections (3 is hero-quality and slowest).",
            ["Gloss"] = "Overall reflection strength. 0 = matte, 1 = full gloss.",
            ["Fresnel F0 (0.05 ceramic … 0.8 metal)"] = "Base reflectivity. ~0.05 reads as ceramic/glass (edge-weighted); ~0.8 reads as metal (reflective face-on).",
            ["Light azimuth"] = "Horizontal angle of the key light, 0-360 degrees. Spins the light around the fractal.",
            ["Light elevation"] = "Vertical angle of the key light. +90 overhead, 0 on the horizon, -90 from below.",
            ["Light intensity"] = "Diffuse light strength. 1.0 is the standard look; 0 is flat ambient; >1 brightens.",
            ["Cam X"] = "Camera world X position. Usually set by flying, but keyframeable for animation.",
            ["Cam Y"] = "Camera world Y position.",
            ["Cam Z"] = "Camera world Z position.",
            ["Cam Yaw"] = "Camera heading (left/right look angle), radians.",
            ["Cam Pitch"] = "Camera tilt (up/down look angle), radians, clamped near +/-90 degrees.",
            ["Cam Roll"] = "Camera roll (bank angle), radians.",
        };
}
