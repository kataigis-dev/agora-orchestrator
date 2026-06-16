namespace Agora.Runs;

/// <summary>Mutable state of a single API run.</summary>
public sealed class RunRecord
{
    public required string Id { get; init; }
    public required string Mode { get; init; }
    public string? AgentId { get; init; }
    public required string Input { get; init; }
    public RunStatus Status { get; set; } = RunStatus.Running;
    public string? Output { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
