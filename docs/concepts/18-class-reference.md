# 18 — Class reference

An **exhaustive** list of every type in the code (class, interface, record, enum), grouped by folder
according to the reorganized structure. For each: one line on *what it is for*. To understand how these
pieces collaborate at runtime see [17 — Execution flow](17-execution-flow.md).

Legend: **I** interface · **R** record · **E** enum · **C** class.

---

## `Agora` (core root)

| Type | Role |
|---|---|
| C `Runtime` | The *composition root*: from the config it resolves the models, builds the retrieval subsystem (RAG/knowledge base/memory)/spec store/check runner and the graph, and exposes the methods to run an agent or a graph |
| R `RunResult` | The outcome of a run: final output + the full `State` + `RunMetrics` + an optional `RunId` |
| C `AgoraInfo` | Package/build information for the framework |

## `Agora/Configuration` — the YAML config model

| Type | Role |
|---|---|
| C `AgoraConfig` | Root of the config: providers, models, agents, graph and optional sections (RAG, skills, MCP, memory, spec, checks) |
| C `Defaults` | Default settings (model, temperature, timeout, retry) applied to agents that don't override them |
| C `AgentConfig` | Config of a single agent (model, role, prompt, tools, approvals, skills) |
| C `ModelConfig` | Maps a model alias onto provider + concrete model name |
| C `ProviderConfig` | Config of an LLM provider (key from env, base URL) |
| C `GraphConfig` | The execution graph: `entry` node + edges (+ optional explicit nodes) |
| C `GraphNodeConfig` | Optional explicit declaration of a node |
| C `GraphEdgeConfig` | A directed edge between two nodes (type, `when`, `max_loops`) |
| C `RagConfig` | RAG / shared knowledge-base config |
| C `RetrievalConfig` | Retrieval parameters: embedder, store, `top_k`, threshold |
| C `EmbedderConfig` | Selects the embedder used to vectorize text |
| C `VectorStoreConfig` | Selects the vector store (memory/file/qdrant) |
| C `RefineConfig` | Optional post-retrieval query refinement |
| C `IngestConfig` | Ingestion settings: sources + chunking |
| C `MemoryConfig` | RAG-backed context memory (opt-in): recall the relevant top-K instead of injecting all artifacts |
| C `McpConfig` | Config of the MCP tool servers |
| C `McpServerConfig` | Config of a single MCP server (stdio via command/args, or HTTP via url) |
| C `SkillsConfig` | Where to discover `SKILL.md` files |
| C `SpecConfig` | Spec-driven-development config (opt-in): enablement, `require_criteria`, store |
| C `SpecStoreConfig` | Where to persist the specification (file or MCP) |
| C `ChecksConfig` | Allow-list of build/test commands runnable by `run_check`/`spec_verify` |
| C `CheckCommandConfig` | A single check: executable + argument tokens (with `{key}` placeholders) |
| C `ConfigLoader` | Loads and validates an `AgoraConfig` from YAML |
| C `ConfigWriter` | Serializes an `AgoraConfig` back to YAML (used by `init`) |
| C `ConfigException` | Thrown when the config is missing, malformed, or references don't resolve |

## `Agora/Providers` — the LLM model abstraction

**Contracts** · I `IChatProvider` (abstraction over the chat backend; the core depends only on this) · I `IStreamingChatProvider` (optional capability: token-by-token streaming).

**Models** · R `ChatMessage` (one message in a request) · R `CompletionResult` (generated text + token counts, incl. cache) · R `ModelSpec` (already-resolved model parameters and credentials).

**Concretes** · C `ModelResolver` (flattens models+providers+defaults into `ModelSpec`s) · C `ResilientChatProvider` (wraps a provider with retry+timeout) · C `FakeChatProvider` (deterministic provider for tests/offline). Prompt caching is owned per provider by an `ICacheAdapter` (`AnthropicCacheAdapter` / `ImplicitCacheAdapter`, resolved by `CacheAdapters.For`) in `Agora.AgentFramework.Providers`.

## `Agora/Agents` — the agent

**Contracts** · I `IAgent` (a runnable agent: input+context → `AgentResult`) · I `IOutputInterpreter` (raw text → text, signals, artifacts) · C `SignalInterpreter` ("natural" implementation: uses `SignalParser`) · I `IAgentBackend` (the single seam to the framework layer: builds tool-capable agents and resolves the non-core embedder/vector store/spec store) · R `AgentBuildContext` (everything the backend needs to build an agent from config).

**Models** · R `AgentCard` (an agent's immutable identity + capabilities) · R `AgentResult` (the outcome of a run: clean output + signals + artifacts + tokens).

**Concretes** · C `Agent` (a single agent's prompt loop, no tools) · C `SignalParser` (extracts and strips the `<<signal>>` and `<<artifact>>` tokens).

## `Agora/Orchestration` — the graph engine

**Contracts** · I `IExecutionObserver` (receives execution events for rendering/logging/metrics) · C `NullExecutionObserver` (ignores every event) · I `ICheckpointStore` (persists per-step snapshots → resume) · C `InMemoryCheckpointStore` (in-process, for tests) · C `FileCheckpointStore` (one JSON snapshot per run id on disk) · I `IRouter` (picks the node among `route` edges) · R `RouteOption` (one branch the router can choose).

**Models** · C `Graph` (a validated, immutable graph: nodes + edges) · R `Node` (a node: agent/human/end) · R `Edge` (a directed edge) · R `Message` (a message between agents on the shared blackboard) · C `State` (the **shared, JSON-serializable blackboard** of the run) · C `StateSnapshot` (a checkpoint: the run cursor — current node + step count — wrapping the `State` it was taken at) · R `RunMetrics` (an aggregate summary of a run: steps, rework, tokens, traceability) · R `TraceabilitySummary` (a summary of spec completeness in the run).

**Concretes** · C `GraphExecutor` (runs the graph: nodes, state, routing, parallel, checkpoint) · C `EdgeResolver` (the pure signal-based next-node decision: returns the next node + updated loop counters without mutating state) · C `GraphBuilder` (builds and validates a `Graph` from the config) · C `LlmRouter` (asks an LLM which branch to take) · C `ConsoleExecutionObserver` (colored console rendering) · C `MetricsExecutionObserver` (aggregates events into `RunMetrics`) · C `CompositeExecutionObserver` (fans every event out to multiple observers) · C `ExecutionError` (a runtime error of the graph) · C `GraphError` (a missing/malformed graph definition).

## `Agora/Communication` — H2C and natural protocols

| Type | Role |
|---|---|
| C `H2cInterpreter` | H2C mode: derives routing signals from H2C blocks |
| C `H2cParser` | A lenient parser/serializer for the H2C block grammar |
| R `H2cBlock` | An H2C block: `[Type:Subtype]` with `key:value` fields |
| C `AgentInstructions` | Owns the communication setup: assembles the system-prompt prefix (language + protocol + handoff, in order) and selects the matching `IOutputInterpreter` for the configured protocol |

## `Agora/Rag` — retrieval and knowledge base

**Contracts** · I `IEmbedder` (text → vectors) · I `IVectorStore` (chunk storage: upsert/query/delete) · I `IRefiner` (rewrites/decomposes the query before search) · I `IConflictJudge` (decides whether a new entry conflicts with the existing one) · R `ConflictAssessment` (the outcome of comparing a new entry against related ones) · E `ConflictVerdict` (the judge's verdict categories: none/duplicate/conflict).

**Models** · R `Chunk` (a stored unit of text) · R `EnrichedInput` (original input + refined query + retrieved chunks) · R `RefinedQuery` (a refined retrieval query + sub-queries) · R `EmbedderSpec` (a resolved embedder request handed to the agent backend).

**Concretes** · C `Retrieval` (the retrieval subsystem as one deep module: owns the shared embedder + vector store and, over them, the pipeline, knowledge base, and memory — concentrating the "reads and writes share one store" rule) · C `RagPipeline` (refines the query, retrieves context, produces an `EnrichedInput`) · C `RagFactory` (assembles the pipeline + shared stores from config) · C `KnowledgeBase` (the **write** path into the shared base: embeds, finds related, judges conflicts) · C `ContextMemory` (RAG-backed working memory to compress context) · R `MemoryOptions` (memory tuning) · C `Ingestor` (reads files, chunks, embeds and upserts) · C `TextChunker` (splits text into overlapping windows) · C `InMemoryVectorStore` (in-process store with cosine similarity) · C `FileVectorStore` (persistent JSON-file store) · C `VectorMath` (vector-similarity utilities) · C `FakeEmbedder` (deterministic embedder for tests) · C `LlmRefiner` (refines the query via an LLM) · C `NoOpRefiner` (pass-through) · C `LlmConflictJudge` (judges conflicts via an LLM) · C `NoOpConflictJudge` (never reports conflicts) · R `WriteResult` (the outcome of a KB write) · E `WriteOutcome` (outcome categories: created/updated/skipped/conflict).

## `Agora/Specs` — the structured specification (SDD)

**Contracts** · I `ISpecStore` (persistence of the specification; the deterministic source of truth).

**Models** · R `SpecDocument` (the specification: requirements + traceable tasks) · R `Requirement` (a requirement with a stable id R1..Rn and acceptance criteria) · R `AcceptanceCriterion` (a criterion with a stable id R1.A1 + a check) · R `SpecCheck` (a verification descriptor bound to a criterion) · E `RequirementPriority` (MoSCoW) · E `RequirementStatus` (lifecycle: proposed→approved→implemented→verified/rejected) · E `CheckKind` (how a criterion is verified: Manual/Test/Command/FileExists) · R `TaskItem` (a unit of work T1..Tn that traces back to requirements) · E `TaskState` (a task's progress state) · R `TaskEvidence` (evidence backing a task's completion) · R `SpecStoreSpec` (a resolved request to build a non-core spec store) · R `TraceabilityReport` (the traceability matrix + the deterministic completion verdict) · R `RequirementCoverage` (one row of the matrix: a requirement's coverage/verification) · R `TraceabilityGap` (a completeness gap: error blocks, warning informs).

**Concretes** · C `FileSpecStore` (a canonical JSON file store) · C `SpecStoreFactory` (resolves the spec store from config — the core file store, or a non-core store via the backend; the spec-store counterpart to `RagFactory`) · C `SpecSerializer` (canonical JSON (de)serialization, shared by every store) · C `SpecValidator` (validates the structural invariants on every write) · E `SpecSeverity` (the severity of an issue) · R `SpecIssue` (a problem found during validation) · C `TraceabilityValidator` (computes requirement↔task↔check traceability and the completion verdict).

## `Agora/Verification` — real check execution

**Contracts** · I `ICheckRunner` (runs a pre-configured check by name and returns its real outcome; only allow-listed checks).

**Models** · R `CheckResult` (the deterministic outcome of a check: passed if exit 0, output, duration).

**Concretes** · C `ProcessCheckRunner` (runs checks as **real processes**, no shell, with timeouts) · C `AcceptanceVerifier` (verifies a requirement by actually running the checks bound to the criteria) · R `CriterionResult` (a criterion's verification outcome) · R `RequirementResult` (a requirement's verification outcome: per-criterion + derived verdict).

## `Agora/Skills` — Agent Skills

| Type | Role |
|---|---|
| R `Skill` | A skill loaded from a `SKILL.md` folder |
| C `SkillLoader` | Loads skills from `SKILL.md` files (YAML frontmatter + Markdown body) |
| C `SkillRegistry` | Lookup of loaded skills by name |

## `Agora/HumanInTheLoop` — human oversight

| Type | Role |
|---|---|
| I `IApprovalHandler` | Decides whether a tool call may proceed (injected at the edges: CLI/API/test) |
| R `ApprovalRequest` | A consequential tool call awaiting a human decision |
| C `FakeApprovalHandler` | A deterministic handler for tests (always approve/deny) |
| I `IConflictResolver` | Asks a human how to resolve a knowledge-base conflict |
| R `ConflictResolutionRequest` | A KB conflict escalated to a human |
| R `ConflictDecision` | A human's decision on how to resolve the conflict |
| E `ConflictResolution` | The conflict-resolution options |
| C `FakeConflictResolver` | A deterministic resolver for tests |

## `Agora/Resilience` — retry and timeout

| Type | Role |
|---|---|
| C `RetryPolicy` | Retries a model call with exponential backoff + a per-agent timeout |
| I `IClock` | A time abstraction to make backoff/timeouts deterministic in tests |
| C `SystemClock` | The real (wall-clock) implementation |
| C `FakeClock` | A deterministic clock for tests (never really waits) |

## `Agora/Observability` — tracing

| Type | Role |
|---|---|
| C `Tracing` | A minimal span recorder + logging skeleton (OpenTelemetry in a later phase) |
| C `SpanScope` | A disposable timing scope: records the elapsed time on dispose |
| C `SpanRecord` | A recorded span: a named, timed operation with attributes |

## `Agora/Runs` — API run state

**Contracts** · I `IRunStore` (stores the lifecycle records of API runs).

**Models** · C `RunRecord` (the mutable state of an API run) · E `RunStatus` (lifecycle state) · R `PendingApproval` (a parked tool call awaiting a decision).

**Concretes** · C `InMemoryRunStore` (a thread-safe in-process store) · C `ApprovalGate` (a registry of approval requests parked on a run) · C `PendingApprovalHandler` (adapts the `IApprovalHandler` for one run, tracking its status).

## `Agora/Eval` — deterministic scenario tests

| Type | Role |
|---|---|
| C `ScenarioRunner` | Runs a scenario against a config with a scripted fake provider, then checks the expectations |
| C `Scenario` | A deterministic eval scenario (scripted LLM responses + expectations) |
| R `EvalResult` | The outcome of a scenario: passed or not + the list of failed expectations |

## `Agora/Cli` — command line

| Type | Role |
|---|---|
| C `CliRunner` | The CLI entry point: parses the arguments, picks the command, runs it with the injected dependencies |
| C `CommandStrategy` | The handlers behind each verb (`init`/`run`/`resume`/`ingest`/`validate`/`eval`) |
| R `ConfigState` | Parsed CLI options + I/O streams + edge dependencies |
| C `ConfigWizard` | An interactive step-by-step builder of the YAML config (the `init` verb); composes a `Prompter` |
| C `Prompter` | Console question/answer primitives (ask/required/list/subset/int/choice/yes-no) over the injected I/O, each re-prompting on invalid input |
| C `ConsoleApprovalHandler` | Prompts a human on the console to approve/reject a tool call |
| C `ConsoleConflictResolver` | Prompts a human on the console to resolve a KB conflict |

---

## `Agora.AgentFramework` — concrete implementations (infrastructure layer)

**Agents** · C `AgentFrameworkAgent` (a tool-capable `IAgent`, based on Microsoft Agent Framework) · C `AgentFrameworkBackend` (the single `IAgentBackend`: builds these agents and resolves the non-core embedders/vector stores/spec stores; injected into the Runtime).

**Providers** · C `AgentFrameworkChatProvider` (a chat provider on Microsoft.Extensions.AI → OpenAI-compatible endpoints) · C `ChatClients` (builds the chat client suited to a `ModelSpec`) · C `ChatClientCache` (a cache of one client per spec, to reuse connections) · I `ICacheAdapter` (per-provider prompt-cache seam: marks cache-stable content on the wire and maps usage back) with `AnthropicCacheAdapter` / `ImplicitCacheAdapter`, resolved by `CacheAdapters.For` · C `AnthropicChatClient` (hand-rolled client for the native Anthropic Messages API) · C `CopilotChatClient` (a client for the GitHub Copilot endpoint) · C `CopilotTokenProvider` (exchanges the OAuth token for the Copilot session token) · C `CopilotAuthHandler` (an HTTP handler that stamps every Copilot request with fresh headers and token).

**Rag** · C `AgentFrameworkEmbedder` (`IEmbedder` over an MEAI/OpenAI `IEmbeddingGenerator`) · C `AgentFrameworkEmbedders` (resolves non-core embedders from config) · C `AgentFrameworkVectorStores` (resolves non-core vector stores from config) · C `QdrantVectorStore` (`IVectorStore` over a Qdrant server via gRPC).

**Specs** · C `AgentFrameworkSpecStores` (resolves non-core spec stores from config) · C `McpSpecStore` (`ISpecStore` over a knowledge base reached via MCP) · I `IMcpInvoker` (calls a single MCP tool and returns its text) · C `McpClientInvoker` (the real MCP invoker: connect→call→dispose per call).

**Mcp** · C `McpToolSession` (opens the configured MCP servers and exposes their tools, filtered to the agent's allow-list).

**Tools** · C `BuiltInFileTools` (`read_file`/`write_file`/`search_files`/`list_directory`, sandboxed to a workspace root — paths that escape it are rejected) · C `RagTools` (`rag_search`/`rag_write`) · C `SkillTools` (exposes the skills as a `load_skill` function) · C `AskAgentTool` (`ask_agent`: asking another agent) · C `SpecTools` (`spec_get`/`spec_gate`/`spec_propose_requirement`/`spec_bind_check`/…) · C `CheckTools` (`run_check`/`spec_verify`: verdicts that assert reality).

> Agora is CLI-only with a human always present; there is no REST server. Read-only retrieval can be
> exposed to other tools via a local MCP stdio server (`agora serve-mcp`, `rag_search` only) — see
> [docs/mcp.md](../mcp.md). Knowledge-base writes (`rag_write`) never leave the CLI + human path.

---

Previous: [17 — Execution flow](17-execution-flow.md) · Back to the [index](README.md).
