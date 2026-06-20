using Agora.Providers;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>Chat provider backed by Microsoft.Extensions.AI, dispatching to OpenAI-compatible
/// endpoints or Ollama based on the model spec. Clients are reused via a <see cref="ChatClientCache"/>
/// and released when the provider is disposed.</summary>
public sealed class AgentFrameworkChatProvider : IStreamingChatProvider, IDisposable
{
    private readonly ChatClientCache _clients = new();

    /// <inheritdoc />
    public async Task<CompletionResult> CompleteAsync(
        IReadOnlyList<Agora.Providers.ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = _clients.Get(spec);
        var response = await chatClient.GetResponseAsync(ToChatMessages(messages), Options(spec),
            cancellationToken: cancellationToken);
        return new CompletionResult { Text = response.Text ?? string.Empty, Model = spec.Model };
    }

    /// <inheritdoc />
    public async Task<CompletionResult> StreamAsync(
        IReadOnlyList<Agora.Providers.ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = _clients.Get(spec);
        var text = new System.Text.StringBuilder();
        await foreach (var update in chatClient.GetStreamingResponseAsync(
            ToChatMessages(messages), Options(spec), cancellationToken: cancellationToken))
        {
            if (string.IsNullOrEmpty(update.Text)) continue;
            text.Append(update.Text);
            onChunk(update.Text);
        }
        return new CompletionResult { Text = text.ToString(), Model = spec.Model };
    }

    /// <summary>Maps core chat messages to Microsoft.Extensions.AI messages.</summary>
    private static List<Microsoft.Extensions.AI.ChatMessage> ToChatMessages(
        IReadOnlyList<Agora.Providers.ChatMessage> messages)
        => messages.Select(m => new Microsoft.Extensions.AI.ChatMessage(MapRole(m.Role), m.Content)).ToList();

    /// <summary>Builds chat options (temperature, max tokens) from the model spec.</summary>
    private static ChatOptions Options(ModelSpec spec)
        => new() { Temperature = (float)spec.Temperature, MaxOutputTokens = spec.MaxTokens };

    /// <summary>Maps a role string to a <see cref="ChatRole"/> (defaults to user).</summary>
    private static ChatRole MapRole(string role) => role switch
    {
        "system" => ChatRole.System,
        "assistant" => ChatRole.Assistant,
        _ => ChatRole.User,
    };

    /// <summary>Disposes the cached chat clients.</summary>
    public void Dispose() => _clients.Dispose();
}
