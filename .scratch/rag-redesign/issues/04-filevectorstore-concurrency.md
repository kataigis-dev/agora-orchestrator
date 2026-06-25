# 04 — FileVectorStore concurrent-write safety

Status: ready-for-human

**Decision:** bug · **Depends:** — · **Effort:** S

## Problem

`FileVectorStore` (`src/Agora/Rag/Concretes/FileVectorStore.cs`) holds an in-memory `List` and
**rewrites the whole JSON file** on every `UpsertAsync`/`DeleteAsync`, with no locking. Concurrent
mutations (parallel graph branches) interleave and **lose writes or corrupt the file**. A crash
mid-write can also truncate `kb.json`.

## Proposal

- Serialise mutations with an async lock around the load/mutate/save section.
- Write atomically: serialise to a temp file then `File.Move`/replace, so a crash never leaves a
  partial file.

## Acceptance criteria

- [ ] Concurrent upserts/deletes do not lose entries (test: N parallel upserts, all present after).
- [ ] The backing file is never left partially written (atomic replace).
- [ ] Existing `FileVectorStoreTests` still pass.

## Notes

Latent today; serialising KB writes (issue 03) reduces but does not remove it — the store must be safe
on its own (memory store and ingest also use vector stores).

## Comments

**2026-06-25 — implemented.** `FileVectorStore` gained a `SemaphoreSlim(1,1) _lock`; `UpsertAsync` and
`DeleteAsync` take it around the mutate+save section, and `QueryAsync` snapshots `_items` under the lock
(so a concurrent mutation can't throw "collection modified" mid-scan). `SaveAsync` now writes to
`<path>.tmp` then `File.Move(temp, path, overwrite: true)` — an atomic replace, so a crash mid-write
never leaves a partial/corrupt `kb.json`. Test added:
`FileVectorStoreTests.ConcurrentUpserts_DoNotLoseEntries` (30 parallel upserts, all present in memory
and after reload, no leftover `.tmp`). Existing FileVectorStore tests still pass. Suite green (341).
Uncommitted.
