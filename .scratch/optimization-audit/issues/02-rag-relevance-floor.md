# 02 — RAG relevance floor (default score threshold)

Status: ready-for-agent

**Addresses:** F3 (HIGH) · **Axes:** token + precision (two-axis win) · **Impact:** ★★★ · **Effort:** S

## Problem

`RetrievalConfig.ScoreThreshold` defaults to `0.0` and `RagPipeline`'s constructor defaults `scoreThreshold = 0.0` (`src/Agora/Rag/Concretes/RagPipeline.cs`, `src/Agora/Configuration/RagConfig.cs`). Retrieval therefore returns the top-K chunks *regardless of similarity*. Low-relevance chunks get injected into context, which both **wastes tokens** and **grounds the model on noise** — a direct hallucination vector. This is the single highest-leverage fix in the audit: one default moves two axes.

## Proposal

- Set a sensible non-zero default relevance floor (validate empirically against the embedder's score distribution; document the chosen value and rationale).
- When all chunks fall below the floor, return *empty* retrieval rather than forcing K low-quality chunks (pairs with issue 03's empty-retrieval handling).
- Keep it configurable per `RetrievalConfig.ScoreThreshold`.

## Acceptance criteria

- [ ] Default `ScoreThreshold > 0`, with a documented justification.
- [ ] Retrieval below the floor yields no chunk rather than noise.
- [ ] A test asserts that sub-threshold chunks are excluded.
- [ ] `docs/rag.md` documents the default and how to tune it.

## Notes

Quantify the win with issue 01 once available (tokens saved + faithfulness delta). Until then, justify the default from the embedder's similarity distribution.

## Comments
