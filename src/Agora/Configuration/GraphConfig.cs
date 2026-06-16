namespace Agora.Configuration;

public sealed class GraphNodeConfig
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "agent";
}

public sealed class GraphEdgeConfig
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Type { get; set; } = "sequential";
    public string? When { get; set; }
    public int? MaxLoops { get; set; }
}

public sealed class GraphConfig
{
    public string Entry { get; set; } = "";
    public List<GraphNodeConfig> Nodes { get; set; } = new();
    public List<GraphEdgeConfig> Edges { get; set; } = new();
}
