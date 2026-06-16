namespace Agora.Resilience;

/// <summary>Deterministic clock for tests: never really waits; can force timeouts to fire.</summary>
public sealed class FakeClock : IClock
{
    private readonly bool _timeoutFires;

    public FakeClock(bool timeoutFires = false) => _timeoutFires = timeoutFires;

    public List<TimeSpan> Delays { get; } = new();

    public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        Delays.Add(duration);
        return Task.CompletedTask;
    }

    public CancellationTokenSource Timeout(TimeSpan duration)
    {
        var cts = new CancellationTokenSource();
        if (_timeoutFires)
            cts.Cancel();
        return cts;
    }
}
