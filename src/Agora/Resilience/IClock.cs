namespace Agora.Resilience;

/// <summary>Time abstraction so retry backoff and timeouts are deterministic under test.</summary>
public interface IClock
{
    /// <summary>Asynchronously waits for the given duration.</summary>
    Task Delay(TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>A cancellation source that cancels after <paramref name="duration"/> (caller disposes).</summary>
    CancellationTokenSource Timeout(TimeSpan duration);
}

/// <summary>Real wall-clock implementation of <see cref="IClock"/>.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public Task Delay(TimeSpan duration, CancellationToken cancellationToken = default)
        => duration <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(duration, cancellationToken);

    /// <inheritdoc />
    public CancellationTokenSource Timeout(TimeSpan duration)
        => new(duration <= TimeSpan.Zero ? System.Threading.Timeout.InfiniteTimeSpan : duration);
}
