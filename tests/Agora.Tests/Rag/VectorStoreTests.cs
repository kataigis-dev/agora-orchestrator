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
    public void Store_ReturnsMostSimilarFirst()
    {
        var store = new InMemoryVectorStore();
        store.Upsert(
            new[] { new Chunk("cat", "a"), new Chunk("dog", "b") },
            new[] { new float[] { 1, 0 }, new float[] { 0, 1 } });
        var hits = store.Query(new float[] { 0.9f, 0.1f }, topK: 2);
        Assert.Equal(new[] { "cat", "dog" }, hits.Select(h => h.Text));
        Assert.True(hits[0].Score > hits[1].Score);
    }

    [Fact]
    public void Store_RespectsThresholdAndTopK()
    {
        var store = new InMemoryVectorStore();
        store.Upsert(
            new[] { new Chunk("cat", "a"), new Chunk("dog", "b") },
            new[] { new float[] { 1, 0 }, new float[] { 0, 1 } });
        var hits = store.Query(new float[] { 1, 0 }, topK: 5, scoreThreshold: 0.5);
        Assert.Equal(new[] { "cat" }, hits.Select(h => h.Text));
        Assert.Single(store.Query(new float[] { 1, 1 }, topK: 1, scoreThreshold: 0.0));
    }
}
