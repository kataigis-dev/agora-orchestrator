---
type: concept
title: Edge Types
tags: [graph, routing, edge, conditional]
related: [agent-graph, signal, h2c-protocol]
created: 2026-06-17
updated: 2026-06-17
---

# Edge Types

Gli edge del grafo definiscono come il flusso passa da un agente al successivo. Esistono tre tipi.

## `sequential`

L'esecuzione passa sempre al nodo target, senza condizioni.

```yaml
- { from: planner, to: executor, type: sequential }
```

## `handoff`

Semanticamente equivalente a `sequential` — il nodo corrente "consegna" il controllo al successivo. Usato per comunicare intenzione progettuale (es. il planner termina e passa all'executor).

```yaml
- { from: planner, to: executor, type: handoff }
```

## `conditional`

L'edge viene percorso solo se il segnale specificato in `when` è presente nell'output dell'agente. Supporta `max_loops` per limitare la ripetizione.

```yaml
- { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
- { from: reviewer, to: END,       type: conditional, when: done }
```

I segnali vengono emessi tramite:
- `<<signal done>>` in modalità **natural**
- `[STATE:DONE]` in modalità **H2C**

## Classe `Edge`

```csharp
public sealed record Edge(
    string Source,
    string Target,
    string Type = "sequential",
    string? When = null,
    int? MaxLoops = null);
```

## Risoluzione del nodo successivo

`GraphExecutor.NextNode` scorre gli edge dal nodo corrente:
1. Se l'edge è `sequential` o `handoff` → target selezionato
2. Se l'edge è `conditional` → target selezionato solo se il segnale `when` è presente in `State.Signals` e `max_loops` (se presente) non è stato raggiunto
3. Se nessun edge condizionale corrisponde → si passa al primo edge non-condizionale dallo stesso nodo, o `END` se non esiste
