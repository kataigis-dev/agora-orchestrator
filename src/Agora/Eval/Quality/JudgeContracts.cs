namespace Agora.Eval.Quality;

/// <summary>Everything the judge needs to grade one output, decoupled from how the run produced it.</summary>
public sealed record JudgeRequest
{
    /// <summary>The task/input the output was produced for.</summary>
    public required string Input { get; init; }

    /// <summary>The output under evaluation.</summary>
    public required string Output { get; init; }

    /// <summary>Dimension name → anchored criteria to grade on the 0/0.5/1 scale.</summary>
    public required IReadOnlyDictionary<string, string> Rubric { get; init; }

    /// <summary>Optional golden reference answer for comparison.</summary>
    public string? Reference { get; init; }

    /// <summary>Retrieved context for groundedness; when set, the rubric must include <c>groundedness</c>
    /// and the judge assesses support strictly against this text.</summary>
    public string? GroundingContext { get; init; }
}

/// <summary>One dimension's graded verdict: the score plus the justification and cited evidence the judge
/// produced before scoring.</summary>
public sealed record DimensionVerdict(double Score, string Justification, string Evidence);

/// <summary>
/// The judge's full verdict. <see cref="Valid"/> is false when the reply could not be parsed into
/// well-formed scores for every requested dimension — a fail-CLOSED signal: an invalid verdict is an
/// evaluation error, never silently a pass.
/// </summary>
public sealed record JudgeVerdict
{
    /// <summary>Per-dimension verdicts, keyed by dimension name.</summary>
    public IReadOnlyDictionary<string, DimensionVerdict> Dimensions { get; init; }
        = new Dictionary<string, DimensionVerdict>();

    /// <summary>True only when every requested dimension parsed into a valid score.</summary>
    public bool Valid { get; init; }

    /// <summary>Why the verdict is invalid, when it is.</summary>
    public string? Error { get; init; }

    /// <summary>A fail-closed invalid verdict carrying the reason.</summary>
    public static JudgeVerdict Invalid(string error) => new() { Valid = false, Error = error };
}

/// <summary>Grades a single output against a rubric. Implementations: <see cref="LlmJudge"/> (real model,
/// temperature 0, constrained JSON, fail-closed) and <see cref="FakeJudge"/> (scripted, for offline tests).</summary>
public interface IJudge
{
    /// <summary>Grades the request and returns a verdict (fail-closed: <see cref="JudgeVerdict.Valid"/> is
    /// false when the reply is unusable).</summary>
    Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default);
}
