# 11 — Read-only RAG exposure via MCP stdio server

Status: ready-for-human

**Decision:** D8 · **Depends:** 10 · **Effort:** L

## Problem

The only legitimate reason to expose the RAG externally is **read-only** retrieval (another tool, e.g.
Claude Code, querying the curated KB). The retired REST server (issue 10) did this as a networked,
unauthenticated, mutation-capable surface. An **MCP stdio server** is strictly more contained: a local
child process the client launches — no network port, no auth hole — and it composes with the existing
MCP machinery Agora already speaks.

## Proposal

- Add a CLI subcommand (e.g. `agora serve-mcp --config <file>`) that runs an **MCP stdio server**
  exposing **only** `rag_search` over the KB store (read path / `RagPipeline`).
- Expose **no** write tool — `rag_write` never leaves the CLI+human path.
- Document it as a local, read-only exposure; the client owns the process lifecycle.

## Acceptance criteria

- [ ] An MCP client can connect over stdio and call `rag_search`, getting the same results as the CLI.
- [ ] No write/mutation tool is exposed by the server.
- [ ] The server opens no network port.
- [ ] Docs describe the read-only MCP exposure and that writes stay CLI-only.

## Notes

Replaces `Agora.Api`'s sole purpose. Largest item — new MCP-server surface; check
`ModelContextProtocol.Core` for the server-side API (Agora currently only uses it as a client).

## Comments

**2026-06-25 — implemented.** New CLI verb `agora serve-mcp --config <file>` runs a local, read-only MCP
**stdio** server exposing only `rag_search` over the KB read pipeline. Edge-DI seam: core defines
`Agora.Cli.IRagMcpServer` (a `Func<string, CancellationToken, Task<string>>` search delegate); the
concrete `RagMcpStdioServer` lives in `Agora.AgentFramework` (server SDK is outside the framework-free
core) using `ModelContextProtocol.Server` (`McpServer.Create` + `StdioServerTransport` + a single
`McpServerTool.Create` named `rag_search`); `Program.cs` injects it. `CommandStrategy.ServeMcp` builds
the runtime, refuses when there's no enabled RAG, and serves `query → runtime.Rag.RunAsync(query).AsContext()`
(the same results as the CLI). No write tool is exposed (`rag_write` stays CLI + human only) and stdio
opens no network port. Docs: new "Agora as an MCP server" section in `docs/mcp.md`, plus `serve-mcp` and
`purge-kb-log` entries in `docs/cli.md`. Tests: `RagMcpStdioServerTests.BuildTools_ExposesOnlyReadOnlyRagSearch`
(read-only: exactly one `rag_search` tool, no write tool) and `CliServeMcpTests` (wires the read pipeline;
errors when no RAG). Server SDK usage compiled first try against `ModelContextProtocol.Core` 1.4.0. Suite
green (361). Uncommitted.
