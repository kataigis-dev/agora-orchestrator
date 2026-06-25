namespace Agora.Eval.Quality;

/// <summary>
/// A real-model quality eval case: run a config against a real provider, gate on deterministic checks,
/// then grade the remaining dimensions with an LLM judge. Unlike <see cref="Scenario"/> (scripted, offline,
/// wiring-only), this measures answer quality and is opt-in (the CLI gates it behind a live flag).
/// </summary>
public sealed class QualityScenario
{
    /// <summary>Stable id used in reports.</summary>
    public string Id { get; set; } = "";

    /// <summary>The input fed to the run.</summary>
    public string Input { get; set; } = "";

    /// <summary>Path to the config-under-test (absolute, or relative to the working directory).</summary>
    public string Config { get; set; } = "";

    /// <summary>Run the graph (true) or a single agent (false).</summary>
    public bool Graph { get; set; } = true;

    /// <summary>Agent id for single-agent mode.</summary>
    public string? Agent { get; set; }

    /// <summary>Optional golden reference answer the judge may compare against.</summary>
    public string? Reference { get; set; }

    /// <summary>Deterministic checks (the HARD gate): if any fails, the case fails with score 0 and the
    /// judge is skipped, regardless of any LLM verdict. Prefer these wherever a result is machine-verifiable.</summary>
    public List<DeterministicCheck> Deterministic { get; set; } = new();

    /// <summary>Rubric: dimension name → anchored criteria the judge grades on the 0/0.5/1 scale. Standard
    /// keys: <c>correctness</c>, <c>completeness</c>, <c>relevance</c>. <c>groundedness</c> is added
    /// automatically when the run retrieved RAG context.</summary>
    public Dictionary<string, string> Rubric { get; set; } = new();

    /// <summary>Optional human-labeled expected aggregate score (0–1); used to report judge↔human
    /// agreement so we know how far to trust the instrument.</summary>
    public double? ExpectedScore { get; set; }
}

/// <summary>One deterministic, off-LLM check over the run's final output (and emitted signals).</summary>
public sealed class DeterministicCheck
{
    /// <summary>Check kind: <c>contains</c>, <c>not_contains</c>, <c>regex</c>, or <c>signal</c>.</summary>
    public string Kind { get; set; } = "contains";

    /// <summary>The substring / regex pattern / signal name the check looks for.</summary>
    public string Value { get; set; } = "";
}
