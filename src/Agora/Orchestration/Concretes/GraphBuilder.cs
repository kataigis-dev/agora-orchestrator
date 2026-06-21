using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using Agora.Configuration;

namespace Agora.Orchestration.Concretes;

/// <summary>Builds and validates a <see cref="Graph"/> from the config's <c>graph</c> section.</summary>
public static class GraphBuilder
{
    /// <summary>Builds the graph, inferring agent nodes referenced by the entry or edges but not
    /// explicitly declared.</summary>
    /// <exception cref="GraphError">If the graph section is missing or malformed.</exception>
    public static Graph Build(AgoraConfig config)
    {
        var raw = config.Graph ?? throw new GraphError("config has no 'graph' section");
        if (string.IsNullOrEmpty(raw.Entry))
            throw new GraphError("graph must define an 'entry' node");

        var nodes = new Dictionary<string, Node>();
        foreach (var n in raw.Nodes)
        {
            if (string.IsNullOrEmpty(n.Id))
                throw new GraphError("each graph node must have an 'id'");
            nodes[n.Id] = new Node(n.Id, n.Type);
        }

        var edges = new List<Edge>();
        foreach (var e in raw.Edges)
        {
            if (string.IsNullOrEmpty(e.From) || string.IsNullOrEmpty(e.To))
                throw new GraphError("edge missing 'from'/'to'");
            edges.Add(new Edge(e.From, e.To, e.Type, e.When, e.MaxLoops));
        }

        // Infer agent nodes referenced by entry/edges but not explicitly declared.
        var referenced = new HashSet<string> { raw.Entry };
        foreach (var e in edges)
        {
            referenced.Add(e.Source);
            referenced.Add(e.Target);
        }
        foreach (var id in referenced)
        {
            if (id == Graph.End) continue;
            if (!nodes.ContainsKey(id)) nodes[id] = new Node(id);
        }

        return new Graph { Entry = raw.Entry, Nodes = nodes, Edges = edges };
    }

    /// <summary>Verifies the entry exists, every agent node maps to a configured agent, and all edge
    /// endpoints (and conditional <c>when</c> fields) are valid.</summary>
    /// <exception cref="GraphError">If any check fails.</exception>
    public static void Validate(Graph graph, IReadOnlySet<string> agentIds)
    {
        if (!graph.Nodes.ContainsKey(graph.Entry))
            throw new GraphError($"entry node '{graph.Entry}' is not defined");

        foreach (var (id, node) in graph.Nodes)
        {
            if (node.Type == "agent" && !agentIds.Contains(id))
                throw new GraphError($"graph node '{id}' has no matching agent in config");
        }

        foreach (var e in graph.Edges)
        {
            if (!graph.Nodes.ContainsKey(e.Source))
                throw new GraphError($"edge source '{e.Source}' is not a known node");
            if (e.Target != Graph.End && !graph.Nodes.ContainsKey(e.Target))
                throw new GraphError($"edge target '{e.Target}' is not a known node or END");
            if (e.Type == "conditional" && string.IsNullOrEmpty(e.When))
                throw new GraphError($"conditional edge {e.Source}->{e.Target} needs a 'when'");
        }
    }
}
