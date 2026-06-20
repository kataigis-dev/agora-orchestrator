---
type: concept
title: Communication Modes
tags: [communication, h2c, natural, protocol]
related: [h2c-protocol, signal, handoff-context]
created: 2026-06-17
updated: 2026-06-20
---

# Communication Modes

Agora supports two agent↔orchestrator communication modes, configured globally.

## Configuration

```yaml
communication: h2c      # default
# or
communication: natural
```

## Comparison

| Aspect | `h2c` | `natural` |
|--------|-------|-----------|
| Output format | `[STATE:DONE]` + fields | `<<signal done>>` inline in the text |
| Structure | Typed blocks with key-value fields | Free text with special tokens |
| Robustness | High (less capable models) | Medium (requires the model to follow the syntax) |
| Output readability | Technical | Natural |
| System-prompt injection | `H2cPreamble` injected automatically | Signal instructions in the role |
| Parsing | `H2cParser` + `H2cInterpreter` | `SignalParser` (signal + artifact) |
| Shared artifacts | — | `<<artifact key=value>>` |

## When to use `h2c`

- Models with moderate instruction-following (e.g. small local models)
- Pipelines where output structure is critical
- When extra fields beyond the signal are needed (e.g. `reason`, `count`)

## When to use `natural`

- Capable models (GPT-4, Claude, > 13B models)
- Pipelines where the output must be readable by the end user
- Rapid prototyping

## Example config files

- `examples/agora-h2c.yaml` — H2C pipeline with a conditional loop
- `examples/agora.yaml` — minimal, no graph (natural implied)
