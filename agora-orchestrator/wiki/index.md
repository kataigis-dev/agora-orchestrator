# Wiki Index

## Entities

- [[agora-orchestrator]] — Core library .NET 10 per orchestrazione multi-agente, framework-free
- [[agora-cli]] — Eseguibile CLI: `init`, `run`, `resume`, `ingest`, `validate`, `eval`
- [[agora-api]] — Server REST ASP.NET Core per esecuzione agenti via HTTP
- [[agora-agent-framework]] — Integrazione OpenAI, Ollama, MCP (progetto satellite)

## Concepts

- [[agent-graph]] — Grafo diretto di agenti LLM con routing basato su segnali
- [[edge-types]] — Tipi di edge: `sequential`, `handoff`, `conditional`, `route`, `parallel`
- [[parallel-execution]] — Esecuzione concorrente di branch (fork/join)
- [[checkpointing]] — Esecuzione durevole: salva lo stato per step e riprende (`run --checkpoint`, `resume`)
- [[streaming]] — Streaming dei token (`IStreamingChatProvider`, `run --stream`)
- [[h2c-protocol]] — Protocollo strutturato `[TYPE:SUBTYPE]` per comunicazione agente→orchestratore
- [[signal]] — Token `<<signal name>>` per routing condizionale in modalità natural
- [[communication-modes]] — Confronto tra modalità `h2c` (default) e `natural`
- [[handoff-context]] — Passaggio di contesto minimo tra agenti (`handoff: true`)
- [[rag-pipeline]] — Pipeline Retrieval-Augmented Generation: ingest, embed, retrieve, inject
- [[shared-knowledge-base]] — RAG scrivibile: `KnowledgeBase`, conflict-judge LLM, risoluzione HITL
- [[context-memory]] — Compressione del contesto via RAG: salva e recupera top-K (`memory: enabled`)
- [[skills]] — Prompt file Markdown riutilizzabili come tool degli agenti
- [[mcp-tools]] — Tool esterni tramite Model Context Protocol (stdio)
- [[human-in-the-loop]] — Approvazione umana per azioni critiche (HITL)
- [[guided-config]] — Comando `agora init`: costruzione interattiva della config YAML

## Sources

## Queries

## Comparisons

## Synthesis
