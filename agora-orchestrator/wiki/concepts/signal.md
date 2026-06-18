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

### Segnali

```
<<signal nome>>
<<signal nome=valore>>
```

### Artifacts

```
<<artifact chiave=valore>>
```

Esempi:
```
Ho completato la revisione. <<signal done>>
Trovati 3 errori da correggere. <<signal fix=3>>
Generato il sommario: <<artifact summary=Il documento descrive...>>
```

## Comportamento

- I segnali e artifact vengono **rimossi** dall'output visibile (`SignalParser.Extract`)
- I segnali vengono registrati in `State.Signals` come `{ "done": true }` o `{ "fix": "3" }`
- Gli artifact vengono registrati in `State.Artifacts` come `{ "summary": "Il documento descrive..." }`
- `GraphExecutor` usa `State.Signals` per risolvere gli edge `conditional`
- `State.ArtifactSummary()` serializza gli artifact correnti nel contesto di ogni agente successivo

## Implementazione

```csharp
// SignalParser.cs
private static readonly Regex SignalRegex =
    new(@"<<signal\s+([a-zA-Z_]\w*)(?:=([^>]*))?>>", RegexOptions.Compiled);

private static readonly Regex ArtifactRegex =
    new(@"<<artifact\s+([a-zA-Z_]\w*)=([^>]*)>>", RegexOptions.Compiled);

public static (string Output, Dictionary<string, object> Signals, Dictionary<string, string> Artifacts) Extract(string text)
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
