# Architecture

## Overview

Agora Orchestrator is layered:

```
┌───────────────────────────────┐
│  CLI / API                     │  user interfaces
├───────────────────────────────┤
│  Runtime → GraphExecutor       │  orchestration engine
├───────────────────────────────┤
│  Agent / AgentFrameworkAgent   │  agent executors
├───────────────────────────────┤
│  IChatProvider  (OpenAI/Ollama)│  providers
│  RagPipeline / KnowledgeBase   │  retrieval + writable KB
│  ISpecStore (file / MCP)       │  structured specification
│  ICheckRunner (process)        │  real build/test execution
│  ContextMemory                 │  context compression
│  McpToolSession                │  external tools
├───────────────────────────────┤
│  Core (Agora)                  │  models, interfaces, resilience
└───────────────────────────────┘
```

## Graph engine (GraphExecutor)

`GraphExecutor` in `src/Agora/Orchestration/GraphExecutor.cs` is the heart of the system. It runs
a directed graph of agents:

1. Starts from the `entry` node in the config.
2. For each node, runs the associated agent with its context.
3. Determines the next node from the edge type and the agent's signals.
4. Continues until `END` or `max_steps` (default 100).

### Edge types

| Type | Behaviour |
|---|---|
| `sequential` | Go to the target node |
| `handoff` | Hand control to the target (semantically: "done, over to you") |
| `conditional` | Take the edge only if the `when` signal is present (with optional `max_loops`) |
| `route` | An LLM router picks the branch from each edge's `when` description |
| `parallel` | Fork: branches run concurrently and converge on a single join node |

### Agent lifecycle in a graph

1. Receives the context (inbox messages + shared context).
2. Processes it with the configured model (optionally streaming).
3. May call tools (built-in, MCP) or skills.
4. Produces an output; routing signals/artifacts are parsed from it.

### Execution observation

The executor stays presentation-free: it emits run events (graph start, node start, parallel
fork, signals, token usage, artifacts, edge taken, complete) to an injected `IExecutionObserver`. The
default `ConsoleExecutionObserver` renders the colored CLI visualization; `NullExecutionObserver`
silences it; `MetricsExecutionObserver` aggregates the events into `RunMetrics` (steps, rework,
token/cache usage) surfaced on `RunResult.Metrics`; `CompositeExecutionObserver` fans events out to
several at once. This seam keeps orchestration testable (assert the event sequence) and lets you plug
structured logging or measurement without touching the engine. See
[observability.md](observability.md).

## Communication

### H2C

Token-compressed protocol: a `[TYPE:SUBTYPE]` header line optionally followed by a
`key:value|key:value` fields line. Completion/verdicts are signalled by the subtype
(e.g. `[STATE:DONE]`, `[TEST:PASS]`, `[STATE:FIX]`).

### Natural

Agents communicate in natural language and use `<<signal name>>` tokens for routing.

## Context sharing

- **Artifacts** — `<<artifact key=value>>` tokens parsed by `SignalParser` into `State.Artifacts`,
  summarized into each agent's context (`State.ArtifactSummary`).
- **Handoff** (`handoff: true`) — pass only the declared `handoff` artifact to the next agent.
- **Context memory** (`memory`) — store declared artifacts and recall only the top-K relevant ones
  per agent (compresses tokens). See the context-memory page.

## Structured specification

When the `spec` section is configured, the runtime builds an `ISpecStore` (the framework-free
`FileSpecStore`, or a RAG-over-MCP `McpSpecStore` via the edge-injected resolver — the same pattern
as the RAG vector-store/embedder resolvers). Agents that allow-list the `spec_*` tools produce and
mutate a structured `SpecDocument` (requirements with stable ids + acceptance criteria, and tasks
that trace back to them); every write is validated by `SpecValidator` before persisting. The store is
the deterministic source of truth, kept separate from similarity-based RAG retrieval.

When a `checks` section is present, the runtime also builds an `ICheckRunner` (`ProcessCheckRunner`)
that runs an allow-list of named build/test commands as real processes (no shell). The `run_check`
and `spec_verify` tools execute these; `AcceptanceVerifier` binds them to acceptance criteria so a
requirement is marked `Verified` only when its checks pass deterministically — moving gate decisions
off LLM judgement.

`TraceabilityValidator` lifts that from one requirement to the whole spec: it projects the document
into a `TraceabilityReport` (the requirement↔task↔check matrix) and derives a deterministic
`COMPLETE`/`INCOMPLETE` verdict — the committed scope is done only when every approved requirement is
covered by a task and verified through real checks. The verdict is exposed both as the read-only
`spec_gate` tool (so an agent routes `done` on the gate, not on self-assessment) and on
`RunMetrics.Traceability`, which the runtime computes from the persisted spec at the end of each run so
completeness can be asserted programmatically. See [spec.md](spec.md).

## Providers and models

Resolution chain: an agent references a model alias (`model: <id>`); the model references a
provider (`provider: <id>`); at runtime the provider's key (from `api_key_env`) and base URL are
resolved into a `ModelSpec`, and `Agora.AgentFramework` builds the concrete `IChatProvider`.

## Durability & streaming

- **Checkpointing** — `ICheckpointStore` persists a `StateSnapshot` per step; `resume` continues a run.
- **Streaming** — `IStreamingChatProvider` streams tokens (`run --stream`).

## Resilience

`RetryPolicy` (`src/Agora/Resilience/`) wraps model calls with retry + exponential backoff and a
per-agent timeout (`defaults.retries`, `retry_base_delay`, `timeout`).
