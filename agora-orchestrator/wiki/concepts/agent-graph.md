---
type: concept
title: Agent Graph
tags: [graph, orchestration, multi-agent, routing]
related: [edge-types, signal, h2c-protocol, agora-orchestrator]
created: 2026-06-17
updated: 2026-06-17
---

# Agent Graph

Il grafo diretto è il modello di orchestrazione centrale di Agora. Ogni **nodo** è un agente LLM; gli **edge** definiscono il flusso di esecuzione.

## Struttura

```csharp
public sealed class Graph
{
    public const string End = "END";
    public required string Entry { get; init; }
    public required IReadOnlyDictionary<string, Node> Nodes { get; init; }
    public required IReadOnlyList<Edge> Edges { get; init; }
}
```

- `Entry` — ID del nodo di partenza
- `END` — nodo sentinella che termina l'esecuzione
- `Nodes` — mappa `id → Node`
- `Edges` — lista di edge tipizzati

## Esecuzione

`GraphExecutor` esegue il grafo in loop:
1. Entra nel nodo corrente → esegue l'agente
2. Raccoglie output e segnali
3. Naviga il prossimo nodo tramite `NextNode(current, state)`
4. Ripete fino a `END` o superamento di `max_steps` (default 100)

## Stato condiviso (`State`)

- `UserInput` — input originale dell'utente (immutabile)
- `Outputs` — dizionario `agentId → output` accumulato
- `Signals` — segnali emessi dall'ultimo agente
- `Messages` — lista di messaggi (inclusi seed RAG)
- `LastAgent` — ultimo agente eseguito

## Configurazione YAML

```yaml
graph:
  entry: generator
  edges:
    - { from: generator, to: reviewer, type: handoff }
    - { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END, type: conditional, when: done }
```

## Vincoli

- `max_steps` globale (default 100) protegge da loop infiniti
- `max_loops` per edge condizionale limita la ripetizione di uno specifico ciclo
