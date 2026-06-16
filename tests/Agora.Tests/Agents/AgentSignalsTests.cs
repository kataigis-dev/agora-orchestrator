using Agora.Agents;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Agents;

public class AgentSignalsTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "fast", Provider = "anthropic", Model = "x",
        Temperature = 0.2, MaxTokens = 100, Timeout = 10, Retries = 0,
    };

    private static Agent MakeAgent(string response) =>
        new(new AgentCard { Id = "critic", Model = "fast" }, new FakeChatProvider(new[] { response }), Spec());

    [Fact]
    public async Task BareSignal_ExtractedAsTrue_AndStripped()
    {
        var result = await MakeAgent("Looks good. <<signal approved>>").RunAsync("review");
        Assert.Equal("Looks good.", result.Output);
        Assert.True((bool)result.Signals["approved"]);
        Assert.Single(result.Signals);
    }

    [Fact]
    public async Task SignalWithValue_Extracted()
    {
        var result = await MakeAgent("<<signal verdict=needs_revision>> please fix").RunAsync("review");
        Assert.Equal("needs_revision", result.Signals["verdict"]);
        Assert.Equal("please fix", result.Output);
    }

    [Fact]
    public async Task NoSignal_LeavesOutputUnchanged_WithEmptySignals()
    {
        var result = await MakeAgent("just a plain answer").RunAsync("q");
        Assert.Equal("just a plain answer", result.Output);
        Assert.Empty(result.Signals);
    }
}
