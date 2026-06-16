using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class PipelineTests
{
    private static async Task<RagPipeline> SeededPipeline(int topK = 2)
    {
        var embedder = new FakeEmbedder(64);
        var store = new InMemoryVectorStore();
        var chunks = new[] { new Chunk("cats are great pets", "a"), new Chunk("databases store rows", "b") };
        var vectors = await embedder.EmbedAsync(chunks.Select(c => c.Text).ToList());
        store.Upsert(chunks, vectors);
        return new RagPipeline(new NoOpRefiner(), embedder, store, topK: topK, scoreThreshold: 0.0);
    }

    [Fact]
    public async Task Pipeline_RetrievesRelevantChunk()
    {
        var pipeline = await SeededPipeline();
        var enriched = await pipeline.RunAsync("tell me about cats");
        Assert.Equal("tell me about cats", enriched.Original);
        Assert.NotEmpty(enriched.Retrieved);
        Assert.Equal("a", enriched.Retrieved[0].Source);
    }

    [Fact]
    public async Task Pipeline_RespectsTopK()
    {
        var pipeline = await SeededPipeline(topK: 1);
        var enriched = await pipeline.RunAsync("cats and databases");
        Assert.Single(enriched.Retrieved);
    }
}
