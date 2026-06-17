using Agora.Providers;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Agora.AgentFramework;

internal static class ChatClients
{
    public static IChatClient Build(ModelSpec spec) => spec.Provider switch
    {
        "ollama" => new OllamaApiClient(new Uri(spec.ApiBase ?? "http://localhost:11434"), spec.Model),
        _ => BuildChatClient(spec).AsIChatClient(),
    };

    public static ChatClient BuildChatClient(ModelSpec spec)
    {
        var openAI = BuildOpenAI(spec);
        return openAI.GetChatClient(spec.Model);
    }

    private static OpenAIClient BuildOpenAI(ModelSpec spec)
    {
        var options = new OpenAIClientOptions { Endpoint = new Uri(spec.ApiBase ?? "https://api.openai.com/v1") };
        return new OpenAIClient(new ApiKeyCredential(spec.ApiKey ?? "no-key"), options);
    }
}
