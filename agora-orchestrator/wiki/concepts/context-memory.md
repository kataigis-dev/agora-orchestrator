---
type: concept
title: Context Memory (RAG-backed compression)
tags: [memory, context, compression, rag, tokens, orchestration]
related: [shared-knowledge-base, handoff-context, agent-graph, rag-pipeline, agora-orchestrator]
created: 2026-06-20
updated: 2026-06-20
---

# Context Memory

Vector-store-backed working memory to **compress context and save tokens** without losing too much
accuracy. Instead of injecting *all* accumulated artifacts (`State.ArtifactSummary`) into every
agent, the system **saves** declared artifacts and **recalls only the top-K most relevant** ones for
the agent's task.

## Activation

Opt-in; disabled by default.

```yaml
memory:
  enabled: true
  top_k: 5             # relevant entries recalled per agent
  max_chars: 0         # budget on recalled context (0 = unlimited)
  remember_outputs: false  # also save the (truncated) output, not just declared artifacts
```

## How it works

At each graph step ([[agent-graph]]):

1. **Save** — the agent's **declared artifacts** (`<<artifact key=value>>`, including `handoff`) are
   written to memory: embed + append-only upsert, **without** conflict-check (it is transient
   context, not a KB "truth").
2. **Recall** — to build the next agent's context, its task (`UserInput` + inbox) is embedded and
   the **top-K most relevant entries** are recalled, injected instead of the full artifact dump.

The result: the shared context stays **capped at K entries** even on long graphs, keeping what is
relevant to the current step.

## Separation from the knowledge base

Memory entries are tagged with a `source` prefix `memory:` and share the same `IVectorStore` as the
[[shared-knowledge-base]]. Recall **filters** to `memory:` entries only, so curated KB facts and
transient context do not mix. Memory reuses the RAG's `Embedder`/`Store` when enabled; otherwise it
falls back to an offline embedder/store.

## Relationship to the other modes

- **[[handoff-context]]**: the handoff passes the *targeted* payload to the next agent; memory
  provides the relevant recalled *background*. They compose.
- **[[shared-knowledge-base]]**: same store technology, different purpose — memory = compressible
  transient context; KB = durable facts with conflict resolution.

## Implementation

| Component | Role |
|-----------|------|
| `Configuration/MemoryConfig` | `enabled` + `top_k` + `max_chars` + `remember_outputs` |
| `Rag/ContextMemory` | `RememberAsync` (append-only) / `RecallAsync` (filtered top-K) |
| `Orchestration/GraphExecutor` | In memory-mode: recall instead of `ArtifactSummary`, remember artifacts |
| `Runtime` | Builds `ContextMemory` (reuses the RAG embedder/store) |

## Example

`examples/agora-memory.yaml` — natural pipeline with context memory enabled.

## Notes

- By default **only declared artifacts** are saved: if agents declare nothing, memory stays empty.
  With `remember_outputs: true` the (truncated) output is also saved, so memory does not depend on
  prompt discipline.
- `max_chars` caps the recalled context size (always keeps the most relevant entry), to guarantee a
  token ceiling.
- Compression is **by relevance** (vector retrieval), with no extra LLM calls.
