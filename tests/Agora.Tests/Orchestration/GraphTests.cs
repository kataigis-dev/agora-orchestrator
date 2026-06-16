using Agora.Configuration;
using Agora.Orchestration;
using Xunit;

namespace Agora.Tests.Orchestration;

public class GraphTests
{
    private static AgoraConfig Config(GraphConfig graph) => new()
    {
        Models = new() { ["m"] = new ModelConfig { Provider = "anthropic", Model = "x" } },
        Agents = new()
        {
            ["planner"] = new AgentConfig { Model = "m" },
            ["writer"] = new AgentConfig { Model = "m" },
        },
        Graph = graph,
    };

    [Fact]
    public void Build_ParsesNodesAndEdges()
    {
        var g = GraphBuilder.Build(Config(new GraphConfig
        {
            Entry = "planner",
            Edges = new()
            {
                new GraphEdgeConfig { From = "planner", To = "writer", Type = "sequential" },
                new GraphEdgeConfig { From = "writer", To = "END", Type = "sequential" },
            },
        }));
        Assert.Equal("planner", g.Entry);
        Assert.Equal(new HashSet<string> { "planner", "writer" }, g.Nodes.Keys.ToHashSet());
        Assert.Equal("agent", g.Nodes["planner"].Type);
        Assert.Equal(2, g.Edges.Count);
        Assert.Equal(Graph.End, g.Edges[1].Target);
    }

    [Fact]
    public void Build_ReadsConditionalFields()
    {
        var g = GraphBuilder.Build(Config(new GraphConfig
        {
            Entry = "writer",
            Edges = new()
            {
                new GraphEdgeConfig { From = "writer", To = "writer", Type = "conditional", When = "needs_revision", MaxLoops = 2 },
            },
        }));
        var e = g.Edges[0];
        Assert.Equal("conditional", e.Type);
        Assert.Equal("needs_revision", e.When);
        Assert.Equal(2, e.MaxLoops);
    }

    [Fact]
    public void Build_WithoutGraph_Throws()
    {
        var cfg = new AgoraConfig
        {
            Models = new() { ["m"] = new ModelConfig { Provider = "anthropic", Model = "x" } },
            Agents = new() { ["a"] = new AgentConfig { Model = "m" } },
        };
        var ex = Assert.Throws<GraphError>(() => GraphBuilder.Build(cfg));
        Assert.Contains("no 'graph'", ex.Message);
    }

    [Fact]
    public void Validate_RejectsUnknownAgentNode()
    {
        var g = GraphBuilder.Build(Config(new GraphConfig
        {
            Entry = "ghost",
            Edges = new() { new GraphEdgeConfig { From = "ghost", To = "END" } },
        }));
        var ex = Assert.Throws<GraphError>(() => GraphBuilder.Validate(g, new HashSet<string> { "planner", "writer" }));
        Assert.Contains("no matching agent", ex.Message);
    }

    [Fact]
    public void Validate_RejectsConditionalWithoutWhen()
    {
        var g = GraphBuilder.Build(Config(new GraphConfig
        {
            Entry = "planner",
            Edges = new() { new GraphEdgeConfig { From = "planner", To = "writer", Type = "conditional" } },
        }));
        var ex = Assert.Throws<GraphError>(() => GraphBuilder.Validate(g, new HashSet<string> { "planner", "writer" }));
        Assert.Contains("needs a 'when'", ex.Message);
    }

    [Fact]
    public void Validate_AcceptsValidGraph()
    {
        var g = GraphBuilder.Build(Config(new GraphConfig
        {
            Entry = "planner",
            Edges = new()
            {
                new GraphEdgeConfig { From = "planner", To = "writer" },
                new GraphEdgeConfig { From = "writer", To = "END" },
            },
        }));
        GraphBuilder.Validate(g, new HashSet<string> { "planner", "writer" }); // must not throw
    }
}
