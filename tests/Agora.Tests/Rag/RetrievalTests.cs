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
        Assert.False(retrieval.MemoryEnabled);
        Assert.Null(retrieval.NewMemory());
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
    public async Task Build_RagAndMemory_UseSeparateStores()
    {
        var retrieval = Retrieval.Build(
            Config(OfflineRag(), new MemoryConfig { Enabled = true }), new FakeChatProvider())!;
        var memory = retrieval.NewMemory()!;

        await retrieval.KnowledgeBase!.WriteAsync("Agora supports MCP tools", "agent:x");
        await memory.RememberAsync("the planner chose plan alpha", "planner");

        // KB reads surface KB facts but never memory entries.
        var enriched = await retrieval.Pipeline!.RunAsync("MCP");
        Assert.Contains(enriched.Retrieved, c => c.Text.Contains("MCP"));
        Assert.DoesNotContain(enriched.Retrieved, c => c.Text.Contains("plan alpha"));

        // Memory recall surfaces memory entries but never KB facts.
        var recalled = await memory.RecallAsync("planner chose plan", topK: 5);
        Assert.Contains("plan alpha", recalled);
        Assert.DoesNotContain("MCP", recalled);
    }

    [Fact]
    public async Task NewMemory_MintsFreshInstanceEachCall()
    {
        var retrieval = Retrieval.Build(
            Config(null, new MemoryConfig { Enabled = true }), new FakeChatProvider())!;

        var first = retrieval.NewMemory()!;
        await first.RememberAsync("run one secret", "a");

        // A second mint is a distinct, empty store: it never sees the first run's entries.
        var second = retrieval.NewMemory()!;
        Assert.NotSame(first, second);
        Assert.Equal("", await second.RecallAsync("run one secret", topK: 5));
    }

    [Fact]
    public void Build_MemoryOnly_MintsMemory_NoPipelineOrKnowledgeBase()
    {
        var retrieval = Retrieval.Build(Config(null, new MemoryConfig { Enabled = true }), new FakeChatProvider());
        Assert.NotNull(retrieval);
        Assert.Null(retrieval!.Pipeline);
        Assert.Null(retrieval.KnowledgeBase);
        Assert.True(retrieval.MemoryEnabled);
        Assert.NotNull(retrieval.NewMemory());
    }
}
