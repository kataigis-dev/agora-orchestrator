# 10 — Retire `Agora.Api` (REST server)

Status: ready-for-human

**Decision:** D7 · **Depends:** — · **Effort:** M

## Problem

`Agora.Api` is a REST server (`POST /runs`, `GET /runs/{id}`, `/ingest`, …) — a headless,
programmatic, no-human, networked surface (also unauthenticated, per the audit). The product is now
**CLI-only, human always present, never CI/CD** (D6). A REST server is the opposite of that and its
only intended purpose (exposing the RAG) is replaced by a read-only MCP server (issue 11).

## Proposal

- Remove the `src/Agora.Api` project and `tests/Agora.Api.Tests` from the solution (`Agora.slnx`).
- Delete the project directories and their build artifacts (`staticwebassets`, etc.).
- Remove README/docs references to the REST API (`docs/api.md`, README "REST API" rows).

## Acceptance criteria

- [ ] `Agora.slnx` no longer references `Agora.Api`/`Agora.Api.Tests`.
- [ ] The solution builds and the remaining test projects pass.
- [ ] No dangling docs/links point to the removed REST API.

## Notes

Land issue 11 (read-only MCP exposure) alongside or just after, so the "expose the RAG" capability is
not lost — just moved to a contained, local, read-only surface.

## Comments

**2026-06-25 — implemented.** Removed `src/Agora.Api/` and `tests/Agora.Api.Tests/` (directories +
build artifacts) and dropped both from `Agora.slnx`. No core/CLI/AgentFramework source referenced
`Agora.Api`, so the solution builds and the remaining suite passes (358; the 8 Api tests are gone).
Docs/examples cleaned: deleted `docs/api.md` and `examples/agora-api.http`; removed the REST rows/links
and project-structure lines from `README.md` and `docs/index.md`; rewrote the API approval paragraph in
`docs/agents.md`; replaced the `Agora.Api` section in `docs/concepts/18-class-reference.md` and the
API-path section + entry-points table in `docs/concepts/17-execution-flow.md` (now CLI-only + the read-
only MCP note); dropped `src/Agora.Api` from `examples/agora-wiki.yaml`'s filesystem-server args. No
dangling REST links remain in README/docs/examples (remaining mentions are only in the auto-generated
`llmwiki/`). The docs forward-reference `agora serve-mcp` (issue 11, implemented next). DESTRUCTIVE but
reversible (uncommitted; `git restore` recovers). Uncommitted.
