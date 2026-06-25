# Agents

## Agent types

### Agent (core)

Defined in `src/Agora/Agents/Agent.cs`. A tool-less agent built from an `AgentCard`:
- `Id` — unique identifier (the node id)
- `Model` — referenced model alias
- `Role` / `SystemPrompt` — system instructions
- `Skills` — skill names to load
- `Tools` — enabled tool names
- `Approvals` — subset of `Tools` requiring human approval (must be ⊆ tools)

Retry/timeout come from the resolved `ModelSpec` (`defaults.retries`, `retry_base_delay`, `timeout`).

### AgentFrameworkAgent

Defined in `src/Agora.AgentFramework/AgentFrameworkAgent.cs`. Used when an agent declares
skills/tools. Adds:
- `Microsoft.Extensions.AI.IChatClient` integration for model calls
- Tool-call handling (built-in tools + MCP)
- Approval-gated tools via `IApprovalHandler` (HITL)

## Tools

Agents can use built-in tools or external ones via MCP. Both are enabled with the same `tools:`
list on the agent; built-in tools are registered before MCP tools.

### Built-in tools (no MCP server needed)

| Tool | Purpose |
|---|---|
| `read_file`, `write_file`, `search_files`, `list_directory` | Filesystem (`System.IO`), sandboxed to the workspace root |
| `rag_search`, `rag_write` | Read/write the shared knowledge base |
| `ask_agent` | Ask another agent and get its answer (after `rag_search`) |

```yaml
agents:
  builder:
    tools: [read_file, write_file, rag_search, rag_write]
```

The filesystem tools are **sandboxed**: every path is resolved against a single root directory (the
config file's directory by default) and any path that escapes it — via `..`, an absolute path, or a
different drive — is rejected before any file access. This bounds a non-deterministic agent (or a
prompt-injection payload) to the workspace; it cannot read or write arbitrary files on the host.

### MCP tools

For external tools (GitHub, search, custom servers), configure an MCP server; tools are discovered
and filtered against the agent's `tools:` list.

```yaml
mcp:
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
agents:
  builder:
    tools: [read_file, write_file]
```

## Skills

Skills are reusable prompt files (`SKILL.md`) discovered from configured directories and exposed
to the agent as a `load_skill` tool (progressive disclosure):

```yaml
skills:
  directories: [./skills]
agents:
  summarizer:
    skills: [summarize]
```

## Human approval (HITL)

List a tool in an agent's `approvals` to require human confirmation before that tool call runs:

```yaml
agents:
  editor:
    tools: [read_file, write_file]
    approvals: [write_file]   # write_file pauses for approval; read_file runs freely
```

- **CLI** — `ConsoleApprovalHandler` prompts on the console (`approve? [y/N]`).

Agora is CLI-only with a human always present, so approvals are answered interactively at the console;
there is no headless/networked approval surface.

A second HITL channel, `IConflictResolver`, resolves knowledge-base write conflicts
(keep existing / keep new / merge). It blocks synchronously on a human; a non-interactive run that could
reach `rag_write` is refused rather than silently degraded.

## Context passing

Agents in a graph share context through the `State` blackboard: `Messages` (inbox), `Outputs`,
`Signals`, `Artifacts`, `LoopCounters`, `LastAgent`. What reaches each agent depends on the mode
(full output, handoff-only, or top-K recalled memory).
