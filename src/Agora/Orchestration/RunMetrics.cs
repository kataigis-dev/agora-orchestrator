namespace Agora.Orchestration;

/// <summary>An aggregate, presentation-free summary of one graph run, collected by
/// <see cref="MetricsExecutionObserver"/>. Captures the signals reliability work cares about —
/// completion, rework/looping, work spread, and token cost (including cache hits) — so runs can be
/// compared across configurations instead of judged on a single final answer.</summary>
public sealed record RunMetrics
{
    /// <summary>Total node executions on the main path (the executor's step count).</summary>
    public int Steps { get; init; }

    /// <summary>Whether the run reached END (vs. aborting on the step limit or an error).</summary>
    public bool Completed { get; init; }

    /// <summary>Wall-clock duration of the run.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>How many times each node ran (includes parallel branches).</summary>
    public IReadOnlyDictionary<string, int> NodeVisits { get; init; } = new Dictionary<string, int>();

    /// <summary>Re-executions beyond each node's first run, summed — a proxy for fix/rework loops and
    /// thus a reliability red flag when high.</summary>
    public int ReworkCount { get; init; }

    /// <summary>Number of parallel fan-outs.</summary>
    public int ParallelForks { get; init; }

    /// <summary>How many times each control signal was emitted (e.g. <c>fix</c> vs <c>pass</c>).</summary>
    public IReadOnlyDictionary<string, int> Signals { get; init; } = new Dictionary<string, int>();

    /// <summary>Total prompt tokens consumed across all agent calls.</summary>
    public int InputTokens { get; init; }

    /// <summary>Total tokens generated across all agent calls.</summary>
    public int OutputTokens { get; init; }

    /// <summary>Prompt tokens served from cache (subset of <see cref="InputTokens"/>).</summary>
    public int CacheReadTokens { get; init; }

    /// <summary>Prompt tokens written to cache (cache creation).</summary>
    public int CacheWriteTokens { get; init; }

    /// <summary>Fraction of prompt tokens served from cache (0 when no input tokens) — the lever for
    /// verifying that prompt caching is actually taking effect.</summary>
    public double CacheHitRate => InputTokens > 0 ? (double)CacheReadTokens / InputTokens : 0;

    /// <summary>A compact one-line summary for logs.</summary>
    public string ToSummary() =>
        $"steps={Steps} completed={Completed} rework={ReworkCount} forks={ParallelForks} "
        + $"tokens={InputTokens}in/{OutputTokens}out cache={CacheHitRate:P0} {Duration.TotalSeconds:F1}s";
}
