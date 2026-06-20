using Agora.Rag;

namespace Agora.AgentFramework;

/// <summary>
/// Resolves real (non-core) embedders from config so the framework-free core stays unaware of
/// them. Injected as the embedder resolver into <c>RagFactory</c>/<c>Runtime</c>; returns null for
/// the core 'fake' type. Supports OpenAI and OpenAI-compatible endpoints (e.g. Ollama via base URL).
/// </summary>
public static class AgentFrameworkEmbedders
{
    /// <summary>Creates a real embedder for <c>openai</c>/<c>ollama</c> specs, or null otherwise.</summary>
    public static IEmbedder? TryCreate(EmbedderSpec spec) => spec.Type switch
    {
        "openai" => AgentFrameworkEmbedder.OpenAI(spec.Model, spec.ApiKey ?? "", spec.ApiBase),
        "ollama" => AgentFrameworkEmbedder.OpenAI(
            spec.Model, spec.ApiKey ?? "ollama", spec.ApiBase ?? "http://localhost:11434/v1"),
        _ => null,
    };
}
