using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
namespace Agora.Rag.Contracts;

/// <summary>Rewrites/decomposes a raw query into a better retrieval query before search.</summary>
public interface IRefiner
{
    /// <summary>Produces a refined query (and optional sub-queries) from the input text.</summary>
    Task<RefinedQuery> RefineAsync(string text, CancellationToken cancellationToken = default);
}
