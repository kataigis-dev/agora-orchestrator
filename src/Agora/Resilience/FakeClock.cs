namespace Agora.Resilience;

/// <summary>Deterministic clock for tests: never really waits; can force timeouts to fire.</summary>
public sealed class FakeClock : IClock
{
    private readonly bool _timeoutFires;

    /// <summary>Creates the clock; when <paramref name="timeoutFires"/> is true every timeout cancels
    /// immediately, simulating an expired deadline.</summary>
    public FakeClock(bool timeoutFires = false) => _timeoutFires = timeoutFires;

    /// <summary>Records the durations passed to <see cref="Delay"/> for test assertions.</summary>
    public List<TimeSpan> Delays { get; } = new();

    /// <inheritdoc />
    public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        Delays.Add(duration);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public CancellationTokenSource Timeout(TimeSpan duration)
    {
        var cts = new CancellationTokenSource();
        if (_timeoutFires)
            cts.Cancel();
        return cts;
    }
}
