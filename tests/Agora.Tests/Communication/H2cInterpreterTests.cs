using Agora.Communication;
using Xunit;

namespace Agora.Tests.Communication;

public class H2cInterpreterTests
{
    [Fact]
    public void Subtype_BecomesTrueSignal()
    {
        var (output, signals) = new H2cInterpreter().Interpret("[STATE:DONE]");
        Assert.True((bool)signals["done"]);
        Assert.Equal("[STATE:DONE]", output); // output preserved verbatim
    }

    [Fact]
    public void Fields_BecomeSignals()
    {
        var (_, signals) = new H2cInterpreter().Interpret("[TEST:FAIL]\nfailed:2|suite:unit");
        Assert.True((bool)signals["fail"]);
        Assert.Equal("2", signals["failed"]);
        Assert.Equal("unit", signals["suite"]);
    }

    [Fact]
    public void NoBlocks_EmptySignals_OutputUnchanged()
    {
        var (output, signals) = new H2cInterpreter().Interpret("nothing structured here");
        Assert.Equal("nothing structured here", output);
        Assert.Empty(signals);
    }
}
