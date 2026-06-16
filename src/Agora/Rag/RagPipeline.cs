namespace Agora.Rag;

/// <summary>Refine the query, retrieve context, and bundle an EnrichedInput.</summary>
public sealed class RagPipeline
{
    private readonly IRefiner _refiner;
    private readonly int _topK;
    private readonly double _scoreThreshold;

    public RagPipeline(
        IRefiner refiner, IEmbedder embedder, IVectorStore store, int topK = 6, double scoreThreshold = 0.0)
    {
        _refiner = refiner;
        Embedder = embedder;
        Store = store;
        _topK = topK;
        _scoreThreshold = scoreThreshold;
    }

    public IEmbedder Embedder { get; }
    public IVectorStore Store { get; }

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
            foreach (var chunk in Store.Query(vector, _topK, _scoreThreshold))
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
