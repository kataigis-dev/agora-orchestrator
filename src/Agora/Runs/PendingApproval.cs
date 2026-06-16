namespace Agora.Runs;

/// <summary>A tool call parked awaiting a human decision, addressable by <see cref="Id"/>.</summary>
public sealed record PendingApproval
{
    public required string Id { get; init; }
    public required string AgentId { get; init; }
    public required string FunctionName { get; init; }
    public string Arguments { get; init; } = "";
}
