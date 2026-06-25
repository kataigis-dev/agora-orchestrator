using Agora.Configuration;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class RagWriteValidatorTests
{
    // A config with a resolvable judge model (defaults.model -> models[balanced]) and a writer agent.
    private static AgoraConfig Base(bool writes, RagConfig? rag) => new()
    {
        Defaults = new() { Model = "balanced" },
        Providers = { ["openai"] = new() { ApiKeyEnv = "K" } },
        Models = { ["balanced"] = new() { Provider = "openai", Model = "gpt-4o" } },
        Agents =
        {
            ["writer"] = new()
            {
                Model = "balanced",
                Tools = writes ? new() { "rag_search", "rag_write" } : new() { "rag_search" },
            },
        },
        Rag = rag,
    };

    private static RagConfig Rag(string embedderType) => new()
    {
        Enabled = true,
        Retrieval = new RetrievalConfig
        {
            Embedder = new EmbedderConfig { Type = embedderType, Model = "text-embedding-3-small" },
            VectorStore = new VectorStoreConfig { Type = "file", Path = "./kb.json" },
        },
    };

    [Fact]
    public void RagWrite_WithFakeEmbedder_Rejected()
    {
        var ex = Assert.Throws<ConfigException>(() => RagWriteValidator.Validate(Base(writes: true, Rag("fake"))));
        Assert.Contains("rag_write", ex.Message);
        Assert.Contains("embedder", ex.Message);
        Assert.Contains("writer", ex.Message);
    }

    [Fact]
    public void RagWrite_WithNoRagSection_Rejected_EmbedderMissing()
    {
        var ex = Assert.Throws<ConfigException>(() => RagWriteValidator.Validate(Base(writes: true, rag: null)));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void RagWrite_WithUnresolvableJudgeModel_Rejected()
    {
        var config = Base(writes: true, Rag("openai"));
        config.Defaults.Model = "does-not-exist";
        var ex = Assert.Throws<ConfigException>(() => RagWriteValidator.Validate(config));
        Assert.Contains("defaults.model", ex.Message);
    }

    [Fact]
    public void RagWrite_WithRealEmbedderAndJudge_Valid()
        => RagWriteValidator.Validate(Base(writes: true, Rag("openai"))); // no throw

    [Fact]
    public void ReadOnlyRag_WithFakeEmbedder_Valid()
        // No agent writes → fake embedder / NoOp judge remain legal.
        => RagWriteValidator.Validate(Base(writes: false, Rag("fake")));
}
