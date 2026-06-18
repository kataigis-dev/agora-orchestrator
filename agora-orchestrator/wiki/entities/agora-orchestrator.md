---
type: entity
title: Agora Orchestrator
tags: [framework, dotnet, multi-agent, orchestration]
related: [agent-graph, h2c-protocol, rag-pipeline, skills, mcp-tools, human-in-the-loop, agora-cli, agora-api]
created: 2026-06-17
updated: 2026-06-17
---

# Agora Orchestrator

Framework .NET 10 per l'orchestrazione di pipeline multi-agente LLM. Progettato per essere **framework-free** nel core (nessuna dipendenza da SemanticKernel, LangChain o simili).

## Progetto: `Agora` (core)

- `Runtime.cs` — punto di ingresso per l'esecuzione singolo-agente o grafo
- `GraphExecutor.cs` — motore di esecuzione del grafo con loop e routing condizionale
- `Agent.cs` / `IAgent` — astrazione agente, eseguibile su qualsiasi provider
- `SignalParser.cs` — estrae token `<<signal name>>` dall'output dell'agente

## Classi di configurazione

| Classe | Ruolo |
|--------|-------|
| `AgoraConfig` | Root della configurazione YAML |
| `AgentConfig` | Model, role, system prompt, skills, tools, approvals |
| `GraphConfig` | Entry point e lista di edge |
| `ModelConfig` | Provider + nome modello |
| `ProviderConfig` | Endpoint, api_key_env |
| `RagConfig` | Sorgenti per RAG |
| `McpConfig` | Server MCP (command + args) |
| `SkillsConfig` | Directory delle skill |

## Dipendenze esterne (Agora core)

Nessuna — solo .NET BCL.

## Dipendenze esterne (Agora.AgentFramework)

- `Microsoft.Extensions.AI.OpenAI`
- `Microsoft.Agents.AI` + `Microsoft.Agents.AI.OpenAI`
- `ModelContextProtocol.Core` (MCP client)
- `OllamaSharp`

## Versioning

La configurazione YAML ha `version: "1"` come unico campo di versione.
