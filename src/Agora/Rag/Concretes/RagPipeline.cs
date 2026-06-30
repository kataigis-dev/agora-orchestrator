using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
namespace Agora.Rag.Concretes;

/// <summary>Refine the query, retrieve context, and bundle an EnrichedInput.</summary>
public sealed class RagPipeline
{
    private readonly IRefiner _refiner;
    private readonly int _topK;
    private readonly double _scoreThreshold;

    /// <summary>Creates the pipeline from its refiner, embedder, store, and query parameters.</summary>
    public RagPipeline(
        IRefiner refiner, IEmbedder embedder, IVectorStore store, int topK = 6,
        double scoreThreshold = Configuration.RetrievalConfig.DefaultScoreThreshold)
    {
        _refiner = refiner;
        Embedder = embedder;
        Store = store;
        _topK = topK;
        _scoreThreshold = scoreThreshold;
    }

    /// <summary>The embedder, used for the pipeline's own retrieval. Not part of the public surface —
    /// the <see cref="Retrieval"/> module owns sharing one embedder across pipeline, knowledge base,
    /// and memory.</summary>
    internal IEmbedder Embedder { get; }

    /// <summary>The vector store, used for the pipeline's own retrieval. Not part of the public surface
    /// (see <see cref="Retrieval"/>).</summary>
    internal IVectorStore Store { get; }

    /// <summary>Refines the query, retrieves and de-duplicates the top chunks across all sub-queries,
    /// and returns them bundled with the original input.</summary>
    public async Task<EnrichedInput> RunAsync(string text, CancellationToken cancellationToken = default)
    {
        var refined = await _refiner.RefineAsync(text, cancellationToken);
        var queries = new List<string> { refined.Query };
        queries.AddRange(refined.SubQueries);
        var vectors = await Embedder.EmbedAsync(queries, cancellationToken);

        var seen = new HashSet<(string Source, string Text)>();
        var hits = new List<Chunk>();
        foreach (var vector in vectors)
        {
            foreach (var chunk in await Store.QueryAsync(vector, _topK, _scoreThreshold, cancellationToken))
            {
                if (seen.Add((chunk.Source, chunk.Text)))
                    hits.Add(chunk);
            }
        }
        hits.Sort((a, b) => b.Score.CompareTo(a.Score));

        return new EnrichedInput
        {
            Original = text,
            RefinedQuery = refined.Query,
            SubQueries = refined.SubQueries,
            Retrieved = hits.Take(_topK).ToList(),
        };
    }
}
