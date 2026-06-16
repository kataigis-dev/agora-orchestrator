namespace Agora.Rag;

public interface IVectorStore
{
    void Upsert(IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors);

    IReadOnlyList<Chunk> Query(IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0);
}
