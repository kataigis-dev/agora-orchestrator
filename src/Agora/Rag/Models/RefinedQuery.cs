using Agora.Rag.Contracts;
using Agora.Rag.Models;
using Agora.Rag.Concretes;
namespace Agora.Rag.Models;

/// <summary>A refined retrieval query plus any decomposed sub-queries.</summary>
public sealed record RefinedQuery
{
    /// <summary>The rewritten query used for retrieval.</summary>
    public required string Query { get; init; }

    /// <summary>Optional sub-queries the original was decomposed into.</summary>
    public IReadOnlyList<string> SubQueries { get; init; } = Array.Empty<string>();
}
