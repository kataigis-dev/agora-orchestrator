using Agora.Specs.Contracts;
using Agora.Specs.Models;
using Agora.Specs.Concretes;
namespace Agora.Specs.Models;

/// <summary>Progress state of an implementation task.</summary>
public enum TaskState { Todo, InProgress, Done }

/// <summary>Evidence backing a task's completion: a check-result id, file, commit, or note.</summary>
public sealed record TaskEvidence(string Kind, string Detail);

/// <summary>A unit of implementation work (T1..Tn) that traces back to one or more requirements.
/// The <see cref="RequirementIds"/> link is what makes requirement↔task traceability machine-checkable.</summary>
public sealed record TaskItem
{
    /// <summary>Stable identifier (T1..Tn).</summary>
    public required string Id { get; init; }

    /// <summary>What the task delivers.</summary>
    public required string Description { get; init; }

    /// <summary>Lane/kind tag (e.g. <c>fe</c>, <c>be</c>); free-form.</summary>
    public string Kind { get; init; } = "";

    /// <summary>Requirements this task implements (must reference existing requirement ids).</summary>
    public IReadOnlyList<string> RequirementIds { get; init; } = Array.Empty<string>();

    /// <summary>Progress state.</summary>
    public TaskState Status { get; init; } = TaskState.Todo;

    /// <summary>Evidence accumulated as the task progresses (check results, files, commits).</summary>
    public IReadOnlyList<TaskEvidence> Evidence { get; init; } = Array.Empty<TaskEvidence>();
}
