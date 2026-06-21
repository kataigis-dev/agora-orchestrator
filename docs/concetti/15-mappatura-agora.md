# 15 — Mapping onto Agora Orchestrator

This chapter connects every concept in the folder to its **concrete implementation** in Agora
Orchestrator and to the technical documentation in [`../`](../index.md).

Agora Orchestrator is a **framework-free** multi-agent orchestration framework for .NET 10: the core
`Agora` has no external dependencies (apart from YamlDotNet), while the integration with models, MCP and
*tooling* lives in `Agora.AgentFramework`, built on **Microsoft.Extensions.AI** and
**Microsoft.Agents.AI** (see [16 — Microsoft Agent Framework](16-microsoft-agent-framework.md)).

## Concept → implementation map

| Concept (chapter) | In Agora | Technical doc |
|-------------------|----------|---------------|
| **LLM / provider** ([01](01-fondamenti-llm.md)) | `IChatProvider` (OpenAI, Ollama, compatible endpoints), `ModelSpec`, model aliases in YAML | [providers.md](../providers.md) |
| **Agent** ([02](02-agenti.md)) | `Agent` / `AgentFrameworkAgent`, defined in YAML with role, model, tools | [agents.md](../agents.md) |
| **Tools / function calling** ([03](03-strumenti-function-calling.md)) | Built-in tools (`read_file`, `rag_search`, `ask_agent`, `spec_*`, `run_check`...), per-agent `tools:` allow-list | [agents.md](../agents.md), [configuration.md](../configuration.md) |
| **MCP** ([04](04-mcp.md)) | `McpToolSession` (stdio/HTTP), the `mcp.servers` section, tool discovery | [mcp.md](../mcp.md) |
| **RAG** ([05](05-rag.md)) | `RagPipeline` (ingest, chunking, embedding, file/memory/Qdrant vector store), `rag_search`/`rag_write` | [rag.md](../rag.md) |
| **Memory / context engineering** ([06](06-memoria-contesto.md)) | `ContextMemory` (top-K recall per agent), artifact handoff, `ArtifactSummary` | [architecture.md](../architecture.md) |
| **Workflow patterns** ([07](07-workflow-pattern.md)) | Graph edges: `sequential`, `handoff`, `conditional` (loop with `max_loops`), `route` (LLM router) | [graph.md](../graph.md) |
| **Multi-agent orchestration** ([08](08-orchestrazione-multi-agente.md)) | `GraphExecutor` with `parallel` edges (fork/join), conditional routing, `ask_agent` for explicit invocation | [graph.md](../graph.md), [architecture.md](../architecture.md) |
| **Spec-driven development** ([09](09-spec-driven-development.md)) | `Specs/` (`SpecDocument`, `SpecValidator`, file/MCP `ISpecStore`), `Verification/` (`ProcessCheckRunner`, `AcceptanceVerifier`, `TraceabilityValidator`), `spec_*`/`run_check`/`spec_verify`/`spec_gate` tools | [spec.md](../spec.md) |
| **Evaluation / observability** ([10](10-valutazione-osservabilita.md)) | `IExecutionObserver`, `MetricsExecutionObserver` → `RunMetrics` (steps, rework, token/cache, traceability) | [observability.md](../observability.md) |
| **Human-in-the-loop** ([11](11-human-in-the-loop.md)) | Approval/conflict-resolution interfaces, tools marked `approvals:` | [agents.md](../agents.md) |
| **Security / governance** ([12](12-sicurezza-governance.md)) | Per-agent tool allow-lists; check execution **with no shell**, commands only from config, token-by-token substitution, timeout + process-tree kill | [spec.md](../spec.md) |
| **Standards / protocols** ([13](13-standard-protocolli.md)) | MCP via `ModelContextProtocol.Core`; optional spec store over RAG-over-MCP | [mcp.md](../mcp.md), [spec.md](../spec.md) |

## Two communications: H2C and natural

Agora supports two communication modes between agents, both based on the
[routing](07-workflow-pattern.md) patterns:

- **Natural**: agents use natural language and `<<signal name>>` / `<<artifact k=v>>` tokens for routing
  and sharing.
- **H2C**: a compressed `[TYPE:SUBTYPE]` protocol with verdicts in the subtype (e.g. `[STATE:DONE]`,
  `[TEST:PASS]`).

See [h2c.md](../h2c.md) and [graph.md](../graph.md).

## The throughline: "the gates assert reality"

Agora's distinctive architectural choice — consistent with all the sources in this folder — is to move
completion decisions from the **model's narration** to **deterministic checks**: checks run real
build/tests, `spec_verify` advances a requirement to *verified* only on a successful exit code, and
`spec_gate`/`TraceabilityValidator` compute a COMPLETE/INCOMPLETE verdict over the traceability. It is the
practical materialization of [spec-driven development](09-spec-driven-development.md) and of Anthropic's
[programmatic gates](07-workflow-pattern.md).

---

Previous: [14 — Glossary](14-glossario.md) · Next:
[16 — Microsoft Agent Framework](16-microsoft-agent-framework.md).
