using Agora.Providers;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

public sealed class AgentFrameworkChatProvider : IChatProvider
{
    public async Task<CompletionResult> CompleteAsync(
        IReadOnlyList<Agora.Providers.ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = ChatClients.Build(spec);

        var chatMessages = messages
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(MapRole(m.Role), m.Content))
            .ToList();

        var response = await chatClient.GetResponseAsync(chatMessages, new ChatOptions
        {
            Temperature = (float)spec.Temperature,
            MaxOutputTokens = spec.MaxTokens,
        }, cancellationToken: cancellationToken);

        return new CompletionResult { Text = response.Text ?? string.Empty, Model = spec.Model };
    }

    private static ChatRole MapRole(string role) => role switch
    {
        "system" => ChatRole.System,
        "assistant" => ChatRole.Assistant,
        _ => ChatRole.User,
    };
}
