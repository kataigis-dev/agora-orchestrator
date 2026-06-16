using Agora.Agents;
using Xunit;

namespace Agora.Tests.Agents;

public class SignalParserTests
{
    [Fact]
    public void Extract_BareSignal_IsTrue_AndStripped()
    {
        var (output, signals) = SignalParser.Extract("done <<signal approved>>");
        Assert.Equal("done", output);
        Assert.True((bool)signals["approved"]);
    }

    [Fact]
    public void Extract_ValuedSignal_KeepsValue()
    {
        var (output, signals) = SignalParser.Extract("<<signal verdict=needs_revision>> fix it");
        Assert.Equal("needs_revision", signals["verdict"]);
        Assert.Equal("fix it", output);
    }

    [Fact]
    public void Extract_MultipleSignals_AllCaptured()
    {
        var (_, signals) = SignalParser.Extract("a <<signal done>> b <<signal score=7>>");
        Assert.True((bool)signals["done"]);
        Assert.Equal("7", signals["score"]);
        Assert.Equal(2, signals.Count);
    }

    [Fact]
    public void Extract_NoSignal_ReturnsOriginal_AndEmpty()
    {
        var (output, signals) = SignalParser.Extract("plain text");
        Assert.Equal("plain text", output);
        Assert.Empty(signals);
    }
}
