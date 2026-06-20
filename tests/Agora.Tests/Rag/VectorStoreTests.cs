using Agora.Rag;
using Xunit;

namespace Agora.Tests.Rag;

public class VectorStoreTests
{
    [Fact]
    public void CosineSimilarity_Basics()
    {
        Assert.Equal(1.0, VectorMath.CosineSimilarity(new float[] { 1, 0 }, new float[] { 1, 0 }), 6);
        Assert.Equal(0.0, VectorMath.CosineSimilarity(new float[] { 1, 0 }, new float[] { 0, 1 }), 6);
        Assert.Equal(0.0, VectorMath.CosineSimilarity(new float[] { 0, 0 }, new float[] { 1, 1 }), 6);
    }

    [Fact]
    public async Task Store_ReturnsMostSimilarFirst()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(
            new[] { new Chunk("cat", "a"), new Chunk("dog", "b") },
            new[] { new float[] { 1, 0 }, new float[] { 0, 1 } });
        var hits = await store.QueryAsync(new float[] { 0.9f, 0.1f }, topK: 2);
        Assert.Equal(new[] { "cat", "dog" }, hits.Select(h => h.Text));
        Assert.True(hits[0].Score > hits[1].Score);
    }

    [Fact]
    public async Task Store_RespectsThresholdAndTopK()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(
            new[] { new Chunk("cat", "a"), new Chunk("dog", "b") },
            new[] { new float[] { 1, 0 }, new float[] { 0, 1 } });
        var hits = await store.QueryAsync(new float[] { 1, 0 }, topK: 5, scoreThreshold: 0.5);
        Assert.Equal(new[] { "cat" }, hits.Select(h => h.Text));
        Assert.Single(await store.QueryAsync(new float[] { 1, 1 }, topK: 1, scoreThreshold: 0.0));
    }

    [Fact]
    public async Task Upsert_AssignsId_AndDeleteRemovesIt()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new[] { new Chunk("cat", "a"), new Chunk("dog", "b") },
            new[] { new float[] { 1, 0 }, new float[] { 0, 1 } });
        var cat = (await store.QueryAsync(new float[] { 1, 0 }, topK: 1)).Single();
        Assert.NotEqual("", cat.Id);

        await store.DeleteAsync(new[] { cat.Id });
        var remaining = await store.QueryAsync(new float[] { 1, 1 }, topK: 5, scoreThreshold: 0.0);
        Assert.Equal(new[] { "dog" }, remaining.Select(h => h.Text));
    }

    [Fact]
    public async Task Upsert_WithExistingId_UpdatesInPlace()
    {
        var store = new InMemoryVectorStore();
        await store.UpsertAsync(new[] { new Chunk("v1", "a") }, new[] { new float[] { 1, 0 } });
        var stored = (await store.QueryAsync(new float[] { 1, 0 }, topK: 1)).Single();

        await store.UpsertAsync(new[] { stored with { Text = "v2" } }, new[] { new float[] { 1, 0 } });
        var hits = await store.QueryAsync(new float[] { 1, 0 }, topK: 5, scoreThreshold: 0.0);
        Assert.Equal("v2", Assert.Single(hits).Text);
    }
}
