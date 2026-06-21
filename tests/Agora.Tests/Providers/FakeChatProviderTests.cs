using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests.Providers;

public class FakeChatProviderTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "fast", Provider = "anthropic", Model = "claude-haiku-4-5",
        Temperature = 0.2, MaxTokens = 10, Timeout = 5, Retries = 0,
    };

    [Fact]
    public async Task ReturnsScriptedResponsesInOrder_AndRecordsCalls()
    {
        var provider = new FakeChatProvider(new[] { "first", "second" });
        var r1 = await provider.CompleteAsync(new[] { new ChatMessage("user", "a") }, Spec());
        var r2 = await provider.CompleteAsync(new[] { new ChatMessage("user", "b") }, Spec());
        Assert.Equal("first", r1.Text);
        Assert.Equal("second", r2.Text);
        Assert.Equal(2, provider.Calls.Count);
        Assert.Equal("fast", provider.Calls[0].Spec.Alias);
    }

    [Fact]
    public async Task FallsBackToDefault()
    {
        var provider = new FakeChatProvider(@default: "DEFAULT");
        var r = await provider.CompleteAsync(new[] { new ChatMessage("user", "a") }, Spec());
        Assert.Equal("DEFAULT", r.Text);
    }
}
