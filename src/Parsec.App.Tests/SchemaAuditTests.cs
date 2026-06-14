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
}
