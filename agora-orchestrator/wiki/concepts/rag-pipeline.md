---
type: concept
title: RAG Pipeline
tags: [rag, retrieval, embedding, vector-store, knowledge]
related: [agora-orchestrator, agora-agent-framework, skills]
created: 2026-06-17
updated: 2026-06-17
---

# RAG Pipeline

Pipeline di **Retrieval-Augmented Generation** integrata in Agora. Permette agli agenti di accedere a conoscenza esterna senza modificarne il codice.

## Architettura

```
Ingest:    Sorgenti → TextChunker → IEmbedder → IVectorStore
Retrieve:  UserInput → IRefiner → query → IVectorStore.Search → EnrichedInput
Execution: EnrichedInput iniettato come seed context nel grafo
```

## Componenti

| Classe | Interfaccia | Ruolo |
|--------|-------------|-------|
| `Ingestor` | — | Legge sorgenti, chunka, embedding, indicizza |
| `TextChunker` | — | Divide testo in chunk sovrapposti |
| `IEmbedder` | → `AgentFrameworkEmbedder` | Genera vettori embedding |
| `IVectorStore` | → `InMemoryVectorStore` | Ricerca per similarità coseno |
| `IRefiner` | → `LlmRefiner` / `NoOpRefiner` | Raffina la query prima del retrieval |
| `RagPipeline` | — | Orchestra retrieve + refine |
| `RagFactory` | — | Istanzia la pipeline dalla config |

## Configurazione YAML

```yaml
rag:
  sources:
    - path: knowledge/agora.md
      type: markdown
  chunk_size: 500
  chunk_overlap: 50
  top_k: 5
  refine: true
```

## Integrazione con il grafo

Il seed RAG viene aggiunto come messaggio `"rag"` all'inizio della coda messaggi del nodo `entry` del grafo (`state.Messages`). L'agente lo riceve come contesto aggiuntivo nel proprio inbox.

## Esempio

`examples/agora-rag.yaml` — configurazione completa con RAG abilitato.

## Note

- `InMemoryVectorStore` è in-memory: lo store viene perso al riavvio (progettato per semplicità, non per produzione)
- `LlmRefiner` chiama il provider LLM per riformulare la query prima del retrieval
- `FakeEmbedder` è usato nei test per evitare chiamate reali ai provider
