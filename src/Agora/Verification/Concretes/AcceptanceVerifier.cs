using Agora.Verification.Contracts;
using Agora.Verification.Models;
using Agora.Verification.Concretes;
using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;

namespace Agora.Verification.Concretes;

/// <summary>The verification outcome of one acceptance criterion.</summary>
public sealed record CriterionResult(string CriterionId, CheckKind Kind, bool Passed, string Detail);

/// <summary>The verification outcome of a requirement: the per-criterion results plus the derived
/// verdict. A requirement is <see cref="Verified"/> only when it has criteria, every automated one
/// passed, and none require manual sign-off — a deterministic decision, not an LLM judgement.</summary>
public sealed record RequirementResult(string RequirementId, IReadOnlyList<CriterionResult> Criteria)
{
    /// <summary>Any criterion that can only be checked by a human.</summary>
    public bool HasManual => Criteria.Any(c => c.Kind == CheckKind.Manual);

    /// <summary>Every non-manual criterion passed.</summary>
    public bool AutomatedPassed => Criteria.Where(c => c.Kind != CheckKind.Manual).All(c => c.Passed);

    /// <summary>The requirement is fully, automatically verified.</summary>
    public bool Verified => Criteria.Count > 0 && AutomatedPassed && !HasManual;

    /// <summary>A readable multi-line report.</summary>
    public string ToReport()
    {
        var verdict = Verified ? "VERIFIED" : HasManual && AutomatedPassed ? "NEEDS MANUAL SIGN-OFF" : "NOT VERIFIED";
        var lines = Criteria.Select(c => $"  - {c.CriterionId} [{(c.Passed ? "pass" : c.Kind == CheckKind.Manual ? "manual" : "fail")}] {c.Detail}");
        return $"{RequirementId}: {verdict}\n{string.Join("\n", lines)}";
    }
}

/// <summary>
/// Verifies a requirement against its acceptance criteria by running the bound checks for real. Each
/// criterion's <see cref="SpecCheck"/> drives the verification: <see cref="CheckKind.Test"/>/<see
/// cref="CheckKind.Command"/> run a configured check (the expression names it, with optional
/// <c>key=value</c> args); <see cref="CheckKind.FileExists"/> tests a path; <see cref="CheckKind.Manual"/>
/// is reported as requiring human sign-off. The verdict is derived purely from exit codes.
/// </summary>
public sealed class AcceptanceVerifier
{
    private readonly ICheckRunner _runner;

    /// <summary>Builds the verifier over a check runner.</summary>
    public AcceptanceVerifier(ICheckRunner runner) => _runner = runner;

    /// <summary>Runs every acceptance criterion of the requirement and returns the aggregate result.</summary>
    public async Task<RequirementResult> VerifyAsync(Requirement requirement, CancellationToken cancellationToken = default)
    {
        var results = new List<CriterionResult>();
        foreach (var criterion in requirement.AcceptanceCriteria)
            results.Add(await VerifyCriterion(criterion, cancellationToken));
        return new RequirementResult(requirement.Id, results);
    }

    private async Task<CriterionResult> VerifyCriterion(AcceptanceCriterion criterion, CancellationToken cancellationToken)
    {
        var check = criterion.Check;
        switch (check.Kind)
        {
            case CheckKind.Manual:
                return new CriterionResult(criterion.Id, check.Kind, false, "manual: needs human sign-off");

            case CheckKind.FileExists:
                var path = Path.IsPathRooted(check.Expression)
                    ? check.Expression
                    : Path.Combine(_runner.Workdir, check.Expression);
                var exists = File.Exists(path) || Directory.Exists(path);
                return new CriterionResult(criterion.Id, check.Kind, exists, exists ? $"exists: {path}" : $"missing: {path}");

            default: // Test / Command
                if (string.IsNullOrWhiteSpace(check.Expression))
                    return new CriterionResult(criterion.Id, check.Kind, false, "no check bound to this criterion");
                var (name, args) = ParseExpression(check.Expression);
                var result = await _runner.RunAsync(name, args, cancellationToken);
                return new CriterionResult(criterion.Id, check.Kind, result.Passed, result.ToSummary());
        }
    }

    /// <summary>Parses a check expression: the first token is the check name, the rest are
    /// <c>key=value</c> argument pairs (e.g. <c>"test filter=AuthTests"</c>).</summary>
    private static (string Name, IReadOnlyDictionary<string, string> Args) ParseExpression(string expression)
    {
        var tokens = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var args = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in tokens.Skip(1))
        {
            var eq = token.IndexOf('=');
            if (eq > 0) args[token[..eq]] = token[(eq + 1)..];
        }
        return (tokens.Length > 0 ? tokens[0] : "", args);
    }
}
