using System.Text;
using System.Text.Json;

namespace Agora.Eval.Quality;

/// <summary>Renders a <see cref="QualitySuiteResult"/> as a human-readable table and a machine-readable JSON
/// report that places quality scores beside the run's <c>RunMetrics</c>, so configs can be compared on
/// quality and cost together.</summary>
public static class QualityReport
{
    /// <summary>A compact human-readable summary + per-scenario lines.</summary>
    public static string Human(QualitySuiteResult suite)
    {
        var sb = new StringBuilder();
        sb.Append("quality suite: score=").Append(F2(suite.SuiteScore))
          .Append(" over ").Append(suite.Scenarios.Count).Append(" case(s), errors=")
          .Append(suite.ErrorCases).AppendLine();
        if (suite.JudgeHumanMae is { } mae)
            sb.Append("judge-vs-human MAE=").Append(F2(mae)).Append(" over ")
              .Append(suite.LabeledCases).AppendLine(" labeled case(s)");
        if (suite.DimensionMeans.Count > 0)
            sb.Append("dimension means: ")
              .AppendLine(string.Join(", ", suite.DimensionMeans.Select(kv => $"{kv.Key}={F2(kv.Value)}")));
        sb.AppendLine();
        foreach (var r in suite.Scenarios)
            sb.AppendLine(Line(r));
        return sb.ToString();
    }

    private static string Line(QualityScenarioResult r)
    {
        if (r.Error is not null)
            return $"  [ERR ] {r.Id}: {r.Error}";
        if (!r.DeterministicPassed)
            return $"  [GATE] {r.Id}: deterministic fail ({string.Join("; ", r.DeterministicFailures)}) -> 0.00";
        var dims = r.Dimensions.Count > 0
            ? " {" + string.Join(", ", r.Dimensions.Select(kv => $"{kv.Key}={F1(kv.Value)}")) + "}"
            : "";
        var tag = r.Aggregate >= 0.999 ? "PASS" : "PART";
        return $"  [{tag}] {r.Id}: {F2(r.Aggregate ?? 0)}{dims}";
    }

    /// <summary>A stable, indented JSON report (quality scores + RunMetrics per scenario).</summary>
    public static string Json(QualitySuiteResult suite)
    {
        var doc = new
        {
            suiteScore = suite.SuiteScore,
            errorCases = suite.ErrorCases,
            judgeHumanMae = suite.JudgeHumanMae,
            labeledCases = suite.LabeledCases,
            dimensionMeans = suite.DimensionMeans,
            scenarios = suite.Scenarios.Select(r => new
            {
                id = r.Id,
                deterministicPassed = r.DeterministicPassed,
                deterministicFailures = r.DeterministicFailures,
                dimensions = r.Dimensions,
                aggregate = r.Aggregate,
                expectedScore = r.ExpectedScore,
                error = r.Error,
                metrics = r.Metrics is null
                    ? null
                    : new
                    {
                        steps = r.Metrics.Steps,
                        completed = r.Metrics.Completed,
                        rework = r.Metrics.ReworkCount,
                        inputTokens = r.Metrics.InputTokens,
                        outputTokens = r.Metrics.OutputTokens,
                        cacheHitRate = r.Metrics.CacheHitRate,
                    },
            }),
        };
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string F1(double v) => v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
    private static string F2(double v) => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
