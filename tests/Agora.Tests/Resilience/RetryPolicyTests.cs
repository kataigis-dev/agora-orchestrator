using Agora.Providers;
using Agora.Resilience;
using Xunit;

namespace Agora.Tests.Resilience;

public class RetryPolicyTests
{
    private static ModelSpec Spec(int retries, double baseDelay = 0.5, double timeout = 30) => new()
    {
        Alias = "a", Provider = "openai", Model = "m",
        Temperature = 0.2, MaxTokens = 100, Timeout = timeout, Retries = retries, RetryBaseDelay = baseDelay,
    };

    [Fact]
    public async Task SucceedsFirstTry_NoDelays()
    {
        var clock = new FakeClock();
        var calls = 0;
        var result = await new RetryPolicy(clock).ExecuteAsync(_ => { calls++; return Task.FromResult("ok"); }, Spec(2));
        Assert.Equal("ok", result);
        Assert.Equal(1, calls);
        Assert.Empty(clock.Delays);
    }

    [Fact]
    public async Task RetriesThenSucceeds_WithExponentialBackoff()
    {
        var clock = new FakeClock();
        var calls = 0;
        var result = await new RetryPolicy(clock).ExecuteAsync(_ =>
        {
            calls++;
            if (calls <= 2) throw new InvalidOperationException("transient");
            return Task.FromResult("ok");
        }, Spec(retries: 2, baseDelay: 0.5));
        Assert.Equal("ok", result);
        Assert.Equal(3, calls);
        Assert.Equal(new[] { TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1.0) }, clock.Delays);
    }

    [Fact]
    public async Task ExhaustsRetries_ThrowsLastError()
    {
        var clock = new FakeClock();
        var calls = 0;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RetryPolicy(clock).ExecuteAsync<string>(_ => { calls++; throw new InvalidOperationException("boom"); }, Spec(retries: 1)));
        Assert.Equal("boom", ex.Message);
        Assert.Equal(2, calls);
        Assert.Equal(new[] { TimeSpan.FromSeconds(0.5) }, clock.Delays);
    }

    [Fact]
    public async Task Timeout_ThrowsTimeoutException()
    {
        var clock = new FakeClock(timeoutFires: true);
        await Assert.ThrowsAsync<TimeoutException>(() =>
            new RetryPolicy(clock).ExecuteAsync(ct =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult("never");
            }, Spec(retries: 0)));
    }

    [Fact]
    public async Task CallerCancellation_NotRetried()
    {
        var clock = new FakeClock();
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var calls = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new RetryPolicy(clock).ExecuteAsync(ct => { calls++; ct.ThrowIfCancellationRequested(); return Task.FromResult("x"); },
                Spec(retries: 3), caller.Token));
        Assert.Equal(1, calls);
        Assert.Empty(clock.Delays);
    }
}
