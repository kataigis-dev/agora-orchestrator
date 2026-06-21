namespace Agora.Providers;

/// <summary>Deterministic in-memory provider for tests and offline runs.</summary>
public sealed class FakeChatProvider : IStreamingChatProvider
{
    private readonly Queue<string> _responses;
    private readonly string _default;

    /// <summary>Records every call's messages and spec for test assertions.</summary>
    public List<(IReadOnlyList<ChatMessage> Messages, ModelSpec Spec)> Calls { get; } = new();

    /// <summary>Creates the provider with a queue of scripted responses and a fallback default.</summary>
    public FakeChatProvider(IEnumerable<string>? responses = null, string @default = "OK")
    {
        _responses = new Queue<string>(responses ?? Enumerable.Empty<string>());
        _default = @default;
    }

    /// <summary>Returns the next scripted response (or the default), recording the call.</summary>
    public Task<CompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        Calls.Add((messages, spec));
        var text = _responses.Count > 0 ? _responses.Dequeue() : _default;
        // Simulate a cache hit on the stable (cacheable) prefix so caching can be exercised offline.
        var cacheRead = messages.Any(m => m.CacheStable) ? 7 : 0;
        return Task.FromResult(new CompletionResult
        {
            Text = text, InputTokens = 10, OutputTokens = 5, CacheReadTokens = cacheRead, Model = spec.Model,
        });
    }

    /// <summary>Completes, then replays the response word-by-word through <paramref name="onChunk"/>.</summary>
    public async Task<CompletionResult> StreamAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        var result = await CompleteAsync(messages, spec, cancellationToken);
        // Emit the response word-by-word so streaming consumers can be exercised deterministically.
        var words = result.Text.Split(' ');
        for (var i = 0; i < words.Length; i++)
            onChunk(i == 0 ? words[i] : " " + words[i]);
        return result;
    }
}
