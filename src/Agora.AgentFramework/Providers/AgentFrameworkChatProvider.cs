using Agora.AgentFramework.Tools;
using Agora.AgentFramework.Mcp;
using Agora.AgentFramework.Specs;
using Agora.AgentFramework.Rag;
using Agora.AgentFramework.Providers;
using Agora.AgentFramework.Agents;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Microsoft.Extensions.AI;

namespace Agora.AgentFramework.Providers;

/// <summary>Chat provider backed by Microsoft.Extensions.AI, dispatching to OpenAI-compatible
/// endpoints or Ollama based on the model spec. Clients are reused via a <see cref="ChatClientCache"/>
/// and released when the provider is disposed. Translates Agora's <see cref="ChatMessage.CacheStable"/>
/// prompt-caching hint into each provider's mechanism and reports cache token usage.</summary>
public sealed class AgentFrameworkChatProvider : IStreamingChatProvider, IDisposable
{
    private readonly ChatClientCache _clients = new();

    /// <inheritdoc />
    public async Task<CompletionResult> CompleteAsync(
        IReadOnlyList<Agora.Providers.Models.ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = _clients.Get(spec);
        var adapter = CacheAdapters.For(spec);
        var response = await chatClient.GetResponseAsync(ToChatMessages(messages, adapter), Options(spec),
            cancellationToken: cancellationToken);
        return Result(response.Text ?? string.Empty, spec, adapter.MapUsage(response.Usage));
    }

    /// <inheritdoc />
    public async Task<CompletionResult> StreamAsync(
        IReadOnlyList<Agora.Providers.Models.ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default)
    {
        IChatClient chatClient = _clients.Get(spec);
        var adapter = CacheAdapters.For(spec);
        var text = new System.Text.StringBuilder();
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in chatClient.GetStreamingResponseAsync(
            ToChatMessages(messages, adapter), Options(spec), cancellationToken: cancellationToken))
        {
            updates.Add(update);
            if (string.IsNullOrEmpty(update.Text)) continue;
            text.Append(update.Text);
            onChunk(update.Text);
        }
        // Usage arrives as a trailing update; recover it from the assembled response.
        var usage = adapter.MapUsage(updates.ToChatResponse().Usage);
        return Result(text.ToString(), spec, usage);
    }

    /// <summary>Builds a completion result from text, model, and mapped token usage.</summary>
    private static CompletionResult Result(
        string text, ModelSpec spec, (int Input, int Output, int CacheRead, int CacheWrite) usage)
        => new()
        {
            Text = text,
            Model = spec.Model,
            InputTokens = usage.Input,
            OutputTokens = usage.Output,
            CacheReadTokens = usage.CacheRead,
            CacheWriteTokens = usage.CacheWrite,
        };

    /// <summary>Maps core chat messages to Microsoft.Extensions.AI messages, marking cache-stable content
    /// via the provider's <see cref="ICacheAdapter"/>.</summary>
    private static List<Microsoft.Extensions.AI.ChatMessage> ToChatMessages(
        IReadOnlyList<Agora.Providers.Models.ChatMessage> messages, ICacheAdapter adapter)
    {
        return messages.Select(m =>
        {
            var mapped = new Microsoft.Extensions.AI.ChatMessage(MapRole(m.Role), m.Content);
            if (m.CacheStable)
                adapter.Mark(mapped);
            return mapped;
        }).ToList();
    }

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
