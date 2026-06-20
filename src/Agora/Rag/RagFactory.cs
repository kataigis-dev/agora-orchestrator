using Agora.Configuration;
using Agora.Providers;

namespace Agora.Rag;

/// <summary>Assembles a <see cref="RagPipeline"/> from config, wiring the embedder, vector store, and
/// refiner. Non-core embedders/stores are produced by edge-injected resolvers so the core stays
/// framework-free.</summary>
public static class RagFactory
{
    /// <summary>Builds the pipeline, or null if RAG is absent/disabled in the config.</summary>
    public static RagPipeline? Build(
        AgoraConfig config, IChatProvider? provider = null, ModelSpec? refineSpec = null,
        Func<VectorStoreConfig?, IVectorStore?>? storeResolver = null,
        Func<EmbedderSpec, IEmbedder?>? embedderResolver = null)
    {
        var rag = config.Rag;
        if (rag is null || !rag.Enabled)
            return null;
        var embedder = BuildEmbedder(rag.Retrieval?.Embedder, config, embedderResolver);
        var store = BuildStore(rag.Retrieval?.VectorStore, storeResolver);
        var refiner = BuildRefiner(rag, provider, refineSpec);
        return new RagPipeline(
            refiner, embedder, store,
            topK: rag.Retrieval?.TopK ?? 6,
            scoreThreshold: rag.Retrieval?.ScoreThreshold ?? 0.0);
    }

    /// <summary>Builds the embedder: the built-in <c>fake</c>, or a real one via the resolver after
    /// resolving the provider's key/base URL.</summary>
    private static IEmbedder BuildEmbedder(
        EmbedderConfig? spec, AgoraConfig config, Func<EmbedderSpec, IEmbedder?>? embedderResolver)
    {
        var type = spec?.Type ?? "fake";
        if (type == "fake")
            return new FakeEmbedder();

        // Real embedders (openai/ollama) live outside the framework-free core and are built by
        // the edge-injected resolver; resolve the provider's key/base here as for chat models.
        var providerName = spec?.Provider ?? type;
        string? apiKey = null, apiBase = null;
        if (config.Providers.TryGetValue(providerName, out var providerCfg))
        {
            if (!string.IsNullOrEmpty(providerCfg.ApiKeyEnv))
                apiKey = Environment.GetEnvironmentVariable(providerCfg.ApiKeyEnv);
            apiBase = providerCfg.BaseUrl;
        }
        var resolved = new EmbedderSpec(type, providerName, spec?.Model ?? "", apiKey, apiBase);
        return embedderResolver?.Invoke(resolved)
            ?? throw new NotSupportedException(
                $"embedder type '{type}' has no built-in implementation and no resolver provided");
    }

    /// <summary>Builds the vector store: built-in <c>memory</c>/<c>file</c>, or a non-core store via
    /// the resolver.</summary>
    private static IVectorStore BuildStore(
        VectorStoreConfig? spec, Func<VectorStoreConfig?, IVectorStore?>? storeResolver)
    {
        var type = spec?.Type ?? "memory";
        return type switch
        {
            "memory" => new InMemoryVectorStore(),
            "file" => new FileVectorStore(spec?.Path
                ?? throw new NotSupportedException("vector_store type 'file' requires a 'path'")),
            // Non-core stores (e.g. a vector DB) are resolved by the edge-injected resolver.
            _ => storeResolver?.Invoke(spec)
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
