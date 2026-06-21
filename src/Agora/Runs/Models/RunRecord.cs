using Agora.Runs.Contracts;
using Agora.Runs.Models;
using Agora.Runs.Concretes;
namespace Agora.Runs.Models;

/// <summary>Mutable state of a single API run.</summary>
public sealed class RunRecord
{
    /// <summary>Unique run id.</summary>
    public required string Id { get; init; }

    /// <summary>Run mode (e.g. single-agent or graph).</summary>
    public required string Mode { get; init; }

    /// <summary>Agent id for single-agent runs; null for graph runs.</summary>
    public string? AgentId { get; init; }

    /// <summary>The run's input.</summary>
    public required string Input { get; init; }

    /// <summary>Current lifecycle status.</summary>
    public RunStatus Status { get; set; } = RunStatus.Running;

    /// <summary>Final output once completed.</summary>
    public string? Output { get; set; }

    /// <summary>Error message if the run failed.</summary>
    public string? Error { get; set; }

    /// <summary>When the run was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the run was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
