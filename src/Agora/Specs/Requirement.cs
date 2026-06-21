namespace Agora.Specs;

/// <summary>MoSCoW priority of a requirement.</summary>
public enum RequirementPriority { Must, Should, Could, Wont }

/// <summary>Lifecycle status of a requirement, advanced by agents/gates over a run.</summary>
public enum RequirementStatus { Proposed, Approved, Implemented, Verified, Rejected }

/// <summary>How an acceptance criterion is verified.</summary>
public enum CheckKind { Manual, Test, Command, FileExists }

/// <summary>A machine-checkable (or manual) verification descriptor bound to an acceptance criterion.
/// <see cref="Expression"/> is interpreted per <see cref="Kind"/>: a configured check name (with
/// optional args) for <see cref="CheckKind.Test"/>/<see cref="CheckKind.Command"/>, a path or glob
/// for <see cref="CheckKind.FileExists"/>, and free text for <see cref="CheckKind.Manual"/>.</summary>
public sealed record SpecCheck(CheckKind Kind = CheckKind.Manual, string Expression = "");

/// <summary>A single acceptance criterion of a requirement. <see cref="Id"/> is stable (e.g. R1.A1)
/// so traceability survives edits. A single constructor keeps it cleanly JSON-serializable.</summary>
public sealed record AcceptanceCriterion(string Id, string Statement, SpecCheck Check);

/// <summary>A single requirement with a stable id (R1..Rn) and its acceptance criteria — the unit
/// of the structured specification that tasks trace back to and gates verify.</summary>
public sealed record Requirement
{
    /// <summary>Stable identifier (R1..Rn).</summary>
    public required string Id { get; init; }

    /// <summary>Short imperative title.</summary>
    public required string Title { get; init; }

    /// <summary>Fuller description / rationale.</summary>
    public string Description { get; init; } = "";

    /// <summary>MoSCoW priority.</summary>
    public RequirementPriority Priority { get; init; } = RequirementPriority.Must;

    /// <summary>Lifecycle status.</summary>
    public RequirementStatus Status { get; init; } = RequirementStatus.Proposed;

    /// <summary>Acceptance criteria that define "done" for this requirement.</summary>
    public IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria { get; init; } = Array.Empty<AcceptanceCriterion>();
}
