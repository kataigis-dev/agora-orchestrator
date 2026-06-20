namespace Agora.HumanInTheLoop;

/// <summary>A consequential tool call awaiting a human decision.</summary>
public sealed record ApprovalRequest
{
    /// <summary>Agent that wants to run the tool.</summary>
    public required string AgentId { get; init; }

    /// <summary>Name of the tool/function awaiting approval.</summary>
    public required string FunctionName { get; init; }

    /// <summary>Serialized arguments the tool would run with.</summary>
    public string Arguments { get; init; } = "";
}

/// <summary>Decides whether a tool call may proceed. Injected at the edges (CLI prompt / API / test fake).</summary>
public interface IApprovalHandler
{
    /// <summary>Returns true to allow the tool call, false to skip it.</summary>
    Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
}
