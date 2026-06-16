using Agora.Providers;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace Agora.AgentFramework;

/// <summary>Builds a Microsoft.Extensions.AI IChatClient from a ModelSpec (OpenAI or Ollama).</summary>
internal static class ChatClients
{
    public static IChatClient Build(ModelSpec spec) => spec.Provider switch
    {
        "ollama" => new OllamaApiClient(new Uri(spec.ApiBase ?? "http://localhost:11434"), spec.Model),
        _ => new OpenAIClient(spec.ApiKey ?? "").GetChatClient(spec.Model).AsIChatClient(),
    };
}
