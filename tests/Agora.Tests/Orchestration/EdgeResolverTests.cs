using Agora.Orchestration.Concretes;
using Agora.Orchestration.Models;
using Xunit;

namespace Agora.Tests.Orchestration;

public class EdgeResolverTests
{
    private static readonly Dictionary<string, int> NoCounters = new();
    private static Dictionary<string, object> Signals(params (string Key, object Value)[] pairs)
        => pairs.ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public void Unconditional_TakesFirstMatchingEdge()
    {
        var edges = new[] { new Edge("a", "b") };
        var decision = EdgeResolver.Next(edges, "a", Signals(), NoCounters);
        Assert.Equal("b", decision.Next);
    }

    [Fact]
    public void NoMatchingEdge_ReturnsEnd()
    {
        var edges = new[] { new Edge("x", "y") };
        var decision = EdgeResolver.Next(edges, "a", Signals(), NoCounters);
        Assert.Equal(Graph.End, decision.Next);
    }

    [Fact]
    public void Conditional_TakenWhenTruthy_ElseFallsThroughToUnconditional()
    {
        var edges = new[]
        {
            new Edge("a", "fix", "conditional", "retry"),
            new Edge("a", "done"),
        };
        Assert.Equal("fix", EdgeResolver.Next(edges, "a", Signals(("retry", true)), NoCounters).Next);
        Assert.Equal("done", EdgeResolver.Next(edges, "a", Signals(), NoCounters).Next);
    }

    [Fact]
    public void Conditional_MaxLoops_IncrementsCounter_WithoutMutatingInput()
    {
        var edges = new[] { new Edge("r", "g", "conditional", "again", 2) };
        var counters = new Dictionary<string, int>();

        var decision = EdgeResolver.Next(edges, "r", Signals(("again", true)), counters);

        Assert.Equal("g", decision.Next);
        Assert.Equal(1, decision.LoopCounters["r->g"]);
        Assert.Empty(counters); // the input dictionary is left untouched — the decision is pure
    }

    [Fact]
    public void Conditional_MaxLoops_StopsAtCap_FallsThrough()
    {
        var edges = new[]
        {
            new Edge("r", "g", "conditional", "again", 2),
            new Edge("r", Graph.End),
        };
        var atCap = new Dictionary<string, int> { ["r->g"] = 2 };

        var decision = EdgeResolver.Next(edges, "r", Signals(("again", true)), atCap);

        Assert.Equal(Graph.End, decision.Next);
        Assert.Same(atCap, decision.LoopCounters); // no edge taken → counters returned unchanged
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData("yes", true)]
    [InlineData("", false)]
    public void IsTruthy_BoolAndStringSemantics(object value, bool expected)
        => Assert.Equal(expected, EdgeResolver.IsTruthy(Signals(("k", value)), "k"));

    [Fact]
    public void IsTruthy_MissingOrNullKey_IsFalse()
    {
        Assert.False(EdgeResolver.IsTruthy(Signals(), "k"));
        Assert.False(EdgeResolver.IsTruthy(Signals(("k", true)), null));
    }
}
