namespace Agora.Rag;

public interface IVectorStore
{
    /// <summary>
    /// Inserts or updates entries. A chunk with a non-empty <see cref="Chunk.Id"/>
    /// replaces the entry with that id; an empty id is inserted with a freshly
    /// assigned id. Implementations may be a local store or a remote vector DB.
    /// </summary>
    void Upsert(IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors);

    IReadOnlyList<Chunk> Query(IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0);

    /// <summary>Removes entries by their stable ids. Unknown ids are ignored.</summary>
    void Delete(IReadOnlyList<string> ids);
}
