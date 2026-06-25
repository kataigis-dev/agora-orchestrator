# Optimization Audit — token consumption, precision, hallucinations

Status: ready-for-human

**Scope.** Framework level (`src/`), not a specific config/workload.
**Priority.** Reliability-first — minimize tokens *subject to* not hurting precision/anti-hallucination.
**Yardstick.** Documented best practice already cited by the repo (`docs/concepts/`: Anthropic/Google/Microsoft/AWS) + comparison with **LangGraph** and **Microsoft Agent Framework** where illuminating.
**Method.** Broad-but-weighted sweep → deep dives on critical paths → cross-reference. Evidence = static reading **+ offline probes** (no real-model runs).
**Evidence captured.** `dotnet build Agora.slnx` → 0 warnings / 0 errors. `dotnet test tests/Agora.Tests` → **293 passed, 0 failed**. No real-model run was performed (deferred to "round C").

---

## Verdict — scorecard per axis

| Axis | Verdict | Dominant constraint |
|---|---|---|
| **Token** | **Partial-but-good** | Strong levers + real measurement, but several defaults leave the un-optimized path on by default; Anthropic caching is dead end-to-end. |
| **Precision** | **Partial** | Excellent *determinism where it counts* (spec gate), weak RAG grounding, and — decisive — **no measurement of answer correctness against real models**. |
| **Hallucinations** | **Levers present, measurement absent** | Best-in-class structural anti-hallucination (deterministic off-LLM completion gate), but no groundedness enforcement, a fail-open KB gate, and **no hallucination-rate instrument**. |

**The headline (cross-reference result).** Two of the three axes (precision, hallucinations) are **not falsifiable today**: the only eval harness (`Agora.Eval.ScenarioRunner`) runs against a *scripted* `FakeChatProvider` and asserts substring/signal presence — a graph-wiring regression test, not a quality measure. `RunMetrics` measures **cost and stability** (tokens, cache-hit rate, rework, spec-traceability), not **answer quality**. So "is Agora optimized?" can be answered with data only for **token** today. The single dominant constraint is **F1 — no real-output quality measurement**: it gates both reliability axes. *You cannot optimize what you cannot measure.*

---

## Findings

Severity: **CRITICAL** blocks the stated goal · **HIGH** materially hurts an axis · **MEDIUM** worth fixing · **LOW** observation · **STRENGTH** keep & lean on it.

### Measurement (cross-cutting → precision + hallucinations)

**F1 [CRITICAL] — No quality eval against real models.**
`src/Agora/Eval/ScenarioRunner.cs` uses `FakeChatProvider(scenario.Responses)` (scripted) and checks `ExpectOutputContains` (substrings) + `ExpectSignals`. That validates *graph wiring*, deterministically and offline — valuable, but it never exercises a real model, so it measures **0%** of answer correctness, faithfulness, or hallucination rate. There is no LLM-as-judge over outputs, no golden dataset run against a live provider, no faithfulness/groundedness score. `docs/concepts/10-evaluation-observability.md` describes exactly this (deterministic checks + LLM-judge for what isn't machine-verifiable) — the doc is ahead of the code. → **This is the constraint that makes precision/hallucinations unprovable.**

**F2 [MEDIUM] — Observability is a stub, not OpenTelemetry.**
`src/Agora/Observability/Tracing.cs` is an in-process `SpanRecord` recorder whose own comment says *"OpenTelemetry comes in a later phase."* It is used by tests, not exported. `docs/concepts/10` and **Microsoft Agent Framework** both make OTel the standard for latency/token/error-rate telemetry. Agora has the *seam* (`IExecutionObserver`, `MetricsExecutionObserver`) but no OTel exporter.

### Precision / hallucinations (grounding)

**F3 [HIGH] — RAG has no relevance floor by default. (cross-axis: token + precision)**
`RetrievalConfig.ScoreThreshold` defaults to `0.0` and `RagPipeline`'s constructor defaults `scoreThreshold = 0.0`. Retrieval returns the top-K chunks *regardless of similarity*. Result: irrelevant chunks get injected into context, which (a) **wastes tokens** and (b) **grounds the model on noise**, a direct hallucination vector. Highest-leverage single fix in the report — one default change improves two axes.

**F4 [HIGH] — No groundedness enforcement or abstention.**
RAG chunks are computed once per run (`Runtime.RunAsync`) and delivered as `seedContext` to the **entry node only**. Nothing instructs the model to ground its answer in the retrieved chunks or cite them; empty retrieval proceeds silently (no "no relevant context found" path); there is no post-hoc faithfulness check. The strongest anti-hallucination levers in the literature — grounded generation, citations, abstention on low evidence — are not present.

**F5 [MEDIUM] — KB conflict judge fails open.**
`LlmConflictJudge.Parse` treats *"anything we cannot classify → NoConflict"*, and `ConflictingEntries` falls back to *all* existing entries on an unparseable list. A malformed/ambiguous judge reply therefore **silently admits** a possibly-contradictory fact into the shared KB, poisoning future retrieval → a downstream hallucination source. For a reliability-first system this gate should **fail closed** (route to `Unresolved`/human) on parse failure.

**F6 [STRENGTH] — Deterministic completion gate. Keep and lean on it.**
`TraceabilityValidator` + `AcceptanceVerifier` + the `spec_gate` tool move the "done?" decision **off LLM judgement** onto real check exit codes, and the `unsubstantiated-verification` gap blocks marking a requirement `Verified` without a machine-checkable criterion. This is best-in-class and **exceeds LangGraph and MS Agent Framework**, neither of which ships a spec-traceability completion gate. It is the part of the system most aligned with the reliability-first goal.

### Token

**F7 [MEDIUM] — Inbox grows unbounded across loops.**
`State.Inbox(agentId)` concatenates *every* message ever addressed to a node. On a conditional loop (e.g. `reviewer↔generator`, `max_loops:5`) each pass appends another full output → monotonic token growth. Context memory does **not** mitigate this: `GraphExecutor.BuildContextAsync` only swaps `ArtifactSummary()` for top-K recall; the inbox is still concatenated verbatim. H2C even defines `COMPACT`/`PRUNE` subtypes, but nothing acts on them to trim the inbox.

**F8 [MEDIUM] — No context compression on the default path.**
With `memory` disabled (the default), `State.ArtifactSummary()` dumps **all** accumulated artifacts into every node's context, growing with run length. The compression lever (`memory: top_k/max_chars`) exists and is good — but it is opt-in and enabled in only one example. Out of the box, the un-optimized path is the default.

**F9 [MEDIUM, documented] — Anthropic prompt caching is dead end-to-end.**
`CacheTranslation.MarkStable` correctly tags Breakpoint-provider messages with `cache_control`, and both agent paths mark the system prompt stable — but `ChatClients.Build` has **no `anthropic` branch**: it falls through to the OpenAI client, which never serializes `cache_control` to Anthropic's wire format. So the *one* provider that needs explicit breakpoints (Claude — the most cache-lucrative) gets **zero** caching. Honestly flagged in `docs/providers.md`, but still a dead lever.

**F10 [LOW] — Single cache breakpoint (system prompt only).**
Fine for implicit prefix caching (OpenAI/Ollama), where the cacheable prefix *is* the system message. Noted only for completeness: stable injected KB context isn't placed in a cacheable position, but it currently lives in the volatile user turn and varies per node, so the upside is limited until F4/context handling changes.

**F11 [STRENGTH] — Solid token levers + real measurement.**
H2C compression (default mode), context-memory top-K with a char cap and **no extra LLM calls**, handoff-only payloads, low default temperature (`0.2`), and a `CacheHitRate` metric that *verifies caching actually engaged*. Token is the one axis where "optimized" is checkable today, and the foundations are good.

### Surfaced by round C (real-model probe — see [roundc/results.md](roundc/results.md))

**F12 [MEDIUM] — Signal-namespace pollution.**
`H2cInterpreter` maps every field (and any stray well-formed block) to a routing signal. Across three live runs, weak models leaked signals `build`, `fix`, and even a full sentence — *"The implementation meets all requirements"* — as a signal key. "Skip malformed blocks" does not catch these (they are well-formed). Latent misrouting risk: a field value colliding with an edge's `when:` would route accidentally. Touches precision (routing correctness) and robustness on weak models — exactly H2C's stated target audience.

**F13 [LOW] — Configured `timeout` not propagated to the HTTP client.**
A slow local generation hit the OpenAI client's default `NetworkTimeout` (100s) *before* Agora's `timeout: 180`, and `RetryPolicy` retried into the same wall (2× wasted partial generations). The `defaults.timeout` lever is ineffective on the actual network call → wasted tokens on slow/large generations.

**Live confirmation of the headline.** Three graph runs with progressively stricter critics (incl. an explicit trap warning) all rubber-stamped `[STATE:DONE]` on the first pass — **0 loops fired**. The LLM critic is unreliable as a control gate, which is precisely the argument for the deterministic `spec_gate` (F6) and for F1 (measure, don't trust). The writer's code was in fact correct each time, so no precision *failure* was observed — but neither could the system *prove* the verdict, which is the point.

---

## Cross-reference (triangulation)

1. **One root explains two verdicts.** Precision and hallucinations are both "unprovable" for the *same* reason — F1, no output-quality measurement. Cost (token) is measured (RunMetrics, exercised by the 293 passing tests), which is precisely why token is the only axis gradable with data.
2. **F3 is the highest-leverage fix.** `ScoreThreshold = 0.0` sits at the intersection of *token waste* and *hallucination risk*: one default change moves two axes. Smallest effort, broadest impact.
3. **Compounding on loops.** F3 (noisy retrieval) + F7/F8 (unpruned/unbounded context) compound exactly where `ReworkCount` (already measured) is high — the existing metric can *flag* the symptom, but no lever *auto-mitigates* the token blow-up. Measurement without a corresponding lever.
4. **Determinism vs. judgement split is sound (F6) but partial.** Agora correctly moves the *completion* decision off the LLM, but leaves *retrieval relevance* (F3) and *KB conflict admission* (F5) on weak/fail-open paths — the same "prefer deterministic checks" principle isn't applied uniformly.

## Comparison (B)

- **LangGraph.** Ships durable execution, interrupts/HITL, and message-trimming/state-reducer helpers (`trim_messages`) that directly address F7/F8; its LangSmith side provides the eval/LLM-judge layer Agora lacks (F1). Agora wins decisively on F6 (no equivalent built-in spec-traceability gate) and on H2C token compression.
- **Microsoft Agent Framework.** First-class OpenTelemetry + evaluation integration — the mature version of F2/F1. Agora's differentiators it does not ship: H2C structured token-compression and the deterministic spec-completion gate (F6).

---

## Remediation roadmap

Prioritized by **reliability impact × (1/effort)**. Each links to a child issue under `issues/`. This audit is diagnosis + roadmap only — no code was changed.

| # | Issue | Addresses | Impact | Effort |
|---|---|---|---|---|
| 1 | [01 — real-output eval harness](issues/01-real-output-eval-harness.md) | F1 | ★★★ | M–L |
| 2 | [02 — RAG relevance floor](issues/02-rag-relevance-floor.md) | F3 | ★★★ | S |
| 3 | [03 — groundedness & abstention](issues/03-groundedness-and-abstention.md) | F4 | ★★ | M |
| 4 | [04 — conflict judge fail-closed](issues/04-conflict-judge-fail-closed.md) | F5 | ★★ | S |
| 5 | [05 — context/inbox compaction](issues/05-context-inbox-compaction.md) | F7, F8 | ★★ | M |
| 6 | [06 — Anthropic caching end-to-end](issues/06-anthropic-caching-end-to-end.md) | F9 | ★ | M |
| 7 | [07 — OpenTelemetry export](issues/07-opentelemetry-export.md) | F2 | ★ | M |
| 8 | [08 — H2C signal hygiene](issues/08-h2c-signal-hygiene.md) | F12 | ★★ | S–M |
| 9 | [09 — propagate timeout to client](issues/09-propagate-timeout-to-client.md) | F13 | ★ | S |

**Suggested order.** #2 and #4 first (tiny, two-axis / fail-closed wins). #1 next — it is the round-C enabler that finally makes precision/hallucinations measurable. Then #3, #5, #6, #7.

## Comments

- 2026-06-23 — Round C (real-model probe) executed against local LM Studio (gemma-4-e4b/e2b, H2C). Results in [roundc/results.md](roundc/results.md). Confirmed live: F8 (per-node input grew 246→276→660 on a linear path; critic = 53% of 3 032 total tokens), CacheHitRate = 0, and the headline — the run reported completion (`[STATE:DONE]`), not correctness, with no instrument to judge the verdict (F1). The generated `is_prime` was actually correct, so no precision failure observed on this task; the critic's `done` could not be distinguished from luck (the F1 point). Also surfaced a new robustness note: H2C maps every field to a signal and weak models leak stray (well-formed) blocks → routing-namespace pollution.
