# 04 — KB conflict judge should fail closed

Status: ready-for-human

**Addresses:** F5 (MEDIUM) · **Axes:** precision, hallucinations · **Impact:** ★★ · **Effort:** S

## Problem

`LlmConflictJudge.Parse` (`src/Agora/Rag/Concretes/LlmConflictJudge.cs`) treats *"anything we cannot classify → `NoConflict`"*, and `ConflictingEntries` falls back to *all* existing entries on an unparseable list. So a malformed or ambiguous judge reply **silently admits** a possibly-contradictory fact into the shared knowledge base, poisoning future retrieval — a downstream hallucination source. For a reliability-first system the KB-write gate should fail **closed**.

## Proposal

- On unparseable/ambiguous verdict, return `Unresolved` (route to the human `IConflictResolver`) instead of `NoConflict`.
- Distinguish a confident `NO_CONFLICT` from a parse failure; only the former admits the entry unattended.

## Acceptance criteria

- [ ] A malformed judge reply does **not** silently admit the entry; it routes to `Unresolved`/human.
- [ ] A confident `NO_CONFLICT` still admits as before (no regression).
- [ ] A test covers the parse-failure → fail-closed path.

## Notes

Marked `ready-for-human` because it is a behavioral/safety policy change to the KB write path (admission semantics) — worth a maintainer decision on the exact fail-closed routing.

## Comments
