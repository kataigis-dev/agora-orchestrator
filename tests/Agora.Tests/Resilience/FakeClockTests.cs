using Agora.Resilience;
using Xunit;

namespace Agora.Tests.Resilience;

public class FakeClockTests
{
    [Fact]
    public async Task Delay_RecordsAndReturnsImmediately()
    {
        var clock = new FakeClock();
        await clock.Delay(TimeSpan.FromSeconds(3));
        await clock.Delay(TimeSpan.FromSeconds(1));
        Assert.Equal(new[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1) }, clock.Delays);
    }

    [Fact]
    public void Timeout_WhenFiring_ReturnsAlreadyCancelledSource()
    {
        using var cts = new FakeClock(timeoutFires: true).Timeout(TimeSpan.FromSeconds(5));
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public void Timeout_WhenNotFiring_ReturnsLiveSource()
    {
        using var cts = new FakeClock().Timeout(TimeSpan.FromSeconds(5));
        Assert.False(cts.IsCancellationRequested);
    }
}
