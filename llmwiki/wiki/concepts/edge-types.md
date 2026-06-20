---
type: concept
title: Edge Types
tags: [graph, routing, edge, conditional, route, parallel]
related: [agent-graph, signal, h2c-protocol, parallel-execution]
created: 2026-06-17
updated: 2026-06-20
---

# Edge Types

Graph edges define how the flow passes from one agent to the next. There are five types.

## `sequential`

Execution always goes to the target node, unconditionally.

```yaml
- { from: planner, to: executor, type: sequential }
```

## `handoff`

Semantically equivalent to `sequential` — the current node "hands over" control to the next. Used
to express design intent (e.g. the planner finishes and hands off to the executor).

```yaml
- { from: planner, to: executor, type: handoff }
```

## `conditional`

The edge is taken only if the signal in `when` is present in the agent's output. Supports
`max_loops` to limit repetition.

```yaml
- { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
- { from: reviewer, to: END,       type: conditional, when: done }
```

Signals are emitted via:
- `<<signal done>>` in **natural** mode
- `[STATE:DONE]` in **H2C** mode

## `route`

Routing **decided by an LLM** instead of signal tokens: each `route` edge carries a description in
`when`; after the node runs, an `IRouter` (`LlmRouter`) picks the target whose description best
fits the output. More robust than `conditional` when the model does not follow the signal syntax.

```yaml
- { from: classifier, to: refund,  type: route, when: "the customer wants a refund" }
- { from: classifier, to: support, type: route, when: "the customer needs help" }
```

With no router available, it falls back to the first `route` edge as sequential.

## `parallel`

`parallel` edges out of a node fork its successors into **concurrent execution** (fork); the
branches converge on a single join node. See [[parallel-execution]].

```yaml
- { from: planner, to: branch_a, type: parallel }
- { from: planner, to: branch_b, type: parallel }
```

## `Edge` class

```csharp
public sealed record Edge(
    string Source,
    string Target,
    string Type = "sequential",
    string? When = null,
    int? MaxLoops = null);
```

## Next-node resolution

`GraphExecutor.NextNode` scans the edges from the current node:
1. `sequential`/`handoff` → target selected
2. `conditional` → target selected only if the `when` signal is in `State.Signals` and `max_loops`
   (if set) has not been reached
3. If no conditional edge matches → the first non-conditional edge from the same node, else `END`

`route` edges are resolved by the LLM router; `parallel` edges fork concurrent branches.
