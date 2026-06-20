using System.ClientModel;
using Agora.Rag;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Agora.AgentFramework;

/// <summary>
/// IEmbedder backed by a Microsoft.Extensions.AI IEmbeddingGenerator (OpenAI). Lets a real
/// embedding-backed RagPipeline be injected into the Runtime while the core stays framework-free.
/// </summary>
public sealed class AgentFrameworkEmbedder : IEmbedder
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    /// <summary>Wraps an existing embedding generator.</summary>
    public AgentFrameworkEmbedder(IEmbeddingGenerator<string, Embedding<float>> generator)
        => _generator = generator;

    /// <summary>Creates an embedder against OpenAI (or an OpenAI-compatible endpoint via
    /// <paramref name="apiBase"/>) for the given model.</summary>
    public static AgentFrameworkEmbedder OpenAI(string model, string apiKey, string? apiBase = null)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(apiBase))
            options.Endpoint = new Uri(apiBase);
        var client = new OpenAIClient(new ApiKeyCredential(string.IsNullOrEmpty(apiKey) ? "no-key" : apiKey), options);
        return new(client.GetEmbeddingClient(model).AsIEmbeddingGenerator());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = await _generator.GenerateAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.Vector.ToArray()).ToList();
    }
}
