using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;

namespace Agora.Resilience;

/// <summary>
/// Retries a model call up to <c>spec.Retries</c> times on transient failure with exponential
/// backoff, and bounds each attempt by <c>spec.Timeout</c>. Genuine caller cancellation is not retried.
/// </summary>
public sealed class RetryPolicy
{
    private readonly IClock _clock;

    /// <summary>Creates the policy using the given clock for delays and timeouts.</summary>
    public RetryPolicy(IClock clock) => _clock = clock;

    /// <summary>Runs <paramref name="operation"/> with retries and per-attempt timeout from
    /// <paramref name="spec"/>; rethrows the last error if all attempts fail. Genuine caller
    /// cancellation propagates without retry.</summary>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        var maxAttempts = Math.Max(1, spec.Retries + 1);
        Exception lastError = new InvalidOperationException("retry policy executed no attempts");

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            using var timeoutCts = _clock.Timeout(TimeSpan.FromSeconds(spec.Timeout));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            try
            {
                return await operation(linked.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // genuine caller cancellation — do not retry
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                lastError = new TimeoutException($"model call exceeded {spec.Timeout}s timeout");
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            if (attempt < maxAttempts - 1)
                await _clock.Delay(TimeSpan.FromSeconds(spec.RetryBaseDelay * Math.Pow(2, attempt)), cancellationToken);
        }

        throw lastError;
    }
}
