---
type: concept
title: Signal
tags: [routing, signal, natural-mode, conditional]
related: [edge-types, communication-modes, h2c-protocol, agent-graph]
created: 2026-06-17
updated: 2026-06-17
---

# Signal

Meccanismo di routing condizionale in modalità **natural**. L'agente include token speciali nell'output per comunicare lo stato all'orchestratore.

## Sintassi

```
<<signal nome>>
<<signal nome=valore>>
```

Esempi:
```
Ho completato la revisione. <<signal done>>
Trovati 3 errori da correggere. <<signal fix=3>>
```

## Comportamento

- Il testo del segnale viene **rimosso** dall'output visibile (`SignalParser.Extract`)
- Il segnale viene registrato in `State.Signals` come `{ "done": true }` o `{ "fix": "3" }`
- `GraphExecutor` usa `State.Signals` per risolvere gli edge `conditional`

## Implementazione

```csharp
// SignalParser.cs
private static readonly Regex SignalRegex =
    new(@"<<signal\s+([a-zA-Z_]\w*)(?:=([^>]*))?>>", RegexOptions.Compiled);

public static (string Output, Dictionary<string, object> Signals) Extract(string text)
```

## Equivalente H2C

In modalità H2C il segnale equivalente è:
```
[STATE:DONE]
```
o
```
[STATE:FIX]
count: 3
```

## Note

- I nomi di segnale sono case-sensitive
- Un agente può emettere più segnali nello stesso output
- Solo i segnali dell'**ultimo** agente eseguito vengono considerati per il routing (`State.Signals` viene sovrascritto ad ogni step)
