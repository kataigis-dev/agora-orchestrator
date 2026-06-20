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
