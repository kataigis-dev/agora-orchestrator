using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Xunit;

namespace Agora.Tests.Agents;

public class OutputInterpreterTests
{
    [Fact]
    public void SignalInterpreter_ExtractsBareSignal_AndStrips()
    {
        var (output, signals, artifacts) = new SignalInterpreter().Interpret("Looks good. <<signal approved>>");
        Assert.Equal("Looks good.", output);
        Assert.True((bool)signals["approved"]);
        Assert.Empty(artifacts);
    }

    [Fact]
    public void SignalInterpreter_NoSignal_PassesThrough()
    {
        var (output, signals, artifacts) = new SignalInterpreter().Interpret("plain answer");
        Assert.Equal("plain answer", output);
        Assert.Empty(signals);
        Assert.Empty(artifacts);
    }
}
