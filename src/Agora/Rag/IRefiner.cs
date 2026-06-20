namespace Agora.Rag;

/// <summary>Rewrites/decomposes a raw query into a better retrieval query before search.</summary>
public interface IRefiner
{
    /// <summary>Produces a refined query (and optional sub-queries) from the input text.</summary>
    Task<RefinedQuery> RefineAsync(string text, CancellationToken cancellationToken = default);
}
