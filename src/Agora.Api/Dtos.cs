namespace Agora.Api;

public sealed record StartRunRequest(string Mode, string? Agent, string Input);
public sealed record StartRunResponse(string RunId);
public sealed record AgentInfo(string Id, string Role, IReadOnlyList<string> Tools, IReadOnlyList<string> Approvals);
public sealed record PendingApprovalDto(string Id, string AgentId, string FunctionName, string Arguments);
public sealed record RunStatusResponse(
    string Id, string Status, string Mode, string? Agent,
    string? Output, string? Error, IReadOnlyList<PendingApprovalDto> PendingApprovals);
public sealed record ApprovalDecision(string Id, bool Approved);
public sealed record ApprovalsRequest(IReadOnlyList<ApprovalDecision> Approvals);
public sealed record IngestResponse(int Chunks);
