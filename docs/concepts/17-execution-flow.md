# 17 — Execution flow (how the app works, step by step)

This chapter follows a request from start to finish, naming **every class and method involved**. It is
meant for someone who does not know the project: reading it you understand *what happens when you run
Agora*. For a description of all the classes one by one see
[18 — Class reference](18-class-reference.md).

## Big picture: three projects

```
Agora.Cli  ─┐                          ┌─ Agora.AgentFramework  (concrete implementations:
Agora.Api  ─┤── use ──▶  Agora ◀───────┤   chat providers, embedders, vector stores, MCP clients)
            │          (the core,       └─ injects the "edge" dependencies into the Runtime
            │       framework-free)
```

- **`Agora`** (core): all the logic, but **with no external dependencies** — it defines *interfaces*
  (`IChatProvider`, `IEmbedder`, `ISpecStore`…) and knows nothing about OpenAI or MCP.
- **`Agora.AgentFramework`**: the **concrete implementations** of those interfaces
  (Microsoft.Extensions.AI, MCP clients, Qdrant…).
- **`Agora.Cli` / `Agora.Api`**: the two **entry points** (command line and REST). They build the
  concrete implementations and pass them to the core (an *edge dependency-injection* pattern).

> Key principle: the core depends only on **abstractions**; the implementations are injected from the
> outside (`provider` and a single `IAgentBackend`). See
> [12 — Security](12-security-governance.md) and
> [16 — Microsoft Agent Framework](16-microsoft-agent-framework.md).

---

## The path of a `run --graph` command

Let's follow: `agora run --config app.yaml --input "..." --graph`.

### 1. Entry point — `Agora.Cli/Program.cs`

It builds the concrete implementations and passes them to `CliRunner.Run`:
`AgentFrameworkChatProvider` (the LLM provider), `AgentFrameworkBackend` (the single `IAgentBackend` that
builds tool-capable agents and resolves the non-core embedder / vector store / spec store), and
`ConsoleApprovalHandler` / `ConsoleConflictResolver` (HITL on the console).

### 2. Parsing and dispatch — `CliRunner.Run` → `CommandStrategy`

`CliRunner.Run` parses the arguments, packs them into a `ConfigState` (options + I/O streams + injected
dependencies) and selects the verb's handler. Each verb
(`init`/`run`/`resume`/`ingest`/`validate`/`eval`) is a `ConfigState → exit code` function in
`CommandStrategy`. Here `CommandStrategy.Run` runs, wrapped by `CommandStrategy.Execute` (which catches any
exception and turns it into `ERROR: …` + exit code 1).

### 3. Loading and validating config — `ConfigLoader.Load`

`CommandStrategy.Run` calls `Runtime.FromConfig`, which first invokes `ConfigLoader.Load(path)`:

1. it reads the YAML file and deserializes it into `AgoraConfig` with YamlDotNet (`snake_case` convention,
   unknown properties ignored → new sections don't break old files);
2. `ConfigLoader.ValidateReferences` verifies the **cross-references**: a valid communication mode, at
   least one agent, every agent resolving an existing `model` and `provider`, and every `approval` being
   among the agent's `tools`. If anything is off it throws `ConfigException`.

### 4. Building the `Runtime` (the *composition root*)

`Runtime.FromConfig` creates `new Runtime(...)`. The constructor assembles **everything** needed:

| Step in the constructor | What it does |
|---|---|
| `ModelResolver.Resolve(config)` | flattens models+providers+defaults into runnable `ModelSpec`s (one per alias) |
| `new ResilientChatProvider(provider)` | wraps the provider with retry+timeout ([`RetryPolicy`](10-evaluation-observability.md)) |
| `Retrieval.Build` | builds the retrieval subsystem in one place — the read `RagPipeline`, the `KnowledgeBase` write path, and `ContextMemory` — over **one shared** embedder + vector store, when `rag`/`memory` are configured |
| `BuildSpecStore()` | `FileSpecStore` (file) or a backend-built store (RAG-over-MCP) if there is a `spec` section |
| `BuildCheckRunner()` | `ProcessCheckRunner` if there is a `checks` section |
| router | an `LlmRouter` if `route` edges need routing |
| `SkillLoader.Load` | loads the skills from `SKILL.md` files into a `SkillRegistry` |

It also picks the output interpreter: `H2cInterpreter` (H2C mode) or `SignalInterpreter` (natural).

### 5. Starting the graph — `Runtime.RunAsync`

```
RunAsync(input)
  ├─ create a MetricsExecutionObserver (collects the run metrics)
  ├─ BuildExecutor(): GraphBuilder.Build(config) + GraphBuilder.Validate  → a GraphExecutor
  ├─ if RAG is enabled: Rag.RunAsync(input) → initial context (seedContext)
  ├─ executor.RunAsync(input, seedContext)          ◀── the heart (step 6)
  └─ WithTraceability(metrics) → BuildResult() → RunResult
```

`BuildExecutor` builds the `Graph` from the config and **validates** it (`GraphBuilder`), then creates the
`GraphExecutor` wired with handoff, memory, router, checkpoint and the observer (composed:
`ConsoleExecutionObserver` for rendering + `MetricsExecutionObserver` for metrics, via
`CompositeExecutionObserver`).

### 6. The heart — `GraphExecutor.RunAsync`

It maintains a `State` (the **shared blackboard**: input, messages, per-node outputs, signals, artifacts,
loop counters) and loops from `entry` until it reaches `END` (or exceeds `max_steps` → `ExecutionError`).
At each step:

1. **`RunOnceAsync(nodeId)`** — runs the node:
   - `BuildContextAsync` assembles the node's context: its *inbox* (`State.Inbox`) + the *shared
     background* (in memory mode, `ContextMemory.RecallAsync` retrieves only the relevant top-K;
     otherwise `State.ArtifactSummary`, the summary of all artifacts);
   - `_agentFactory(nodeId)` — which is `Runtime.BuildAgent` (step 7) — builds the agent;
   - `agent.RunAsync(input, context)` runs it → an `AgentResult`.
2. **Updates the state**: saves the output (`State.Outputs`), the signals (`State.Signals`) and the
   artifacts (`State.Artifacts`); `RememberAsync` writes the artifacts into `ContextMemory` if enabled.
3. **Routing** — picks the next node:
   - if the outgoing edges are `parallel` → `FanOutAsync` runs the branches **concurrently**
     (`Task.WhenAll`) and converges them on the single *join* node;
   - otherwise `NextNodeAsync`: for `route` edges it asks `LlmRouter.ChooseAsync`; otherwise the pure
     `EdgeResolver.Next` applies the **signals** — it takes the first `conditional` edge whose `when` is
     *truthy* (respecting `max_loops`), or the first unconditional edge, or `END` — returning the updated
     loop counters without mutating the state.
4. **Passes the baton**: in normal mode it puts the node's output into the next node's *inbox*
   (`State.Messages`); in *handoff* mode it passes **only** the `handoff` artifact.
5. **Events and checkpoint**: it notifies the observer (`OnSignals`, `OnUsage`, `OnArtifact`, `OnEdge`)
   and `Checkpoint` saves a `StateSnapshot` (if an `ICheckpointStore` is configured → resume possible).

### 7. Building the agent — `Runtime.BuildAgent`

For the current node: it resolves the `ModelSpec`, composes the *system prompt* by prepending the active
preambles (`HandoffPreamble`, `H2cPreamble`, `LanguagePreamble`), and creates an `AgentCard` (identity +
capabilities). Then:

- if the agent declares **skills or tools** → `IAgentBackend.CreateToolAgent(AgentBuildContext)` →
  `AgentFrameworkAgent` (step 8b);
- otherwise → a plain `Agent` (step 8a).

### 8a. Plain agent — `Agent.RunAsync`

It builds the messages: `system` (from `AgentCard.ComposeInstructions`, marked `CacheStable` for prompt
caching) + `user` (context + input). It calls `IChatProvider.CompleteAsync` (or `StreamAsync` if there is
streaming and a provider that supports it). Finally `IOutputInterpreter.Interpret` turns the raw text into
`(clean output, signals, artifacts)` — this is where `SignalParser` acts (see
[signals and artifacts](14-glossary.md)). It returns an `AgentResult` including the token counts.

### 8b. Tool-capable agent — `AgentFrameworkAgent.RunAsync`

Richer, because the agent can **use tools** (function calling):

1. **Assembles the tools** allowed by the agent's allow-list: `SkillTools` (skills as a `load_skill`
   function), `BuiltInFileTools` (files), `RagTools` (the knowledge base), `SpecTools` + `CheckTools`
   (SDD), `AskAgentTool` (asking another agent), and the MCP tools via `McpToolSession.ConnectAsync`.
2. **Approval gating**: the tools listed in `approvals` are wrapped in `ApprovalRequiredAIFunction` (HITL
   at the tool level).
3. **Executes** via Microsoft Agent Framework: `ChatClientCache.Get(spec)` → a `ChatClientAgent`; calls
   to the model go through `RetryPolicy`.
4. **Approval loop**: as long as the model asks to run gated tools, `ApprovalFor` queries the
   `IApprovalHandler` (e.g. `ConsoleApprovalHandler`) and re-runs, up to a maximum number of rounds.
5. `Interpret` + `UsageMapping.From` → `AgentResult`.

### 9. Closing — `WithTraceability` → `BuildResult`

Back in `Runtime.RunAsync`: if a spec store is configured, `WithTraceability` loads the `SpecDocument`
and computes the completion verdict with `TraceabilityValidator`, attaching it as a `TraceabilitySummary`
to the metrics (best-effort). `BuildResult` packs everything into a `RunResult` (final output + `State` +
`RunMetrics` + an optional `RunId`). The CLI prints `result.Output`.

---

## The other paths (in brief)

| Command / scenario | Entry point | What changes |
|---|---|---|
| **Single agent** (`run` without `--graph`) | `Runtime.RunAgentAsync` | builds **one** agent and runs it, with no graph or routing |
| **`resume`** | `Runtime.ResumeAsync` | loads the last `StateSnapshot` from `FileCheckpointStore` and restarts from the saved node |
| **`ingest`** | `CommandStrategy.Ingest` → `Retrieval.IngestAsync` | indexes the RAG documents (chunk→embedding→vector store), no chat LLM |
| **`validate`** | `CommandStrategy.Validate` → `ConfigLoader.Load` | only loads+validates the config, prints `OK`/`INVALID` |
| **`eval`** | `ScenarioRunner.RunAsync` | replaces the real provider with a scripted `FakeChatProvider` and checks the expectations (a deterministic test) |
| **REST API** | `Agora.Api` | see below |

### The API path

`Agora.Api` exposes the same runs over HTTP. `AgoraRuntimeFactory` holds the dependencies built at startup
and creates a fresh `Runtime` for each run. `RunEndpoints` maps the endpoints (start, status, approvals,
ingest); a run queued in `RunQueue` is drained by `RunExecutor` on a **background thread**. HITL approvals
are **asynchronous**: `PendingApprovalHandler` parks the request in an `ApprovalGate`, the status endpoint
surfaces it, and a decision `POST` unblocks it. Each run's state lives in an `IRunStore`
(`InMemoryRunStore`).

---

## Sequence diagram (graph)

```
Program.cs → CliRunner → CommandStrategy.Run
   └─▶ Runtime.FromConfig ─▶ ConfigLoader.Load (+Validate)
   └─▶ Runtime.RunAsync
         ├─ RagPipeline.RunAsync ............ initial context (opt.)
         └─ GraphExecutor.RunAsync  ┐
              while(≠END):           │
                RunOnceAsync         │  ┌─ BuildContextAsync (inbox + ContextMemory/ArtifactSummary)
                  ├─ Runtime.BuildAgent ─┤  └─ Agent / AgentFrameworkAgent
                  └─ agent.RunAsync ──────▶ IChatProvider.CompleteAsync ─▶ IOutputInterpreter.Interpret
                update State (Outputs/Signals/Artifacts)
                NextNodeAsync (signals / LlmRouter)  +  Checkpoint  +  observer
              ┘
         └─ WithTraceability ─▶ BuildResult ─▶ RunResult ─▶ stdout
```

---

Previous: [16 — Microsoft Agent Framework](16-microsoft-agent-framework.md) · Next:
[18 — Class reference](18-class-reference.md).
