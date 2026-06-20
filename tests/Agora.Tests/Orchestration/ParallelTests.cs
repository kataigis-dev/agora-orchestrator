using Agora.Agents;
using Agora.Orchestration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Orchestration;

public class ParallelTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "m", Provider = "x", Model = "x", Temperature = 0, MaxTokens = 50, Timeout = 5, Retries = 0,
    };

    private static Func<string, Agent> Factory(Dictionary<string, FakeChatProvider> providers) =>
        id => new Agent(new AgentCard { Id = id, Model = "m" }, providers[id], Spec());

    private static Graph ForkJoin(string aJoin, string bJoin) => new()
    {
        Entry = "planner",
        Nodes = new Dictionary<string, Node>
        {
            ["planner"] = new("planner"), ["a"] = new("a"), ["b"] = new("b"),
            ["synth"] = new("synth"), ["other"] = new("other"),
        },
        Edges = new[]
        {
            new Edge("planner", "a", "parallel"),
            new Edge("planner", "b", "parallel"),
            new Edge("a", aJoin),
            new Edge("b", bJoin),
            new Edge("synth", "END"),
            new Edge("other", "END"),
        },
    };

    [Fact]
    public async Task FanOut_RunsBranches_JoinsAndSynthesizes()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN" }),
            ["a"] = new(new[] { "RESULT_A" }),
            ["b"] = new(new[] { "RESULT_B" }),
            ["synth"] = new(new[] { "FINAL" }),
        };

        var state = await new GraphExecutor(ForkJoin("synth", "synth"), Factory(providers)).RunAsync("task");

        Assert.Equal("RESULT_A", state.Outputs["a"]);
        Assert.Equal("RESULT_B", state.Outputs["b"]);
        Assert.Equal("FINAL", state.Outputs["synth"]);
        Assert.Equal("synth", state.LastAgent);

        // The join sees BOTH branch outputs in its inbox.
        var synthMsg = providers["synth"].Calls[0].Messages[^1].Content;
        Assert.Contains("RESULT_A", synthMsg);
        Assert.Contains("RESULT_B", synthMsg);
    }

    [Fact]
    public async Task FanOut_BranchesSeeForkOutput_NotEachOther()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN" }),
            ["a"] = new(new[] { "RESULT_A" }),
            ["b"] = new(new[] { "RESULT_B" }),
            ["synth"] = new(new[] { "FINAL" }),
        };

        await new GraphExecutor(ForkJoin("synth", "synth"), Factory(providers)).RunAsync("task");

        var aMsg = providers["a"].Calls[0].Messages[^1].Content;
        Assert.Contains("PLAN", aMsg);
        Assert.DoesNotContain("RESULT_B", aMsg);
    }

    [Fact]
    public async Task FanOut_DivergentJoins_Throws()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN" }),
            ["a"] = new(new[] { "RESULT_A" }),
            ["b"] = new(new[] { "RESULT_B" }),
        };

        var ex = await Assert.ThrowsAsync<GraphError>(
            () => new GraphExecutor(ForkJoin("synth", "other"), Factory(providers)).RunAsync("task"));
        Assert.Contains("converge", ex.Message);
    }
}
