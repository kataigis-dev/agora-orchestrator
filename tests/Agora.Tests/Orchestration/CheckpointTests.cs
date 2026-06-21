using System.Text.Json;
using Agora.Agents.Contracts;
using Agora.Agents.Models;
using Agora.Agents.Concretes;
using Agora.Orchestration.Contracts;
using Agora.Orchestration.Models;
using Agora.Orchestration.Concretes;
using Agora.Providers.Contracts;
using Agora.Providers.Models;
using Agora.Providers.Concretes;
using Xunit;

namespace Agora.Tests.Orchestration;

public class CheckpointTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "m", Provider = "x", Model = "x", Temperature = 0, MaxTokens = 50, Timeout = 5, Retries = 0,
    };

    private static Func<string, Agent> Factory(Dictionary<string, FakeChatProvider> providers) =>
        id => new Agent(new AgentCard { Id = id, Model = "m" }, providers[id], Spec());

    private static Graph Graph2() => new()
    {
        Entry = "planner",
        Nodes = new Dictionary<string, Node> { ["planner"] = new("planner"), ["writer"] = new("writer") },
        Edges = new[] { new Edge("planner", "writer"), new Edge("writer", "END") },
    };

    [Fact]
    public void Snapshot_RoundTripsThroughJson_PreservingSignalTypes()
    {
        var state = new State("task") { LastAgent = "planner" };
        state.Outputs["planner"] = "PLAN";
        state.Signals["done"] = true;
        state.Signals["reason"] = "looks good";
        state.Artifacts["plan"] = "do X then Y";
        state.LoopCounters["critic->writer"] = 2;
        state.Messages.Add(new Message("planner", "writer", "PLAN"));

        var json = JsonSerializer.Serialize(StateSnapshot.From(state, "writer", 1));
        var restored = JsonSerializer.Deserialize<StateSnapshot>(json)!.ToState();

        Assert.Equal("PLAN", restored.Outputs["planner"]);
        Assert.Equal(true, restored.Signals["done"]);       // bool preserved
        Assert.Equal("looks good", restored.Signals["reason"]); // string preserved
        Assert.Equal("do X then Y", restored.Artifacts["plan"]);
        Assert.Equal(2, restored.LoopCounters["critic->writer"]);
        Assert.Equal("planner", restored.LastAgent);
        Assert.Single(restored.Messages);
    }

    [Fact]
    public async Task Run_WithCheckpoints_SavesPerStep()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN" }),
            ["writer"] = new(new[] { "FINAL" }),
        };
        var store = new InMemoryCheckpointStore();

        await new GraphExecutor(Graph2(), Factory(providers), checkpoints: store, runId: "r1").RunAsync("task");

        Assert.True(store.Saves >= 2);
        Assert.Equal(Graph.End, store.Load("r1")!.Current);
    }

    [Fact]
    public async Task Resume_ContinuesFromSnapshot_SkippingCompletedNodes()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(@default: "SHOULD-NOT-RUN"),
            ["writer"] = new(new[] { "FINAL" }),
        };
        var snapshot = new StateSnapshot
        {
            Current = "writer",
            Steps = 1,
            UserInput = "task",
            Outputs = { ["planner"] = "PLAN" },
            Messages = { new Message("planner", "writer", "PLAN") },
        };

        var state = await new GraphExecutor(Graph2(), Factory(providers)).RunAsync("task", resumeFrom: snapshot);

        Assert.Equal("FINAL", state.Outputs["writer"]);
        Assert.Equal("PLAN", state.Outputs["planner"]); // carried from the snapshot, not recomputed
        Assert.Empty(providers["planner"].Calls);       // planner was not re-run
        // the writer saw the carried-over inbox message
        Assert.Contains("PLAN", providers["writer"].Calls[0].Messages[^1].Content);
    }
}
