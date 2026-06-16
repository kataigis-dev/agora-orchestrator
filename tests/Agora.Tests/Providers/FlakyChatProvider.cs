using Agora.Providers;

namespace Agora.Tests.Providers;

/// <summary>Throws a transient error the first N calls, then returns a fixed completion.</summary>
internal sealed class FlakyChatProvider : IChatProvider
{
    private readonly int _failures;
    private readonly string _text;

    public FlakyChatProvider(int failures, string text = "ok") => (_failures, _text) = (failures, text);

    public int Calls { get; private set; }

    public Task<CompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        Calls++;
        if (Calls <= _failures)
            throw new InvalidOperationException("transient");
        return Task.FromResult(new CompletionResult { Text = _text, Model = spec.Model });
    }
}
