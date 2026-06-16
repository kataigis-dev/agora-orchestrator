namespace Agora.Resilience;

/// <summary>Time abstraction so retry backoff and timeouts are deterministic under test.</summary>
public interface IClock
{
    Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>A cancellation source that cancels after <paramref name="duration"/> (caller disposes).</summary>
    CancellationTokenSource Timeout(TimeSpan duration);
}

public sealed class SystemClock : IClock
{
    public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
        => duration <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(duration, cancellationToken);

    public CancellationTokenSource Timeout(TimeSpan duration)
        => new(duration <= TimeSpan.Zero ? System.Threading.Timeout.InfiniteTimeSpan : duration);
}
