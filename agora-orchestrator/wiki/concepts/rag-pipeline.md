---
type: concept
title: RAG Pipeline
tags: [rag, retrieval, embedding, vector-store, knowledge]
related: [agora-orchestrator, agora-agent-framework, skills, shared-knowledge-base]
created: 2026-06-17
updated: 2026-06-19
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
| `IVectorStore` | → `InMemoryVectorStore`, `FileVectorStore`, `QdrantVectorStore` | Ricerca coseno; `Upsert`/`Query`/`Delete` per Id |
| `IRefiner` | → `LlmRefiner` / `NoOpRefiner` | Raffina la query prima del retrieval |
| `RagPipeline` | — | Orchestra retrieve + refine |
| `RagFactory` | — | Istanzia la pipeline dalla config |

## Configurazione YAML

```yaml
rag:
  enabled: true
  refine:
    strategy: none        # "none" | "llm"
    model: ""             # modello per il refine LLM
  retrieval:
    embedder:
      type: fake          # "fake" | "openai" | "ollama"
      provider: openai    # quale provider per chiave/base (per type non-fake)
      model: text-embedding-3-small
    vector_store:
      type: memory        # "memory" | "file" | "qdrant"
      path: ""            # per "file"
      url: ""             # per "qdrant" (gRPC, es. http://localhost:6334)
      collection: ""      # per "qdrant"
    top_k: 6
    score_threshold: 0.0
  ingest:
    sources:
      - path: knowledge/agora.md
      - path: docs/
    chunk_size: 800
    chunk_overlap: 120
```

## Integrazione con il grafo

Il seed RAG viene aggiunto come messaggio `"rag"` all'inizio della coda messaggi del nodo `entry` del grafo (`state.Messages`). L'agente lo riceve come contesto aggiuntivo nel proprio inbox.

## Percorso di scrittura

Oltre alla lettura (retrieve), il RAG può essere **scritto** dagli agenti tramite
`KnowledgeBase`, con rilevamento conflitti e risoluzione (agente o umano). Vedi
[[shared-knowledge-base]].

## Esempio

`examples/agora-rag.yaml` — configurazione completa con RAG abilitato.

## Note

- `InMemoryVectorStore` è in-memory: lo store viene perso al riavvio (progettato per semplicità, non per produzione)
- `FileVectorStore` (`type: file`) persiste su disco come JSON, sopravvive ai riavvii
- `QdrantVectorStore` (`type: qdrant`) usa un server Qdrant via gRPC (client ufficiale
  `Qdrant.Client`); vive in `Agora.AgentFramework` ed è iniettato dal bordo — vedi
  [[shared-knowledge-base]]
- `LlmRefiner` chiama il provider LLM per riformulare la query prima del retrieval
- `FakeEmbedder` è usato nei test per evitare chiamate reali ai provider
- Embedder reali (`type: openai`/`ollama`) sono selezionabili **da config**: il core risolve
  chiave/base dal provider e li passa a un resolver iniettato dal bordo
  (`AgentFrameworkEmbedders.TryCreate`), come per il vector store
