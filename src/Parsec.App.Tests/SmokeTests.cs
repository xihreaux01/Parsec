using Parsec.App;
using Xunit;

namespace Parsec.App.Tests;

public class SmokeTests
{
    [Fact]
    public void StateClassExposesSchema()
    {
        var schema = new MandelboxState().BuildSchema();
        Assert.NotEmpty(schema.Parameters);
    }
}
