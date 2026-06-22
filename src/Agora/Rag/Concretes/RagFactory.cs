using Agora.Agents.Contracts;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Agora.Configuration;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;

namespace Agora.Rag.Concretes;

/// <summary>Assembles a <see cref="RagPipeline"/> from config, wiring the embedder, vector store, and
/// refiner. Non-core embedders/stores are produced by the injected <see cref="IAgentBackend"/> so the
/// core stays framework-free.</summary>
public static class RagFactory
{
    /// <summary>Builds the pipeline, or null if RAG is absent/disabled in the config.</summary>
    public static RagPipeline? Build(
        AgoraConfig config, IChatProvider? provider = null, ModelSpec? refineSpec = null,
        IAgentBackend? backend = null)
    {
        var rag = config.Rag;
        if (rag is null || !rag.Enabled)
            return null;
        var (embedder, store) = BuildStores(config, backend);
        return BuildPipeline(rag, provider, refineSpec, embedder, store);
    }

    /// <summary>Resolves the shared embedder + vector store from config. The <see cref="Retrieval"/>
    /// module builds these once and threads the same instances through the pipeline, knowledge base,
    /// memory, and ingestor so reads and writes hit one store.</summary>
    internal static (IEmbedder Embedder, IVectorStore Store) BuildStores(AgoraConfig config, IAgentBackend? backend)
        => (BuildEmbedder(config.Rag?.Retrieval?.Embedder, config, backend),
            BuildStore(config.Rag?.Retrieval?.VectorStore, backend));

    /// <summary>Builds the read pipeline over already-resolved shared stores.</summary>
    internal static RagPipeline BuildPipeline(
        RagConfig rag, IChatProvider? provider, ModelSpec? refineSpec, IEmbedder embedder, IVectorStore store)
        => new(
            BuildRefiner(rag, provider, refineSpec), embedder, store,
            topK: rag.Retrieval?.TopK ?? 6,
            scoreThreshold: rag.Retrieval?.ScoreThreshold ?? 0.0);

    /// <summary>Builds the embedder: the built-in <c>fake</c>, or a non-core one via the backend after
    /// resolving the provider's key/base URL.</summary>
    private static IEmbedder BuildEmbedder(
        EmbedderConfig? spec, AgoraConfig config, IAgentBackend? backend)
    {
        var type = spec?.Type ?? "fake";
        if (type == "fake")
            return new FakeEmbedder();

        // Real embedders (openai/ollama) live outside the framework-free core and are built by
        // the agent backend; resolve the provider's key/base here as for chat models.
        var providerName = spec?.Provider ?? type;
        string? apiKey = null, apiBase = null;
        if (config.Providers.TryGetValue(providerName, out var providerCfg))
        {
            if (!string.IsNullOrEmpty(providerCfg.ApiKeyEnv))
                apiKey = Environment.GetEnvironmentVariable(providerCfg.ApiKeyEnv);
            apiBase = providerCfg.BaseUrl;
        }
        var resolved = new EmbedderSpec(type, providerName, spec?.Model ?? "", apiKey, apiBase);
        return backend?.TryCreateEmbedder(resolved)
            ?? throw new NotSupportedException(
                $"embedder type '{type}' has no built-in implementation and no backend provided");
    }

    /// <summary>Builds the vector store: built-in <c>memory</c>/<c>file</c>, or a non-core store via
    /// the backend.</summary>
    private static IVectorStore BuildStore(
        VectorStoreConfig? spec, IAgentBackend? backend)
    {
        var type = spec?.Type ?? "memory";
        return type switch
        {
            "memory" => new InMemoryVectorStore(),
            "file" => new FileVectorStore(spec?.Path
                ?? throw new NotSupportedException("vector_store type 'file' requires a 'path'")),
            // Non-core stores (e.g. a vector DB) are resolved by the agent backend.
            _ => backend?.TryCreateVectorStore(spec)
                ?? throw new NotSupportedException($"unknown vector_store type '{type}'"),
        };
    }

    /// <summary>Builds the refiner: <see cref="LlmRefiner"/> when the <c>llm</c> strategy and a
    /// provider/model are available, otherwise the no-op refiner.</summary>
    private static IRefiner BuildRefiner(RagConfig rag, IChatProvider? provider, ModelSpec? spec)
    {
        var strategy = rag.Refine?.Strategy ?? "none";
        if (strategy == "none" || provider is null || spec is null)
            return new NoOpRefiner();
        return new LlmRefiner(provider, spec);
    }
}
