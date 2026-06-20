using Agora.Configuration;
using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class RagFactoryTests
{
    private static AgoraConfig Config(RagConfig? rag) => new()
    {
        Models = new() { ["m"] = new ModelConfig { Provider = "anthropic", Model = "x" } },
        Agents = new() { ["a"] = new AgentConfig { Model = "m" } },
        Rag = rag,
    };

    [Fact]
    public void Factory_ReturnsNull_WhenRagAbsentOrDisabled()
    {
        Assert.Null(RagFactory.Build(Config(null)));
        Assert.Null(RagFactory.Build(Config(new RagConfig { Enabled = false })));
    }

    [Fact]
    public void Factory_BuildsOfflinePipeline()
    {
        var rag = new RagConfig
        {
            Enabled = true,
            Refine = new RefineConfig { Strategy = "none" },
            Retrieval = new RetrievalConfig
            {
                Embedder = new EmbedderConfig { Type = "fake" },
                VectorStore = new VectorStoreConfig { Type = "memory" },
                TopK = 3,
                ScoreThreshold = 0.1,
            },
        };
        var pipeline = RagFactory.Build(Config(rag));
        Assert.NotNull(pipeline);
        Assert.IsType<FakeEmbedder>(pipeline!.Embedder);
        Assert.IsType<InMemoryVectorStore>(pipeline.Store);
    }

    private static RagConfig RagWithStore(string type) => new()
    {
        Enabled = true,
        Retrieval = new RetrievalConfig { VectorStore = new VectorStoreConfig { Type = type } },
    };

    [Fact]
    public void Factory_UnknownStoreType_UsesResolver()
    {
        var resolved = new InMemoryVectorStore();
        var pipeline = RagFactory.Build(Config(RagWithStore("qdrant")),
            storeResolver: spec => spec?.Type == "qdrant" ? resolved : null);
        Assert.Same(resolved, pipeline!.Store);
    }

    [Fact]
    public void Factory_UnknownStoreType_NoResolver_Throws()
        => Assert.Throws<NotSupportedException>(() => RagFactory.Build(Config(RagWithStore("qdrant"))));
}
