---
type: concept
title: MCP Tools
tags: [mcp, tools, model-context-protocol, external]
related: [agora-agent-framework, skills, agora-orchestrator, shared-knowledge-base]
created: 2026-06-17
updated: 2026-06-20
---

# MCP Tools (Model Context Protocol)

Integration with external MCP servers to expose tools to the system (filesystem, network,
databases, etc.).

## What MCP is

The **Model Context Protocol** is an open standard that lets LLMs invoke tools implemented in
separate processes via a stdio/JSON-RPC protocol (or HTTP).

## YAML configuration

```yaml
mcp:
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
    custom-tool:
      command: python
      args: ["my_tool_server.py"]

agents:
  builder:
    tools: [write_file, read_file, list_directory]
```

## How it works

1. `McpToolSession` starts the external process via stdio (or connects via `url` for HTTP).
2. Performs the MCP handshake (initialize / list tools).
3. Available tools are exposed to the agent as function-calling tools, filtered by the agent's `tools:` list.
4. When the agent calls a tool, `McpToolSession` forwards the call and returns the response.

## `McpToolSession` class

```csharp
// Agora.AgentFramework/McpToolSession.cs
// Manages an MCP session lifecycle: start the process, initialize the protocol,
// expose the available tools, and forward tool calls.
```

## Security

- MCP tools run in separate processes — filesystem access is limited to the configured path.
- Configure `args` carefully: avoid absolute paths or excessive permissions.
- Gate consequential tools behind HITL approvals (the agent's `approvals` list).

## Built-in filesystem tools

Agora includes four **built-in** filesystem tools (no external MCP server needed), implemented in
`BuiltInFileTools.cs` with `System.IO`:

| Tool | Description |
|------|-------------|
| `read_file` | Read a file from disk |
| `write_file` | Write a file to disk |
| `search_files` | Find files by glob pattern |
| `list_directory` | List a directory's contents |

They are registered before MCP tools and use the same names as their MCP equivalents (drop-in).

## Built-in knowledge base & collaboration tools

Other built-in tools (same allow-list mechanism) for the shared knowledge base and inter-agent
collaboration — see [[shared-knowledge-base]]:

| Tool | Description |
|------|-------------|
| `rag_search` | Search the shared KB (`RagTools`) |
| `rag_write` | Write an entry to the KB with conflict-check (`RagTools`) |
| `ask_agent` | Ask another agent and get its answer (`AskAgentTool`) |

No MCP server configuration required.

## Examples

- `examples/agora-tools.yaml` — pipeline with skills + MCP tools
- `examples/agora-generate-api.yaml` — code generation with MCP + a review loop
