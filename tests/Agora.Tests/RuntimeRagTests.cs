using Agora;
using Agora.Providers;
using Agora.Rag;
using Xunit;

namespace Agora.Tests;

public class RuntimeRagTests
{
    private const string Config = """
        defaults: { model: balanced }
        providers:
          anthropic: { api_key_env: ANTHROPIC_API_KEY }
        models:
          balanced: { provider: anthropic, model: claude-sonnet-4-6 }
        agents:
          answerer: { model: balanced, role: "Answer." }
        graph:
          entry: answerer
          edges:
            - { from: answerer, to: END, type: sequential }
        """;

    private static async Task<RagPipeline> PipelineWithChunk(string text, string source)
    {
        var embedder = new FakeEmbedder(64);
        var store = new InMemoryVectorStore();
        var vectors = await embedder.EmbedAsync(new[] { text });
        store.Upsert(new[] { new Chunk(text, source) }, vectors);
        return new RagPipeline(new NoOpRefiner(), embedder, store, topK: 2, scoreThreshold: 0.0);
    }

    [Fact]
    public async Task Run_InjectsRagContext_IntoEntryAgent()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        var provider = new FakeChatProvider(new[] { "ANSWER" });
        var rag = await PipelineWithChunk("Agora is a multi-agent framework", "kb.md");
        var rt = Runtime.FromConfig(path, provider, rag);

        var result = await rt.RunAsync("what is agora");

        Assert.Equal("ANSWER", result.Output);
        var entryUserMsg = provider.Calls[0].Messages[^1].Content;
        Assert.Contains("multi-agent framework", entryUserMsg);
        Assert.NotNull(result.Enriched);
        Assert.NotEmpty(result.Enriched!.Retrieved);
    }

    [Fact]
    public async Task KnowledgeBase_SharesStore_WithRagPipeline()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".yaml");
        File.WriteAllText(path, Config);
        var rag = await PipelineWithChunk("seed entry", "kb.md");
        var rt = Runtime.FromConfig(path, new FakeChatProvider(new[] { "ANSWER" }), rag);

        Assert.NotNull(rt.KnowledgeBase);
        await rt.KnowledgeBase!.WriteAsync("Agora supports MCP tools", "agent:x");

        // The write lands in the same store the read pipeline queries.
        var enriched = await rt.Rag!.RunAsync("MCP");
        Assert.Contains(enriched.Retrieved, c => c.Text.Contains("MCP"));
    }
}
