using Agora.Agents;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Providers;

public class PromptCachingTests
{
    private static ModelSpec Spec(string provider) => new()
    {
        Alias = "m", Provider = provider, Model = "x", Temperature = 0, MaxTokens = 50, Timeout = 5, Retries = 0,
    };

    [Theory]
    [InlineData("anthropic", CachingMode.Breakpoint)]
    [InlineData("openai", CachingMode.Implicit)]
    [InlineData("ollama", CachingMode.Implicit)]
    [InlineData("gemini", CachingMode.Resource)]
    [InlineData("some-unknown-endpoint", CachingMode.Implicit)]
    public void For_MapsProviderToMechanism(string provider, CachingMode expected)
        => Assert.Equal(expected, PromptCaching.For(provider));

    [Fact]
    public async Task Agent_MarksSystemPromptAsCacheable_NotTheUserTurn()
    {
        var provider = new FakeChatProvider(new[] { "ok" });
        var agent = new Agent(new AgentCard { Id = "a", Model = "m", Role = "You are stable." }, provider, Spec("anthropic"));

        await agent.RunAsync("hello");

        var messages = provider.Calls[0].Messages;
        Assert.True(messages[0].CacheStable);   // system prompt → cacheable prefix
        Assert.False(messages[^1].CacheStable);  // user turn → volatile
    }

    [Fact]
    public async Task Agent_PropagatesCacheReadTokens()
    {
        var provider = new FakeChatProvider(new[] { "ok" });
        var agent = new Agent(new AgentCard { Id = "a", Model = "m", Role = "You are stable." }, provider, Spec("anthropic"));

        var result = await agent.RunAsync("hello");

        // FakeChatProvider reports a cache hit whenever a cacheable message is present.
        Assert.Equal(7, result.CacheReadTokens);
        Assert.Equal(10, result.InputTokens);
    }
}
