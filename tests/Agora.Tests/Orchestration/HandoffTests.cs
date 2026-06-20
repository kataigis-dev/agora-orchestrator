using Agora.Agents;
using Agora.Orchestration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Orchestration;

public class HandoffTests
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
        Edges = new[] { new Edge("planner", "writer", "handoff"), new Edge("writer", "END") },
    };

    [Fact]
    public async Task HandoffMode_PassesOnlyHandoffArtifact_NotFullOutput()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "lots of verbose reasoning <<artifact handoff=use JWT auth>>" }),
            ["writer"] = new(new[] { "FINAL" }),
        };
        var state = await new GraphExecutor(Graph2(), Factory(providers), handoff: true).RunAsync("do the task");

        var writerUserMsg = providers["writer"].Calls[0].Messages[^1].Content;
        Assert.Equal("use JWT auth\n\ndo the task", writerUserMsg);
        Assert.DoesNotContain("verbose reasoning", writerUserMsg);
        // The handoff is a targeted channel, not part of the global shared artifacts.
        Assert.DoesNotContain("handoff", state.Artifacts.Keys);
    }

    [Fact]
    public async Task HandoffMode_NoHandoffArtifact_NextAgentGetsNoInbox()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN with no handoff" }),
            ["writer"] = new(new[] { "FINAL" }),
        };
        var state = await new GraphExecutor(Graph2(), Factory(providers), handoff: true).RunAsync("do the task");

        Assert.Equal("do the task", providers["writer"].Calls[0].Messages[^1].Content);
        Assert.DoesNotContain(state.Messages, m => m.Sender == "planner" && m.Recipient == "writer");
    }

    [Fact]
    public async Task DefaultMode_StillPassesFullOutput()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["planner"] = new(new[] { "PLAN" }),
            ["writer"] = new(new[] { "FINAL" }),
        };
        var state = await new GraphExecutor(Graph2(), Factory(providers)).RunAsync("do the task");

        Assert.Equal("PLAN\n\ndo the task", providers["writer"].Calls[0].Messages[^1].Content);
    }
}
