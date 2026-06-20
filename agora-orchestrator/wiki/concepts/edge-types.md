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

## `route`

Routing **deciso da un LLM** invece che da signal token: ogni edge `route` porta una
descrizione in `when`; dopo l'esecuzione del nodo, un `IRouter` (`LlmRouter`) sceglie il
target la cui descrizione meglio si adatta all'output. Più robusto del `conditional`
quando il modello non rispetta la sintassi dei segnali.

```yaml
- { from: classifier, to: refund,  type: route, when: "il cliente vuole un rimborso" }
- { from: classifier, to: support, type: route, when: "il cliente ha bisogno di aiuto" }
```

Senza router disponibile, si ricade sul primo edge `route` come sequenziale.

## `parallel`

Gli edge `parallel` in uscita da un nodo ne diramano i successori in **esecuzione
concorrente** (fork); i branch convergono su un unico nodo join. Vedi
[[parallel-execution]].

```yaml
- { from: planner, to: branch_a, type: parallel }
- { from: planner, to: branch_b, type: parallel }
```

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
