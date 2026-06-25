using Agora.AgentFramework.Providers;
using Agora.Providers.Models;
using Microsoft.Extensions.AI;
using Xunit;

namespace Agora.Tests.AgentFramework;

public class CacheAdaptersTests
{
    private static ModelSpec Spec(string provider) => new() { Alias = "a", Provider = provider, Model = "m" };

    [Theory]
    [InlineData("anthropic")]
    [InlineData("Claude")]            // case-insensitive
    public void For_BreakpointProviders_ResolveAnthropicAdapter(string provider)
        => Assert.IsType<AnthropicCacheAdapter>(CacheAdapters.For(Spec(provider)));

    [Theory]
    [InlineData("openai")]
    [InlineData("ollama")]
    [InlineData("github-copilot")]
    [InlineData("some-unknown-endpoint")]
    public void For_OtherProviders_ResolveImplicitAdapter(string provider)
        => Assert.IsType<ImplicitCacheAdapter>(CacheAdapters.For(Spec(provider)));

    [Fact]
    public void AnthropicAdapter_Mark_TagsCacheControl()
    {
        var message = new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, "stable");
        new AnthropicCacheAdapter().Mark(message);
        Assert.True(message.AdditionalProperties?.ContainsKey("cache_control"));
    }

    [Fact]
    public void ImplicitAdapter_Mark_IsNoOp()
    {
        var message = new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, "stable");
        new ImplicitCacheAdapter().Mark(message);
        Assert.True(message.AdditionalProperties is null || !message.AdditionalProperties.ContainsKey("cache_control"));
    }

    [Fact]
    public void AnthropicAdapter_MapUsage_ReadsCacheReadAndWrite()
    {
        var usage = new UsageDetails
        {
            InputTokenCount = 113,
            OutputTokenCount = 7,
            CachedInputTokenCount = 100,
            AdditionalCounts = new AdditionalPropertiesDictionary<long> { ["cache_creation_input_tokens"] = 3 },
        };

        var mapped = new AnthropicCacheAdapter().MapUsage(usage);

        Assert.Equal((113, 7, 100, 3), mapped);
    }

    [Fact]
    public void ImplicitAdapter_MapUsage_IgnoresCacheWriteKey()
    {
        var usage = new UsageDetails
        {
            InputTokenCount = 50,
            OutputTokenCount = 20,
            CachedInputTokenCount = 10,
            // Even if a write key is present, the implicit mechanism reports no separate cache-write count.
            AdditionalCounts = new AdditionalPropertiesDictionary<long> { ["cache_creation_input_tokens"] = 9 },
        };

        var mapped = new ImplicitCacheAdapter().MapUsage(usage);

        Assert.Equal((50, 20, 10, 0), mapped);
    }

    [Fact]
    public void MapUsage_NullUsage_IsZero()
    {
        Assert.Equal((0, 0, 0, 0), new AnthropicCacheAdapter().MapUsage(null));
        Assert.Equal((0, 0, 0, 0), new ImplicitCacheAdapter().MapUsage(null));
    }
}
