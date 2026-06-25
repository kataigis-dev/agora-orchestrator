# 10 — Symmetric run metrics (single-agent runs stop returning `metrics: null`)

Status: ready-for-agent

**Addresses:** round-D follow-up of issue 01 (eval harness) · **Axes:** token (cost visibility), precision (measurability) · **Impact:** ★★ · **Effort:** S
**Surfaced by:** the architecture review (candidate 1) · **Unblocks:** gated-vs-lean cost comparison in the quality harness

## Problem

The run interface lies about metrics. `Runtime.RunAsync` (graph) returns a `RunResult` carrying
`RunMetrics`; `Runtime.RunAgentAsync` (single agent) returns a bare `AgentResult` with **no** metrics —
two entry points, two return types. The metrics seam (`IExecutionObserver` → `MetricsExecutionObserver`)
is wired onto the graph executor only, so a single-agent run produces no `RunMetrics` even though the
token counts it needs are **already on the `AgentResult`** it returns (`Agent.cs` sets
`InputTokens`/`OutputTokens`/`CacheReadTokens`/`CacheWriteTokens`) and are simply discarded by
`RunAgentAsync`.

Consequence: the quality harness (issue 01) reports `metrics: null` for every `graph=false` (lean) case,
so the **gated-vs-lean cost comparison is one-sided** — exactly the round-D known limitation. A caller
cannot read metrics uniformly across run kinds.

## Design (locked via `/grilling`, 2026-06-23)

| # | Decision | Choice |
|---|----------|--------|
| 1 | **Unification depth** | **Unify the return type.** `RunAgentAsync` returns `RunResult` (Output + State + Metrics), like `RunAsync`. One result surface. The lean path stays lean — **no `GraphExecutor`**, no RAG seeding, no graph render — the single-agent metrics are a trivial projection of the `AgentResult`. (Rejected: attaching `Metrics` to `AgentResult` — keeps two return types; collapsing the single path into a 1-node graph — over-couples and changes behaviour.) |
| 2 | **Agent boundary** | `IAgent.RunAsync` **unchanged** (still returns `AgentResult`). Only `Runtime.RunAgentAsync` projects up to `RunResult`. Keeps the agent-vs-runtime split clean. |
| 3 | **Metrics assembler (locality)** | New factory **`RunMetrics.ForAgent(AgentResult, TimeSpan, string agentId)`** — the single, named place that builds single-agent metrics: `Steps=1`, `Completed=true`, `NodeVisits={id:1}`, `ReworkCount=0`, `ParallelForks=0`, `Signals` from the result's signal keys, tokens from the result, `Duration` measured. `MetricsExecutionObserver` stays the graph assembler. Two genuinely different cases, two named functions. (Rejected: driving the observer with synthetic events — drags graph vocabulary into the lean path; inlining in `Runtime` — worsens the god-object.) |
| 4 | **Traceability** | `WithTraceability(...)` is applied to single-agent metrics too (a single tool-agent can mutate the spec) — symmetric with the graph path. |
| 5 | **Single-agent `State`** | Build a minimal `State`: `Outputs[id]=output`, `Signals`, `Artifacts` (copy `<string,string>`→`<string,object>`), `LastAgent=id`. `RunResult.State` stays non-null/honest. |
| 6 | **CLI visibility** | `CommandStrategy` prints `metrics.ToSummary()` to **stderr** for `run` (graph), `run --agent`, and `resume` — always on, same channel as `run-id`, so stdout stays clean for piping. |

## Consumer migration (replace, don't layer)

- `Agora.Api/RunExecutor.cs` and `Cli/CommandStrategy.cs`: read `.Output` → unchanged.
- `Eval/ScenarioRunner.cs` and `Eval/Quality/QualityRunner.cs`: `.Signals` → **`.State.Signals`**; `QualityRunner`'s `graph=false` branch also captures `r.Metrics` → closes `metrics: null`.
- Raw token fields are no longer exposed on `RunAgentAsync` (they live in `Metrics`); **no current consumer reads them**, so nothing is lost.

## Acceptance criteria

- [x] `Runtime.RunAgentAsync` returns `RunResult` whose `Metrics` is populated (`steps=1`, token counts from the run, `completed=true`).
- [x] `RunMetrics.ForAgent` exists and is unit-tested as a pure projection.
- [x] A `graph=false` quality scenario carries non-null `Metrics` in the JSON report (gated-vs-lean cost comparison is two-sided).
- [x] `agora run --agent` and `agora run --graph` both print a metrics summary line to stderr.
- [x] Full suite green; no test reaches past the `RunResult` interface.

## Notes

This is the deepening from the architecture review's **candidate 1** (top recommendation): the highest
leverage for the least surface, because the token counts already exist on `AgentResult`. It is
*concentration*, not new machinery — deliberately **not** a strategy-per-feature refactor of the run
path. Closes the round-D follow-up recorded in issue 01 and `roundd-eval-harness.md`.

## Comments

- 2026-06-23 — Design locked via `/grilling` (6 decisions, table above) as a side effect of the
  `/improve-codebase-architecture` review. Chosen path: unify the return type to `RunResult`; single-agent
  metrics via a dedicated `RunMetrics.ForAgent` projection (lean path keeps no graph machinery); metrics
  surfaced to stderr for all run kinds.
- 2026-06-23 — **Implemented.** `RunMetrics.ForAgent(AgentResult, TimeSpan, agentId)` added
  (`Orchestration/Models/RunMetrics.cs`); `Runtime.RunAgentAsync` now returns `RunResult` (builds a minimal
  `State`, projects metrics, applies `WithTraceability`); `IAgent.RunAsync` left unchanged. Consumers
  migrated: `ScenarioRunner`/`QualityRunner` read `.State.Signals`, and `QualityRunner`'s `graph=false`
  branch captures `r.Metrics` → **`metrics: null` gap closed**. `CommandStrategy` prints
  `RunMetrics.ToSummary()` to stderr for `run` (graph + `--agent`) and `resume`; the two `run` branches were
  also collapsed into one. Tests added: pure `RunMetricsForAgentTests`, `RunAgent_ReturnsMetrics_LikeAGraphRun`,
  and `Runner_LeanScenario_CarriesMetrics`. Full suite green (**317**: 309 Agora.Tests + 8 Agora.Api.Tests).
