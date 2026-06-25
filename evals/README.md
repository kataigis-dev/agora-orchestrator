# Quality evals (LLM-judge harness)

Real-model quality evaluation for Agora — the instrument that turns "is the output correct / faithful?"
into a number, so configs can be tuned and compared. This complements the scripted, offline
`agora eval` (which only regression-tests graph wiring) and the cost/stability `RunMetrics`.

Design and rationale: `.scratch/optimization-audit/issues/01-real-output-eval-harness.md`.

## What it measures

Per case, on the anchored **0 / 0.5 / 1** scale (fail / partial / pass), aggregated to 0–1:

- **correctness** — does the output actually solve the task?
- **completeness** — are all required properties covered?
- **relevance** — does it answer what was asked, on-topic?
- **groundedness** — (RAG cases, added automatically) is every claim supported by the retrieved context?

**Deterministic checks are hard gates**: if a `regex`/`contains`/`not_contains`/`signal` check fails, the
case scores 0 and the judge is skipped. The LLM judge grades only what isn't machine-verifiable, replies in
constrained JSON, and is **fail-closed** — an unparseable verdict is an evaluation error, never a free pass.

## Running

It is **opt-in** (it spends real tokens). CI stays offline by default.

```sh
export AGORA_EVAL_LIVE=1
export LMSTUDIO_API_KEY=...        # system-under-test provider (local LM Studio)
export OPENAI_API_KEY=...          # strong judge (evals/judge.openai.yaml)

# RAG cases need an embedding model + an ingest pass first:
agora ingest --config evals/configs/rag.yaml

# run the whole suite, write a machine-readable report:
agora eval-quality --suite evals/cases --judge-config evals/judge.openai.yaml --out report.json
```

Run from the repo root — case `config:` paths are resolved relative to the working directory.
`--suite` accepts a directory of `*.quality.json`, a single file, or a JSON array.

## The judge

Use a **strong** judge model (round C showed a weak local judge rubber-stamps "done"). The harness forces
**temperature 0**. Keep the judge model **different** from the system-under-test (no self-judging).

- `evals/judge.claude.yaml` — native Claude (recommended; works end-to-end via `AnthropicChatClient`).
  Set `ANTHROPIC_API_KEY`.
- `evals/judge.openai.yaml` — strong GPT-class judge.
- `evals/judge.lmstudio.yaml` — local stopgap, opt-in, unreliable; check the judge-vs-human MAE first.

## Comparing configs

Run the same suite against two configs (or two judge/model choices) and diff the JSON reports: each carries
quality scores **beside** `RunMetrics` (tokens, cache, rework), so you compare quality **and** cost together.
Cases `01` (gated graph) vs `02` (lean) are the same task on two pipelines for exactly this.

## Calibration

Cases may carry an `expectedScore` (human label). The suite reports **judge-vs-human MAE** over labeled
cases — read it before trusting the absolute numbers. The bool-trap (`03`) and abstention (`05`) cases are
deliberately hard (expected ~0.5): they should expose weak writers and a hallucinating RAG answerer.

## Layout

```
evals/
  cases/        *.quality.json — the golden set (~8 cross-cutting cases)
  configs/      systems-under-test (codegen-graph, codegen-lean, rag)
  corpus/       small RAG corpus for the groundedness/abstention cases
  judge.*.yaml  judge model configs
```

## Adding a case

Drop a `*.quality.json` in `cases/`:

```jsonc
{
  "id": "my-case",
  "input": "…",
  "config": "evals/configs/codegen-lean.yaml",
  "graph": false, "agent": "writer",        // or "graph": true for a graph config
  "reference": "optional golden answer",
  "deterministic": [ { "kind": "regex", "value": "def\\s+foo" } ],
  "rubric": { "correctness": "anchored criteria…", "relevance": "…" },
  "expectedScore": 1.0                        // optional human label
}
```
