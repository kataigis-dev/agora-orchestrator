---
type: concept
title: Human-in-the-Loop (HITL)
tags: [hitl, approval, human, safety]
related: [agora-orchestrator, agora-cli, agora-api, shared-knowledge-base]
created: 2026-06-17
updated: 2026-06-20
---

# Human-in-the-Loop (HITL)

Human-approval mechanism for critical actions. It pauses an agent's execution and waits for
confirmation before proceeding.

## Configuration

```yaml
agents:
  deployer:
    approvals: [deploy_to_production, delete_database]
```

The `approvals` list names the (function-calling) tools that require explicit human approval before
they run (it must be a subset of the agent's `tools`).

## Interface

```csharp
// Agora/HumanInTheLoop/IApprovalHandler.cs
public sealed record ApprovalRequest
{
    public required string AgentId { get; init; }
    public required string FunctionName { get; init; }
    public string Arguments { get; init; } = "";
}

public interface IApprovalHandler
{
    Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
}
```

## Implementations

| Class | Context | Behaviour |
|-------|---------|-----------|
| `ConsoleApprovalHandler` | CLI | Prints the request and waits for `y/N` from the keyboard |
| `FakeApprovalHandler` | Tests | Auto-approves or auto-denies (configurable) |
| `PendingApprovalHandler` | API | Accumulates requests answered via `ApprovalGate` |

## Flow

1. The agent (built via `IAgentBackend`) is about to run a tool.
2. If the tool name is in the agent's `approvals` list, `IApprovalHandler.RequestAsync` is called.
3. If the handler returns `true` → the tool runs.
4. If it returns `false` → the tool is skipped (no exception).

## Tests

`tests/Agora.Api.Tests/ApprovalFlowTests.cs` — integration test of the HITL flow via API.

## Conflict resolution (second HITL channel)

Besides the yes/no tool approval, there is a HITL channel dedicated to writable knowledge-base
conflicts: `IConflictResolver` with three-way outcomes (`KeepExisting` / `KeepNew` / `Merge`). It
fires when an agent cannot resolve a RAG write conflict on its own. See [[shared-knowledge-base]].

## Security notes

- HITL is the main built-in mechanism to prevent automated destructive actions.
- In production, implement `IApprovalHandler` with a webhook, Slack, or dedicated UI notification.
- Do not rely solely on `FakeApprovalHandler` outside tests.
