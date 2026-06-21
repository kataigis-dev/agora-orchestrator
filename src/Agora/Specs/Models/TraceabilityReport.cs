using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;
namespace Agora.Specs.Models;

/// <summary>One row of the traceability matrix: a requirement and how completely it is traced —
/// which tasks implement it and whether its acceptance is machine-verifiable. Pure projection of the
/// <see cref="SpecDocument"/>; carries no judgement of its own beyond the derived flags below.</summary>
public sealed record RequirementCoverage(
    string RequirementId,
    string Title,
    RequirementPriority Priority,
    RequirementStatus Status,
    IReadOnlyList<string> TaskIds,
    int CriteriaCount,
    int CheckableCriteriaCount)
{
    /// <summary>In the committed scope: approved or beyond (excludes <see cref="RequirementStatus.Proposed"/>
    /// and <see cref="RequirementStatus.Rejected"/>). Only in-scope requirements gate completion.</summary>
    public bool InScope => Status is RequirementStatus.Approved or RequirementStatus.Implemented or RequirementStatus.Verified;

    /// <summary>At least one task traces back to this requirement.</summary>
    public bool Covered => TaskIds.Count > 0;

    /// <summary>Reached <see cref="RequirementStatus.Verified"/>.</summary>
    public bool Verified => Status is RequirementStatus.Verified;

    /// <summary>A <see cref="Verified"/> claim is substantiated only if every acceptance criterion is
    /// machine-checkable (non-<see cref="CheckKind.Manual"/> with an expression) — i.e. it could only
    /// have been reached through <c>spec_verify</c>'s deterministic pass, not a hand-set status. A
    /// Verified requirement with a manual or empty-expression criterion is a verification claim that
    /// no check could have produced.</summary>
    public bool VerificationSubstantiated => CriteriaCount > 0 && CheckableCriteriaCount == CriteriaCount;
}

/// <summary>A completeness gap found by <see cref="TraceabilityValidator"/>. Errors block the
/// completion gate; warnings surface lower-severity gaps without blocking.</summary>
public sealed record TraceabilityGap(SpecSeverity Severity, string Code, string Subject, string Message);

/// <summary>The traceability matrix plus the deterministic completion verdict for a
/// <see cref="SpecDocument"/>. <see cref="IsComplete"/> is the gate: true only when every in-scope
/// requirement is covered by a task and verified through real checks. Read-only — computed from the
/// document, never mutates it.</summary>
public sealed record TraceabilityReport
{
    /// <summary>One row per requirement, in document order.</summary>
    public IReadOnlyList<RequirementCoverage> Rows { get; init; } = Array.Empty<RequirementCoverage>();

    /// <summary>The completeness gaps (errors block, warnings inform).</summary>
    public IReadOnlyList<TraceabilityGap> Gaps { get; init; } = Array.Empty<TraceabilityGap>();

    /// <summary>Total implementation tasks in the document.</summary>
    public int TaskCount { get; init; }

    /// <summary>Requirements in the committed scope (approved or beyond).</summary>
    public int InScopeCount => Rows.Count(r => r.InScope);

    /// <summary>In-scope requirements with at least one implementing task.</summary>
    public int CoveredCount => Rows.Count(r => r.InScope && r.Covered);

    /// <summary>In-scope requirements that are verified.</summary>
    public int VerifiedCount => Rows.Count(r => r.InScope && r.Verified);

    /// <summary>Fraction of in-scope requirements that are covered (1 when nothing is in scope).</summary>
    public double CoverageRate => InScopeCount > 0 ? (double)CoveredCount / InScopeCount : 1.0;

    /// <summary>Fraction of in-scope requirements that are verified (1 when nothing is in scope).</summary>
    public double VerificationRate => InScopeCount > 0 ? (double)VerifiedCount / InScopeCount : 1.0;

    /// <summary>The completion gate: true only when there are no blocking (error) gaps. An empty or
    /// proposal-only spec is not complete (it has the <c>no-approved-requirements</c> gap).</summary>
    public bool IsComplete => !Gaps.Any(g => g.Severity == SpecSeverity.Error);

    /// <summary>A human-readable matrix with the verdict, one row per requirement, then the gaps.</summary>
    public string ToReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Traceability: {InScopeCount} in-scope requirement(s), coverage {CoverageRate:P0}, "
            + $"verification {VerificationRate:P0} — {(IsComplete ? "COMPLETE" : "INCOMPLETE")}");
        foreach (var row in Rows)
        {
            var tasks = row.TaskIds.Count > 0 ? string.Join(",", row.TaskIds) : "—";
            var mark = row.Verified ? (row.VerificationSubstantiated ? "verified" : "verified(unsubstantiated)")
                : row.Covered ? "covered" : "uncovered";
            sb.AppendLine($"  {row.RequirementId} [{row.Priority}/{row.Status}] {row.Title} "
                + $"tasks: {tasks}  criteria {row.CheckableCriteriaCount}/{row.CriteriaCount} checkable  [{mark}]");
        }
        if (Gaps.Count > 0)
        {
            sb.AppendLine("Gaps:");
            foreach (var gap in Gaps)
                sb.AppendLine($"  [{gap.Severity.ToString().ToLowerInvariant()}] {gap.Code} {gap.Subject}: {gap.Message}");
        }
        return sb.ToString().TrimEnd();
    }
}
