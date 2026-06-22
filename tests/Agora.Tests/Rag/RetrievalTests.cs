using Agora.Configuration;
using Agora.Providers.Concretes;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class RetrievalTests
{
    private static AgoraConfig Config(RagConfig? rag, MemoryConfig? memory = null) => new()
    {
        Providers = { ["openai"] = new() { ApiKeyEnv = "K" } },
        Models = { ["m"] = new() { Provider = "openai", Model = "x" } },
        Agents = { ["a"] = new() { Model = "m" } },
        Rag = rag,
        Memory = memory,
    };

    private static RagConfig OfflineRag() => new()
    {
        Enabled = true,
        Retrieval = new RetrievalConfig
        {
            Embedder = new EmbedderConfig { Type = "fake" },
            VectorStore = new VectorStoreConfig { Type = "memory" },
        },
    };

    [Fact]
    public void Build_BothDisabled_ReturnsNull()
        => Assert.Null(Retrieval.Build(Config(null), new FakeChatProvider()));

    [Fact]
    public void Build_RagEnabled_ExposesPipelineAndKnowledgeBase_NoMemory()
    {
        var retrieval = Retrieval.Build(Config(OfflineRag()), new FakeChatProvider());
        Assert.NotNull(retrieval);
        Assert.NotNull(retrieval!.Pipeline);
        Assert.NotNull(retrieval.KnowledgeBase);
        Assert.Null(retrieval.Memory);
    }

    [Fact]
    public async Task Build_RagEnabled_WritesAndReadsShareOneStore()
    {
        var retrieval = Retrieval.Build(Config(OfflineRag()), new FakeChatProvider())!;

        await retrieval.KnowledgeBase!.WriteAsync("Agora supports MCP tools", "agent:x");
        var enriched = await retrieval.Pipeline!.RunAsync("MCP");

        Assert.Contains(enriched.Retrieved, c => c.Text.Contains("MCP"));
    }

    [Fact]
    public void Build_MemoryOnly_ExposesMemory_NoPipelineOrKnowledgeBase()
    {
        var retrieval = Retrieval.Build(Config(null, new MemoryConfig { Enabled = true }), new FakeChatProvider());
        Assert.NotNull(retrieval);
        Assert.Null(retrieval!.Pipeline);
        Assert.Null(retrieval.KnowledgeBase);
        Assert.NotNull(retrieval.Memory);
    }
}
