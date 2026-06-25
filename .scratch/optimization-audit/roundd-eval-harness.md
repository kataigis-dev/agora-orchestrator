# Round D — eval harness, live validation (local LM Studio)

**Date:** 2026-06-23 · **SUT:** local LM Studio (`gemma-4-e4b`/`e2b`, nomic embedder) · **Judge:** local
`gemma-4-e4b` (weak, partially self-judging — see caveat) · Suite: `evals/cases` (8 cases).

This run validates the issue-01 harness **end-to-end against real models**. With no cloud key available, the
judge is a weak local model, so the **absolute scores are not trustworthy** — the point here is that the
*plumbing* works and the *calibration signal* (judge-vs-human MAE) correctly flags the weak judge.

## Result (after fixes)

```
quality suite: score=0.88 over 8 case(s), errors=3
judge-vs-human MAE=0.32 over 5 labeled case(s)
dimension means: correctness=0.90, completeness=0.67, relevance=1.00, groundedness=1.00

[PART] is-prime-graph: 0.67 {correctness=0.5, completeness=0.5, relevance=1.0}
[ERR ] is-prime-lean: judge verdict unusable (fail-closed): could not parse 'correctness'
[PASS] is-prime-bool-trap: 1.00 {correctness=1.0, completeness=1.0}
[ERR ] factual-grounded: judge verdict unusable (fail-closed): could not parse 'correctness'
[PASS] factual-abstention: 1.00 {correctness=1.0, relevance=1.0, groundedness=1.0}
[ERR ] relevance-guard: judge verdict unusable (fail-closed): could not parse 'correctness'
[PART] summary-completeness: 0.75 {completeness=0.5, correctness=1.0}
[PASS] json-format: 1.00 {correctness=1.0}
```

## What this proves works (live)

- **Full pipeline** runs against real models: graph (planner→writer→reviewer), single-agent, and RAG
  (answerer) all execute and get graded.
- **Deterministic hard gates** fire: `json-format` passed the `^{`/`}$` regex gate; `factual-abstention`
  passed the `not_contains "1889"` gate (model abstained in **8 output tokens**).
- **Groundedness auto-added** on RAG retrieval: `factual-abstention` scored `groundedness=1.0` against the
  retrieved chunks.
- **Fail-CLOSED** holds: all 3 errors are the weak judge failing to emit parseable JSON → excluded from the
  aggregate, **never** a silent pass.
- **Calibration works**: `MAE=0.32` over 5 labeled cases quantifies that the weak local judge is *not*
  trustworthy. The single biggest miss is `is-prime-bool-trap` (judge said 1.0, human label 0.5) — the same
  ambiguity round C flagged: we cannot tell if the 4B writer truly nailed the bool guard or the weak judge
  rubber-stamped it. A strong judge (`evals/judge.claude.yaml`) is the fix.
- **Quality beside cost**: the JSON report carries `RunMetrics` per graph/RAG case
  (`is-prime-graph`: steps=3, 545 in / 2304 out; `factual-grounded`: 490/349; `factual-abstention`: 483/8).

## Bugs/issues this live run surfaced — and the fixes applied

1. **Harness defect (fixed):** `QualityRunner` built the SUT `Runtime` **without the agent backend**, so
   real embedders (`openai`/`ollama`) and tools could not be created → both RAG cases errored with
   *"embedder type 'openai' has no built-in implementation and no backend provided"*. Fixed by threading
   `IAgentBackend?` through `RunScenarioAsync`/`RunSuiteAsync` and passing `state.Backend` from the CLI. RAG
   cases now run.
2. **F13 / issue 09 (fixed):** the 3-node graph case hit the OpenAI client's ~100s `NetworkTimeout` before
   the configured `timeout: 180` and was cancelled. Fixed by setting `OpenAIClientOptions.NetworkTimeout`
   from `spec.Timeout` in `ChatClients.BuildOpenAI` (the same fix already applied to `AnthropicChatClient`).
   The graph case now completes.

## Known limitations (follow-ups, not blockers)

- ~~**Single-agent runs report `metrics: null`** — only graph runs emit `RunMetrics`, so the gated-vs-lean
  *cost* comparison (case 01 graph vs 02 lean) is currently one-sided. Capturing per-agent token usage on
  the single-agent path would complete it.~~ **Resolved (issue 10):** `RunAgentAsync` now returns
  `RunResult` carrying a `RunMetrics.ForAgent(...)` projection, so lean cases report metrics and the
  comparison is two-sided.
- The weak local judge produced 3 fail-closed errors; rerun with `evals/judge.claude.yaml` (+
  `ANTHROPIC_API_KEY`) for trustworthy numbers and a meaningful MAE.

## Reproduce

```sh
export AGORA_EVAL_LIVE=1 LMSTUDIO_API_KEY=lm-studio
agora ingest --config evals/configs/rag.yaml
agora eval-quality --suite evals/cases --judge-config evals/judge.lmstudio.yaml --out evals/report.local.json
# trustworthy: --judge-config evals/judge.claude.yaml  (export ANTHROPIC_API_KEY)
```
