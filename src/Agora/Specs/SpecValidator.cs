namespace Agora.Specs;

/// <summary>Severity of a <see cref="SpecIssue"/>.</summary>
public enum SpecSeverity { Error, Warning }

/// <summary>A single problem found while validating a specification.</summary>
public sealed record SpecIssue(SpecSeverity Severity, string Code, string Message);

/// <summary>
/// Validates a <see cref="SpecDocument"/>'s structural invariants — the machine-checkable rules that
/// replace prompt convention. Errors block a write; warnings surface coverage gaps without blocking
/// (the hard completion gate that consumes coverage arrives in the traceability phase).
/// </summary>
public static class SpecValidator
{
    /// <summary>Returns every issue found. <paramref name="requireCriteria"/> turns "a requirement
    /// has no acceptance criteria" from a warning into an error.</summary>
    public static IReadOnlyList<SpecIssue> Validate(SpecDocument document, bool requireCriteria = true)
    {
        var issues = new List<SpecIssue>();
        var requirementIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in document.Requirements.GroupBy(r => r.Id).Where(g => g.Count() > 1))
            issues.Add(new(SpecSeverity.Error, "duplicate-requirement-id",
                $"requirement id '{group.Key}' is used {group.Count()} times"));

        foreach (var requirement in document.Requirements)
        {
            requirementIds.Add(requirement.Id);

            if (requirement.AcceptanceCriteria.Count == 0)
                issues.Add(new(
                    requireCriteria ? SpecSeverity.Error : SpecSeverity.Warning,
                    "missing-acceptance-criteria",
                    $"requirement '{requirement.Id}' has no acceptance criteria"));

            foreach (var group in requirement.AcceptanceCriteria.GroupBy(c => c.Id).Where(g => g.Count() > 1))
                issues.Add(new(SpecSeverity.Error, "duplicate-criterion-id",
                    $"requirement '{requirement.Id}' reuses criterion id '{group.Key}'"));

            foreach (var criterion in requirement.AcceptanceCriteria)
                if (criterion.Check.Kind is not CheckKind.Manual && string.IsNullOrWhiteSpace(criterion.Check.Expression))
                    issues.Add(new(SpecSeverity.Warning, "empty-check-expression",
                        $"criterion '{criterion.Id}' is {criterion.Check.Kind} but has no check expression"));
        }

        var coveredRequirements = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in document.Tasks.GroupBy(t => t.Id).Where(g => g.Count() > 1))
            issues.Add(new(SpecSeverity.Error, "duplicate-task-id",
                $"task id '{group.Key}' is used {group.Count()} times"));

        foreach (var task in document.Tasks)
        {
            if (task.RequirementIds.Count == 0)
                issues.Add(new(SpecSeverity.Error, "task-without-requirement",
                    $"task '{task.Id}' references no requirement"));

            foreach (var reqId in task.RequirementIds)
            {
                coveredRequirements.Add(reqId);
                if (!requirementIds.Contains(reqId))
                    issues.Add(new(SpecSeverity.Error, "dangling-requirement-ref",
                        $"task '{task.Id}' references unknown requirement '{reqId}'"));
            }
        }

        // Coverage gap: an approved requirement with no implementing task. A warning for now; the
        // traceability phase turns this into the hard completion gate.
        foreach (var requirement in document.Requirements)
            if (requirement.Status is RequirementStatus.Approved && !coveredRequirements.Contains(requirement.Id))
                issues.Add(new(SpecSeverity.Warning, "uncovered-requirement",
                    $"approved requirement '{requirement.Id}' has no implementing task"));

        return issues;
    }

    /// <summary>True when validation finds no <see cref="SpecSeverity.Error"/> issues.</summary>
    public static bool IsValid(SpecDocument document, bool requireCriteria = true)
        => Validate(document, requireCriteria).All(i => i.Severity != SpecSeverity.Error);
}
