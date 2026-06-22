using Agora.Agents.Contracts;
using Agora.Configuration;
using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class RagFactoryTests
{
    /// <summary>Test backend that resolves only non-core stores/embedders via optional delegates.</summary>
    private sealed class Backend : IAgentBackend
    {
        private readonly Func<VectorStoreConfig?, IVectorStore?>? _store;
        private readonly Func<EmbedderSpec, IEmbedder?>? _embedder;
        public Backend(Func<VectorStoreConfig?, IVectorStore?>? store = null, Func<EmbedderSpec, IEmbedder?>? embedder = null)
        {
            _store = store;
            _embedder = embedder;
        }
        public IAgent CreateToolAgent(AgentBuildContext context) => throw new NotSupportedException();
        public IVectorStore? TryCreateVectorStore(VectorStoreConfig? spec) => _store?.Invoke(spec);
        public IEmbedder? TryCreateEmbedder(EmbedderSpec spec) => _embedder?.Invoke(spec);
    }

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
    public void Factory_UnknownStoreType_UsesBackend()
    {
        var resolved = new InMemoryVectorStore();
        var pipeline = RagFactory.Build(Config(RagWithStore("qdrant")),
            backend: new Backend(store: spec => spec?.Type == "qdrant" ? resolved : null));
        Assert.Same(resolved, pipeline!.Store);
    }

    [Fact]
    public void Factory_UnknownStoreType_NoBackend_Throws()
        => Assert.Throws<NotSupportedException>(() => RagFactory.Build(Config(RagWithStore("qdrant"))));

    private static AgoraConfig ConfigWithEmbedder(EmbedderConfig embedder, string? apiKeyEnv) => new()
    {
        Providers = apiKeyEnv is null
            ? new()
            : new() { ["openai"] = new ProviderConfig { ApiKeyEnv = apiKeyEnv, BaseUrl = "http://base" } },
        Models = new() { ["m"] = new ModelConfig { Provider = "openai", Model = "x" } },
        Agents = new() { ["a"] = new AgentConfig { Model = "m" } },
        Rag = new RagConfig { Enabled = true, Retrieval = new RetrievalConfig { Embedder = embedder } },
    };

    [Fact]
    public void Factory_RealEmbedder_UsesResolver_WithResolvedKeyAndModel()
    {
        Environment.SetEnvironmentVariable("EMB_KEY_TEST", "secret-123");
        try
        {
            EmbedderSpec? captured = null;
            var fake = new FakeEmbedder();
            var cfg = ConfigWithEmbedder(
                new EmbedderConfig { Type = "openai", Provider = "openai", Model = "text-embedding-3-small" },
                "EMB_KEY_TEST");

            var pipeline = RagFactory.Build(cfg, backend: new Backend(embedder: spec => { captured = spec; return fake; }));

            Assert.Same(fake, pipeline!.Embedder);
            Assert.Equal("openai", captured!.Type);
            Assert.Equal("text-embedding-3-small", captured.Model);
            Assert.Equal("secret-123", captured.ApiKey);
            Assert.Equal("http://base", captured.ApiBase);
        }
        finally { Environment.SetEnvironmentVariable("EMB_KEY_TEST", null); }
    }

    [Fact]
    public void Factory_RealEmbedder_NoBackend_Throws()
        => Assert.Throws<NotSupportedException>(() =>
            RagFactory.Build(ConfigWithEmbedder(new EmbedderConfig { Type = "openai" }, null)));
}
