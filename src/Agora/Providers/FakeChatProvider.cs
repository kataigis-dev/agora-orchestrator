namespace Agora.Providers;

/// <summary>Deterministic in-memory provider for tests and offline runs.</summary>
public sealed class FakeChatProvider : IChatProvider
{
    private readonly Queue<string> _responses;
    private readonly string _default;

    public List<(IReadOnlyList<ChatMessage> Messages, ModelSpec Spec)> Calls { get; } = new();

    public FakeChatProvider(IEnumerable<string>? responses = null, string @default = "OK")
    {
        _responses = new Queue<string>(responses ?? Enumerable.Empty<string>());
        _default = @default;
    }

    public Task<CompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        Calls.Add((messages, spec));
        var text = _responses.Count > 0 ? _responses.Dequeue() : _default;
        return Task.FromResult(new CompletionResult
        {
            Text = text, InputTokens = 10, OutputTokens = 5, Model = spec.Model,
        });
    }
}
