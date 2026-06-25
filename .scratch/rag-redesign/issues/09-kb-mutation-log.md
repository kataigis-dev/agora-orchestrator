# 09 — KB mutation log (append-only) + retention/purge

Status: ready-for-human

**Decision:** D11 · **Depends:** 06 · **Effort:** M

## Problem

`KnowledgeBase.ReplaceAsync` hard-deletes superseded entries and adds the reconciled text. The
deleted text is gone — no record of *what* was removed, *by whom*, *when*, or *why*. Human
confirmation (issue 06) gives authorization at decision time but no accountability or reversibility
afterwards — the last gap from the enterprise/privacy audit.

## Proposal

- Append every KB mutation (add / replace / delete) to an **append-only mutation log**: old text, new
  text, agent id, the human decision, timestamp, judge explanation.
- Keep the vector store hard-deleting (hot path / query semantics stay simple); the log is the audit
  surface, separate from retrieval.
- The log must have its own **retention/purge** procedure (it retains deleted content → subject to
  GDPR erasure). Provide a purge command/API; do not make it eternal-and-untouchable.

## Acceptance criteria

- [ ] Every add/replace/delete on the KB produces one log record with the fields above.
- [ ] The log is append-only and survives restarts.
- [ ] A documented purge path can remove records (with a test).
- [ ] Retrieval/query performance is unaffected (no tombstones in the vector store).

## Notes

Closes the audit finding "no audit trail for KB mutations". Mind the erasure tension: the log itself
holds deleted content.

## Comments

**2026-06-25 — implemented.** New `KbMutation` record (Kind add/replace/delete, AgentId, OldText[],
NewText, Decision, JudgeExplanation, Timestamp), `IKbMutationLog` (Append/ReadAll/Purge), and
`FileKbMutationLog` — JSON Lines so a normal append never rewrites history and the log survives restarts;
`PurgeAsync` is the only rewrite (atomic temp+move), supporting retention/GDPR erasure. `KnowledgeBase`
takes an optional `IKbMutationLog` and records one record per real mutation (every add branch → Add;
human keep-new/merge → Replace with the superseded OldText, decision, and judge explanation); a Rejected
(keep-existing) write records nothing. The vector store keeps hard-deleting (no tombstones → retrieval
perf unchanged); the log is the separate audit surface. Wired: `Retrieval.Build` forwards the log to
`KnowledgeBase`; `Runtime` builds a `FileKbMutationLog` at `<configDir>/kb-mutations.jsonl` when RAG is
enabled (lazily created on first mutation). New CLI verb `purge-kb-log --config <file> [--before <ISO>]`
is the documented purge path. Tests: `FileKbMutationLogTests` (append-only/survives-restart, purge by
cutoff, purge-all, missing-file), `KnowledgeBaseMutationLogTests` (add logged, replace logged with
old/new/decision/explanation, rejected logs nothing), `CliPurgeKbLogTests`. Suite green (366).
Uncommitted.
