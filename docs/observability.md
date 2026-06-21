# Observability & run metrics

Agora keeps the `GraphExecutor` presentation-free: it emits events to an `IExecutionObserver` instead
of rendering or measuring inline. That seam carries two built-in consumers — console rendering and a
metrics collector — and you can plug your own (structured logging, tracing export, …).

## The observer seam

`IExecutionObserver` (`src/Agora/Orchestration/IExecutionObserver.cs`) receives one call per run event:

| Event | When |
|---|---|
| `OnGraphStart(graph)` | once, before the run |
| `OnNodeStart(step, nodeId, input)` | a node's agent is about to run |
| `OnParallel(branches)` | a node fans out into parallel branches |
| `OnSignals(signals)` | after a node runs, with the signals it emitted |
| `OnUsage(usage)` | after each agent call, with its `TokenUsage` (incl. cache reads) |
| `OnArtifact(key, value)` | once per artifact a node produced |
| `OnEdge(label, target, isEnd)` | the executor takes an edge |
| `OnGraphComplete()` | once, after reaching END |

`OnUsage` is a default-interface method (empty body), so existing observers need no change.
Implementations are best-effort and must not throw.

Built-in implementations:

- **`ConsoleExecutionObserver`** — the default colored CLI rendering (now also prints a per-node
  token line).
- **`NullExecutionObserver`** — silent (tests / quiet runs).
- **`MetricsExecutionObserver`** — aggregates events into `RunMetrics` (below).
- **`CompositeExecutionObserver`** — fans every event out to several observers, so presentation and
  measurement run side by side.

## Run metrics

Every **graph run** is measured automatically: `Runtime` composes a `MetricsExecutionObserver` with
the presentation observer and exposes the result on `RunResult.Metrics` (`RunMetrics`).

```csharp
var result = await runtime.RunAsync("Build a todo app");
var m = result.Metrics!;
Console.WriteLine(m.ToSummary());
// steps=14 completed=True rework=3 forks=2 tokens=48210in/9120out cache=62% 41.7s
```

`RunMetrics` (`src/Agora/Orchestration/RunMetrics.cs`):

| Field | Meaning |
|---|---|
| `Steps` | node executions on the main path |
| `Completed` | reached END (vs. aborted on the step limit / error) |
| `Duration` | wall-clock time |
| `NodeVisits` | per-node execution count (includes parallel branches) |
| `ReworkCount` | re-executions beyond each node's first run — a fix/rework-loop proxy |
| `ParallelForks` | number of parallel fan-outs |
| `Signals` | histogram of emitted signals (e.g. `fix` vs `pass`) |
| `InputTokens` / `OutputTokens` | total token cost |
| `CacheReadTokens` / `CacheWriteTokens` | prompt-cache hits / writes |
| `CacheHitRate` | `CacheReadTokens / InputTokens` — verifies caching is working |
| `Traceability` | spec completion summary when a `spec` store is configured (else null) — see below |

When a `spec` store is configured, the runtime analyses the persisted specification at the end of the
run and attaches a `TraceabilitySummary`: `Requirements` (the approved scope), `Covered`, `Verified`,
`Tasks`, the `CoverageRate`/`VerificationRate`, and `Complete` — the deterministic completion verdict
(every approved requirement covered by a task and verified through real checks). It is the same verdict
the `spec_gate` tool returns, surfaced as a metric so a caller or CI can gate on it. See
[spec.md](spec.md#gating-completion).

### Why these

They target the signals that distinguish *capable* from *reliable* runs: completion, how much the
graph looped back to redo work (`ReworkCount`, `Signals[fix]`), where effort concentrated
(`NodeVisits`), cost (`*Tokens`), whether prompt caching actually engaged (`CacheHitRate`), and whether
the committed spec scope was genuinely traced and verified (`Traceability.Complete`) rather than merely
declared done. Use them to compare configurations — e.g. a leaner graph vs. the full gated pipeline, or
memory on vs. off — instead of judging a run on its final answer alone.

## Plugging a custom observer

Pass one to `Runtime`; it is composed with the metrics collector, so you keep metrics for free:

```csharp
var runtime = Runtime.FromConfig(path, provider, observer: new MyLoggingObserver());
```

Or use the executor directly:

```csharp
var metrics = new MetricsExecutionObserver();
var executor = new GraphExecutor(graph, agentFactory,
    observer: new CompositeExecutionObserver(new ConsoleExecutionObserver(), metrics));
await executor.RunAsync(input);
var summary = metrics.Metrics.ToSummary();
```
