using Agora.Agents.Contracts;
using Agora.Orchestration.Models;
using Agora.Providers.Contracts;
using Agora.Rag.Models;

namespace Agora.Eval.Quality;

/// <summary>
/// Runs quality scenarios against a real provider, applies deterministic hard gates, grades the rest with
/// an <see cref="IJudge"/>, and aggregates. Provider-agnostic: the system-under-test provider and the judge
/// are injected, so the logic is fully exercisable offline with fakes.
/// </summary>
public static class QualityRunner
{
    private const string GroundednessCriteria =
        "Every claim in the answer is supported by the provided context; nothing is invented or unsupported.";

    /// <summary>Runs one scenario end to end and returns its graded result. A <paramref name="backend"/>
    /// is required for configs that use real embedders or tools (RAG / tool agents).</summary>
    public static async Task<QualityScenarioResult> RunScenarioAsync(
        QualityScenario scenario, IChatProvider sut, IJudge judge, IAgentBackend? backend = null,
        CancellationToken cancellationToken = default)
    {
        string output;
        IReadOnlyDictionary<string, object> signals;
        EnrichedInput? enriched = null;
        RunMetrics? metrics = null;
        try
        {
            var configPath = Path.GetFullPath(scenario.Config);
            var runtime = Runtime.FromConfig(configPath, sut, backend: backend);
            if (scenario.Graph)
            {
                var r = await runtime.RunAsync(scenario.Input);
                output = r.Output;
                signals = r.State.Signals;
                enriched = r.Enriched;
                metrics = r.Metrics;
            }
            else
            {
                var agentId = scenario.Agent
                    ?? throw new ArgumentException("scenario with graph=false must set 'agent'");
                var r = await runtime.RunAgentAsync(agentId, scenario.Input);
                output = r.Output;
                signals = r.State.Signals;
                metrics = r.Metrics;
            }
        }
        catch (Exception ex)
        {
            return new QualityScenarioResult
            {
                Id = scenario.Id,
                DeterministicPassed = false,
                Error = $"run failed: {ex.Message}",
                ExpectedScore = scenario.ExpectedScore,
            };
        }

        // Deterministic hard gate — a failure fails the case at 0, and the judge is skipped.
        var detFailures = DeterministicChecks.Run(scenario.Deterministic, output, signals);
        if (detFailures.Count > 0)
            return new QualityScenarioResult
            {
                Id = scenario.Id,
                DeterministicPassed = false,
                DeterministicFailures = detFailures,
                Aggregate = 0,
                Metrics = metrics,
                ExpectedScore = scenario.ExpectedScore,
            };

        // Build the rubric, auto-adding groundedness when the run retrieved context.
        var rubric = new Dictionary<string, string>(scenario.Rubric);
        var grounding = enriched is { Retrieved.Count: > 0 } ? enriched.AsContext() : null;
        if (grounding is not null && !rubric.ContainsKey("groundedness"))
            rubric["groundedness"] = GroundednessCriteria;

        // No rubric dimensions (deterministic-only case): the gate passing is the whole verdict.
        if (rubric.Count == 0)
            return new QualityScenarioResult
            {
                Id = scenario.Id,
                DeterministicPassed = true,
                Aggregate = 1,
                Metrics = metrics,
                ExpectedScore = scenario.ExpectedScore,
            };

        var verdict = await judge.JudgeAsync(
            new JudgeRequest
            {
                Input = scenario.Input,
                Output = output,
                Rubric = rubric,
                Reference = scenario.Reference,
                GroundingContext = grounding,
            },
            cancellationToken);

        // Fail-closed: an invalid or incomplete verdict is an evaluation error, never a pass.
        if (!verdict.Valid)
            return EvalError(scenario, metrics, $"judge verdict unusable (fail-closed): {verdict.Error}");

        var dims = rubric.Keys
            .Where(verdict.Dimensions.ContainsKey)
            .ToDictionary(k => k, k => verdict.Dimensions[k].Score);
        if (dims.Count != rubric.Count)
            return EvalError(scenario, metrics, "judge verdict missing dimensions (fail-closed)");

        return new QualityScenarioResult
        {
            Id = scenario.Id,
            DeterministicPassed = true,
            Dimensions = dims,
            Aggregate = dims.Values.Average(),
            Metrics = metrics,
            ExpectedScore = scenario.ExpectedScore,
        };
    }

    /// <summary>Runs a whole suite sequentially and aggregates.</summary>
    public static async Task<QualitySuiteResult> RunSuiteAsync(
        IReadOnlyList<QualityScenario> scenarios, IChatProvider sut, IJudge judge,
        IAgentBackend? backend = null, CancellationToken cancellationToken = default)
    {
        var results = new List<QualityScenarioResult>();
        foreach (var s in scenarios)
            results.Add(await RunScenarioAsync(s, sut, judge, backend, cancellationToken));
        return Aggregate(results);
    }

    /// <summary>Aggregates per-scenario results into a suite summary (unweighted means; errors excluded).</summary>
    public static QualitySuiteResult Aggregate(IReadOnlyList<QualityScenarioResult> results)
    {
        var scored = results.Where(r => r.Aggregate is not null).ToList();
        var suiteScore = scored.Count > 0 ? scored.Average(r => r.Aggregate!.Value) : 0;

        var dimensionMeans = results
            .SelectMany(r => r.Dimensions)
            .GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.Average(kv => kv.Value));

        var labeled = results.Where(r => r.ExpectedScore is not null && r.Aggregate is not null).ToList();
        double? mae = labeled.Count > 0
            ? labeled.Average(r => Math.Abs(r.Aggregate!.Value - r.ExpectedScore!.Value))
            : null;

        return new QualitySuiteResult
        {
            Scenarios = results,
            SuiteScore = suiteScore,
            DimensionMeans = dimensionMeans,
            JudgeHumanMae = mae,
            LabeledCases = labeled.Count,
            ErrorCases = results.Count(r => r.Error is not null),
        };
    }

    private static QualityScenarioResult EvalError(QualityScenario scenario, RunMetrics? metrics, string error)
        => new()
        {
            Id = scenario.Id,
            DeterministicPassed = true,
            Error = error,
            Metrics = metrics,
            ExpectedScore = scenario.ExpectedScore,
        };
}
