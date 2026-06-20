namespace Agora.Rag;

/// <summary>Simple in-process vector store using cosine similarity.</summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<(Chunk Chunk, float[] Vector)> _items = new();

    public Task UpsertAsync(
        IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < chunks.Count; i++)
        {
            var id = string.IsNullOrEmpty(chunks[i].Id) ? Guid.NewGuid().ToString("N") : chunks[i].Id;
            var stored = (chunks[i] with { Id = id }, vectors[i]);
            var existing = _items.FindIndex(item => item.Chunk.Id == id);
            if (existing >= 0) _items[existing] = stored;
            else _items.Add(stored);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Chunk>> QueryAsync(
        IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Chunk> hits = _items
            .Select(item => item.Chunk with { Score = VectorMath.CosineSimilarity(vector, item.Vector) })
            .Where(c => c.Score >= scoreThreshold)
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();
        return Task.FromResult(hits);
    }

    public Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        _items.RemoveAll(item => ids.Contains(item.Chunk.Id));
        return Task.CompletedTask;
    }
}
