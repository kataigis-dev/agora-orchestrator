---
type: concept
title: Agent Graph
tags: [graph, orchestration, multi-agent, routing]
related: [edge-types, signal, h2c-protocol, handoff-context, parallel-execution, agora-orchestrator]
created: 2026-06-17
updated: 2026-06-20
---

# Agent Graph

The directed graph is Agora's central orchestration model. Each **node** is an LLM agent; the
**edges** define the execution flow.

## Structure

```csharp
public sealed class Graph
{
    public const string End = "END";
    public required string Entry { get; init; }
    public required IReadOnlyDictionary<string, Node> Nodes { get; init; }
    public required IReadOnlyList<Edge> Edges { get; init; }
}
```

- `Entry` — id of the start node
- `END` — sentinel node that terminates execution
- `Nodes` — `id → Node` map
- `Edges` — list of typed edges

## Execution

`GraphExecutor` runs the graph in a loop:
1. Enter the current node → run the agent
2. Collect output and signals
3. Resolve the next node via `NextNode(current, state)` (or the LLM router for `route` edges)
4. Repeat until `END` or `max_steps` (default 100) is exceeded

## Shared state (`State`)

- `UserInput` — the original user input (immutable)
- `Outputs` — accumulated `agentId → output` dictionary
- `Signals` — signals emitted by the last agent
- `Messages` — list of messages (including the RAG seed)
- `Artifacts` — accumulated shared artifacts (`<<artifact key=value>>`)
- `LoopCounters` — `source→target` counters for `max_loops` on `conditional` edges
- `LastAgent` — the last executed agent
- `Inbox(agentId)` — all messages addressed to the given agent
- `ArtifactSummary()` — a formatted string of all current artifacts

On each hop the current node's output is forwarded to the next as a message in `Messages`. With
`handoff: true` only the declared handoff payload (or nothing) is forwarded instead of the full
output — see [[handoff-context]]. With `memory: { enabled: true }` the shared context is no longer
the dump of all artifacts but the **top-K most relevant entries** recalled from memory — see
[[context-memory]]. `parallel` edges fork concurrent branches — see [[parallel-execution]].

## YAML configuration

```yaml
graph:
  entry: generator
  edges:
    - { from: generator, to: reviewer, type: handoff }
    - { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END, type: conditional, when: done }
```

## Constraints

- A global `max_steps` (default 100) guards against infinite loops
- `max_loops` per conditional edge limits the repetition of a specific cycle
