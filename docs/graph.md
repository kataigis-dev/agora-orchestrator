# Graph

## Basics

A graph defines the execution flow between agents. It is declared in the `graph` section of the
YAML config:

```yaml
graph:
  entry: generator
  edges:
    - { from: generator, to: reviewer, type: handoff }
    - { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END, type: conditional, when: done }
```

## Nodes

Each graph node corresponds to an agent in the `agents:` section. The special node `END`
terminates execution.

## Edges

### sequential

Go to the target node:

```yaml
- { from: planner, to: executor, type: sequential }
```

### handoff

Hand control to a specific node:

```yaml
- { from: generator, to: reviewer, type: handoff }
```

### conditional

Pick the next node from the agent's signal:

```yaml
- { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
- { from: reviewer, to: END, type: conditional, when: done }
```

- `when` — the signal that activates this edge
- `max_loops` — maximum iterations for this edge (prevents infinite loops)

### route

An LLM router picks the branch whose `when` description best fits the agent's output — more robust
than signal tokens. See the routing/edge-types pages.

```yaml
- { from: classifier, to: refund,  type: route, when: "the customer wants a refund" }
- { from: classifier, to: support, type: route, when: "the customer needs help" }
```

### parallel

Fork: branches run concurrently and converge on a single join node.

```yaml
- { from: planner, to: fe, type: parallel }
- { from: planner, to: be, type: parallel }
- { from: fe, to: synth, type: sequential }
- { from: be, to: synth, type: sequential }
```

## Signals

Agents drive routing with signals: `<<signal done>>` (natural) or the block subtype `[STATE:DONE]`
(h2c). If no conditional edge matches, the first non-conditional edge is taken, else `END`.

## Execution

`GraphExecutor.RunAsync()`:

1. Reads the `entry` node.
2. Builds the `State` (with the optional RAG seed message).
3. Loop: runs the current agent, parses signals/artifacts, resolves the next node, until `END` or
   `max_steps` (default 100). `parallel` edges fork; `route` edges use the LLM router.
4. Returns the final `State`.

### Real-time visualization (CLI)

With `--graph` the CLI prints the graph and each step (agent, input, signals, artifacts, chosen edge).

## State (blackboard)

- `Messages` — all messages (filtered per recipient via `Inbox()`)
- `Outputs` — last output per agent
- `Signals` — routing signals from the last agent
- `LoopCounters` — per conditional edge
- `Artifacts` — structured data shared across agents (`<<artifact key=value>>`)
- `LastAgent` — id of the last executed agent

Context delivery depends on the mode: full output, handoff-only (`handoff: true`), or top-K
recalled memory (`memory`). See the handoff-context and context-memory pages.
