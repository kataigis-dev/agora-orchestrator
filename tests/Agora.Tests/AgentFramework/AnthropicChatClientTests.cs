using System.Text.Json;
using Agora.AgentFramework.Providers;
using Microsoft.Extensions.AI;
using Xunit;

namespace Agora.Tests.AgentFramework;

public class AnthropicChatClientTests
{
    [Fact]
    public void BuildRequest_LiftsSystem_AndCarriesCacheControl()
    {
        var system = new ChatMessage(ChatRole.System, "stable instructions")
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["cache_control"] = new Dictionary<string, object> { ["type"] = "ephemeral" },
            },
        };
        var user = new ChatMessage(ChatRole.User, "hello");

        var json = AnthropicChatClient.BuildRequestJson(
            new[] { system, user }, new ChatOptions { Temperature = 0f, MaxOutputTokens = 256 }, "claude-test");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("claude-test", root.GetProperty("model").GetString());
        Assert.Equal(256, root.GetProperty("max_tokens").GetInt32());

        // System is lifted out of messages and carries the cache breakpoint.
        var sysBlock = root.GetProperty("system")[0];
        Assert.Equal("stable instructions", sysBlock.GetProperty("text").GetString());
        Assert.Equal("ephemeral", sysBlock.GetProperty("cache_control").GetProperty("type").GetString());

        // The conversation holds only the user turn, with no cache_control.
        var messages = root.GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.False(messages[0].GetProperty("content")[0].TryGetProperty("cache_control", out _));
    }

    [Fact]
    public void ParseResponse_MapsTextAndCacheTokens()
    {
        const string body = """
            {
              "model": "claude-test",
              "content": [ { "type": "text", "text": "the answer" } ],
              "usage": {
                "input_tokens": 10,
                "output_tokens": 7,
                "cache_creation_input_tokens": 3,
                "cache_read_input_tokens": 100
              }
            }
            """;

        var response = AnthropicChatClient.ParseResponse(body, "fallback");

        Assert.Equal("the answer", response.Text);
        Assert.NotNull(response.Usage);
        // InputTokenCount is the total prompt processed (fresh + cache read + cache write) so cache-read
        // stays a subset, matching the OpenAI cache semantics.
        Assert.Equal(113, response.Usage!.InputTokenCount);
        Assert.Equal(7, response.Usage.OutputTokenCount);
        Assert.Equal(100, response.Usage.CachedInputTokenCount);

        // The mapped usage flows through the Anthropic cache adapter into CacheRead/CacheWrite (RunMetrics.CacheHitRate).
        var mapped = new AnthropicCacheAdapter().MapUsage(response.Usage);
        Assert.Equal(113, mapped.Input);
        Assert.Equal(100, mapped.CacheRead);
        Assert.Equal(3, mapped.CacheWrite);
        Assert.True((double)mapped.CacheRead / mapped.Input > 0);   // CacheHitRate > 0
    }

    [Fact]
    public void ParseResponse_NoUsage_IsSafe()
    {
        const string body = """{ "content": [ { "type": "text", "text": "hi" } ] }""";
        var response = AnthropicChatClient.ParseResponse(body, "fallback");
        Assert.Equal("hi", response.Text);
        Assert.Equal("fallback", response.ModelId);
    }
}
