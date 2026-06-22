# Research Log

## 2026-06-23 — Architecture deepening: backend seam, retrieval module, pure routing

Five refactors turning shallow modules/leaky seams into deep ones (273 → 301 tests green):
- **One framework seam**: the four parallel injections (`IToolAgentFactory` + the vector-store /
  embedder / spec-store resolver delegates) collapse into a single **`IAgentBackend`**, with one
  adapter `AgentFrameworkBackend`. Removed `IToolAgentFactory`/`AgentFrameworkToolAgentFactory`
- **Retrieval as one module**: new **`Retrieval`** owns the shared embedder + vector store and, over
  them, the pipeline, knowledge base, and memory — concentrating the "reads and writes share one
  store" rule and the build order that `Runtime` used to hand-wire. `RagPipeline.Embedder/Store` are
  no longer public; ingest goes through `Retrieval.IngestAsync`
- **Pure routing**: signal-based next-node logic extracted to **`EdgeResolver.Next`**, a pure function
  `(edges, signals, loopCounters) → (next, loopCounters')`; `GraphExecutor.NextNode` no longer mutates
  the state's loop counters as a side effect
- **State/snapshot**: `State` is now JSON-serializable and **`StateSnapshot`** wraps it with the run
  cursor instead of mirroring every field — no more hand-written `From`/`ToState` duplication
- **Console prompter**: the wizard's prompt primitives extracted to a testable **`Prompter`** over the
  injected I/O; `ConfigWizard` composes it. +28 tests (`EdgeResolver`/`Retrieval`/`Prompter`)

## 2026-06-20 — Improvements: embedder from config, efficient conflict-judge, memory, eval

Started the improvement program (10 tracked items). Completed so far:
- **Embedder from config**: `type: openai|ollama` selectable from YAML via an edge-injected resolver
  (`AgentFrameworkEmbedders`); keys resolved from the provider just like the chat client
- **Efficient conflict-judge**: similarity prefilter (`conflictThreshold`) + outcome cache
- **Evolved context memory**: `max_chars` (recall budget) + `remember_outputs` (also saves outputs)
- **Eval/replay harness**: `agora eval --config --scenario` with scripted replies (Fake provider),
  deterministic run and checks on output/signals; `Eval/Scenario` + `ScenarioRunner`
- **Smarter routing**: `type: route` edge with `IRouter`/`LlmRouter` — an LLM picks the branch from
  the `when` descriptions, more robust than signal-token routing
- **Parallel execution** (fork/join): `type: parallel` edge, concurrent branches via `Task.WhenAll`
  (`State` mutation serialized, no race), converging on a join. `GraphExecutor.FanOutAsync`; concept
  `parallel-execution`, example `agora-parallel.yaml`
- **Checkpointing & resume**: `StateSnapshot` (JSON, typed signals) + `ICheckpointStore`
  (`InMemory`/`File`); checkpoint after every node; `GraphExecutor.RunAsync(resumeFrom:)` +
  `Runtime.ResumeAsync`; CLI `run --checkpoint`/`--run-id` and `resume` command. Concept `checkpointing`
- **Token streaming**: `IStreamingChatProvider` (capability, doesn't break `IChatProvider`) on
  Fake/AgentFramework/Resilient; `Action<string>` sink propagated Runtime→executor→agent;
  CLI `run --stream`. Tool agents and parallel branches excluded in v1. Concept `streaming`
- **Async `IVectorStore`**: `UpsertAsync`/`QueryAsync`/`DeleteAsync`; removed the sync-over-async
  in `QdrantVectorStore`. Updated InMemory/File/Qdrant + RagPipeline/Ingestor/KnowledgeBase/ContextMemory
  and all tests. 209 core green
- **Wiki sync**: aligned `overview` (concepts, architecture, status) and `index` (CLI entity) with the
  new capabilities; added the `--stream`/`--checkpoint`/`--run-id` options to the `agora-cli` entity

## 2026-06-20 — Context memory (context compression via RAG)

- Opt-in `memory: { enabled, top_k }` mode: instead of injecting all artifacts, **save** the declared
  artifacts and **recall the top-K relevant** ones per agent
- `ContextMemory` (append-only, no conflict-check; `memory:` source to distinguish from the KB;
  filtered recall). `GraphExecutor` uses recall instead of `ArtifactSummary`; `Runtime` reuses the
  RAG's embedder/store
- New `context-memory` concept page, example `examples/agora-memory.yaml`; +5 tests (182 core green)
- Isolation fix: `TracingTests` in a non-parallelizable collection (global recorder)

## 2026-06-20 — Qdrant adapter (containerized vector DB)

- `QdrantVectorStore` (`Agora.AgentFramework`): `IVectorStore` over a Qdrant server via **gRPC**
  (official `Qdrant.Client` 1.18.1 client). Collection created lazily on first upsert (cosine)
- Extension seam: `RagFactory`/`Runtime` accept an edge-injected `storeResolver`;
  `AgentFrameworkVectorStores.TryCreate` resolves `type: qdrant` (`url`/`collection`). Core stays framework-free
- `VectorStoreConfig.Url` added; wired through to `Program.cs`
- Seam + resolver tests (177 core, 8 API green). Integration against a real Qdrant deferred (no container here)

## 2026-06-19 — `init` wizard: handoff, vector store, RAG tools

- `agora init` now asks for the `handoff` mode, the vector store type (`memory`/`file` + path) in the
  RAG section, and proposes per-agent tools when RAG/handoff/skills/MCP are configured (including
  `rag_search`/`rag_write`/`ask_agent`)
- RAG reordered before the agents; updated `guided-config` page; 172 core green

- Agents access the shared KB via built-in tools (`Agora.AgentFramework`): `rag_search` (read),
  `rag_write` (write via `KnowledgeBase`), enabled by the `tools` allow-list — `RagTools`
- `ask_agent(target, question)`: the receiver queries another agent after `rag_search`. Synchronous
  tool that re-runs the target in "answer-mode" (without `ask_agent`, no recursion) — `AskAgentTool`
- `Runtime` builds the `KnowledgeBase` reusing the `RagPipeline`'s `Embedder`/`Store` (a single
  `IVectorStore` for read+write); `IConflictResolver` wired through to `Program.cs`
- `AgentBuildContext` carries `Rag`/`KnowledgeBase`/`AskAgent`; +12 tests (171 core, 8 API)
- Updated `shared-knowledge-base`, `handoff-context`, `mcp-tools`

## 2026-06-19 — Handoff context, Phase 2

- Opt-in `handoff: true` mode: on a hop, pass to the next agent **only** the sender's `handoff`
  artifact (or nothing), not the full output. Default unchanged
- `GraphExecutor` `handoff` param; the `handoff` artifact is a targeted channel, excluded from the
  global artifact summary; `H2cInterpreter` extracts the `handoff` field in h2c
- `HandoffPreamble` injected when active; new `handoff-context` concept page, updated `agent-graph`,
  example `examples/agora-handoff.yaml`
- +3 tests (160 core green)

## 2026-06-19 — Writable RAG, Phase 1 (shared knowledge base)

- Started evolving the RAG from read-only to a **writable knowledge base** with conflict detection
  and resolution. Created the `shared-knowledge-base` concept page
- `IVectorStore` extended: stable `Chunk.Id` + `Delete(ids)` + upsert-by-id → conflict resolution
  **replaces** the superseded entry instead of appending
- New `FileVectorStore` (disk-persistent, JSON); `vector_store: { type: file }`
- `KnowledgeBase` (embed → neighbors → judge → write/escalate); `IConflictJudge` + `LlmConflictJudge`
  (marker protocol with `CONFLICTS_WITH`) + `NoOpConflictJudge`
- `IConflictResolver` (KeepExisting/KeepNew/Merge) + `ConsoleConflictResolver` / `FakeConflictResolver`
- RAG access behind an interface: disk now, containerized DB (Qdrant/pgvector) later
- +17 tests (157 core, 8 API green). Not yet wired to the agents (Phase 3)
- Updated `rag-pipeline`, `human-in-the-loop`, index

## 2026-06-19 — `init` command (guided config)

- Added the `agora init` CLI command: an interactive wizard that builds the YAML config
  (communication, providers, models, agents and the multi-agent graph with conditional edges)
- New `ConfigWriter` (YAML serialization, counterpart of `ConfigLoader`); previously YamlDotNet was
  only used for reading
- `ConfigWizard` with injectable I/O → unit-testable (`ConfigWizardTests`)
- Created the `guided-config` concept page; updated the `agora-cli` entity and the index
- **"Complete" extension**: the wizard now also covers the optional sections — skills (directories),
  MCP (stdio/http servers), RAG (ingest/chunk/top_k/refine, `fake` embedder + `memory` store) and
  HITL (`approvals` ⊆ `tools` per agent). Optional sections are gated behind yes/no defaulting to no;
  test extended to 4 cases

## 2026-06-17

- Project created
- Wiki initialized with knowledge of the Agora Orchestrator project
- Created 4 entity pages: `agora-orchestrator`, `agora-cli`, `agora-api`, `agora-agent-framework`
- Created 8 concept pages: `agent-graph`, `edge-types`, `h2c-protocol`, `signal`, `communication-modes`, `rag-pipeline`, `skills`, `mcp-tools`, `human-in-the-loop`
- Updated `purpose.md`, `wiki/overview.md`, `wiki/index.md`

## 2026-06-18 — Wiki page corrections

- **signal.md**: added `<<artifact key=value>>` syntax, 3-tuple `Extract` signature, artifact example
- **agent-graph.md**: added `Artifacts`, `LoopCounters`, `Inbox()`, `ArtifactSummary()` to the shared state
- **mcp-tools.md**: added the Built-in Filesystem Tools section (read/write/search/list)
- **communication-modes.md**: added the artifact column to the comparison table
- **overview.md**: `IProvider` → `IChatProvider` in the architecture
- **agora-orchestrator.md**: corrected external dependencies (Microsoft.Extensions.AI.OpenAI, ModelContextProtocol.Core, OllamaSharp)
- **human-in-the-loop.md**: corrected `IApprovalHandler` (signature with `ApprovalRequest` + `CancellationToken`), tool-based flow, `PendingApprovalHandler` API
- **rag-pipeline.md**: corrected the nested YAML structure (retrieval.embedder, ingest.sources, etc.)
- **skills.md**: `directory` → `directories` (list)
- **edge-types.md**: fallback to the first non-conditional edge (not an error)
