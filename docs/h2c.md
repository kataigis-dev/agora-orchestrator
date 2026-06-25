# H2C — agent↔orchestrator protocol

## What it is

H2C is a token-compressed, structured communication protocol. Agents express state and data as
parsable blocks, robust even with less capable models. Enabled with `communication: h2c` (default).

## Syntax

A header line `[TYPE:SUBTYPE]` optionally followed by ONE fields line `key:value|key:value`.
Surrounding prose is ignored; malformed blocks are skipped. Lists use `[a,b,c]`; file revisions
use `file~N`.

### Example

```
[ARCH:PLAN]
id:api-meteo|fw:net10|lib:[fastapi,httpx]

[STATE:DONE]
```

## Types

`ARCH`, `BUILD`, `TEST`, `CTX`, `STATE`, `ORCH`, `SKILL`.

## Subtypes

`PLAN`, `EXEC`, `DONE`, `FIX`, `REVERT`, `NACK`, `RUN`, `PASS`, `FAIL`, `PRIMITIVES`, `UPDATE`,
`PRUNE`, `COMPACT`, `FREEZE`, `NEGOTIATE`, `FINDINGS`, `ACK`, `END`, `PROMPT`.

Completion/verdicts are signalled by the subtype, e.g. `[STATE:DONE]`, `[TEST:PASS]`, `[STATE:FIX]`.

## How signals are derived

`H2cInterpreter` (`src/Agora/Communication/`) parses every block and:
- maps each block's subtype to a `true` signal (lowercased, e.g. `[STATE:DONE]` → signal `done`);
- maps each field to a signal (`key:value`);
- recognizes a `handoff` field as the handoff artifact (used in handoff mode).

`H2cParser` extracts the blocks; surrounding prose is ignored and malformed blocks are skipped.
A system-prompt preamble (assembled by `AgentInstructions`) instructs the agent to reply in H2C.

## H2C vs Natural

| Aspect | H2C | Natural |
|---|---|---|
| Format | `[TYPE:SUBTYPE]` blocks + `key:value` fields | natural language |
| Routing | `[STATE:DONE]` | `<<signal done>>` |
| Artifacts | `handoff` field | `<<artifact key=value>>` |
| When to use | weaker/local models, strict structure | capable models, readable output |
