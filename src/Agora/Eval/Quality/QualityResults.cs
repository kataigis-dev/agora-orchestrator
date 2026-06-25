using Agora.Orchestration.Models;

namespace Agora.Eval.Quality;

/// <summary>The outcome of grading one <see cref="QualityScenario"/>.</summary>
public sealed record QualityScenarioResult
{
    /// <summary>The scenario id.</summary>
    public required string Id { get; init; }

    /// <summary>False when a deterministic hard gate failed (or the run errored before checks).</summary>
    public bool DeterministicPassed { get; init; }

    /// <summary>Deterministic checks that failed, if any.</summary>
    public IReadOnlyList<string> DeterministicFailures { get; init; } = Array.Empty<string>();

    /// <summary>Per-dimension scores (empty when gated out or on an evaluation error).</summary>
    public IReadOnlyDictionary<string, double> Dimensions { get; init; } = new Dictionary<string, double>();

    /// <summary>Unweighted mean of the applicable dimensions; 0 when a deterministic gate failed; null on an
    /// evaluation error (unparseable verdict / run failure) so the case is excluded from the suite mean.</summary>
    public double? Aggregate { get; init; }

    /// <summary>Set when the case could not be evaluated (fail-closed judge error or a run error).</summary>
    public string? Error { get; init; }

    /// <summary>The run's cost/stability metrics, surfaced beside quality for config comparison.</summary>
    public RunMetrics? Metrics { get; init; }

    /// <summary>The human-labeled expected aggregate, when the case carried one.</summary>
    public double? ExpectedScore { get; init; }
}

/// <summary>Aggregate over a suite of scenarios.</summary>
public sealed record QualitySuiteResult
{
    /// <summary>Per-scenario results in input order.</summary>
    public required IReadOnlyList<QualityScenarioResult> Scenarios { get; init; }

    /// <summary>Mean aggregate over evaluated cases (deterministic-failed count as 0; errors excluded).</summary>
    public double SuiteScore { get; init; }

    /// <summary>Mean score per dimension across the cases where that dimension applied.</summary>
    public IReadOnlyDictionary<string, double> DimensionMeans { get; init; } = new Dictionary<string, double>();

    /// <summary>Mean absolute error of the aggregate vs the human label, over labeled cases (null when none).</summary>
    public double? JudgeHumanMae { get; init; }

    /// <summary>How many cases carried a human label.</summary>
    public int LabeledCases { get; init; }

    /// <summary>How many cases could not be evaluated.</summary>
    public int ErrorCases { get; init; }
}
