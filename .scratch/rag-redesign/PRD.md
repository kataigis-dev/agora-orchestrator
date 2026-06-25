# PRD — RAG redesign: separate KB from memory, human-gated writes, CLI-only

Status: ready-for-human

## Context

The RAG subsystem conflated three different concerns into one shared store with incoherent
semantics (discovered in a design grilling, 2026-06-25):

- the conflict-check lived only on the deliberate `rag_write` path, never on ingest;
- the run "context memory" was append-only **and** — because `Retrieval.Build` lets memory reuse the
  RAG store — was **persisted forever** into the same `kb.json` as durable knowledge;
- conflict resolution could run with a `fake` (non-semantic) embedder or a `NoOpConflictJudge`,
  silently degrading to a no-op; the `Resolved` verdict let an **LLM delete/replace** KB entries with
  no human and no audit trail;
- the `Agora.Api` REST server exposed the whole thing headless, without auth.

This redesign re-separates the three concerns and tightens the product scope.

## Decision log (settled in grilling)

- **D1** — the writable, self-reconciling KB is a real requirement; it stays.
- **D2** — fail-loud config validation: if any agent allow-lists `rag_write`, `validate`+build reject a
  `fake`/missing embedder or an unresolvable conflict-judge model. `NoOp`+`fake` legal only for a
  read-only KB.
- **D3** — the LLM judge only **detects and proposes** a merge; it never auto-applies. Every conflict
  **and** every deletion requires human confirmation.
- **D4** — synchronous gate: `rag_write` blocks until the human resolves (reuse the `ApprovalGate`
  pattern).
- **D5** — serialize the KB write critical section (`query→judge→resolve→apply`) with an async mutex;
  the assessment cache becomes thread-safe / lock-guarded.
- **D6** — product is **CLI-only, human always present, never CI/CD**. A non-interactive context
  (no TTY) where `rag_write` is reachable is **refused**, not silently degraded.
- **D7** — `Agora.Api` (REST server) is retired entirely.
- **D8** — external RAG exposure is **read-only**, via an **MCP stdio server** exposing only
  `rag_search`. `rag_write` never leaves the CLI+human path.
- **D9** — KB and memory live in **separate stores**. KB persists; memory is **per-run, in-memory,
  discarded at run end**, re-seeded from `State.Artifacts` on resume.
- **D10** — conflict-detection neighbours are scoped to **KB-authored entries only**;
  `ConflictingEntries` is **fail-narrow** (empty list → NoConflict, never "all").
- **D11** — KB mutations are hard-deleted from the store but recorded in an **append-only mutation
  log** (old/new text, agent, human decision, timestamp, judge explanation) with its own
  retention/purge.

## Non-goals

- Re-introducing any headless/programmatic write path.
- Conflict-checking the ingest corpus (curated by a human via CLI).
- Multi-tenant / networked access.

## Pillars (target state)

1. **Ingest** — corpus loaded by a human via CLI; no conflict-check.
2. **KB (writable, durable)** — D1–D5, D10, D11.
3. **Memory (ephemeral, handover token-optimization)** — D9.

Scope/exposure cross-cuts: D6, D7, D8.

## Issues (dependency order)

| # | Issue | Decision | Depends |
|---|---|---|---|
| 01 | Separate the memory store from the KB store | D9a | — |
| 02 | Per-run ephemeral memory + resume re-seed from artifacts | D9b | 01 |
| 03 | Serialize KB write critical section + thread-safe cache | D5 | — |
| 04 | FileVectorStore concurrent-write safety | bug | — |
| 05 | Scope conflict-detection to KB entries + fail-narrow | D10 | 01 |
| 06 | Judge detects+proposes; remove auto-apply; human on every conflict | D3 | 05 |
| 07 | Synchronous conflict gate + always-present resolver; refuse non-interactive | D4, D6 | 06 |
| 08 | Fail-loud config validation for `rag_write` | D2 | — |
| 09 | KB mutation log (append-only) + retention/purge | D11 | 06 |
| 10 | Retire `Agora.Api` (REST) | D7 | — |
| 11 | Read-only RAG exposure via MCP stdio server | D8 | 10 |

## Related

Supersedes the approach of `.scratch/optimization-audit/issues/04-conflict-judge-fail-closed.md`:
under D3 every conflict already routes to a human, so the fix becomes "fail-narrow candidate set"
(issue 05) rather than "fail-closed to Unresolved".

## Comments

**2026-06-25 — all 11 issues implemented (uncommitted).** Built autonomously in dependency order; each
issue file carries its own implementation note. Roll-up:

- **Memory vs KB (D9):** memory has its own in-memory store, minted **per run** by `Retrieval.NewMemory()`
  and discarded at run end, re-seeded from `State.Artifacts` on resume (01, 02).
- **KB safety (D5, bug):** the `KnowledgeBase` write critical section is serialised with a `SemaphoreSlim`,
  and `FileVectorStore` mutations are locked + atomically written (temp+move) (03, 04).
- **Conflict policy (D10, D3, D4, D6):** the judge is fail-narrow (empty/unparseable `CONFLICTS_WITH` →
  NoConflict) and detect-only; **every** conflict/deletion goes to a human via `IConflictResolver`
  (no auto-apply), the judge's merge rides along as a suggestion, the gate is synchronous, and a
  non-interactive run that could reach `rag_write` is refused (05, 06, 07).
- **Fail-loud config (D2):** `RagWriteValidator` rejects `rag_write` + fake/missing embedder or
  unresolvable judge model at `validate` and at Runtime build; the wizard and `agora.yaml` updated (08).
- **Audit (D11):** every KB add/replace is recorded to an append-only `kb-mutations.jsonl`; purgeable via
  `agora purge-kb-log` (09).
- **Scope/exposure (D7, D8):** `Agora.Api` (REST) removed entirely; external exposure is read-only via
  `agora serve-mcp` (local MCP stdio, `rag_search` only, no port) (10, 11).

Full suite green throughout (final: **361** in Agora.Tests; the 8 Agora.Api.Tests retired with issue 10).
Nothing committed — awaiting human review and per-issue commits.
