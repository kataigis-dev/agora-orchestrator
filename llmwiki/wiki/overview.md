---
type: overview
title: Agora Orchestrator — Project Overview
tags: [multi-agent, orchestration, dotnet, llm, yaml, graph]
related: [agora-orchestrator, agent-graph, edge-types, parallel-execution, checkpointing, streaming, handoff-context, rag-pipeline, shared-knowledge-base, context-memory, skills, mcp-tools, human-in-the-loop, guided-config]
created: 2026-06-17
updated: 2026-06-20
---

# Agora Orchestrator — Overview

**Agora Orchestrator** is a .NET 10 framework for orchestrating multi-agent LLM pipelines,
independent of any specific AI framework.

## Architecture at a glance

```
Agora.Cli / MCP stdio
       │
   Runtime.cs
       │
   GraphExecutor ──── Graph (nodes + edges)
       │                      │
   Agent (IAgent) ◄───── AgentConfig (YAML)
       │
    IChatProvider (OpenAI / Ollama / compatible, streaming)
       │
 [Skills] [RAG / Knowledge Base] [Context Memory] [MCP Tools] [HITL]
 [Parallel fork/join] [Router LLM] [Checkpoint/Resume]
```

## Projects in the solution

| Project | Role |
|---------|------|
| `Agora` | Core library — no external dependencies |
| `Agora.AgentFramework` | OpenAI SDK, Ollama, MCP, Qdrant integration |
| `Agora.Cli` | CLI executable and local read-only MCP stdio entry point |

## Key concepts

Orchestration:
- **[[agent-graph]]** — Directed graph where each node is an LLM agent
- **[[edge-types]]** — `sequential`, `handoff`, `conditional`, `route` (LLM), `parallel` (fork/join)
- **[[parallel-execution]]** — Concurrent fork/join branches
- **[[checkpointing]]** — Durable execution: per-step checkpoints + `resume`
- **[[streaming]]** — Token streaming (`run --stream`)

Communication and context:
- **[[h2c-protocol]]** — Structured `[TYPE:SUBTYPE]` protocol, agent→orchestrator
- **[[signal]]** — `<<signal name>>` tokens in natural mode
- **[[communication-modes]]** — `h2c` (default) vs `natural`
- **[[handoff-context]]** — Minimal context passing between agents (`handoff: true`)

Knowledge and tools:
- **[[rag-pipeline]]** — Optional RAG pipeline (ingest, embed, retrieve)
- **[[shared-knowledge-base]]** — Writable RAG with conflict detection/resolution
- **[[context-memory]]** — Context compression via RAG (top-K)
- **[[skills]]** — Reusable prompt files as agent tools
- **[[mcp-tools]]** — External tool integration via MCP
- **[[human-in-the-loop]]** — Approval system for critical actions

## YAML configuration

A single YAML file defines everything. To generate one without writing it by hand, use the
guided `agora init` wizard — see [[guided-config]].
```yaml
version: "1"
communication: natural
defaults: { model: fast, temperature: 0.2 }
providers:
  openai: { api_key_env: OPENAI_API_KEY }
models:
  fast: { provider: openai, model: gpt-4o-mini }
agents:
  planner:
    model: fast
    role: "You are an expert planner."
graph:
  entry: planner
  edges:
    - { from: planner, to: reviewer, type: handoff }
    - { from: reviewer, to: planner, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END, type: conditional, when: done }
```

## Current status

Core execution working. Operational: RAG (read + writable knowledge base), context memory, HITL,
MCP, parallel execution (fork/join), LLM routing, checkpointing/resume, token streaming,
config-driven embedder/vector store (incl. Qdrant), eval harness, REST API and CLI.
See [[log]] for recent activity.
