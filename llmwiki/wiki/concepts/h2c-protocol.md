---
type: concept
title: H2C Protocol
tags: [communication, protocol, structured-output, h2c]
related: [communication-modes, signal, edge-types, agent-graph]
created: 2026-06-17
updated: 2026-06-20
---

# H2C Protocol

Structured protocol for communication from the LLM agent toward the orchestrator. It is the
**default** mode (`communication: h2c`).

## Format

An H2C block is a header line `[TYPE:SUBTYPE]` optionally followed by one fields line
`key:value|key:value`. Surrounding prose is ignored.

```
[STATE:DONE]
```

```
[ARCH:PLAN]
id:api-meteo|fw:net10|lib:[fastapi,httpx]
```

Types: `ARCH`, `BUILD`, `TEST`, `CTX`, `STATE`, `ORCH`, `SKILL`. Completion/verdicts are signalled
by the subtype (e.g. `[STATE:DONE]`, `[TEST:PASS]`, `[STATE:FIX]`).

## `H2cBlock` class

```csharp
public sealed record H2cBlock
{
    public required string Type { get; init; }
    public required string Subtype { get; init; }
    public IReadOnlyDictionary<string, string> Fields { get; init; }
}
```

## Components

| Class | Role |
|-------|------|
| `H2cParser` | Parses the LLM output into a list of `H2cBlock` |
| `H2cInterpreter` | Interprets the blocks into signals for the orchestrator (subtype → signal; a `handoff` field → handoff artifact) |
| `H2cPreamble` | The instruction text injected into the system prompt |

## Advantages over natural

- Structured, deterministic output
- More robust with less capable models
- Supports arbitrary key-value fields beyond a plain signal

## When to use H2C vs Natural

See [[communication-modes]] for the full comparison.
