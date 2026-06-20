---
type: concept
title: Shared Knowledge Base (writable RAG)
tags: [rag, knowledge, vector-store, conflict, hitl, write]
related: [rag-pipeline, human-in-the-loop, agent-graph, agora-orchestrator]
created: 2026-06-19
updated: 2026-06-19
---

# Shared Knowledge Base (writable RAG)

Evoluzione del [[rag-pipeline]] da store di sola lettura a **knowledge base
condivisa e scrivibile**: gli agenti vi scrivono quando producono modifiche, e
ogni scrittura viene confrontata con ciò che già esiste per individuare conflitti.

> **Stato:** implementata fino alla Fase 3. Gli agenti leggono/scrivono la KB via tool
> (`rag_search`/`rag_write`) e possono interpellare un altro agente (`ask_agent`) dopo
> aver cercato nel RAG. Vedi anche [[agent-graph]] e [[handoff-context]].

## Accesso al RAG: `IVectorStore`

L'accesso passa **sempre** dall'interfaccia `IVectorStore`. L'implementazione può
essere locale o remota; il core resta framework-free e un eventuale adapter verso
un DB su container viene iniettato dal bordo (come l'embedder/provider reale).

```csharp
public interface IVectorStore
{
    void Upsert(IReadOnlyList<Chunk> chunks, IReadOnlyList<float[]> vectors); // insert o update per Id
    IReadOnlyList<Chunk> Query(IReadOnlyList<float> vector, int topK, double scoreThreshold = 0.0);
    void Delete(IReadOnlyList<string> ids);                                   // necessario per sostituire voci
}
```

| Implementazione | Dove | Persistenza |
|-----------------|------|-------------|
| `InMemoryVectorStore` | core | in-process (persa al riavvio) |
| `FileVectorStore` | core | JSON su disco (`vector_store: { type: file, path: … }`) |
| `QdrantVectorStore` | `Agora.AgentFramework` | server Qdrant via **gRPC** (`vector_store: { type: qdrant, url, collection }`) |

I tipi non-core (es. `qdrant`) non sono noti al core: vengono risolti da un **resolver
iniettato dal bordo** (`AgentFrameworkVectorStores.TryCreate`, passato a `RagFactory`/`Runtime`
come `storeResolver`), così il core resta framework-free.

`Chunk` porta un `Id` stabile assegnato dallo store; `Score` è la similarità
transitoria dell'ultima query. `Id` + `Delete` permettono di **sostituire** una
voce superata invece di accodarla.

## Scrittura: `KnowledgeBase`

`KnowledgeBase.WriteAsync(text, source, agentId)` è il percorso di scrittura
agent-driven:

1. **embed** del nuovo testo → query dei vicini nello store;
2. nessun vicino → scrive direttamente (`Added`);
3. vicini presenti → `IConflictJudge` valuta il conflitto;
4. `NoConflict` → scrive (`NoConflict`);
5. `Resolved` → **sostituisce** le voci in conflitto con quella riconciliata (`AutoResolved`);
6. `Unresolved` → escala a `IConflictResolver` (umano).

## Rilevamento conflitti: `IConflictJudge`

`LlmConflictJudge` chiede a un LLM di rispondere con un protocollo a marker:

```
VERDICT: <NO_CONFLICT|RESOLVED|UNRESOLVED>
CONFLICTS_WITH: <indici delle voci esistenti in conflitto, o NONE>
RESOLUTION: <se RESOLVED: un'unica frase vera per vecchio e nuovo>
EXPLANATION: <una riga>
```

`CONFLICTS_WITH` consente di **sostituire solo le voci effettivamente in
conflitto**, non tutti i vicini. `NoOpConflictJudge` non segnala mai conflitti
(usato quando manca un provider).

## Risoluzione umana: `IConflictResolver`

Quando l'agente non può risolvere, il conflitto sale all'utente. È un secondo
canale HITL, distinto dal `IApprovalHandler` sì/no di [[human-in-the-loop]],
perché serve una scelta a tre esiti:

```csharp
public enum ConflictResolution { KeepExisting, KeepNew, Merge }
```

| Implementazione | Contesto |
|-----------------|----------|
| `ConsoleConflictResolver` | CLI — chiede `[e]xisting / [n]ew / [m]erge` |
| `FakeConflictResolver` | Test — decisione preimpostata |

- `KeepExisting` → scarta il nuovo (`Rejected`, nessuna scrittura)
- `KeepNew` → sostituisce le voci in conflitto con il nuovo (`UserResolved`)
- `Merge` → scrive il testo riconciliato dall'utente (`UserResolved`)

Se non c'è resolver disponibile, un conflitto irrisolto produce `Rejected`.

## Tool per gli agenti (Fase 3)

Gli agenti accedono alla KB tramite tool built-in, abilitati elencandoli in `tools`
(stesso meccanismo dei tool filesystem). Vivono in `Agora.AgentFramework`:

| Tool | Azione |
|------|--------|
| `rag_search(query)` | Recupera contesto rilevante dalla KB (lettura via `RagPipeline`) |
| `rag_write(text)` | Registra una voce (scrittura via `KnowledgeBase`, con conflict-check) |
| `ask_agent(target, question)` | Interpella un altro agente e ne ottiene la risposta |

```yaml
agents:
  writer:
    tools: [rag_search, rag_write, ask_agent]
```

**Flusso "RAG-first, poi chiedi"** (vedi [[handoff-context]]): in modalità handoff il
ricevente parte con poco contesto; la descrizione dei tool lo istruisce a usare prima
`rag_search` e solo dopo `ask_agent`. `ask_agent` riesegue **sincronicamente** l'agente
target e ne restituisce la risposta nel turno corrente; il target viene costruito in
"answer-mode" **senza** `ask_agent`, per evitare ricorsioni A↔B.

`Runtime` costruisce la `KnowledgeBase` riusando l'`Embedder`/`Store` del `RagPipeline`,
quindi lettura e scrittura insistono sulla **stessa** istanza di `IVectorStore`.

## Componenti

| Classe / interfaccia | Ruolo |
|----------------------|-------|
| `Rag/KnowledgeBase` | Orchestrazione embed → vicini → judge → write/escalate |
| `Rag/IConflictJudge`, `LlmConflictJudge`, `NoOpConflictJudge` | Rilevamento conflitti |
| `Rag/FileVectorStore` | Store persistente su disco |
| `HumanInTheLoop/IConflictResolver` | Risoluzione conflitti via umano |
| `AgentFramework/RagTools`, `AskAgentTool` | Tool agente `rag_search`/`rag_write`/`ask_agent` |

## Note

- Lettura (`RagPipeline`) e scrittura (`KnowledgeBase`) condividono **la stessa istanza**
  di `IVectorStore` (cruciale con `FileVectorStore`/DB).
- La soglia di similarità per considerare due voci "vicine" è configurabile sulla
  `KnowledgeBase` (`scoreThreshold`, default 0.5).
- Il judge usa `defaults.model`; senza provider/modello si ricade su `NoOpConflictJudge`.
