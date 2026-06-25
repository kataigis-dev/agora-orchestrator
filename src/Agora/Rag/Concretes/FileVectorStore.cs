using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
using System.Text.Json;

namespace Agora.Rag.Concretes;

/// <summary>
/// File-backed vector store: persists chunks + vectors as JSON so a shared
/// knowledge base survives across runs. Loads on construction, saves on upsert.
/// Cosine similarity for queries, like <see cref="InMemoryVectorStore"/>.
/// </summary>
public sealed class FileVectorStore : IVectorStore
{
    private readonly string _path;
    private readonly List<(Chunk Chunk, float[] Vector)> _items = new();
    // The in-memory list and the whole-file rewrite are not thread-safe; parallel graph branches can
    // mutate concurrently. Serialise every load/mutate/save (and snapshot reads) through this lock.
    private readonly SemaphoreSlim _lock = new(1, 1);

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
        await _lock.WaitAsync(cancellationToken);
        try
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
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Chunk>> QueryAsync(
        IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0, CancellationToken cancellationToken = default)
    {
        // Snapshot under the lock so a concurrent mutation can't throw "collection modified" mid-scan.
        List<(Chunk Chunk, float[] Vector)> snapshot;
        await _lock.WaitAsync(cancellationToken);
        try { snapshot = _items.ToList(); }
        finally { _lock.Release(); }

        return snapshot
            .Select(item => item.Chunk with { Score = VectorMath.CosineSimilarity(vector, item.Vector) })
            .Where(c => c.Score >= scoreThreshold)
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_items.RemoveAll(item => ids.Contains(item.Chunk.Id)) > 0)
                await SaveAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
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

    /// <summary>Serializes all chunks and vectors to the backing JSON file, creating its directory.
    /// Writes to a temp file then atomically replaces the target, so a crash mid-write never leaves a
    /// partially-written (corrupt) file. Callers hold <see cref="_lock"/>, so the temp path is exclusive.</summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(_path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var records = _items.Select(i => new Record(i.Chunk.Id, i.Chunk.Text, i.Chunk.Source, i.Vector)).ToList();
        var temp = fullPath + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(records), cancellationToken);
        File.Move(temp, fullPath, overwrite: true);
    }

    private sealed record Record(string Id, string Text, string Source, float[] Vector);
}
