namespace Agora.Rag;

public sealed record RefinedQuery
{
    public required string Query { get; init; }
    public IReadOnlyList<string> SubQueries { get; init; } = Array.Empty<string>();
}
