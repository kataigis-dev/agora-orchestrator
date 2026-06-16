namespace Agora.Orchestration;

public sealed class Graph
{
    public const string End = "END";

    public required string Entry { get; init; }
    public required IReadOnlyDictionary<string, Node> Nodes { get; init; }
    public required IReadOnlyList<Edge> Edges { get; init; }
}
