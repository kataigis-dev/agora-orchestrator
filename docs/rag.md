# RAG — Retrieval-Augmented Generation

## Overview

The RAG pipeline indexes knowledge (text files) and makes it available to agents as enriched
context. It is also **writable**: agents can record findings into a shared knowledge base.

## Configuration

```yaml
rag:
  enabled: true
  refine:
    strategy: none           # "none" | "llm"
    model: balanced          # model alias for the "llm" refiner
  retrieval:
    embedder:
      type: fake             # "fake" | "openai" | "ollama"
      provider: openai       # provider for key/base when not "fake"
      model: text-embedding-3-small
    vector_store:
      type: file             # "memory" | "file" | "qdrant"
      path: ./kb.json        # for "file"
      url: http://localhost:6334   # for "qdrant"
      collection: agora      # for "qdrant"
    top_k: 6
    score_threshold: 0.2
  ingest:
    sources: [ ./docs ]
    chunk_size: 800
    chunk_overlap: 120
```

## Ingest pipeline

```
1. Load   — read .txt/.md files from the configured sources (files or directories, recursive)
2. Chunk  — split into overlapping segments (chunk_size, chunk_overlap)
3. Embed  — generate vectors for each chunk
4. Store  — upsert into the vector store
```

```bash
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml
```

## Embedders

- `fake` — deterministic, offline, no key (good to start; not semantic)
- `openai` — e.g. `text-embedding-3-small` (needs an OpenAI provider/key)
- `ollama` — local (e.g. `nomic-embed-text`)

Real embedders are resolved by the edge-injected resolver (`AgentFrameworkEmbedders`), so the core
stays framework-free. Note: Anthropic has no embeddings endpoint — use `openai`/`ollama` for those.

## Vector stores

| Type | Notes |
|---|---|
| `memory` | In-process; lost on restart |
| `file` | JSON on disk; persists across runs |
| `qdrant` | Qdrant server over gRPC (`Agora.AgentFramework`, injected from the edge) |

`IVectorStore` is async (`UpsertAsync`/`QueryAsync`/`DeleteAsync`) with stable ids.

## Retrieval

At the start of a graph run, the user input is refined (optional), embedded, and the top-K most
similar chunks (above `score_threshold`) are injected as seed context for the entry agent.
Agents can also query on demand with the `rag_search` tool.

The default `score_threshold` is `0.2`, which avoids injecting obviously unrelated chunks while staying
lenient enough for lightweight local embedders. Set it lower for deterministic/fake embedder demos, or
raise it after measuring retrieval quality with `eval-quality`.

## Writable knowledge base

Agents write to the shared KB with the `rag_write` tool. `KnowledgeBase` embeds the new entry,
finds similar entries, asks an LLM `IConflictJudge` whether it conflicts, and either stores it,
auto-reconciles, or escalates to a human (`IConflictResolver`: keep existing / keep new / merge).

## Context memory

With `memory: { enabled: true }`, declared artifacts are stored and only the top-K relevant ones
are recalled into each agent's context — compressing tokens. See the context-memory page.
