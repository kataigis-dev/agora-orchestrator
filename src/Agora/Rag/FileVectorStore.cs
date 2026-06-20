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

    /// <summary>Opens the store at <paramref name="path"/>, loading any existing data.</summary>
    public FileVectorStore(string path)
    {
        _path = path;
        Load();
    }

    /// <inheritdoc />
    public async Task UpsertAsync(
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
        await SaveAsync(cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        if (_items.RemoveAll(item => ids.Contains(item.Chunk.Id)) > 0)
            await SaveAsync(cancellationToken);
    }

    /// <summary>Loads persisted chunks and vectors from disk, if the file exists.</summary>
    private void Load()
    {
        if (!File.Exists(_path))
            return;
        var records = JsonSerializer.Deserialize<List<Record>>(File.ReadAllText(_path)) ?? new();
        foreach (var r in records)
            _items.Add((new Chunk(r.Text, r.Source, Id: r.Id), r.Vector));
    }

    /// <summary>Serializes all chunks and vectors to the backing JSON file, creating its directory.</summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var records = _items.Select(i => new Record(i.Chunk.Id, i.Chunk.Text, i.Chunk.Source, i.Vector)).ToList();
        await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(records), cancellationToken);
    }

    private sealed record Record(string Id, string Text, string Source, float[] Vector);
}
