---
type: entity
title: Agora Orchestrator
tags: [framework, dotnet, multi-agent, orchestration]
related: [agent-graph, h2c-protocol, rag-pipeline, shared-knowledge-base, context-memory, parallel-execution, checkpointing, streaming, skills, mcp-tools, human-in-the-loop, agora-cli, agora-api]
created: 2026-06-17
updated: 2026-06-20
---

# Agora Orchestrator

.NET 10 framework for orchestrating multi-agent LLM pipelines. Designed to be **framework-free**
in the core (no dependency on SemanticKernel, LangChain or similar).

## Project: `Agora` (core)

- `Runtime.cs` — entry point for single-agent or graph execution
- `GraphExecutor.cs` — graph execution engine with loops, conditional routing, parallel fork/join,
  routing, checkpointing and streaming
- `Agent.cs` / `IAgent` — agent abstraction, runnable on any provider
- `SignalParser.cs` — extracts `<<signal name>>` / `<<artifact key=value>>` tokens from agent output

## Configuration classes

| Class | Role |
|-------|------|
| `AgoraConfig` | Root of the YAML configuration (incl. `handoff`, `language`, `memory`) |
| `AgentConfig` | Model, role, system prompt, skills, tools, approvals |
| `GraphConfig` | Entry point and edge list |
| `ModelConfig` | Provider + model name |
| `ProviderConfig` | Endpoint, api_key_env |
| `RagConfig` | Retrieval (embedder, vector store), refine, ingest sources |
| `MemoryConfig` | Context memory (enabled, top_k, max_chars, remember_outputs) |
| `McpConfig` | MCP servers (command + args / url) |
| `SkillsConfig` | Skill directories |

## External dependencies (Agora core)

None — only the .NET BCL.

## External dependencies (Agora.AgentFramework)

- `Microsoft.Extensions.AI` (+ OpenAI)
- `Microsoft.Agents.AI`
- `ModelContextProtocol.Core` (MCP client)
- `OllamaSharp`
- `Qdrant.Client` (gRPC vector DB adapter)

## Versioning

The YAML configuration uses `version: "1"` as its only version field.
