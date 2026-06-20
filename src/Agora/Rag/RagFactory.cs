using Agora.Configuration;
using Agora.Providers;

namespace Agora.Rag;

public static class RagFactory
{
    public static RagPipeline? Build(
        AgoraConfig config, IChatProvider? provider = null, ModelSpec? refineSpec = null,
        Func<VectorStoreConfig?, IVectorStore?>? storeResolver = null)
    {
        var rag = config.Rag;
        if (rag is null || !rag.Enabled)
            return null;
        var embedder = BuildEmbedder(rag.Retrieval?.Embedder);
        var store = BuildStore(rag.Retrieval?.VectorStore, storeResolver);
        var refiner = BuildRefiner(rag, provider, refineSpec);
        return new RagPipeline(
            refiner, embedder, store,
            topK: rag.Retrieval?.TopK ?? 6,
            scoreThreshold: rag.Retrieval?.ScoreThreshold ?? 0.0);
    }

    private static IEmbedder BuildEmbedder(EmbedderConfig? spec)
    {
        var type = spec?.Type ?? "fake";
        return type switch
        {
            "fake" => new FakeEmbedder(),
            _ => throw new NotSupportedException(
                $"embedder type '{type}' has no built-in implementation; inject a RagPipeline with a real " +
                "IEmbedder (e.g. Agora.AgentFramework.AgentFrameworkEmbedder)"),
        };
    }

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

    private static IRefiner BuildRefiner(RagConfig rag, IChatProvider? provider, ModelSpec? spec)
    {
        var strategy = rag.Refine?.Strategy ?? "none";
        if (strategy == "none" || provider is null || spec is null)
            return new NoOpRefiner();
        return new LlmRefiner(provider, spec);
    }
}
