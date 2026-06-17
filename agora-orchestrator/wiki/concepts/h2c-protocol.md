---
type: concept
title: H2C Protocol
tags: [communication, protocol, structured-output, h2c]
related: [communication-modes, signal, edge-types, agent-graph]
created: 2026-06-17
updated: 2026-06-17
---

# H2C Protocol (Human-to-Computer)

Protocollo strutturato per la comunicazione dall'agente LLM verso l'orchestratore. È la modalità **default** (`communication: h2c`).

## Formato

Un blocco H2C ha la forma:

```
[TYPE:SUBTYPE]
key: value
key: value
```

Esempio reale per segnalare lo stato:

```
[STATE:DONE]
summary: Revisione completata senza errori.
```

## Classe `H2cBlock`

```csharp
public sealed record H2cBlock
{
    public required string Type { get; init; }
    public required string Subtype { get; init; }
    public IReadOnlyDictionary<string, string> Fields { get; init; }
}
```

## Componenti

| Classe | Ruolo |
|--------|-------|
| `H2cParser` | Parsa l'output LLM in una lista di `H2cBlock` |
| `H2cInterpreter` | Interpreta i blocchi e produce segnali per l'orchestratore |
| `H2cPreamble` | Genera il testo di istruzione da iniettare nel system prompt |

## Vantaggi rispetto a natural

- Output strutturato e deterministico
- Più robusto con modelli meno capaci
- Supporta campi chiave-valore arbitrari oltre al semplice segnale

## Quando usare H2C vs Natural

Vedi [[communication-modes]] per il confronto completo.
