using System.Collections.Generic;

namespace Parsec.App;

/// <summary>One renderable block of a fractal guide. The window maps each variant
/// to a styled, read-only text control.</summary>
public abstract record GuideBlock
{
    public sealed record Heading(string Text) : GuideBlock;
    public sealed record Paragraph(string Text) : GuideBlock;
    public sealed record SettingGroupHeading(string Group) : GuideBlock;
    public sealed record SettingDefinition(string Name, string Range, string Note) : GuideBlock;
    public sealed record GlossaryItem(string Term, string Definition) : GuideBlock;
}

/// <summary>Render-ready guide: a title plus an ordered list of blocks. Produced by
/// <see cref="FractalGuide.Build"/> from the live schema; consumed by GuideWindow.</summary>
public sealed record GuideDocument(string Title, IReadOnlyList<GuideBlock> Blocks);

/// <summary>Hand-written guide prose for one fractal (or one deep-zoom formula).
/// The settings list itself is auto-derived from the live schema; this only supplies
/// the per-setting NOTE text, keyed by the exact ParamDescriptor.Label.</summary>
public sealed record GuideContent
{
    public required string Title { get; init; }
    public required IReadOnlyList<string> WhatItIs { get; init; }
    public required IReadOnlyList<string> HowComputed { get; init; }
    public IReadOnlyList<string> Math { get; init; } = new List<string>();
    public required IReadOnlyList<string> BestResults { get; init; }
    public IReadOnlyDictionary<string, string> SettingNotes { get; init; }
        = new Dictionary<string, string>();
}
