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

    /// <summary>Pure: turn explicit content + a schema into render-ready blocks.
    /// No Avalonia, no GL, so this is the unit-tested seam.</summary>
    public static GuideDocument BuildDocument(GuideContent content, ParamSchema schema)
    {
        var blocks = new List<GuideBlock>();

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
