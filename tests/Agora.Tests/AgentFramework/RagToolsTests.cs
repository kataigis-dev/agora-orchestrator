using System.Text.Json;
using Agora.AgentFramework;
using Agora.HumanInTheLoop;
using Agora.Rag;
using Microsoft.Extensions.AI;

namespace Agora.Tests.AgentFramework;

public class RagToolsTests
{
    private static AIFunctionArguments Args(params (string Key, object? Value)[] items)
    {
        var args = new AIFunctionArguments();
        foreach (var (k, v) in items) args[k] = v;
        return args;
    }

    private static string AsString(object? result) =>
        result is JsonElement je ? je.GetString() ?? "" : result?.ToString() ?? "";

    private static async Task<RagPipeline> PipelineWith(string text, IVectorStore store)
    {
        var embedder = new FakeEmbedder(64);
        var vectors = await embedder.EmbedAsync(new[] { text });
        store.Upsert(new[] { new Chunk(text, "kb.md") }, vectors);
        return new RagPipeline(new NoOpRefiner(), embedder, store, topK: 3, scoreThreshold: 0.0);
    }

    [Fact]
    public async Task RagSearch_ReturnsMatchingContext()
    {
        var rag = await PipelineWith("Agora orchestrates multi-agent graphs", new InMemoryVectorStore());
        var tools = RagTools.Create(new[] { "rag_search" }, rag, knowledgeBase: null, agentId: "a");
        var search = tools.OfType<AIFunction>().First(t => t.Name == "rag_search");

        var result = AsString(await search.InvokeAsync(Args(("query", "agora multi-agent"))));
        Assert.Contains("multi-agent graphs", result);
    }

    [Fact]
    public async Task RagWrite_StoresEntry()
    {
        var store = new InMemoryVectorStore();
        var kb = new KnowledgeBase(new FakeEmbedder(64), store, new NoOpConflictJudge());
        var tools = RagTools.Create(new[] { "rag_write" }, rag: null, knowledgeBase: kb, agentId: "writer");
        var write = tools.OfType<AIFunction>().First(t => t.Name == "rag_write");

        var result = AsString(await write.InvokeAsync(Args(("text", "the deadline is friday"))));
        Assert.Contains("Added", result);
        Assert.Single(store.Query(new float[64], topK: 100, scoreThreshold: 0.0));
    }

    [Fact]
    public void OnlyAllows_ConfiguredTools()
    {
        var store = new InMemoryVectorStore();
        var kb = new KnowledgeBase(new FakeEmbedder(), store, new NoOpConflictJudge());
        var rag = new RagPipeline(new NoOpRefiner(), new FakeEmbedder(), store);

        Assert.Empty(RagTools.Create(Array.Empty<string>(), rag, kb, "a"));
        var onlyWrite = RagTools.Create(new[] { "rag_write" }, rag, kb, "a");
        Assert.Contains(onlyWrite, t => t.Name == "rag_write");
        Assert.DoesNotContain(onlyWrite, t => t.Name == "rag_search");
    }

    [Fact]
    public void Tools_RequireTheirService()
    {
        // rag_search allow-listed but no pipeline available → not created.
        Assert.Empty(RagTools.Create(new[] { "rag_search" }, rag: null, knowledgeBase: null, agentId: "a"));
    }
}
