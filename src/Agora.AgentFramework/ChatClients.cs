using Agora.Providers;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Agora.AgentFramework;

/// <summary>Builds the appropriate Microsoft.Extensions.AI chat client for a model spec.</summary>
internal static class ChatClients
{
    /// <summary>Returns an <see cref="IChatClient"/> for the spec's provider (Ollama, or OpenAI-compatible).</summary>
    public static IChatClient Build(ModelSpec spec) => spec.Provider switch
    {
        "ollama" => new OllamaApiClient(new Uri(spec.ApiBase ?? "http://localhost:11434"), spec.Model),
        _ => BuildChatClient(spec).AsIChatClient(),
    };

    /// <summary>Builds an OpenAI <see cref="ChatClient"/> for the spec's model.</summary>
    public static ChatClient BuildChatClient(ModelSpec spec)
    {
        var openAI = BuildOpenAI(spec);
        return openAI.GetChatClient(spec.Model);
    }

    /// <summary>Builds the OpenAI client with the spec's key and endpoint.</summary>
    private static OpenAIClient BuildOpenAI(ModelSpec spec)
    {
        var options = new OpenAIClientOptions { Endpoint = new Uri(spec.ApiBase ?? "https://api.openai.com/v1") };
        return new OpenAIClient(new ApiKeyCredential(spec.ApiKey ?? "no-key"), options);
    }
}
