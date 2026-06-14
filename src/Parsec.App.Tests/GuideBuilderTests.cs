using System.Collections.Generic;
using System.Linq;
using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class GuideBuilderTests
{
    private static ParamSchema TwoGroupSchema() => new()
    {
        Parameters = new[]
        {
            new ParamDescriptor { Label = "Scale", Group = "Fold", Min = -3.0, Max = 3.0,
                Decimals = 2, Get = () => 0, Set = _ => { } },
            new ParamDescriptor { Label = "Iterations", Group = "Quality", Min = 4, Max = 500,
                Step = 1, Decimals = 0, Get = () => 0, Set = _ => { } },
            new ParamDescriptor { Label = "Gloss", Group = "Reflections", Min = 0, Max = 1,
                Decimals = 2, Get = () => 0, Set = _ => { } },
        },
    };

    private static GuideContent Content() => new()
    {
        Title = "Test Fractal",
        WhatItIs = new[] { "It is a test." },
        HowComputed = new[] { "Folded in a loop." },
        BestResults = new[] { "Turn the knobs." },
        SettingNotes = new Dictionary<string, string> { ["Scale"] = "Overall fold scale." },
    };

    [Fact]
    public void Document_title_comes_from_content()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        Assert.Equal("Test Fractal", doc.Title);
    }

    [Fact]
    public void Primer_is_first_section_of_every_guide()
    {
        Assert.NotEmpty(FractalGuide.Primer);
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        var firstHeading = doc.Blocks.OfType<GuideBlock.Heading>().First();
        Assert.Equal("How these fractals are made (start here)", firstHeading.Text);
        Assert.Contains(doc.Blocks.OfType<GuideBlock.Heading>(), h => h.Text == "What this fractal is");
    }

    [Fact]
    public void Settings_emitted_in_schema_group_order_with_group_headings()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        var groups = doc.Blocks.OfType<GuideBlock.SettingGroupHeading>().Select(g => g.Group).ToList();
        Assert.Equal(new[] { "Fold", "Quality", "Reflections" }, groups);
        Assert.Equal(3, doc.Blocks.OfType<GuideBlock.SettingDefinition>().Count());
    }

    [Fact]
    public void Range_string_uses_decimals_and_step()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        var defs = doc.Blocks.OfType<GuideBlock.SettingDefinition>().ToDictionary(d => d.Name);
        Assert.Equal("range -3.00 .. 3.00", defs["Scale"].Range);
        Assert.Equal("range 4 .. 500, step 1", defs["Iterations"].Range);
    }

    [Fact]
    public void Note_lookup_prefers_content_then_shared_then_empty()
    {
        var doc = FractalGuide.BuildDocument(Content(), TwoGroupSchema());
        var defs = doc.Blocks.OfType<GuideBlock.SettingDefinition>().ToDictionary(d => d.Name);
        Assert.Equal("Overall fold scale.", defs["Scale"].Note);          // content-specific
        Assert.NotEqual("", defs["Gloss"].Note);                           // shared table
        Assert.Equal("", defs["Iterations"].Note);                        // graceful fallback
    }
}
