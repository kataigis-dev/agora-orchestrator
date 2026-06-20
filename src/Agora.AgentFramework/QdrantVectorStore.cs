using Agora.Rag;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Agora.AgentFramework;

/// <summary>
/// <see cref="IVectorStore"/> backed by a Qdrant server over gRPC (port 6334 by default).
/// Lives outside the framework-free core; injected from the edge via a store resolver so
/// <c>vector_store: { type: qdrant, url, collection }</c> selects it. The collection is created
/// lazily on first upsert using the embedding dimension, with cosine distance.
/// </summary>
public sealed class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly string _collection;
    private bool _collectionReady;

    public QdrantVectorStore(string url, string collection, QdrantClient? client = null)
    {
        _collection = collection;
        if (client is not null)
        {
            _client = client;
            return;
        }
        var uri = new Uri(url);
        var port = uri.Port is > 0 and not 80 and not 443 ? uri.Port : 6334;
        _client = new QdrantClient(uri.Host, port, https: uri.Scheme == "https");
    }

    public void Upsert(IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors)
    {
        if (chunks.Count == 0)
            return;
        EnsureCollectionAsync(vectors[0].Length).GetAwaiter().GetResult();

        var points = new List<PointStruct>(chunks.Count);
        for (var i = 0; i < chunks.Count; i++)
        {
            var id = string.IsNullOrEmpty(chunks[i].Id) ? Guid.NewGuid() : Guid.Parse(chunks[i].Id);
            var point = new PointStruct { Id = id, Vectors = vectors[i] };
            point.Payload["text"] = chunks[i].Text;
            point.Payload["source"] = chunks[i].Source;
            points.Add(point);
        }
        _client.UpsertAsync(_collection, points).GetAwaiter().GetResult();
    }

    public IReadOnlyList<Chunk> Query(IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0)
        => QueryAsync(vector, topK, scoreThreshold).GetAwaiter().GetResult();

    public void Delete(IReadOnlyList<string> ids)
    {
        var guids = ids.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList();
        if (guids.Count > 0)
            _client.DeleteAsync(_collection, guids).GetAwaiter().GetResult();
    }

    private async Task EnsureCollectionAsync(int dimension)
    {
        if (_collectionReady)
            return;
        if (!await _client.CollectionExistsAsync(_collection).ConfigureAwait(false))
            await _client.CreateCollectionAsync(
                _collection,
                new VectorParams { Size = (ulong)dimension, Distance = Distance.Cosine }).ConfigureAwait(false);
        _collectionReady = true;
    }

    private async Task<IReadOnlyList<Chunk>> QueryAsync(IReadOnlyList<float> vector, int topK, double scoreThreshold)
    {
        IReadOnlyList<ScoredPoint> hits;
        try
        {
            hits = await _client.QueryAsync(
                _collection,
                query: vector.ToArray(),
                limit: (ulong)topK,
                scoreThreshold: (float)scoreThreshold,
                payloadSelector: true).ConfigureAwait(false);
        }
        catch (Grpc.Core.RpcException)
        {
            // Collection not created yet (no writes) → nothing to return.
            return Array.Empty<Chunk>();
        }

        return hits.Select(h => new Chunk(
            Text: h.Payload.TryGetValue("text", out var text) ? text.StringValue : "",
            Source: h.Payload.TryGetValue("source", out var source) ? source.StringValue : "",
            Score: h.Score,
            Id: h.Id.Uuid)).ToList();
    }
}
