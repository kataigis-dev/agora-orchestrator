---
type: concept
title: RAG Pipeline
tags: [rag, retrieval, embedding, vector-store, knowledge]
related: [agora-orchestrator, agora-agent-framework, skills, shared-knowledge-base, context-memory]
created: 2026-06-17
updated: 2026-06-20
---

# RAG Pipeline

The **Retrieval-Augmented Generation** pipeline built into Agora. It lets agents access external
knowledge without changing their code.

## Architecture

```
Ingest:    Sources → TextChunker → IEmbedder → IVectorStore
Retrieve:  UserInput → IRefiner → query → IVectorStore.Query → EnrichedInput
Execution: EnrichedInput injected as seed context into the graph
```

## Components

| Class | Interface | Role |
|-------|-----------|------|
| `Ingestor` | — | Reads sources, chunks, embeds, indexes |
| `TextChunker` | — | Splits text into overlapping chunks |
| `IEmbedder` | → `FakeEmbedder`, `AgentFrameworkEmbedder` | Generates embedding vectors |
| `IVectorStore` | → `InMemoryVectorStore`, `FileVectorStore`, `QdrantVectorStore` | Cosine search; async `Upsert`/`Query`/`Delete` by id |
| `IRefiner` | → `LlmRefiner` / `NoOpRefiner` | Refines the query before retrieval |
| `RagPipeline` | — | Orchestrates retrieve + refine |
| `RagFactory` | — | Instantiates the pipeline from config |

## YAML configuration

```yaml
rag:
  enabled: true
  refine:
    strategy: none        # "none" | "llm"
    model: balanced       # model alias for the "llm" refiner
  retrieval:
    embedder:
      type: fake          # "fake" | "openai" | "ollama"
      provider: openai    # provider for key/base (for non-fake types)
      model: text-embedding-3-small
    vector_store:
      type: memory        # "memory" | "file" | "qdrant"
      path: ./kb.json     # for "file"
      url: http://localhost:6334   # for "qdrant" (gRPC)
      collection: agora   # for "qdrant"
    top_k: 6
    score_threshold: 0.0
  ingest:
    sources: [ ./docs ]
    chunk_size: 800
    chunk_overlap: 120
```

## Graph integration

The RAG seed is added as a `"rag"` message at the front of the entry node's message queue
(`state.Messages`). The agent receives it as extra context in its inbox.

## Write path

Beyond reading (retrieve), the RAG can be **written** by agents via `KnowledgeBase`, with conflict
detection and resolution (agent or human). See [[shared-knowledge-base]].

## Example

`examples/agora-rag.yaml` — full configuration with RAG enabled.

## Notes

- `InMemoryVectorStore` is in-memory: lost on restart (simple, not for production)
- `FileVectorStore` (`type: file`) persists to disk as JSON, surviving restarts
- `QdrantVectorStore` (`type: qdrant`) uses a Qdrant server over gRPC (official `Qdrant.Client`);
  it lives in `Agora.AgentFramework` and is injected from the edge — see [[shared-knowledge-base]]
- `LlmRefiner` calls the LLM provider to rewrite the query before retrieval
- `FakeEmbedder` is used in tests to avoid real provider calls
- Real embedders (`type: openai`/`ollama`) are selectable **from config**: the core resolves
  key/base from the provider and passes them to an edge-injected resolver
  (`AgentFrameworkEmbedders.TryCreate`), like the vector store
