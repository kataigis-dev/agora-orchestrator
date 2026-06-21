using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Xunit;

namespace Agora.Tests.Agents;

public class SignalParserTests
{
    [Fact]
    public void Extract_BareSignal_IsTrue_AndStripped()
    {
        var (output, signals, artifacts) = SignalParser.Extract("done <<signal approved>>");
        Assert.Equal("done", output);
        Assert.True((bool)signals["approved"]);
        Assert.Empty(artifacts);
    }

    [Fact]
    public void Extract_ValuedSignal_KeepsValue()
    {
        var (output, signals, artifacts) = SignalParser.Extract("<<signal verdict=needs_revision>> fix it");
        Assert.Equal("needs_revision", signals["verdict"]);
        Assert.Equal("fix it", output);
        Assert.Empty(artifacts);
    }

    [Fact]
    public void Extract_MultipleSignals_AllCaptured()
    {
        var (_, signals, artifacts) = SignalParser.Extract("a <<signal done>> b <<signal score=7>>");
        Assert.True((bool)signals["done"]);
        Assert.Equal("7", signals["score"]);
        Assert.Equal(2, signals.Count);
        Assert.Empty(artifacts);
    }

    [Fact]
    public void Extract_NoSignal_ReturnsOriginal_AndEmpty()
    {
        var (output, signals, artifacts) = SignalParser.Extract("plain text");
        Assert.Equal("plain text", output);
        Assert.Empty(signals);
        Assert.Empty(artifacts);
    }

    [Fact]
    public void Extract_Artifact_ReturnsValue_AndStripped()
    {
        var (output, signals, artifacts) = SignalParser.Extract("the result is <<artifact language=C#>>");
        Assert.Equal("the result is", output);
        Assert.Empty(signals);
        Assert.Equal("C#", artifacts["language"]);
    }

    [Fact]
    public void Extract_MultipleArtifacts_AllCaptured()
    {
        var (output, signals, artifacts) = SignalParser.Extract(
            "<<artifact lang=C#>> <<artifact framework=net10>> done <<signal done>>");
        Assert.Equal("done", output);
        Assert.True((bool)signals["done"]);
        Assert.Equal("C#", artifacts["lang"]);
        Assert.Equal("net10", artifacts["framework"]);
    }
}
