# Agora Orchestrator

Version: 0.0.1

Agora Orchestrator is a multi-agent orchestration framework for .NET 10, with no external
dependencies in the core. It lets you define graphs of AI agents, each configured with a
different language model (OpenAI, Ollama, compatible endpoints), and orchestrate them over a
directed graph with sequential, handoff and conditional edges.

## Purpose

Build autonomous multi-agent pipelines where specialized agents collaborate on complex tasks:
code generation, document analysis, review, human approval, RAG ingest.

## Principles

- Framework-free: the `Agora` core has no external dependencies
- Config-driven: everything is declared in YAML (providers, models, agents, graph, MCP, skills, RAG, spec)
- Extensible: chat providers, MCP tools, skills, and custom graph executors
- Two communication modes: H2C (formal) and natural (natural language + signals)

## Directory structure

```
src/
├── Agora/                   # Core library (no external dependencies)
│   ├── Orchestration/       # Graph engine (GraphExecutor, nodes, edges)
│   ├── Agents/              # Agent models (base Agent)
│   ├── Cli/                 # Internal CLI commands
│   ├── Configuration/       # YAML parsing and configuration models
│   ├── Communication/       # H2C and natural protocols
│   ├── Providers/           # Chat provider interfaces
│   ├── HumanInTheLoop/      # Approval / conflict-resolution interfaces
│   ├── Rag/                 # RAG pipeline (ingest, chunking, embedding, search)
│   ├── Specs/               # Structured spec models, store, validator, traceability gate (SDD)
│   ├── Verification/        # Real build/test execution + acceptance verification
│   └── Resilience/          # Retry, timeout
├── Agora.AgentFramework/    # Concrete implementations with Microsoft.Extensions.AI
├── Agora.Api/               # ASP.NET REST API server
└── Agora.Cli/               # CLI executable
tests/
├── Agora.Tests/             # Core unit tests
└── Agora.Api.Tests/         # API integration tests
examples/
├── agora.yaml               # Minimal config
├── agora-h2c.yaml           # H2C with a conditional graph
├── agora-tools.yaml         # Skills + MCP
├── agora-rag.yaml           # RAG pipeline
├── agora-hitl.yaml          # Human-in-the-loop
├── agora-handoff.yaml       # Minimal handoff context
├── agora-memory.yaml        # RAG-backed context memory
├── agora-parallel.yaml      # Parallel fork/join
├── agora-spec.yaml          # Structured spec-driven development
└── agora-llama.yaml         # Local model via LLM studio
docs/
├── (documentazione tecnica in inglese)
└── concetti/               # 🇮🇹 Documentazione concettuale in italiano (agenti, RAG, MCP, pattern, sicurezza)
```

> 🇮🇹 Per i **fondamenti concettuali** (cosa è un agente, RAG, MCP, i pattern e gli standard per i
> workflow agentici, sicurezza), con fonti Anthropic/Google/Microsoft/AWS, vedi
> [`concetti/`](concetti/README.md).

## Quick start

```bash
# Build
dotnet build

# Generate a config interactively
dotnet run --project src/Agora.Cli -- init

# Run a single agent
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Write a note"

# Run a graph
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Build a todo app" --graph

# Validate a config
dotnet run --project src/Agora.Cli -- validate --config examples/agora.yaml

# Ingest RAG knowledge
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml

# Tests
dotnet test tests/Agora.Tests
dotnet test tests/Agora.Api.Tests
```

## Main components

| Component | Description |
|---|---|
| **GraphExecutor** | Runs a directed graph of agents, managing state, messages and transitions |
| **Agent** | Base agent: prompt + configuration + optional tools/skills |
| **ChatProvider** | Interface for chat providers (OpenAI, Ollama, custom); prompt-caching hints |
| **MetricsExecutionObserver** | Aggregates run events into `RunMetrics` (steps, rework, token/cache usage, spec traceability) |
| **H2cParser** | Parses the H2C protocol (structured `[TYPE:SUBTYPE]` blocks) |
| **McpToolSession** | Connection to MCP servers via stdio or HTTP |
| **RagPipeline** | Ingest, chunking, embedding, vector search |
| **SpecStore** | Persists the structured `SpecDocument` (requirements/tasks); file or RAG-over-MCP |
| **CheckRunner** | Runs allow-listed build/test commands; `AcceptanceVerifier` binds them to criteria |
| **TraceabilityValidator** | Derives the deterministic `COMPLETE`/`INCOMPLETE` completion gate from requirement↔task↔check coverage (`spec_gate` tool + `RunMetrics.Traceability`) |
| **RetryPolicy** | Retry and timeout for API calls |
