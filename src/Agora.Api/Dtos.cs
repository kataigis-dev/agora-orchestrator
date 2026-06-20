namespace Agora.Api;

/// <summary>Request body to start a run: mode (<c>agent</c>/<c>graph</c>), optional agent id, and input.</summary>
public sealed record StartRunRequest(string Mode, string? Agent, string Input);

/// <summary>Response when a run is accepted, carrying its id.</summary>
public sealed record StartRunResponse(string RunId);

/// <summary>Summary of a configured agent returned by the <c>/agents</c> endpoint.</summary>
public sealed record AgentInfo(string Id, string Role, IReadOnlyList<string> Tools, IReadOnlyList<string> Approvals);

/// <summary>A pending tool-approval surfaced in a run's status.</summary>
public sealed record PendingApprovalDto(string Id, string AgentId, string FunctionName, string Arguments);

/// <summary>A run's current status, output/error, and any pending approvals.</summary>
public sealed record RunStatusResponse(
    string Id, string Status, string Mode, string? Agent,
    string? Output, string? Error, IReadOnlyList<PendingApprovalDto> PendingApprovals);

/// <summary>A decision (approve/reject) for a single pending approval.</summary>
public sealed record ApprovalDecision(string Id, bool Approved);

/// <summary>Request body submitting a batch of approval decisions.</summary>
public sealed record ApprovalsRequest(IReadOnlyList<ApprovalDecision> Approvals);

/// <summary>Response from the ingest endpoint with the number of chunks ingested.</summary>
public sealed record IngestResponse(int Chunks);
