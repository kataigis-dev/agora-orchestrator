namespace Agora.Orchestration;

/// <summary>A validated, immutable execution graph: nodes plus the edges connecting them.</summary>
public sealed class Graph
{
    /// <summary>Sentinel target id that terminates the run.</summary>
    public const string End = "END";

    /// <summary>Id of the entry node.</summary>
    public required string Entry { get; init; }

    /// <summary>Nodes keyed by id.</summary>
    public required IReadOnlyDictionary<string, Node> Nodes { get; init; }

    /// <summary>Directed edges between nodes.</summary>
    public required IReadOnlyList<Edge> Edges { get; init; }
}
