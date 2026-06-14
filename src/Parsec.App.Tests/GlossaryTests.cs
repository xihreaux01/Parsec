using System;
using System.Linq;
using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class GlossaryTests
{
    [Fact]
    public void Match_finds_whole_word_term()
    {
        var hits = FractalGlossary.Match("Parsec uses a distance estimator to step the ray.");
        Assert.Contains(hits, e => e.Term == "Distance estimator (DE)");
    }

    [Fact]
    public void Match_finds_abbreviation_alias_as_whole_word()
    {
        var hits = FractalGlossary.Match("Raise DE fudge toward 1.0 for speed.");
        Assert.Contains(hits, e => e.Term == "Distance estimator (DE)"); // via "DE"
        Assert.Contains(hits, e => e.Term == "DE fudge");                // via "DE fudge"
    }

    [Fact]
    public void Match_does_not_match_substrings_inside_words()
    {
        // "decided" and "ride" contain the letters "de" but not the whole word "DE".
        var hits = FractalGlossary.Match("I decided to ride along the side.");
        Assert.DoesNotContain(hits, e => e.Term == "Distance estimator (DE)");
    }

    [Fact]
    public void Match_empty_text_returns_empty()
    {
        Assert.Empty(FractalGlossary.Match(""));
    }

    [Fact]
    public void Match_dedups_entry_and_sorts_alphabetically()
    {
        var hits = FractalGlossary.Match("An orbit, an orbit trap, and another orbit-trap appear.");
        // "orbit trap" and "orbit-trap" are two tokens of one entry -> appears once.
        Assert.Single(hits, e => e.Term == "Orbit trap");
        // Sorted by Term, case-insensitive.
        var terms = hits.Select(e => e.Term).ToList();
        Assert.Equal(terms.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList(), terms);
    }

    [Fact]
    public void BuildDocument_appends_glossary_after_best_results()
    {
        var content = new GuideContent
        {
            Title = "T",
            WhatItIs = new[] { "It uses a distance estimator and an orbit trap." },
            HowComputed = new[] { "It marches a ray." },
            BestResults = new[] { "Have fun." },
        };
        var schema = new ParamSchema { Parameters = Array.Empty<ParamDescriptor>() };

        var doc = FractalGuide.BuildDocument(content, schema);

        var headings = doc.Blocks.OfType<GuideBlock.Heading>().Select(h => h.Text).ToList();
        Assert.Contains("Glossary", headings);

        int best = doc.Blocks.ToList().FindIndex(b => b is GuideBlock.Heading h && h.Text == "For best results");
        int gloss = doc.Blocks.ToList().FindIndex(b => b is GuideBlock.Heading h && h.Text == "Glossary");
        Assert.True(gloss > best, "Glossary section must come after For best results");

        var terms = doc.Blocks.OfType<GuideBlock.GlossaryItem>().Select(g => g.Term).ToList();
        Assert.Contains("Distance estimator (DE)", terms);
        Assert.Contains("Orbit trap", terms);
    }

    [Theory]
    [InlineData(FractalType.Mandelbulb)]
    [InlineData(FractalType.Mandelbox)]
    [InlineData(FractalType.Kleinian)]
    [InlineData(FractalType.Apollonian)]
    [InlineData(FractalType.Attractor)]
    public void Real_guides_include_a_glossary(FractalType type)
    {
        ParamSchema schema = type switch
        {
            FractalType.Mandelbulb => new MandelbulbState().BuildSchema(),
            FractalType.Mandelbox => new MandelboxState().BuildSchema(),
            FractalType.Kleinian => new KleinianState().BuildSchema(),
            FractalType.Apollonian => new ApollonianState().BuildSchema(),
            FractalType.Attractor => new AttractorState().BuildSchema(),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        var doc = FractalGuide.Build(type, 0, schema);
        Assert.NotEmpty(doc.Blocks.OfType<GuideBlock.GlossaryItem>());
    }

    [Fact]
    public void BuildDocument_omits_glossary_when_no_obscure_terms()
    {
        var content = new GuideContent
        {
            Title = "Plain",
            WhatItIs = new[] { "A smooth colorful surface." },
            HowComputed = new[] { "It runs a simple loop and stops." },
            BestResults = new[] { "Enjoy the view." },
        };
        var schema = new ParamSchema { Parameters = Array.Empty<ParamDescriptor>() };

        var doc = FractalGuide.BuildDocument(content, schema);

        Assert.DoesNotContain(doc.Blocks.OfType<GuideBlock.Heading>(), h => h.Text == "Glossary");
        Assert.Empty(doc.Blocks.OfType<GuideBlock.GlossaryItem>());
    }
}
