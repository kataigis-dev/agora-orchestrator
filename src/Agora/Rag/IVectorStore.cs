namespace Agora.Rag;

/// <summary>Storage abstraction for embedded chunks: upsert, similarity query, and delete. The sole
/// seam through which the system reads/writes vectors (local store or remote DB).</summary>
public interface IVectorStore
{
    /// <summary>
    /// Inserts or updates entries. A chunk with a non-empty <see cref="Chunk.Id"/>
    /// replaces the entry with that id; an empty id is inserted with a freshly
    /// assigned id. Implementations may be a local store or a remote vector DB.
    /// </summary>
    Task UpsertAsync(
        IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors, CancellationToken cancellationToken = default);

    /// <summary>Returns the <paramref name="topK"/> most similar chunks to the query vector that meet
    /// <paramref name="scoreThreshold"/>, ordered by descending similarity.</summary>
    Task<IReadOnlyList<Chunk>> QueryAsync(
        IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0, CancellationToken cancellationToken = default);

    /// <summary>Removes entries by their stable ids. Unknown ids are ignored.</summary>
    Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default);
}
