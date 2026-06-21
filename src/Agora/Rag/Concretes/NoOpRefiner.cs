using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
namespace Agora.Rag.Concretes;

/// <summary>Pass-through refiner: returns the input text unchanged as the query.</summary>
public sealed class NoOpRefiner : IRefiner
{
    /// <inheritdoc />
    public Task<RefinedQuery> RefineAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(new RefinedQuery { Query = text });
}
