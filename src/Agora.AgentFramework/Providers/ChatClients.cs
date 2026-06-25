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
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Agora.AgentFramework.Providers;

/// <summary>Builds the appropriate Microsoft.Extensions.AI chat client for a model spec.</summary>
internal static class ChatClients
{
    /// <summary>Returns an <see cref="IChatClient"/> for the spec's provider (Anthropic, Ollama, GitHub
    /// Copilot, or any OpenAI-compatible endpoint).</summary>
    public static IChatClient Build(ModelSpec spec) => spec.Provider switch
    {
        "anthropic" or "claude" => AnthropicChatClient.Build(spec),
        "ollama" => new OllamaApiClient(new Uri(spec.ApiBase ?? "http://localhost:11434"), spec.Model),
        "github-copilot" or "copilot" => CopilotChatClient.Build(spec),
        _ => BuildChatClient(spec).AsIChatClient(),
    };

    /// <summary>Builds an OpenAI <see cref="ChatClient"/> for the spec's model.</summary>
    public static ChatClient BuildChatClient(ModelSpec spec)
    {
        var openAI = BuildOpenAI(spec);
        return openAI.GetChatClient(spec.Model);
    }

    /// <summary>Builds the OpenAI client with the spec's key and endpoint. The client's network timeout is
    /// set from the spec so a long generation isn't cut off by the ~100s SDK default before the configured
    /// <c>timeout</c> (issue 09 / F13); <see cref="ResilientChatProvider"/>'s RetryPolicy still applies.</summary>
    private static OpenAIClient BuildOpenAI(ModelSpec spec)
    {
        var options = new OpenAIClientOptions { Endpoint = new Uri(spec.ApiBase ?? "https://api.openai.com/v1") };
        if (spec.Timeout > 0)
            options.NetworkTimeout = TimeSpan.FromSeconds(spec.Timeout);
        return new OpenAIClient(new ApiKeyCredential(spec.ApiKey ?? "no-key"), options);
    }
}
