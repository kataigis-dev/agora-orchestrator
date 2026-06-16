using Agora.Providers;
using Agora.Resilience;
using Xunit;

namespace Agora.Tests.Providers;

public class ResilientChatProviderTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "a", Provider = "openai", Model = "m",
        Temperature = 0.2, MaxTokens = 100, Timeout = 30, Retries = 2, RetryBaseDelay = 0.5,
    };

    [Fact]
    public async Task RetriesInnerProvider_UntilSuccess()
    {
        var flaky = new FlakyChatProvider(failures: 2, text: "done");
        var provider = new ResilientChatProvider(flaky, new FakeClock());
        var result = await provider.CompleteAsync(new[] { new ChatMessage("user", "hi") }, Spec());
        Assert.Equal("done", result.Text);
        Assert.Equal(3, flaky.Calls);
    }

    [Fact]
    public async Task PassesThrough_OnFirstSuccess()
    {
        var flaky = new FlakyChatProvider(failures: 0, text: "x");
        var provider = new ResilientChatProvider(flaky, new FakeClock());
        var result = await provider.CompleteAsync(new[] { new ChatMessage("user", "hi") }, Spec());
        Assert.Equal("x", result.Text);
        Assert.Equal(1, flaky.Calls);
    }
}
