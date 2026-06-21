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

    /// <summary>Spec traceability/completion summary, when a spec store is configured; null otherwise.
    /// Lets a run be measured on whether its committed scope was actually traced and verified, not only
    /// on whether the graph reached END.</summary>
    public TraceabilitySummary? Traceability { get; init; }

    /// <summary>A compact one-line summary for logs.</summary>
    public string ToSummary()
    {
        var s = $"steps={Steps} completed={Completed} rework={ReworkCount} forks={ParallelForks} "
            + $"tokens={InputTokens}in/{OutputTokens}out cache={CacheHitRate:P0} {Duration.TotalSeconds:F1}s";
        return Traceability is null ? s : $"{s} {Traceability.ToSummary()}";
    }
}

/// <summary>A presentation-free summary of spec completeness for a run, derived from a
/// <see cref="Agora.Specs.TraceabilityReport"/>. Surfaced on <see cref="RunMetrics.Traceability"/> so a
/// caller (or CI) can gate on whether the committed scope was covered and verified, deterministically.</summary>
public sealed record TraceabilitySummary
{
    /// <summary>Requirements in the committed scope (approved or beyond).</summary>
    public int Requirements { get; init; }

    /// <summary>In-scope requirements with at least one implementing task.</summary>
    public int Covered { get; init; }

    /// <summary>In-scope requirements that are verified.</summary>
    public int Verified { get; init; }

    /// <summary>Total implementation tasks.</summary>
    public int Tasks { get; init; }

    /// <summary>Whether the completion gate passes (every in-scope requirement covered and verified).</summary>
    public bool Complete { get; init; }

    /// <summary>Fraction of in-scope requirements covered (0 when nothing is in scope).</summary>
    public double CoverageRate => Requirements > 0 ? (double)Covered / Requirements : 0;

    /// <summary>Fraction of in-scope requirements verified (0 when nothing is in scope).</summary>
    public double VerificationRate => Requirements > 0 ? (double)Verified / Requirements : 0;

    /// <summary>A compact one-line summary for logs.</summary>
    public string ToSummary() =>
        $"spec={Verified}/{Requirements} verified, {Covered}/{Requirements} covered, complete={Complete}";
}
