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
between MCP and built-in by just removing/adding the MCP server.

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
