using Agora.Providers;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

public sealed class AgentFrameworkChatProvider : IStreamingChatProvider
{
    public async Task<CompletionResult> CompleteAsync(
        IReadOnlyList<Agora.Providers.ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = ChatClients.Build(spec);
        var response = await chatClient.GetResponseAsync(ToChatMessages(messages), Options(spec),
            cancellationToken: cancellationToken);
        return new CompletionResult { Text = response.Text ?? string.Empty, Model = spec.Model };
    }

    public async Task<CompletionResult> StreamAsync(
        IReadOnlyList<Agora.Providers.ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = ChatClients.Build(spec);
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

    private static List<Microsoft.Extensions.AI.ChatMessage> ToChatMessages(
        IReadOnlyList<Agora.Providers.ChatMessage> messages)
        => messages.Select(m => new Microsoft.Extensions.AI.ChatMessage(MapRole(m.Role), m.Content)).ToList();

    private static ChatOptions Options(ModelSpec spec)
        => new() { Temperature = (float)spec.Temperature, MaxOutputTokens = spec.MaxTokens };

    private static ChatRole MapRole(string role) => role switch
    {
        "system" => ChatRole.System,
        "assistant" => ChatRole.Assistant,
        _ => ChatRole.User,
    };
}
