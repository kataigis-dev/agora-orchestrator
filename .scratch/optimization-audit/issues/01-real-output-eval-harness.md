# 01 — Real-output eval harness (LLM-judge + golden runs)

Status: ready-for-agent

**Addresses:** F1 (CRITICAL) · **Axes:** precision, hallucinations · **Impact:** ★★★ · **Effort:** M–L
**Depends on:** 06 (F9) for the native-Claude judge path · **Validates:** 02 (relevance floor), 03 (groundedness/abstention)

## Problem

The only eval harness, `Agora.Eval.ScenarioRunner`, runs against a scripted `FakeChatProvider` and asserts
`ExpectOutputContains` (substrings) + `ExpectSignals`. That is a graph-wiring regression test — it never
exercises a real model, so it measures nothing about answer correctness, faithfulness, or hallucination
rate. `RunMetrics` covers cost/stability only. Result: two of the three optimization axes (precision,
hallucinations) are unmeasurable, so "optimized?" is unfalsifiable for them.

Round C demonstrated the gap live: a run reported **completion** (`[STATE:DONE]`), never **correctness**,
and the LLM critic rubber-stamped `done` even when told to be strict and warned about the exact trap. We
need an instrument that produces a *quality number*, not a completion flag.

`docs/concepts/10-evaluation-observability.md` already prescribes the design: deterministic checks where
possible, LLM-as-judge (clear rubric, constrained JSON output, calibration against human labels) for what
is not machine-verifiable.

## Design decisions (locked via grilling, 2026-06-23)

| # | Decision | Choice |
|---|----------|--------|
| 1 | **Purpose** | Tuning/comparison **instrument** — graded scores per scenario + aggregate so configs can be compared (memory on/off, gated vs lean, model A vs B). No hard CI gate in v1; an optional pass threshold can be layered on later. |
| 2 | **Reference model** | **Hybrid per-case**: deterministic checks where they exist + rubric LLM-judge for the rest + optional reference text. "Prefer deterministic checks" (doc + spec_gate philosophy). |
| 3 | **Dimensions** | Correctness, **Groundedness** (RAG-only), Relevance, Completeness. (Conciseness left out — that is the token axis, already in `RunMetrics`.) |
| 4 | **Scale** | **3-level anchored** per dimension: `0 / 0.5 / 1` (fail / partial / pass) with explicit anchor text in the rubric; judge must emit a short **justification + cited evidence BEFORE** the score; aggregated to `0–1`. |
| 5 | **Judge protocol** | **Constrained JSON**, tolerant parser (marker-protocol fallback for weak models), **fail-CLOSED**: an unparseable/invalid verdict is an *evaluation error* — the case is flagged/excluded from the aggregate, **never** promoted to pass. (Contrast `LlmConflictJudge`, which fails *open* — do not imitate.) |
| 6 | **Judge model** | **Strong, configurable, separate from the SUT, temperature 0**; self-judging (same model grading its own output) disabled by default to avoid bias. Local/weak judge stays opt-in with a documented caveat (round C proved weak judges are unreliable). |
| 7 | **Golden set format** | New `QualityScenario` type (does not touch `Scenario`), **JSON** files (consistent with the existing System.Text.Json eval path), under a first-class versioned `evals/` directory. |
| 8 | **Composition v1** | **~8–12 cross-cutting cases**: code-gen with a deterministic check (reuse `is_prime`), RAG-grounded (answer supported by corpus), RAG-adversarial (answer NOT in corpus → must abstain), and a known-hard trap (the `bool` gap). Designed to run with memory on/off and gated vs lean. |
| 9 | **Calibration** | Default temp 0, **1 sample**; harness *designed for* optional N-sample (median/majority per dimension). A small subset carries a human-labeled `expected_score`; the harness reports **judge↔human agreement** so we know how far to trust the numbers. |
| 10 | **Granularity** | LLM **quality judgment on the final output**; **trajectory captured** (per-node output/signals/tokens via existing observers) for diagnostics + targeted deterministic checks (e.g. "did the critic actually re-check or stamp done?"). No per-node LLM judging in v1. |
| 11 | **Integration** | New CLI verb **`eval-quality`** (separate from the scripted `eval`), env-gated provider + judge. **Dual output**: human-readable per-scenario table + a machine-readable **JSON report** that places `RunMetrics` (tokens/cache/rework) *alongside* quality scores → quality-vs-cost comparison across configs. |
| 12 | **Test/CI** | Dedicated **`IJudge` abstraction + `FakeJudge`**; offline tests cover parsing, `0/0.5/1` aggregation, fail-closed, and deterministic checks without any LLM. Real runs (SUT + strong judge) gated behind `AGORA_EVAL_LIVE=1`; **CI stays offline by default, unchanged**. |
| 13 | **Judge wire path** | **Resolve F9 first** (issue 06): add the native `anthropic` branch in `ChatClients.Build` so the strong judge runs on native Claude. This makes issue 01 depend on 06. |
| 14 | **Aggregation** | **Unweighted mean** of the *applicable* dimensions per scenario (groundedness counts only when RAG is on), then mean over cases for the suite score. **Per-dimension breakdown always reported** (not just the aggregate). Deterministic checks are **HARD gates**: any deterministic failure (e.g. code doesn't compile) → case fails regardless of the LLM verdict. |

## Shape sketches (not final API)

```jsonc
// evals/cases/is_prime.quality.json — a QualityScenario
{
  "id": "is-prime-codegen",
  "input": "Implement is_prime(n) correct for all integers incl. n<2 and large primes.",
  "config": "examples/agora-graph.yaml",      // config-under-test (relative to repo)
  "rag": null,                                  // or { "corpus": [...], "query": "..." } for RAG cases
  "reference": null,                            // optional golden answer
  "deterministic": [                            // HARD gates, no LLM
    { "kind": "regex", "pattern": "def is_prime" },
    { "kind": "python_runs", "asserts": ["is_prime(1)==False", "is_prime(7)==True"] }
  ],
  "rubric": {                                   // graded by the LLM judge, 0/0.5/1 each
    "correctness":  "Handles n<2, even/odd, and large primes correctly.",
    "completeness": "Covers all declared edge cases (negatives, 0, 1).",
    "relevance":    "Answers the asked task, no off-topic content."
    // groundedness auto-added & required only when rag != null
  },
  "expected_score": 1.0                         // optional, human label → judge↔human agreement
}
```

```jsonc
// LLM judge verdict (constrained JSON, justification+evidence BEFORE score; fail-closed if invalid)
{
  "correctness":  { "justification": "...", "evidence": "line 3 handles n<2", "score": 1 },
  "completeness": { "justification": "...", "evidence": "...",               "score": 0.5 },
  "relevance":    { "justification": "...", "evidence": "...",               "score": 1 }
}
```

```csharp
public interface IJudge   // real: LlmJudge(provider, spec, temp:0); test: FakeJudge(scriptedVerdicts)
{
    Task<JudgeVerdict> JudgeAsync(QualityScenario scenario, RunOutput run, CancellationToken ct = default);
}
```

## Flow

`eval-quality --suite evals/ --config <cfg> [--judge-config <cfg>] [--out report.json]`
→ for each `QualityScenario`: run the config against the **real** SUT provider (env-gated) → capture final
output + trajectory + `RunMetrics` → run **deterministic checks** (hard gate) → if passed, call the **judge**
(strong, temp 0, JSON, fail-closed) for the applicable rubric dimensions → aggregate (unweighted mean of
applicable dims; deterministic failure forces 0) → emit human table + JSON report (quality scores beside
`RunMetrics`). Suite score = mean over cases; judge↔human agreement reported for labeled cases.

## Acceptance criteria

- [x] `eval-quality` runs a config against a **real** provider (env-gated; CI offline by default unchanged) and emits per-scenario **per-dimension** `0/0.5/1` scores plus an aggregate `0–1`.
- [x] An `IJudge` with a documented rubric and **constrained JSON** output exists, is **fail-closed**, and is unit-tested offline via `FakeJudge` (parsing, aggregation, fail-closed, deterministic gates).
- [x] Deterministic checks act as **hard gates** (a deterministic failure fails the case regardless of the LLM verdict).
- [x] Groundedness is scored for RAG cases and an **abstention** case (answer-not-in-corpus) is present; ties to issues 02/03.
- [x] Output is comparable across configs: a JSON report carries quality scores **beside** `RunMetrics` (tokens/cache/rework).
- [x] Judge↔human agreement is reported for the human-labeled subset.
- [x] Native Claude judge path works once issue **06** lands — **06 landed** (`AnthropicChatClient`); `evals/judge.claude.yaml` added. *(Live confirmation pending a real Anthropic-key run.)*

## Notes

This is the **round-C enabler**: until it lands, precision/hallucination claims remain "the framework
enables it", not a number. LangSmith (LangGraph) and Microsoft Agent Framework evaluation are reference
points. The harness scaffolding (`IJudge`/`FakeJudge`, `QualityScenario`, parser, deterministic checks,
offline tests) is **not** blocked by 06 and can proceed in parallel; only the *native-Claude* judge wire
path depends on 06.

## Comments

- 2026-06-23 — Design locked via a `/grill-me` session (14 decisions, table above). Scope confirmed as a
  graded **tuning instrument**, not a CI gate; hybrid deterministic+rubric judging; strong fail-closed JSON
  judge at temp 0; `~8–12` cross-cutting golden cases incl. RAG abstention and the `bool` trap; new
  `eval-quality` verb with a quality-beside-cost JSON report. Added hard dependency on issue 06 (F9) for the
  native-Claude judge, per user choice to "resolve F9 first".
- 2026-06-23 — **Implemented** (scaffolding, not blocked by 06). New `Agora.Eval.Quality` namespace in
  `src/Agora/Eval/Quality/`: `QualityScenario`/`DeterministicCheck`, `IJudge`+`JudgeRequest`/`JudgeVerdict`,
  `LlmJudge` (temp-0, constrained JSON, tolerant parse + marker fallback, **fail-closed**), `FakeJudge`,
  `DeterministicChecks` (regex/contains/not_contains/signal hard gates), `QualityRunner`
  (run→gate→judge→unweighted-mean aggregate, groundedness auto-added on RAG retrieval), `QualityResults`,
  `QualityReport` (human + JSON-beside-RunMetrics), `QualitySuiteLoader`, `QualityJudgeFactory`. CLI verb
  `eval-quality --suite --judge-config [--out]` gated behind `AGORA_EVAL_LIVE`. Golden set under `evals/`
  (8 cross-cutting cases, 3 SUT configs, 2 judge configs, RAG corpus, README). 10 offline unit tests added
  (`tests/Agora.Tests/Eval/QualityHarnessTests.cs`); full suite green (311 tests), solution builds clean,
  all eval configs pass `agora validate`. **Remaining:** native-Claude judge (gated on issue 06; OpenAI-compat
  stopgap works today) and a real-model calibration pass to populate judge↔human MAE.
- 2026-06-23 — **Live-validated** end-to-end against local LM Studio (`roundd-eval-harness.md`):
  graph + single-agent + RAG cases all run; deterministic gates fire; groundedness auto-added on RAG
  (`groundedness=1.0` on the abstention case); fail-closed held (weak local judge → 3 eval errors, never a
  pass); calibration worked (`MAE=0.32` flagged the weak judge); JSON report carried `RunMetrics` beside
  scores. Two infra fixes shipped from this run: (1) **harness defect** — `QualityRunner` now threads the
  `IAgentBackend` so RAG/tool configs can build real embedders; (2) **F13/issue 09** — client network
  timeout now honors `spec.Timeout` (the graph case no longer dies at the ~100s default). 314 tests green.
  Known follow-up: single-agent runs report `metrics: null` (only graph runs emit `RunMetrics`), so the
  gated-vs-lean *cost* comparison is one-sided until per-agent usage is captured. **— Resolved in issue 10:
  `RunAgentAsync` now returns `RunResult` with `RunMetrics` (single-agent projection), so lean cases carry
  metrics and the gated-vs-lean comparison is two-sided.**
