namespace Agora.Configuration;

/// <summary>Optional explicit declaration of a graph node.</summary>
public sealed class GraphNodeConfig
{
    /// <summary>Node id (matches an agent id).</summary>
    public string Id { get; set; } = "";

    /// <summary>Node kind (currently <c>agent</c>).</summary>
    public string Type { get; set; } = "agent";
}

/// <summary>A directed edge between two graph nodes.</summary>
public sealed class GraphEdgeConfig
{
    /// <summary>Source node id.</summary>
    public string From { get; set; } = "";

    /// <summary>Target node id, or <c>END</c> to terminate.</summary>
    public string To { get; set; } = "";

    /// <summary>Edge type: <c>sequential</c>, <c>handoff</c>, <c>conditional</c>, <c>route</c>, or <c>parallel</c>.</summary>
    public string Type { get; set; } = "sequential";

    /// <summary>Signal/condition that activates a <c>conditional</c> or <c>route</c> edge.</summary>
    public string? When { get; set; }

    /// <summary>Maximum times a <c>conditional</c> loop edge may fire before it is disabled.</summary>
    public int? MaxLoops { get; set; }
}

/// <summary>The execution graph: an entry node plus its edges (and optional explicit nodes).</summary>
public sealed class GraphConfig
{
    /// <summary>Id of the first node to run.</summary>
    public string Entry { get; set; } = "";

    /// <summary>Optional explicit node declarations (nodes are otherwise inferred from edges).</summary>
    public List<GraphNodeConfig> Nodes { get; set; } = new();

    /// <summary>The directed edges connecting the nodes.</summary>
    public List<GraphEdgeConfig> Edges { get; set; } = new();
}
