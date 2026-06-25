using System.Text.RegularExpressions;

namespace Agora.Eval.Quality;

/// <summary>Runs a scenario's deterministic checks over the final output and emitted signals. These are the
/// hard gates: any failure fails the case regardless of the LLM judge (the project's "prefer deterministic
/// checks" rule, mirroring the spec gate).</summary>
public static class DeterministicChecks
{
    /// <summary>Evaluates every check and returns the human-readable failures (empty when all pass).</summary>
    public static IReadOnlyList<string> Run(
        IReadOnlyList<DeterministicCheck> checks, string output, IReadOnlyDictionary<string, object> signals)
    {
        var failures = new List<string>();
        foreach (var check in checks)
        {
            var ok = (check.Kind ?? "").ToLowerInvariant() switch
            {
                "contains" => output.Contains(check.Value, StringComparison.OrdinalIgnoreCase),
                "not_contains" => !output.Contains(check.Value, StringComparison.OrdinalIgnoreCase),
                "regex" => Regex.IsMatch(output, check.Value, RegexOptions.IgnoreCase | RegexOptions.Multiline),
                "signal" => signals.ContainsKey(check.Value),
                _ => throw new ArgumentException($"unknown deterministic check kind '{check.Kind}'"),
            };
            if (!ok)
                failures.Add($"{check.Kind} '{check.Value}'");
        }
        return failures;
    }
}
