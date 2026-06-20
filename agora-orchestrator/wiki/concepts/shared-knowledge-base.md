---
type: concept
title: Shared Knowledge Base (writable RAG)
tags: [rag, knowledge, vector-store, conflict, hitl, write]
related: [rag-pipeline, human-in-the-loop, agent-graph, handoff-context, agora-orchestrator]
created: 2026-06-19
updated: 2026-06-20
---

# Shared Knowledge Base (writable RAG)

Evolution of the [[rag-pipeline]] from a read-only store into a **shared, writable knowledge base**:
agents write to it when they produce changes, and every write is compared against what already
exists to detect conflicts.

> Agents read/write the KB via tools (`rag_search`/`rag_write`) and can ask another agent
> (`ask_agent`) after searching the RAG. See also [[agent-graph]] and [[handoff-context]].

## RAG access: `IVectorStore`

Access **always** goes through the `IVectorStore` interface. The implementation can be local or
remote; the core stays framework-free and any adapter toward a containerized DB is injected from the
edge (like the real embedder/provider).

```csharp
public interface IVectorStore // async: no sync-over-async with remote DBs
{
    Task UpsertAsync(IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors, CancellationToken ct = default);
    Task<IReadOnlyList<Chunk>> QueryAsync(IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0, CancellationToken ct = default);
    Task DeleteAsync(IReadOnlyList<string> ids, CancellationToken ct = default); // needed to replace entries
}
```

| Implementation | Where | Persistence |
|----------------|-------|-------------|
| `InMemoryVectorStore` | core | in-process (lost on restart) |
| `FileVectorStore` | core | JSON on disk (`vector_store: { type: file, path: … }`) |
| `QdrantVectorStore` | `Agora.AgentFramework` | Qdrant server over **gRPC** (`vector_store: { type: qdrant, url, collection }`) |

Non-core types (e.g. `qdrant`) are unknown to the core: they are resolved by an **edge-injected
resolver** (`AgentFrameworkVectorStores.TryCreate`, passed to `RagFactory`/`Runtime` as
`storeResolver`), keeping the core framework-free.

`Chunk` carries a stable `Id` assigned by the store; `Score` is the transient similarity of the last
query. `Id` + `Delete` allow **replacing** a superseded entry instead of appending.

## Writing: `KnowledgeBase`

`KnowledgeBase.WriteAsync(text, source, agentId)` is the agent-driven write path:

1. **embed** the new text → query the store for neighbors;
2. no neighbors → write directly (`Added`);
3. neighbors present → `IConflictJudge` assesses the conflict;
4. `NoConflict` → write (`NoConflict`);
5. `Resolved` → **replace** the conflicting entries with the reconciled one (`AutoResolved`);
6. `Unresolved` → escalate to `IConflictResolver` (human).

**Efficiency**: the LLM judge only fires if the closest neighbor exceeds `conflictThreshold`
(default 0.8) — loosely related entries are added without judging; the judge outcomes are also
**cached** per `(text, neighbors)`, so identical writes do not re-invoke the LLM.

## Conflict detection: `IConflictJudge`

`LlmConflictJudge` asks an LLM to reply with a marker protocol:

```
VERDICT: <NO_CONFLICT|RESOLVED|UNRESOLVED>
CONFLICTS_WITH: <indices of the conflicting existing entries, or NONE>
RESOLUTION: <if RESOLVED: a single statement true to both old and new>
EXPLANATION: <one line>
```

`CONFLICTS_WITH` lets it **replace only the actually-conflicting entries**, not all neighbors.
`NoOpConflictJudge` never reports a conflict (used when no provider is available).

## Human resolution: `IConflictResolver`

When the agent cannot resolve it, the conflict escalates to the user. It is a second HITL channel,
distinct from the yes/no `IApprovalHandler` of [[human-in-the-loop]], because it needs a three-way
choice:

```csharp
public enum ConflictResolution { KeepExisting, KeepNew, Merge }
```

| Implementation | Context |
|----------------|---------|
| `ConsoleConflictResolver` | CLI — prompts `[e]xisting / [n]ew / [m]erge` |
| `FakeConflictResolver` | Tests — preset decision |

- `KeepExisting` → discard the new entry (`Rejected`, no write)
- `KeepNew` → replace the conflicting entries with the new one (`UserResolved`)
- `Merge` → write the user-reconciled text (`UserResolved`)

With no resolver available, an unresolved conflict yields `Rejected`.

## Agent tools

Agents access the KB via built-in tools, enabled by listing them in `tools` (same mechanism as the
filesystem tools). They live in `Agora.AgentFramework`:

| Tool | Action |
|------|--------|
| `rag_search(query)` | Retrieve relevant context from the KB (read via `RagPipeline`) |
| `rag_write(text)` | Record an entry (write via `KnowledgeBase`, with conflict-check) |
| `ask_agent(target, question)` | Ask another agent and get its answer |

```yaml
agents:
  writer:
    tools: [rag_search, rag_write, ask_agent]
```

**"RAG-first, then ask" flow** (see [[handoff-context]]): in handoff mode the receiver starts with
little context; the tool descriptions instruct it to use `rag_search` first and only then
`ask_agent`. `ask_agent` re-runs the target agent **synchronously** and returns its answer in the
current turn; the target is built in "answer-mode" **without** `ask_agent`, preventing A↔B recursion.

`Runtime` builds the `KnowledgeBase` reusing the `RagPipeline`'s `Embedder`/`Store`, so read and
write use the **same** `IVectorStore` instance.

## Components

| Class / interface | Role |
|-------------------|------|
| `Rag/KnowledgeBase` | Orchestrates embed → neighbors → judge → write/escalate |
| `Rag/IConflictJudge`, `LlmConflictJudge`, `NoOpConflictJudge` | Conflict detection |
| `Rag/FileVectorStore` | Disk-persistent store |
| `HumanInTheLoop/IConflictResolver` | Human conflict resolution |
| `AgentFramework/RagTools`, `AskAgentTool` | Agent tools `rag_search`/`rag_write`/`ask_agent` |

## Notes

- Reading (`RagPipeline`) and writing (`KnowledgeBase`) share the **same** `IVectorStore` instance
  (crucial with `FileVectorStore`/DB).
- The similarity threshold for considering two entries "neighbors" is configurable on the
  `KnowledgeBase` (`scoreThreshold`, default 0.5).
- The judge uses `defaults.model`; with no provider/model it falls back to `NoOpConflictJudge`.
