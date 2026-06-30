# Project Purpose — Agora Orchestrator

## Research Question

> How can you build a multi-agent orchestration framework for .NET that is independent of any
> specific AI framework, entirely configurable via YAML, and supports agent graphs with conditional
> routing, RAG, HITL and MCP?

## Hypothesis / Working Thesis

> A directed-graph architecture where each node is an independent LLM agent, connected by typed
> edges (sequential, handoff, conditional, route, parallel), lets you compose complex AI pipelines
> without depending on specific frameworks like SemanticKernel or LangChain — keeping the
> orchestration logic in a pure core and delegating integrations to satellite projects.

## Background

**Agora Orchestrator** is a .NET 10 framework for orchestrating multi-agent pipelines. It arose from
the need for a tool that is:
- **framework-free** in the core (no dependency on SK, LangChain, etc.)
- **configurable via YAML** (providers, models, agents, graphs, RAG, MCP)
- **extensible** through satellite projects (Agora.AgentFramework for OpenAI/Ollama/MCP/Qdrant)

Existing frameworks (LangChain, AutoGen, SemanticKernel) tend to couple orchestration logic with
specific providers, making it hard to swap model or provider.

## Sub-questions

1. How do you model conditional routing based on signals emitted by the agents?
2. What is the optimal protocol for structured agent↔orchestrator communication (H2C vs natural)?
3. How do you integrate RAG transparently without modifying the agents?
4. How do you implement HITL (Human-in-the-Loop) with a configurable approval system?
5. How do you expose external tools via MCP (Model Context Protocol)?

## Scope

**In scope:**
- Orchestrating LLM agent graphs via YAML
- Providers: OpenAI, Ollama, any OpenAI-compatible endpoint
- Edge types: sequential, handoff, conditional (with max_loops), route (LLM), parallel (fork/join)
- RAG pipeline: ingest, embedding, retrieval, refinement; writable knowledge base with conflict resolution
- Context engineering: minimal handoff context, RAG-backed context memory
- Skills: reusable prompt files as agent tools
- MCP: external tool integration via stdio/HTTP
- HITL: configurable approval handler for critical actions
- Durable execution (checkpoint/resume) and token streaming
- CLI (Agora.Cli) and read-only MCP stdio exposure for RAG search
- Observability: tracing

**Out of scope:**
- Specific AI frameworks in the core (SK, LangChain, LlamaIndex)
- Graphical UI
- Multi-tenancy / auth in the core

## Methodology

- TDD: unit and integration-style tests in `Agora.Tests`
- Design by contract via interfaces (`IAgent`, `IChatProvider`, `IEmbedder`, `IVectorStore`,
  `IApprovalHandler`, `IConflictResolver`, `ICheckpointStore`)
- YAML configuration as the single source of truth for a run
- Real examples in `examples/` for each main feature

## Success Criteria

- A multi-agent graph with a conditional loop works end-to-end via CLI
- The provider is swappable without changing the graph logic
- RAG and MCP are optional and enabled only via config
- Tests pass across all layers (unit + integration)
- The YAML configuration is validatable with clear error feedback

## Current Status

> Actively developed — core graph execution and AgentFramework integration working. RAG (read +
> writable KB), context memory, HITL, MCP, parallel execution, LLM routing, checkpointing/resume,
> token streaming, CLI, and read-only MCP stdio exposure are operational. The former REST API was
> retired to keep writable RAG on the CLI + human path.
