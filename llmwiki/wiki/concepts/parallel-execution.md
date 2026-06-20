---
type: concept
title: Parallel Execution (fork/join)
tags: [graph, parallel, fan-out, join, concurrency, orchestration]
related: [agent-graph, edge-types, agora-orchestrator]
created: 2026-06-20
updated: 2026-06-20
---

# Parallel Execution (fork/join)

**Concurrent** execution of independent agents via a fork→join pattern: a *fork* node branches into
N branches that run in parallel, and their outputs converge on a single *join* node (e.g. a
synthesizer). Reduces latency when the branches are independent. See [[agent-graph]] and
[[edge-types]].

## Configuration

The fork's outgoing edges are `type: parallel`; the branches converge on one join:

```yaml
graph:
  entry: planner
  edges:
    - { from: planner, to: research_web, type: parallel }
    - { from: planner, to: research_docs, type: parallel }
    - { from: research_web,  to: synthesizer, type: sequential }
    - { from: research_docs, to: synthesizer, type: sequential }
    - { from: synthesizer, to: END, type: sequential }
```

## Semantics

1. The **fork** runs normally; its output is delivered to **every** branch's inbox.
2. The **branches** run **concurrently** (`Task.WhenAll`): the concurrency is in the model calls
   (the expensive part); `State` is mutated **serially** after they all finish → no races.
3. The **join** is the single node the branches converge on; it receives **all** branch outputs in
   its inbox and continues the normal flow. Diverging joins are an error (`GraphError`).
4. Branches that go to `END` → no join (the pipeline ends after the branches).

## Constraints (v1)

- Branches are **single agents** (no nested fan-out inside a branch).
- All branches of a fork must converge on **one** join node.

## Implementation

`GraphExecutor.FanOutAsync`: adds the fork→branch messages, runs the branches with `Task.WhenAll`,
computes the join (the branches' common target), then records outputs and branch→join messages
serially. Non-parallel paths are unchanged.

## Example

`examples/agora-parallel.yaml`.

## Notes

- Each agent uses its own chat client/provider, so concurrent calls are independent; the
  store/memory is only read during fan-out (safe concurrency).
