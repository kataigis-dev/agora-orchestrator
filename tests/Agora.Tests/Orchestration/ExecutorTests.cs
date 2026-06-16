using Agora.Agents;
using Agora.Orchestration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Orchestration;

public class ExecutorTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "m", Provider = "x", Model = "x", Temperature = 0, MaxTokens = 50, Timeout = 5, Retries = 0,
    };

    private static Func<string, Agent> Factory(Dictionary<string, FakeChatProvider> providers) =>
        id => new Agent(new AgentCard { Id = id, Model = "m" }, providers[id], Spec());

    private static Graph Graph2(params Edge[] edges) => new()
    {
        Entry = "planner",
        Nodes = new Dictionary<string, Node> { ["planner"] = new("planner"), ["writer"] = new("writer") },
        Edges = edges,
    };

    [Fact]
    public async Task SequentialFlow_RecordsOutputs_AndTerminates()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN" }),
            ["writer"] = new(new[] { "FINAL" }),
        };
        var graph = Graph2(new Edge("planner", "writer"), new Edge("writer", "END"));
        var state = await new GraphExecutor(graph, Factory(providers)).RunAsync("do the task");

        Assert.Equal("PLAN", state.Outputs["planner"]);
        Assert.Equal("FINAL", state.Outputs["writer"]);
        Assert.Equal("writer", state.LastAgent);
    }

    [Fact]
    public async Task Handoff_DeliversPreviousOutputToNextInbox()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN" }),
            ["writer"] = new(new[] { "FINAL" }),
        };
        var graph = Graph2(new Edge("planner", "writer", "handoff"), new Edge("writer", "END"));
        var state = await new GraphExecutor(graph, Factory(providers)).RunAsync("do the task");

        var writerUserMsg = providers["writer"].Calls[0].Messages[^1].Content;
        Assert.Equal("PLAN\n\ndo the task", writerUserMsg);
        Assert.Contains(state.Messages, m => m.Sender == "planner" && m.Recipient == "writer" && m.Content == "PLAN");
    }

    [Fact]
    public async Task MaxSteps_Exceeded_Throws()
    {
        var providers = new Dictionary<string, FakeChatProvider> { ["planner"] = new(@default: "X") };
        var graph = new Graph
        {
            Entry = "planner",
            Nodes = new Dictionary<string, Node> { ["planner"] = new("planner") },
            Edges = new[] { new Edge("planner", "planner") },
        };
        var ex = await Assert.ThrowsAsync<ExecutionError>(
            () => new GraphExecutor(graph, Factory(providers), maxSteps: 5).RunAsync("go"));
        Assert.Contains("max_steps", ex.Message);
    }

    private static Graph CriticLoopGraph() => new()
    {
        Entry = "writer",
        Nodes = new Dictionary<string, Node> { ["writer"] = new("writer"), ["critic"] = new("critic") },
        Edges = new[]
        {
            new Edge("writer", "critic"),
            new Edge("critic", "writer", "conditional", "needs_revision", 2),
            new Edge("critic", "END", "conditional", "approved"),
        },
    };

    [Fact]
    public async Task Conditional_Approved_ExitsAfterOneRevision()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["writer"] = new(new[] { "DRAFT1", "DRAFT2" }),
            ["critic"] = new(new[] { "<<signal needs_revision>>", "<<signal approved>>" }),
        };
        var state = await new GraphExecutor(CriticLoopGraph(), Factory(providers)).RunAsync("write it");

        Assert.Equal(1, state.LoopCounters["critic->writer"]);
        Assert.Equal("DRAFT2", state.Outputs["writer"]);
        Assert.Equal("critic", state.LastAgent);
    }

    [Fact]
    public async Task LoopGuard_CapsRevisionsAtMaxLoops()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["writer"] = new(@default: "DRAFT"),
            ["critic"] = new(@default: "<<signal needs_revision>>"),
        };
        var state = await new GraphExecutor(CriticLoopGraph(), Factory(providers), maxSteps: 50).RunAsync("write it");

        Assert.Equal(2, state.LoopCounters["critic->writer"]);
    }
}
