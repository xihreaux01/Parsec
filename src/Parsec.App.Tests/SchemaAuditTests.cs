using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class SchemaAuditTests
{
    private static ParamDescriptor Desc(ParamSchema s, string label)
        => s.Parameters.Single(p => p.Label == label);

    [Fact]
    public void QuaternionJulia_Reset_restores_stereo_params()
    {
        var s = new QuaternionJuliaState { StereoK = 2.5f, StereoR = 1.4f };
        s.Reset();
        Assert.Equal(1.0f, s.StereoK);
        Assert.Equal(0.8f, s.StereoR);
    }

    [Fact]
    public void Mosely_Reset_restores_wedge_and_fudge()
    {
        var s = new MoselyState { WedgeDeg = 120f, Fudge = 0.5f };
        s.Reset();
        Assert.Equal(360f, s.WedgeDeg);
        Assert.Equal(0.9f, s.Fudge);
    }

    [Fact]
    public void OrbitHybrid_bound_radius_max_reaches_default()
    {
        var s = new OrbitHybridState();
        var d = Desc(s.BuildSchema(), "Bound radius");
        Assert.True(d.Max >= 16.0, $"Max {d.Max} must reach default 16.0");
    }

    [Fact]
    public void Mandelbox_scale_max_allows_cityscape()
    {
        Assert.Equal(3.0, Desc(new MandelboxState().BuildSchema(), "Scale").Max, 3);
    }

    [Fact]
    public void Mandalay_scale_range_is_negative_only()
    {
        var d = Desc(new MandalayState().BuildSchema(), "Scale");
        Assert.Equal(-3.0, d.Min, 3);
        Assert.Equal(-0.5, d.Max, 3);
    }

    [Theory]
    [InlineData("Post-rot X")]
    [InlineData("Pre-rot X")]
    public void Kifs_rotation_range_is_plus_minus_90(string label)
    {
        var d = Desc(new KifsState().BuildSchema(), label);
        Assert.Equal(-90.0, d.Min, 3);
        Assert.Equal(90.0, d.Max, 3);
    }

    [Fact]
    public void Kleinian_and_pk4d_de_fudge_capped_at_one()
    {
        Assert.Equal(1.0, Desc(new KleinianState().BuildSchema(), "DE fudge").Max, 3);
        Assert.Equal(1.0, Desc(new PseudoKleinian4DState().BuildSchema(), "DE fudge").Max, 3);
    }

    [Fact]
    public void Apollonian_outer_radius_min_avoids_clipping()
    {
        Assert.Equal(0.95, Desc(new ApollonianState().BuildSchema(), "Outer radius x").Min, 3);
    }
}
