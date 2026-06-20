namespace Agora.Eval;

/// <summary>
/// A deterministic eval scenario: scripted LLM responses replace real model calls (via the
/// fake provider), so a config can be regression-tested with no API access. Expectations are
/// checked against the run's final output and signals.
/// </summary>
public sealed class Scenario
{
    public string Input { get; set; } = "";

    /// <summary>Scripted model responses, returned in order to each agent call.</summary>
    public List<string> Responses { get; set; } = new();

    /// <summary>Substrings the final output must contain (case-insensitive).</summary>
    public List<string> ExpectOutputContains { get; set; } = new();

    /// <summary>Signals that must be present in the final state.</summary>
    public List<string> ExpectSignals { get; set; } = new();

    /// <summary>Run the graph (true) or a single agent (false).</summary>
    public bool Graph { get; set; } = true;

    /// <summary>Agent id for single-agent mode.</summary>
    public string? Agent { get; set; }
}

public sealed record EvalResult(bool Passed, IReadOnlyList<string> Failures);
