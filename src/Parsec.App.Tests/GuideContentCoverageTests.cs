using System;
using System.Collections.Generic;
using System.Linq;
using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class GuideContentCoverageTests
{
    private static readonly (FractalType Type, Func<ParamSchema> Schema)[] Fractals =
    {
        (FractalType.AmazingBox, () => new AmazingBoxState().BuildSchema()),
        (FractalType.Mandelbox, () => new MandelboxState().BuildSchema()),
        (FractalType.Kifs, () => new KifsState().BuildSchema()),
        (FractalType.Kleinian, () => new KleinianState().BuildSchema()),
        (FractalType.Attractor, () => new AttractorState().BuildSchema()),
        (FractalType.Mandelbulb, () => new MandelbulbState().BuildSchema()),
        (FractalType.QuaternionJulia, () => new QuaternionJuliaState().BuildSchema()),
        (FractalType.RotBox, () => new RotBoxState().BuildSchema()),
        (FractalType.Hybrid, () => new HybridState().BuildSchema()),
        (FractalType.QJBox, () => new QJBoxState().BuildSchema()),
        (FractalType.Menger, () => new MengerState().BuildSchema()),
        (FractalType.Bicomplex, () => new BicomplexState().BuildSchema()),
        (FractalType.Apollonian, () => new ApollonianState().BuildSchema()),
        (FractalType.Phoenix, () => new PhoenixState().BuildSchema()),
        (FractalType.Biomorph, () => new BiomorphState().BuildSchema()),
        (FractalType.Mosely, () => new MoselyState().BuildSchema()),
        (FractalType.PseudoKleinian4D, () => new PseudoKleinian4DState().BuildSchema()),
        (FractalType.RiemannSphere, () => new RiemannSphereState().BuildSchema()),
        (FractalType.Mandalay, () => new MandalayState().BuildSchema()),
        (FractalType.Anisotropic, () => new AnisotropicState().BuildSchema()),
        (FractalType.OrbitHybrid, () => new OrbitHybridState().BuildSchema()),
        (FractalType.BurningShip, () => new BurningShipState().BuildSchema()),
    };

    public static IEnumerable<object[]> FractalCases() => Fractals.Select(f => new object[] { f.Type });

    [Theory]
    [MemberData(nameof(FractalCases))]
    public void Every_fractal_has_nonempty_prose(FractalType type)
    {
        var c = FractalGuide.Resolve(type, 0);
        Assert.False(string.IsNullOrWhiteSpace(c.Title));
        Assert.NotEmpty(c.WhatItIs);
        Assert.All(c.WhatItIs, s => Assert.False(string.IsNullOrWhiteSpace(s)));
        Assert.NotEmpty(c.HowComputed);
        Assert.All(c.HowComputed, s => Assert.False(string.IsNullOrWhiteSpace(s)));
        Assert.NotEmpty(c.BestResults);
        Assert.All(c.BestResults, s => Assert.False(string.IsNullOrWhiteSpace(s)));
    }

    [Theory]
    [MemberData(nameof(FractalCases))]
    public void Every_fractal_specific_setting_has_a_note(FractalType type)
    {
        var schema = Fractals.Single(f => f.Type == type).Schema();
        var content = FractalGuide.Resolve(type, 0);
        foreach (var d in schema.Parameters)
            Assert.True(content.SettingNotes.ContainsKey(d.Label),
                $"{type}: missing note for setting '{d.Label}'");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Every_deep_formula_has_distinct_nonempty_prose(int formula)
    {
        var c = FractalGuide.Resolve(FractalType.DeepZoom, formula);
        Assert.False(string.IsNullOrWhiteSpace(c.Title));
        Assert.NotEmpty(c.WhatItIs);
        Assert.NotEmpty(c.BestResults);
    }

    [Fact]
    public void Deep_formulas_have_different_titles()
    {
        var titles = Enumerable.Range(0, 4)
            .Select(f => FractalGuide.Resolve(FractalType.DeepZoom, f).Title).ToList();
        Assert.Equal(4, titles.Distinct().Count());
    }
}
