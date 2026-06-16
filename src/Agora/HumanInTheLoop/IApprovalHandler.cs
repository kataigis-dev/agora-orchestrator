namespace Agora.HumanInTheLoop;

/// <summary>A consequential tool call awaiting a human decision.</summary>
public sealed record ApprovalRequest
{
    public required string AgentId { get; init; }
    public required string FunctionName { get; init; }
    public string Arguments { get; init; } = "";
}

/// <summary>Decides whether a tool call may proceed. Injected at the edges (CLI prompt / API / test fake).</summary>
public interface IApprovalHandler
{
    Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
}
