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

    [Fact]
    public void Thomas_damping_default_is_chaotic_and_capped()
    {
        var fresh = new AttractorState();
        Assert.Equal(0.19f, fresh.B);
        var mutated = new AttractorState { B = 0.30f };
        mutated.Reset();
        Assert.Equal(0.19f, mutated.B);
        Assert.Equal(0.30, Desc(fresh.BuildSchema(), "Damping b").Max, 3);
    }

    [Fact]
    public void Menger_offset_z_default_is_canonical_sponge()
    {
        var fresh = new MengerState();
        Assert.Equal(1.0f, fresh.OffsetZ);
        var mutated = new MengerState { OffsetZ = 0.0f };
        mutated.Reset();
        Assert.Equal(1.0f, mutated.OffsetZ);
    }

    [Fact]
    public void Mandelbulb_defaults_bumped()
    {
        var fresh = new MandelbulbState();
        Assert.Equal(10, fresh.Iterations);
        Assert.Equal(8.0f, fresh.Bailout);
        var m = new MandelbulbState { Iterations = 4, Bailout = 4.0f };
        m.Reset();
        Assert.Equal(10, m.Iterations);
        Assert.Equal(8.0f, m.Bailout);
    }

    [Fact]
    public void Iteration_defaults_bumped()
    {
        Assert.Equal(12, new QuaternionJuliaState().Iterations);
        Assert.Equal(10, new QJBoxState().Iterations);
        Assert.Equal(24, new BiomorphState().Iterations);
    }
}
