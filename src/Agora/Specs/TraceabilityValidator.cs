namespace Agora.Specs;

/// <summary>
/// Computes requirement↔task↔check traceability for a <see cref="SpecDocument"/> and derives the
/// deterministic completion verdict. Where <see cref="SpecValidator"/> guards structural invariants on
/// every write (and blocks bad saves), this answers a different question read-only: "is the committed
/// scope fully traced and verified?" — the gate the completion phase asserts instead of trusting a
/// model's "done". It does not re-run checks; it asserts what the persisted document can prove.
/// </summary>
public static class TraceabilityValidator
{
    /// <summary>Builds the traceability matrix and the blocking/non-blocking gaps for a document.</summary>
    public static TraceabilityReport Analyze(SpecDocument document)
    {
        var rows = document.Requirements.Select(r =>
        {
            var taskIds = document.Tasks
                .Where(t => t.RequirementIds.Contains(r.Id))
                .Select(t => t.Id)
                .ToList();
            var checkable = r.AcceptanceCriteria.Count(c =>
                c.Check.Kind is not CheckKind.Manual && !string.IsNullOrWhiteSpace(c.Check.Expression));
            return new RequirementCoverage(
                r.Id, r.Title, r.Priority, r.Status, taskIds, r.AcceptanceCriteria.Count, checkable);
        }).ToList();

        var gaps = new List<TraceabilityGap>();
        var inScope = rows.Where(r => r.InScope).ToList();

        if (inScope.Count == 0)
            gaps.Add(new(SpecSeverity.Error, "no-approved-requirements", "—",
                "no requirement has been approved; there is nothing to complete"));

        foreach (var row in inScope)
        {
            if (!row.Covered)
                gaps.Add(new(SpecSeverity.Error, "uncovered-requirement", row.RequirementId,
                    $"in-scope requirement '{row.RequirementId}' has no implementing task"));

            if (!row.Verified)
                gaps.Add(new(SpecSeverity.Error, "unverified-requirement", row.RequirementId,
                    $"requirement '{row.RequirementId}' is {row.Status}, not Verified"));
            else if (!row.VerificationSubstantiated)
                gaps.Add(new(SpecSeverity.Error, "unsubstantiated-verification", row.RequirementId,
                    $"requirement '{row.RequirementId}' is Verified but has no machine-checkable acceptance "
                    + "criteria — verification could not have come from a real check"));
        }

        // Orphan tasks can't be reached through the validated tools, but the report stands alone, so
        // surface them (non-blocking — SpecValidator already errors on writes that would create them).
        foreach (var task in document.Tasks.Where(t => t.RequirementIds.Count == 0))
            gaps.Add(new(SpecSeverity.Warning, "orphan-task", task.Id,
                $"task '{task.Id}' traces back to no requirement"));

        return new TraceabilityReport { Rows = rows, Gaps = gaps, TaskCount = document.Tasks.Count };
    }
}
