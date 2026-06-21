using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using Xunit;

namespace Agora.Tests.Rag;

public class ContextMemoryTests
{
    private static ContextMemory Mem(IVectorStore store) => new(new FakeEmbedder(64), store);

    [Fact]
    public async Task Recall_ReturnsRememberedEntries()
    {
        var mem = Mem(new InMemoryVectorStore());
        await mem.RememberAsync("plan: do X then Y", "planner");
        var recalled = await mem.RecallAsync("what is the plan", topK: 5);
        Assert.Contains("plan: do X then Y", recalled);
    }

    [Fact]
    public async Task Recall_FiltersOutNonMemoryEntries()
    {
        var store = new InMemoryVectorStore();
        var embedder = new FakeEmbedder(64);
        await store.UpsertAsync(new[] { new Chunk("a knowledge base fact", "kb.md") },
            await embedder.EmbedAsync(new[] { "a knowledge base fact" }));
        var mem = Mem(store);
        await mem.RememberAsync("a memory entry", "planner");

        var recalled = await mem.RecallAsync("fact entry", topK: 5);
        Assert.Contains("a memory entry", recalled);
        Assert.DoesNotContain("knowledge base fact", recalled);
    }

    [Fact]
    public async Task Recall_Empty_WhenNothingRemembered()
        => Assert.Equal("", await Mem(new InMemoryVectorStore()).RecallAsync("anything", topK: 5));

    [Fact]
    public async Task Recall_CapsAtTopK()
    {
        var mem = Mem(new InMemoryVectorStore());
        for (var i = 0; i < 6; i++)
            await mem.RememberAsync($"entry number {i}", "a");
        var recalled = await mem.RecallAsync("entry", topK: 2);
        Assert.Equal(2, recalled.Split("\n- ").Length - 1);
    }

    [Fact]
    public async Task Recall_RespectsMaxCharsBudget()
    {
        var mem = Mem(new InMemoryVectorStore());
        for (var i = 0; i < 6; i++)
            await mem.RememberAsync($"entry number {i} with several words", "a");

        var entries = (await mem.RecallAsync("entry", topK: 6, maxChars: 35)).Split("\n- ").Length - 1;
        Assert.InRange(entries, 1, 2); // at least the top one, but the budget caps the rest
    }
}
