---
type: concept
title: Checkpointing & Resume
tags: [durability, checkpoint, resume, state, orchestration]
related: [agent-graph, parallel-execution, agora-cli, agora-orchestrator]
created: 2026-06-20
updated: 2026-06-20
---

# Checkpointing & Resume

**Durable** execution: the graph `State` is saved at every step to a pluggable checkpoint store, so
an interrupted run (crash, kill) can be **resumed** from where it stopped, without re-running
already-completed nodes. See [[agent-graph]].

## Model

- **`StateSnapshot`** — a JSON-serializable snapshot of `State` (current node, step, input,
  messages, outputs, artifacts, signals, loop counters, last agent). The `signals` (a `string|bool`
  map) round-trip through JSON and are normalized on load.
- **`ICheckpointStore`** — `Save(runId, snapshot)` / `Load(runId)`. Implementations:
  `InMemoryCheckpointStore` (tests) and `FileCheckpointStore` (one JSON per run id).

## Semantics

A checkpoint is saved **after** each node completes (with `Current` = the next node). Therefore:

- Completed nodes are preserved (executed **once**).
- Only the node that was about to start is re-run on resume (at-least-once).
- For fan-out ([[parallel-execution]]) the checkpoint happens after the join.

On resume, `GraphExecutor.RunAsync(resumeFrom: snapshot)` rebuilds `State`, restarts from the
`Current` node and **skips** the RAG seed (already present in the saved messages).

## CLI

```bash
# run while saving checkpoints; prints the run-id on stderr
agora run --config app.yaml --input "..." --graph --checkpoint ./checkpoints

# resume the interrupted run
agora resume --config app.yaml --checkpoint ./checkpoints --run-id <id>
```

`Runtime.RunAsync(input, runId?)` generates a run id when checkpointing is on;
`Runtime.ResumeAsync(runId)` loads the snapshot and continues.

## Limits / notes

- If the crash happens **during** a model call, that node restarts from scratch (its LLM call is
  redone) — node execution is not transactional.
- It is the foundation for **durable HITL** (an approval pause that survives restarts): a future
  extension on top of this mechanism. See [[human-in-the-loop]].
