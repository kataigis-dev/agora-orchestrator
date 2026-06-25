# 03 — Groundedness enforcement & abstention

Status: ready-for-agent

**Addresses:** F4 (HIGH) · **Axes:** hallucinations, precision · **Impact:** ★★ · **Effort:** M

## Problem

RAG chunks are computed once per run (`Runtime.RunAsync`) and delivered as `seedContext` to the **entry node only**. Nothing instructs the model to ground its answer in the retrieved chunks or cite them; empty retrieval proceeds silently; there is no post-hoc faithfulness check. The strongest anti-hallucination levers — grounded generation, citation, abstention on weak evidence — are absent.

## Proposal

- A RAG-grounding preamble (analogous to `H2cPreamble`/`HandoffPreamble`) for agents consuming retrieved context: "answer only from the provided context; if it is insufficient, say so" + lightweight citation convention.
- Explicit empty-retrieval handling: signal "no relevant context" rather than injecting nothing silently (pairs with issue 02).
- Optional post-hoc groundedness check (can reuse the issue-01 judge) flagging unsupported claims.

## Acceptance criteria

- [ ] RAG agents receive a grounding/abstention instruction; covered by a test.
- [ ] Empty/low-confidence retrieval is surfaced, not silent.
- [ ] (If issue 01 landed) a groundedness score is available for RAG runs.

## Notes

Keep deterministic where possible; use the LLM-judge only for the faithfulness portion that isn't machine-checkable — consistent with the project's stated "prefer deterministic checks" rule.

## Comments
