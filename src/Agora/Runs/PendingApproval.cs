namespace Agora.Runs;

/// <summary>A tool call parked awaiting a human decision, addressable by <see cref="Id"/>.</summary>
public sealed record PendingApproval
{
    /// <summary>Approval id used to resolve the request.</summary>
    public required string Id { get; init; }

    /// <summary>Agent requesting the approval.</summary>
    public required string AgentId { get; init; }

    /// <summary>Name of the tool/function awaiting approval.</summary>
    public required string FunctionName { get; init; }

    /// <summary>Serialized arguments the tool was called with.</summary>
    public string Arguments { get; init; } = "";
}
