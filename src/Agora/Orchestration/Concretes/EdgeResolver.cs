using Agora.Orchestration.Models;

namespace Agora.Orchestration.Concretes;

/// <summary>
/// Pure signal-based edge resolution. Given a node's outgoing edges plus the current signals and loop
/// counters, it decides the next node and the loop counters that result from taking the chosen edge —
/// without mutating anything. The executor threads the returned counters back into its shared state.
/// Keeping this a function (no side effects) makes routing — including the <c>max_loops</c> cap —
/// testable through one interface, separate from the orchestration that drives it.
/// </summary>
public static class EdgeResolver
{
    /// <summary>The next node to run and the loop counters after applying the chosen edge. When no
    /// conditional edge with <c>max_loops</c> is taken, <see cref="LoopCounters"/> is the same instance
    /// that was passed in.</summary>
    public readonly record struct Decision(string Next, IReadOnlyDictionary<string, int> LoopCounters);

    /// <summary>Picks the next node by signals: the first matching conditional edge (honoring
    /// <c>max_loops</c>) or the first unconditional edge; <see cref="Graph.End"/> if none apply.
    /// Taking a conditional edge with <c>max_loops</c> returns counters with that edge's counter
    /// incremented; otherwise the counters are returned unchanged.</summary>
    public static Decision Next(
        IReadOnlyList<Edge> edges, string source,
        IReadOnlyDictionary<string, object> signals, IReadOnlyDictionary<string, int> loopCounters)
    {
        foreach (var edge in edges)
        {
            if (edge.Source != source) continue;
            if (edge.Type == "conditional")
            {
                if (!IsTruthy(signals, edge.When)) continue;
                if (edge.MaxLoops is int max)
                {
                    var key = $"{edge.Source}->{edge.Target}";
                    var count = loopCounters.GetValueOrDefault(key, 0);
                    if (count >= max) continue;
                    var updated = new Dictionary<string, int>(loopCounters) { [key] = count + 1 };
                    return new Decision(edge.Target, updated);
                }
                return new Decision(edge.Target, loopCounters);
            }
            return new Decision(edge.Target, loopCounters);
        }
        return new Decision(Graph.End, loopCounters);
    }

    /// <summary>True when the named signal is present and truthy (non-false bool, non-empty string).</summary>
    public static bool IsTruthy(IReadOnlyDictionary<string, object> signals, string? key)
    {
        if (key is null || !signals.TryGetValue(key, out var value))
            return false;
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            _ => true,
        };
    }
}
