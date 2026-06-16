using Agora.Resilience;

namespace Agora.Providers;

/// <summary>Wraps an inner <see cref="IChatProvider"/> with retry + timeout via <see cref="RetryPolicy"/>.</summary>
public sealed class ResilientChatProvider : IChatProvider
{
    private readonly IChatProvider _inner;
    private readonly RetryPolicy _retry;

    public ResilientChatProvider(IChatProvider inner, IClock? clock = null)
    {
        _inner = inner;
        _retry = new RetryPolicy(clock ?? new SystemClock());
    }

    public Task<CompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
        => _retry.ExecuteAsync(token => _inner.CompleteAsync(messages, spec, token), spec, cancellationToken);
}
