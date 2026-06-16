namespace Agora.Rag;

/// <summary>Simple in-process vector store using cosine similarity.</summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<(Chunk Chunk, float[] Vector)> _items = new();

    public void Upsert(IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors)
    {
        for (var i = 0; i < chunks.Count; i++)
            _items.Add((chunks[i], vectors[i]));
    }

    public IReadOnlyList<Chunk> Query(IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0)
    {
        return _items
            .Select(item => item.Chunk with { Score = VectorMath.CosineSimilarity(vector, item.Vector) })
            .Where(c => c.Score >= scoreThreshold)
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();
    }
}
