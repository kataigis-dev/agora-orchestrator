# 03 — Serialize KB write critical section + thread-safe cache

Status: ready-for-human

**Decision:** D5 · **Depends:** — · **Effort:** S

## Problem

In a graph with parallel branches (`fe`/`be`) every agent shares the **same** `KnowledgeBase`
instance. `KnowledgeBase.WriteAsync` has no synchronisation:

- `_assessmentCache` is a plain `Dictionary` → concurrent writes are a data race (can throw/corrupt);
- the `query neighbours → judge → DeleteAsync → AddAsync` sequence is not atomic → branch A can delete
  a neighbour while branch B is judging against it.

The synchronous human gate (issue 07) widens the critical section to **human-length** waits, making
the race far more likely.

## Proposal

- Guard the `query→judge→resolve→apply` section with an async mutex (`SemaphoreSlim(1,1)`) per
  `KnowledgeBase`.
- Move `_assessmentCache` access inside the lock (or make it a `ConcurrentDictionary`).

## Acceptance criteria

- [ ] Concurrent `WriteAsync` calls on one `KnowledgeBase` are serialised (test: N parallel writes,
      no exception, deterministic final store state).
- [ ] No data race on the assessment cache under concurrency.
- [ ] Single-write path behaviour unchanged.

## Notes

Independent of the store separation; can land early. The human-gate widening is the motivation but the
race exists today.

## Comments

**2026-06-25 — implemented.** `KnowledgeBase` gained a `SemaphoreSlim(1,1) _writeLock`; `WriteAsync`
embeds outside the lock (no shared state) then takes the lock around the whole
`query→judge→resolve→apply` section, releasing in `finally`. The plain `_assessmentCache` Dictionary is
now only touched inside that section, so it stays a safe single-writer (no `ConcurrentDictionary`
needed). Matches the existing `CopilotTokenProvider` gate pattern (no `IDisposable`). Test added:
`KnowledgeBaseTests.ConcurrentWrites_AreSerialised_AllPersisted` (20 parallel writes, none rejected,
deterministic count, no cache/store race). Full suite green (341). Uncommitted.
