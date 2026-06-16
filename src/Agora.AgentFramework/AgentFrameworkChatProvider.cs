using Agora.Providers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework;

/// <summary>
/// IChatProvider backed by Microsoft Agent Framework. Wraps an IChatClient (OpenAI or
/// Ollama) in a ChatClientAgent and runs the message list. No native Anthropic connector —
/// use OpenAI, an OpenAI-compatible endpoint, or Ollama (local).
/// </summary>
public sealed class AgentFrameworkChatProvider : IChatProvider
{
    public async Task<CompletionResult> CompleteAsync(
        IReadOnlyList<Agora.Providers.ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = ChatClients.Build(spec);

        AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Temperature = (float)spec.Temperature,
                MaxOutputTokens = spec.MaxTokens,
            },
        });

        var afMessages = messages
            .Select(m => new Microsoft.Extensions.AI.ChatMessage(MapRole(m.Role), m.Content))
            .ToList();

        var response = await agent.RunAsync(afMessages, cancellationToken: cancellationToken);
        return new CompletionResult { Text = response.Text ?? string.Empty, Model = spec.Model };
    }

    private static ChatRole MapRole(string role) => role switch
    {
        "system" => ChatRole.System,
        "assistant" => ChatRole.Assistant,
        _ => ChatRole.User,
    };
}
