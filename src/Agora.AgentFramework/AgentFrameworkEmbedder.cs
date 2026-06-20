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

    public AgentFrameworkEmbedder(IEmbeddingGenerator<string, Embedding<float>> generator)
        => _generator = generator;

    public static AgentFrameworkEmbedder OpenAI(string model, string apiKey, string? apiBase = null)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(apiBase))
            options.Endpoint = new Uri(apiBase);
        var client = new OpenAIClient(new ApiKeyCredential(string.IsNullOrEmpty(apiKey) ? "no-key" : apiKey), options);
        return new(client.GetEmbeddingClient(model).AsIEmbeddingGenerator());
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = await _generator.GenerateAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => e.Vector.ToArray()).ToList();
    }
}
