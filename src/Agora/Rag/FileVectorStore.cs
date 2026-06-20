using System.Text.Json;

namespace Agora.Rag;

/// <summary>
/// File-backed vector store: persists chunks + vectors as JSON so a shared
/// knowledge base survives across runs. Loads on construction, saves on upsert.
/// Cosine similarity for queries, like <see cref="InMemoryVectorStore"/>.
/// </summary>
public sealed class FileVectorStore : IVectorStore
{
    private readonly string _path;
    private readonly List<(Chunk Chunk, float[] Vector)> _items = new();

    public FileVectorStore(string path)
    {
        _path = path;
        Load();
    }

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
        Save();
    }

    public IReadOnlyList<Chunk> Query(IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0)
        => _items
            .Select(item => item.Chunk with { Score = VectorMath.CosineSimilarity(vector, item.Vector) })
            .Where(c => c.Score >= scoreThreshold)
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();

    public void Delete(IReadOnlyList<string> ids)
    {
        if (_items.RemoveAll(item => ids.Contains(item.Chunk.Id)) > 0)
            Save();
    }

    private void Load()
    {
        if (!File.Exists(_path))
            return;
        var records = JsonSerializer.Deserialize<List<Record>>(File.ReadAllText(_path)) ?? new();
        foreach (var r in records)
            _items.Add((new Chunk(r.Text, r.Source, Id: r.Id), r.Vector));
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var records = _items.Select(i => new Record(i.Chunk.Id, i.Chunk.Text, i.Chunk.Source, i.Vector)).ToList();
        File.WriteAllText(_path, JsonSerializer.Serialize(records));
    }

    private sealed record Record(string Id, string Text, string Source, float[] Vector);
}
