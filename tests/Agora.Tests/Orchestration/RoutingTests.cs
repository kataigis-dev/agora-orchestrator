using Agora.Agents;
using Agora.Orchestration;
using Agora.Providers;
using Xunit;

namespace Agora.Tests.Orchestration;

public class RoutingTests
{
    private static ModelSpec Spec() => new()
    {
        Alias = "m", Provider = "x", Model = "x", Temperature = 0, MaxTokens = 50, Timeout = 5, Retries = 0,
    };

    private static Func<string, Agent> Factory(Dictionary<string, FakeChatProvider> providers) =>
        id => new Agent(new AgentCard { Id = id, Model = "m" }, providers[id], Spec());

    private sealed class FixedRouter : IRouter
    {
        private readonly string _target;
        public FixedRouter(string target) => _target = target;
        public Task<string> ChooseAsync(string context, IReadOnlyList<RouteOption> options, CancellationToken ct = default)
            => Task.FromResult(_target);
    }

    [Fact]
    public async Task LlmRouter_PicksLabelFromReply()
    {
        var router = new LlmRouter(new FakeChatProvider(new[] { "support" }), Spec());
        var choice = await router.ChooseAsync("ctx",
            new[] { new RouteOption("refund", "wants money back"), new RouteOption("support", "needs help") });
        Assert.Equal("support", choice);
    }

    [Fact]
    public async Task LlmRouter_FallsBackToFirst_WhenReplyUnknown()
    {
        var router = new LlmRouter(new FakeChatProvider(new[] { "no idea" }), Spec());
        var choice = await router.ChooseAsync("ctx",
            new[] { new RouteOption("a", "x"), new RouteOption("b", "y") });
        Assert.Equal("a", choice);
    }

    [Fact]
    public async Task RouteEdges_FollowRouterChoice()
    {
        var providers = new Dictionary<string, FakeChatProvider>
        {
            ["classifier"] = new(new[] { "the customer needs help" }),
            ["support"] = new(new[] { "SUPPORTED" }),
            ["refund"] = new(new[] { "REFUNDED" }),
        };
        var graph = new Graph
        {
            Entry = "classifier",
            Nodes = new Dictionary<string, Node>
            {
                ["classifier"] = new("classifier"), ["refund"] = new("refund"), ["support"] = new("support"),
            },
            Edges = new[]
            {
                new Edge("classifier", "refund", "route", "customer wants money back"),
                new Edge("classifier", "support", "route", "customer needs help"),
                new Edge("refund", "END"),
                new Edge("support", "END"),
            },
        };

        var state = await new GraphExecutor(graph, Factory(providers), router: new FixedRouter("support"))
            .RunAsync("help me please");

        Assert.Equal("support", state.LastAgent);
        Assert.Equal("SUPPORTED", state.Outputs["support"]);
        Assert.False(state.Outputs.ContainsKey("refund"));
    }
}
