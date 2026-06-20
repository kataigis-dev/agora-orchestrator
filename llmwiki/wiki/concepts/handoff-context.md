---
type: concept
title: Handoff Context (minimal inter-agent context)
tags: [graph, handoff, context, orchestration, artifacts]
related: [agent-graph, communication-modes, shared-knowledge-base, context-memory, signal, agora-orchestrator]
created: 2026-06-19
updated: 2026-06-20
---

# Handoff Context

Optional mode for passing context between agents: instead of delivering the **whole output** of the
previous agent to the next node, it passes **only the necessary context** the sender declares
explicitly.

> The fallback "the receiver searches the RAG before asking the originating agent" is available via
> the `rag_search`/`ask_agent` tools — see [[shared-knowledge-base]].

## Activation

Opt-in, global; the default keeps the full-output behaviour:

```yaml
handoff: true   # default: false
```

## Behaviour

On a `current → next` hop ([[agent-graph]]):

| `handoff` | What the next node receives in its inbox |
|-----------|------------------------------------------|
| `false` (default) | The sender's **full output** |
| `true` | **Only** the sender's `handoff` artifact; if absent, **nothing** |

In handoff mode the `handoff` artifact is a **targeted channel** to the next agent: it is not added
to the global shared-artifact summary (`State.ArtifactSummary`), to avoid duplicating it or widening
its visibility.

If the sender declares no handoff, the receiver starts with no inbox context (it still gets
`UserInput` and shared artifacts) — it then relies on the RAG.

## How to declare the handoff

| Mode ([[communication-modes]]) | Syntax |
|--------------------------------|--------|
| `natural` | `<<artifact handoff=...>>` (token supported by [[signal]]) |
| `h2c` | a `handoff:<text>` field inside a block, e.g. `[CTX:UPDATE]`\n`handoff:auth uses JWT` |

When `handoff: true`, a short preamble (`HandoffPreamble`) is injected into every agent instructing
it to emit the handoff payload, because the next agent **does not see** its full output.

## Implementation

| Component | Role |
|-----------|------|
| `AgoraConfig.Handoff` | Config flag (`bool?`) |
| `GraphExecutor` (param `handoff`) | Passes the handoff payload or nothing; excludes `handoff` from the global artifacts |
| `H2cInterpreter` | Extracts the `handoff` field as an artifact in h2c mode |
| `Communication/HandoffPreamble` | Instruction injected when the mode is on |

## Example

`examples/agora-handoff.yaml` — natural pipeline with `handoff: true`.

## Notes

- Orthogonal to `communication`: works with both `h2c` and `natural`.
- Reduces context bloat and tokens passed downstream; the price is that agents must declare what to
  share (or lean on the shared RAG).
