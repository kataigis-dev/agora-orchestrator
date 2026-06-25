# 02 — Per-run ephemeral memory + resume re-seed from artifacts

Status: ready-for-human

**Decision:** D9b · **Depends:** 01 · **Effort:** M

## Problem

Memory exists only to compress handover context **within a single run**, but today it can be backed by
a persistent store (issue 01) and survives across runs with no defined lifecycle. Cross-run memory
both bloats the store and creates a privacy footprint (run context on disk) for no benefit.

## Proposal

- Back `ContextMemory` with an **in-memory** store, created fresh per run and discarded at run end —
  regardless of the KB store type.
- On `ResumeAsync`, **re-seed** the memory from the restored `State.Artifacts` (the checkpoint already
  carries them) rather than persisting the memory itself. The checkpoint stays the single source of
  truth.

## Acceptance criteria

- [ ] Memory is never written to disk, even when the KB uses a `file`/`qdrant` store.
- [ ] Two consecutive runs do not share memory entries.
- [ ] A resumed run recalls memory consistent with the artifacts present at the checkpoint (test:
      checkpoint mid-run, resume, assert recall includes the pre-checkpoint artifacts).

## Notes

Keeps "memory = handover optimization" honest. Pairs with issue 01.

## Comments

**2026-06-25 — implemented.** `Retrieval` no longer holds a single `Memory` instance; it exposes
`MemoryEnabled` + a `NewMemory()` factory that mints a fresh `ContextMemory` over its own
`InMemoryVectorStore` (memory is never written to the KB's possibly-persistent store — AC1).
`Runtime.BuildExecutor` calls `NewMemory()` per `RunAsync`/`ResumeAsync`, so consecutive runs get
independent memory (AC2). `GraphExecutor.RunAsync` re-seeds memory from `State.Artifacts` on resume
via `ReseedMemoryAsync`, mirroring `RememberAsync`'s `"{key}: {value}"` format so recall matches the
pre-checkpoint artifacts (AC3). `Retrieval.ForPipeline` lost its unused `memory` param. Tests:
`RetrievalTests` use `NewMemory()`/`MemoryEnabled`; added `NewMemory_MintsFreshInstanceEachCall`,
`MemoryExecutorTests.Resume_ReseedsMemoryFromCheckpointArtifacts`, and
`RuntimeMemoryTests.TwoRuns_DoNotShareMemory`. Full suite green (339). All changes uncommitted.
