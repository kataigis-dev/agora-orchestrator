# MCP — Model Context Protocol

## What it is

MCP lets AI agents interact with external tools and resources in a standardized way.

## Built-in tools

Agora includes **built-in tools** that need no MCP server (filesystem via `System.IO`, plus the
knowledge-base and collaboration tools). Agents that list `read_file`, `write_file`,
`search_files`, `list_directory`, `rag_search`, `rag_write` or `ask_agent` use them automatically.

```yaml
agents:
  builder:
    tools: [read_file, write_file, search_files, list_directory]
    # No MCP server required.
```

The built-in filesystem tools share the same names as their MCP equivalents, so you can switch
between MCP and built-in by just removing/adding the MCP server. They are **sandboxed** to the
workspace root (the config file's directory by default): paths that escape it — via `..`, an
absolute path, or a different drive — are rejected before any file access.

## Configuration

### stdio server (a child process)

```yaml
mcp:
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
```

Started as a child process; JSON-RPC over stdin/stdout.

### HTTP server

```yaml
mcp:
  servers:
    remote:
      url: http://localhost:3000/mcp
```

The transport is inferred: a `url` means HTTP, otherwise `command`/`args` means stdio.

## Tool discovery & execution

On startup Agora connects to each MCP server (`McpToolSession`), lists its tools, and filters them
against the agent's `tools:` list. When the model calls a tool, Agora forwards the call to the
server and returns the result to the model.

## Approval gates (HITL)

Approvals are **per agent, per tool**, not an MCP-wide setting: list a tool in the agent's
`approvals` to require human confirmation before it runs.

```yaml
agents:
  ops:
    tools: [read_file, write_file]
    approvals: [write_file]   # write_file pauses for approval; read_file runs freely
```

## Common servers

| Server | Command |
|---|---|
| filesystem | `npx @modelcontextprotocol/server-filesystem` |
| github | `npx @modelcontextprotocol/server-github` |

(Exact tool names come from each server's `tools/list`.)

## Exposing the RAG read-only (Agora *as* an MCP server)

Everything above is Agora as an MCP **client**. Agora can also act as a **server** to let another tool
(e.g. Claude Code) query the curated knowledge base — but **read-only**:

```bash
agora serve-mcp --config agora.yaml
```

This runs a local MCP **stdio** server that exposes a single tool, `rag_search`, backed by the same read
pipeline the CLI uses. It is the only sanctioned way to expose the RAG externally, and it is deliberately
contained:

- **Read-only** — only `rag_search` is exposed. **No write tool**: `rag_write` (which deletes/replaces
  curated knowledge and needs a human to resolve conflicts) **never leaves the CLI + human path**.
- **No network port** — it speaks JSON-RPC over stdin/stdout as a child process the client launches and
  owns; there is no socket to reach over the network and no authentication surface to misconfigure.
- **Local** — the client controls the process lifecycle (it starts and stops with the client).

Point an MCP client at it like any other stdio server — for example:

```json
{ "command": "agora", "args": ["serve-mcp", "--config", "agora.yaml"] }
```

> Agora is CLI-only with a human always present; there is no REST server. Read-only retrieval over MCP is
> the one external surface, and writes stay strictly on the CLI + human path.
