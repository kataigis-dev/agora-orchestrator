# Project Purpose — Agora Orchestrator

## Research Question

> Come si può costruire un framework di orchestrazione multi-agente per .NET che sia indipendente da qualsiasi framework AI specifico, configurabile interamente tramite YAML, e che supporti grafi di agenti con routing condizionale, RAG, HITL e MCP?

## Hypothesis / Working Thesis

> Un'architettura a grafo diretto dove ogni nodo è un agente LLM indipendente, collegato da edge tipizzati (sequential, handoff, conditional), permette di comporre pipeline AI complesse senza dipendere da framework specifici come SemanticKernel o LangChain — mantenendo la logica di orchestrazione nel core puro e delegando le integrazioni a progetti satellite.

## Background

**Agora Orchestrator** è un framework .NET 10 per orchestrare pipeline multi-agente. Nasce dall'esigenza di avere uno strumento:
- **framework-free** nel core (nessuna dipendenza da SK, LangChain, ecc.)
- **configurabile via YAML** (provider, modelli, agenti, grafi, RAG, MCP)
- **estendibile** tramite progetti satellite (Agora.AgentFramework per OpenAI/Ollama/MCP)

I framework esistenti (LangChain, AutoGen, SemanticKernel) tendono ad accoppiare la logica di orchestrazione con provider specifici, rendendo difficile il cambio di modello o provider.

## Sub-questions

1. Come si modella il routing condizionale basato su segnali emessi dagli agenti?
2. Qual è il protocollo ottimale per la comunicazione strutturata tra agente e orchestratore (H2C vs natural)?
3. Come si integra RAG in modo trasparente senza modificare gli agenti?
4. Come si implementa HITL (Human-in-the-Loop) con un sistema di approval configurabile?
5. Come si espongono tool esterni tramite MCP (Model Context Protocol)?

## Scope

**In scope:**
- Orchestrazione di grafi di agenti LLM tramite YAML
- Provider: OpenAI, Ollama, qualsiasi endpoint OpenAI-compatibile
- Edge types: sequential, handoff, conditional (con max_loops)
- RAG pipeline: ingest, embedding, retrieval, refinement
- Skills: prompt file riutilizzabili come strumenti dell'agente
- MCP: integrazione tool esterni via stdio
- HITL: approval handler configurabile per azioni critiche
- REST API (Agora.Api) e CLI (Agora.Cli)
- Osservabilità: tracing con OpenTelemetry

**Out of scope:**
- Framework AI specifici nel core (SK, LangChain, LlamaIndex)
- UI grafica
- Persistent memory tra esecuzioni separate (oltre RAG)
- Multi-tenancy / auth nel core

## Methodology

- Sviluppo TDD: test unitari in `Agora.Tests`, integration test in `Agora.Api.Tests`
- Design by contract tramite interfacce (`IAgent`, `IEmbedder`, `IVectorStore`, `IApprovalHandler`)
- Configurazione YAML come unica fonte di verità per un'esecuzione
- Esempi reali in `examples/` per ogni feature principale

## Success Criteria

- Un grafo multi-agente con loop condizionale funziona end-to-end via CLI e API
- Il provider è intercambiabile senza modificare la logica del grafo
- RAG e MCP sono opzionali e attivabili solo via config
- I test passano su tutti i layer (unit + integration)
- La configurazione YAML è validabile con feedback chiaro sugli errori

## Current Status

> In sviluppo attivo — core graph execution e AgentFramework integration funzionanti. RAG, HITL e MCP integrati. API REST operativa.
