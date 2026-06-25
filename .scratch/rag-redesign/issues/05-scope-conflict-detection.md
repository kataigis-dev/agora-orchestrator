# 05 — Scope conflict-detection to KB entries + fail-narrow candidate set

Status: ready-for-human

**Decision:** D10 · **Depends:** 01 · **Effort:** S

## Problem

Two ways the conflict check asks the wrong question:

- **Wrong store.** `KnowledgeBase.WriteAsync` queries neighbours without scoping to KB-authored
  entries. After issue 01 the memory store is separate, so this is mostly resolved — but the KB write
  path must explicitly bind to the **KB store only** so it can never judge/delete a non-KB entry.
- **Fail-broad set.** `LlmConflictJudge.ConflictingEntries` falls back to **all** existing neighbours
  when the model returns no usable `CONFLICTS_WITH` list. A `Resolved`/replace then targets the entire
  neighbourhood, not just the conflicting entry — and (after issue 06) presents the human with the
  wrong candidates, training rubber-stamping.

## Proposal

- Confirm/enforce that `KnowledgeBase` neighbour queries hit only the KB store.
- Make `ConflictingEntries` **fail-narrow**: an empty/unparseable list → empty set (treated as
  `NoConflict`, no deletion), never "all".

## Acceptance criteria

- [ ] A conflict candidate set is never broader than what the judge explicitly named.
- [ ] An empty `CONFLICTS_WITH` yields no deletion (test).
- [ ] A `rag_write` cannot return a memory entry as a conflict candidate (regression of issue 01).

## Notes

Refines `.scratch/optimization-audit/issues/04-conflict-judge-fail-closed.md`: under D3 every conflict
already routes to a human, so the safety fix here is "narrow the candidates", not "fail to Unresolved".

## Comments

**2026-06-25 — implemented.** `LlmConflictJudge.ConflictingEntries` is now fail-narrow: an empty,
`NONE`, or unparseable `CONFLICTS_WITH` yields `Array.Empty<Chunk>()` instead of falling back to all
neighbours. `Parse` additionally guards both conflict branches on `conflicting.Count > 0`, so a conflict
verdict that names no entry collapses to `NoConflict` (nothing to delete or escalate) — D10's "empty
list → NoConflict". Part 1 (KB neighbour queries hit only the KB store) is already structural after
issue 01: `KnowledgeBase` is constructed with the KB `_store` only and memory has its own store; the
regression is covered by `RetrievalTests.Build_RagAndMemory_UseSeparateStores`. Tests: updated the two
parser tests that had omitted `CONFLICTS_WITH` (now `: 1`); added
`ConflictVerdict_WithNoNamedEntry_FailsNarrowToNoConflict` and
`UnparseableConflictsWith_NeverFallsBackToAll`. Suite green (343). Uncommitted.
