# 01 — Separate the memory store from the KB store

Status: ready-for-human

**Decision:** D9a · **Depends:** — · **Effort:** M

## Problem

`Retrieval.Build` (`src/Agora/Rag/Concretes/Retrieval.cs`) builds the RAG embedder+store first, then
lets context memory **reuse the same store** (`store ??= ...` is a no-op once RAG is on). So KB facts
and ephemeral `memory:` entries share one vector store. This forces two hacks and one leak:

- `KnowledgeBase.WriteAsync` queries neighbours across **both** kinds (handled in issue 05);
- `ContextMemory` tags entries with a `memory:` source prefix and `RecallAsync` over-fetches
  (`topK*4`) then filters by that prefix so KB facts don't crowd out memory;
- with `vector_store: file`, every run's working memory is persisted into `kb.json` forever.

## Proposal

- Give `ContextMemory` its **own** vector store, distinct from the KB store, built in `Retrieval.Build`.
- Remove the `SourcePrefix`/`memory:` tagging and the over-fetch+filter in
  `ContextMemory.RecallAsync` — a dedicated store makes them unnecessary.
- Keep `Retrieval` as the owner of both stores; `KnowledgeBase` binds to the KB store only,
  `ContextMemory` to the memory store only.
- This issue only separates the stores; the memory store's lifecycle is issue 02.

## Acceptance criteria

- [ ] KB writes and memory writes target different `IVectorStore` instances.
- [ ] `ContextMemory` no longer prefixes/filters by source; recall returns only memory entries because
      the store contains only memory entries.
- [ ] A test with both RAG and memory enabled asserts a `rag_write` neighbour query never returns a
      memory entry (and vice-versa).
- [ ] No memory entry is written to the KB `file` store.

## Notes

Enabler for issues 02 and 05. Pure refactor — no behavioural change to conflict handling yet.

## Comments

**2026-06-25 — implemented.** `Retrieval.Build` now gives `ContextMemory` its own `InMemoryVectorStore`,
separate from the KB store; `Retrieval._embedder/_store` are nullable (KB-only) and `IngestAsync`
guards on RAG being enabled. `ContextMemory` lost the `memory:` `SourcePrefix` tagging and the
over-fetch+filter in `RecallAsync` (plain top-K now). Tests: dropped `ContextMemoryTests.
Recall_FiltersOutNonMemoryEntries` (encoded the removed hack); added `RetrievalTests.
Build_RagAndMemory_UseSeparateStores` asserting KB reads never surface memory entries and vice-versa.
Full suite green (336/336). All changes uncommitted.
