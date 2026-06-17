---
type: overview
title: Agora Orchestrator — Project Overview
tags: [multi-agent, orchestration, dotnet, llm, yaml, graph]
related: [agora-orchestrator, agent-graph, h2c-protocol, rag-pipeline, skills, mcp-tools, human-in-the-loop]
created: 2026-06-17
updated: 2026-06-17
---

# Agora Orchestrator — Overview

**Agora Orchestrator** è un framework .NET 10 per l'orchestrazione di pipeline multi-agente LLM, indipendente da qualsiasi framework AI specifico.

## Architettura a colpo d'occhio

```
Agora.Cli / Agora.Api
       │
   Runtime.cs
       │
   GraphExecutor ──── Graph (nodes + edges)
       │                      │
   Agent (IAgent) ◄───── AgentConfig (YAML)
       │
   IProvider (OpenAI / Ollama / compatibile)
       │
 [Skills] [RAG] [MCP Tools] [HITL]
```

## Progetti nella solution

| Progetto | Ruolo |
|----------|-------|
| `Agora` | Core library — nessuna dipendenza esterna |
| `Agora.AgentFramework` | Integrazione OpenAI SDK, Ollama, MCP |
| `Agora.Api` | REST API (ASP.NET Core) |
| `Agora.Cli` | Eseguibile CLI |

## Concetti chiave

- **[[agent-graph]]** — Grafo diretto dove ogni nodo è un agente LLM
- **[[edge-types]]** — `sequential`, `handoff`, `conditional` (con `when` e `max_loops`)
- **[[h2c-protocol]]** — Protocollo strutturato `[TYPE:SUBTYPE]` per la comunicazione agente→orchestratore
- **[[signal]]** — Token `<<signal name>>` in modalità natural per routing condizionale
- **[[rag-pipeline]]** — Pipeline di Retrieval-Augmented Generation opzionale
- **[[skills]]** — Prompt file riutilizzabili come strumenti degli agenti
- **[[mcp-tools]]** — Integrazione tool esterni tramite Model Context Protocol
- **[[human-in-the-loop]]** — Sistema di approvazione per azioni critiche
- **[[communication-modes]]** — `h2c` (default) vs `natural`

## Configurazione YAML

Un singolo file YAML definisce tutto:
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
    role: "Sei un planner esperto."
graph:
  entry: planner
  edges:
    - { from: planner, to: reviewer, type: handoff }
    - { from: reviewer, to: planner, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END, type: conditional, when: done }
```

## Stato attuale

Core execution funzionante. RAG, HITL, MCP, REST API e CLI operativi. Vedi [[log]] per l'attività recente.
