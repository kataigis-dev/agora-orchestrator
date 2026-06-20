---
type: concept
title: Context Memory (RAG-backed compression)
tags: [memory, context, compression, rag, tokens, orchestration]
related: [shared-knowledge-base, handoff-context, agent-graph, rag-pipeline, agora-orchestrator]
created: 2026-06-20
updated: 2026-06-20
---

# Context Memory

Memoria di lavoro basata sul vector store per **comprimere il contesto e risparmiare
token** senza perdere troppa accuratezza. Invece di iniettare in ogni agente *tutti*
gli artifact accumulati (`State.ArtifactSummary`), il sistema **salva** gli artifact
dichiarati e ne **recupera solo i top-K più rilevanti** per il task dell'agente.

## Attivazione

Opt-in; di default disattivata.

```yaml
memory:
  enabled: true
  top_k: 5      # quante voci rilevanti recuperare per agente
```

## Come funziona

A ogni step del grafo ([[agent-graph]]):

1. **Save** — gli **artifact dichiarati** dall'agente (`<<artifact key=value>>`, incluso
   l'`handoff`) vengono scritti nella memoria: embed + upsert append-only, **senza**
   conflict-check (è contesto transitorio, non una "verità" della KB).
2. **Recall** — per costruire il contesto del prossimo agente, si embedda il suo task
   (`UserInput` + inbox) e si recuperano le **top-K voci più rilevanti**, iniettate al
   posto del dump completo degli artifact.

Il risultato: il contesto condiviso resta **limitato a K voci** anche su grafi lunghi,
mantenendo ciò che è pertinente al passo corrente.

## Separazione dalla knowledge base

Le voci di memoria sono taggate con `source` prefisso `memory:` e condividono lo stesso
`IVectorStore` della [[shared-knowledge-base]]. Il recall **filtra** sulle sole voci
`memory:`, così i fatti curati della KB e il contesto transitorio non si mescolano.
La memoria riusa l'`Embedder`/`Store` del RAG quando abilitato; altrimenti ricade su un
embedder/store offline.

## Rapporto con le altre modalità

- **[[handoff-context]]**: l'handoff passa il payload *mirato* al prossimo; la memoria
  fornisce il *background* rilevante recuperato. Si compongono.
- **[[shared-knowledge-base]]**: stessa tecnologia di store, ma scopo diverso — memoria =
  contesto transitorio comprimibile; KB = fatti durevoli con conflict-resolution.

## Implementazione

| Componente | Ruolo |
|------------|-------|
| `Configuration/MemoryConfig` | Flag `enabled` + `top_k` |
| `Rag/ContextMemory` | `RememberAsync` (append-only) / `RecallAsync` (top-K filtrati) |
| `Orchestration/GraphExecutor` | In memory-mode: recall al posto di `ArtifactSummary`, remember degli artifact |
| `Runtime` | Costruisce `ContextMemory` (riusa embedder/store del RAG) |

## Esempio

`examples/agora-memory.yaml` — pipeline natural con memoria di contesto attiva.

## Note

- Vengono salvati **solo gli artifact dichiarati**: se gli agenti non dichiarano nulla,
  la memoria resta vuota e il contesto condiviso si azzera (compressione massima, ma è
  responsabilità degli agenti dichiarare ciò che conta).
- La compressione è **per rilevanza** (recupero vettoriale), senza chiamate LLM aggiuntive.
