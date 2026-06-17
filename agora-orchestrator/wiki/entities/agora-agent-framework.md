---
type: entity
title: Agora.AgentFramework
tags: [integration, openai, ollama, mcp, dotnet]
related: [agora-orchestrator, mcp-tools]
created: 2026-06-17
updated: 2026-06-17
---

# Agora.AgentFramework

Progetto satellite che fornisce le integrazioni concrete con provider LLM e tool esterni. Il core `Agora` dipende solo da interfacce — `AgentFramework` le implementa.

## Classi principali

| Classe | Ruolo |
|--------|-------|
| `AgentFrameworkAgent` | Implementazione di `IAgent` tramite OpenAI/Ollama |
| `AgentFrameworkChatProvider` | Gestione della sessione di chat con il provider |
| `AgentFrameworkEmbedder` | `IEmbedder` per RAG — genera embedding via provider |
| `AgentFrameworkToolAgentFactory` | `IToolAgentFactory` — crea agenti con tool abilitati |
| `McpToolSession` | Gestisce la sessione MCP con un server esterno (stdio) |
| `SkillTools` | Converte le Skill in tool chiamabili dall'agente |
| `ChatClients.cs` | Factory per client OpenAI / Ollama / compatibili |

## Provider supportati

| Provider | Config key | Note |
|----------|-----------|-------|
| OpenAI | `openai` | Richiede `api_key_env` |
| Ollama | `ollama` | Richiede `base_url` (default `http://localhost:11434`) |
| LM Studio / llama.cpp | qualsiasi | `base_url` con `/v1` suffix, `api_key_env: ~` |
| Qualsiasi OpenAI-compatible | qualsiasi | Stesso schema di Ollama |

## Note importanti

- Il `/v1` suffix nel `base_url` è obbligatorio per provider compatibili OpenAI (l'SDK costruisce i path relativamente alla base URL).
- `AgentFrameworkEmbedder` è usato da `RagFactory` quando il RAG è abilitato.
