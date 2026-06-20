namespace Agora.Providers;

/// <summary>
/// Optional capability: a chat provider that can stream the response token-by-token. Each chunk
/// is delivered to <paramref name="onChunk"/> as it arrives; the full <see cref="CompletionResult"/>
/// is still returned at the end (so signal/artifact parsing is unchanged).
/// </summary>
public interface IStreamingChatProvider : IChatProvider
{
    Task<CompletionResult> StreamAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default);
}
