namespace Agora.Rag;

/// <summary>Simple in-process vector store using cosine similarity.</summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<(Chunk Chunk, float[] Vector)> _items = new();

    public void Upsert(IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors)
    {
        for (var i = 0; i < chunks.Count; i++)
        {
            var id = string.IsNullOrEmpty(chunks[i].Id) ? Guid.NewGuid().ToString("N") : chunks[i].Id;
            var stored = (chunks[i] with { Id = id }, vectors[i]);
            var existing = _items.FindIndex(item => item.Chunk.Id == id);
            if (existing >= 0) _items[existing] = stored;
            else _items.Add(stored);
        }
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

    public void Delete(IReadOnlyList<string> ids)
        => _items.RemoveAll(item => ids.Contains(item.Chunk.Id));
}
