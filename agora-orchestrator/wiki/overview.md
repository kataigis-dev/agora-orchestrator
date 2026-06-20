---
type: overview
title: Agora Orchestrator — Project Overview
tags: [multi-agent, orchestration, dotnet, llm, yaml, graph]
related: [agora-orchestrator, agent-graph, edge-types, parallel-execution, checkpointing, streaming, handoff-context, rag-pipeline, shared-knowledge-base, context-memory, skills, mcp-tools, human-in-the-loop, guided-config]
created: 2026-06-17
updated: 2026-06-20
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
    IChatProvider (OpenAI / Ollama / compatibile, streaming)
       │
 [Skills] [RAG / Knowledge Base] [Context Memory] [MCP Tools] [HITL]
 [Parallel fork/join] [Router LLM] [Checkpoint/Resume]
```

## Progetti nella solution

| Progetto | Ruolo |
|----------|-------|
| `Agora` | Core library — nessuna dipendenza esterna |
| `Agora.AgentFramework` | Integrazione OpenAI SDK, Ollama, MCP |
| `Agora.Api` | REST API (ASP.NET Core) |
| `Agora.Cli` | Eseguibile CLI |

## Concetti chiave

Orchestrazione:
- **[[agent-graph]]** — Grafo diretto dove ogni nodo è un agente LLM
- **[[edge-types]]** — `sequential`, `handoff`, `conditional`, `route` (LLM), `parallel` (fork/join)
- **[[parallel-execution]]** — Branch concorrenti fork/join
- **[[checkpointing]]** — Esecuzione durevole: checkpoint per step + `resume`
- **[[streaming]]** — Streaming dei token (`run --stream`)

Comunicazione e contesto:
- **[[h2c-protocol]]** — Protocollo strutturato `[TYPE:SUBTYPE]` agente→orchestratore
- **[[signal]]** — Token `<<signal name>>` in modalità natural
- **[[communication-modes]]** — `h2c` (default) vs `natural`
- **[[handoff-context]]** — Passaggio di contesto minimo tra agenti (`handoff: true`)

Conoscenza e tool:
- **[[rag-pipeline]]** — Pipeline RAG opzionale (ingest, embed, retrieve)
- **[[shared-knowledge-base]]** — RAG scrivibile con rilevamento/risoluzione conflitti
- **[[context-memory]]** — Compressione del contesto via RAG (top-K)
- **[[skills]]** — Prompt file riutilizzabili come strumenti degli agenti
- **[[mcp-tools]]** — Integrazione tool esterni tramite MCP
- **[[human-in-the-loop]]** — Sistema di approvazione per azioni critiche

## Configurazione YAML

Un singolo file YAML definisce tutto. Per generarlo senza scriverlo a mano c'è
il wizard guidato `agora init` — vedi [[guided-config]].
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

Core execution funzionante. Operativi: RAG (lettura + knowledge base scrivibile), context
memory, HITL, MCP, esecuzione parallela (fork/join), routing LLM, checkpointing/resume,
token streaming, embedder/vector store da config (incl. Qdrant), eval harness, REST API e CLI.
Vedi [[log]] per l'attività recente.
