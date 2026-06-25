# Round C — real-model probe results

**Date:** 2026-06-23 · **Provider:** local LM Studio (`http://127.0.0.1:1234/v1`, OpenAI-compatible)
**Models:** `google/gemma-4-e4b` (planner/writer), `google/gemma-4-e2b` (critic) · **Mode:** H2C
**Config:** `agora-lmstudio.yaml` · **Task:** "Implement `is_prime(n)` correct for all integers incl. n<2 and large primes."

These are real local-model numbers (offline-equivalent: free, no cloud). They confirm several static findings empirically.

## Run 1 — full graph (planner → writer → critic)

| Node | Model | Tokens in | Tokens out | Cached | Signals emitted |
|---|---|---:|---:|---:|---|
| planner | e4b | 246 | 317 | 0 | plan, id, lang, build |
| writer | e4b | 276 | 589 | 0 | fix, done |
| critic | e2b | 660 | 944 | 0 | done |
| **Total** | | **1 182** | **1 850** | **0** | — |

- **Total: 3 032 tokens**, single pass (critic routed `done` on first try → **ReworkCount = 0**, loop F7 not triggered).
- **CacheHitRate = 0** — no `cached` on any node (local Implicit; LM Studio reports no cache reads). Consistent with the token-axis caveat.
- Final run output = `[STATE:DONE]` — the **verdict**, not the code (the deliverable lived in the writer's output, invisible in the result).

## Run 2 — writer standalone (to inspect the actual code)

Generated code (gemma-4-e4b):

```python
def is_prime(n: int) -> bool:
    if n < 2: return False
    if n == 2 or n == 3: return True
    if n % 2 == 0 or n % 3 == 0: return False
    i = 5
    while i * i <= n:
        if n % i == 0 or n % (i + 2) == 0: return False
        i += 6
    return True
```

**Correctness: PASS.** Handles n<2 (negatives/0/1), 2/3, the 6k±1 wheel, and bounds with `i*i <= n`. No hallucinated APIs. Minor: the plan declared `lib:[math]` but the code imports nothing (harmless metadata inaccuracy).

## What this confirmed, per axis

- **Token (F8, live).** Per-node input grew **246 → 276 → 660** along a *linear* path: the critic's input is 2.4× the planner's purely because it ingests the writer's 589-token output. Downstream context balloons with no compression on the default path; the critic node alone was **53%** of total tokens. On a fix-loop this compounds (F7).
- **Token (F9-adjacent).** Zero cache reads — no caching cushion (moot locally, but the run shows the un-cached baseline).
- **Precision.** A 4B local model produced *correct* code on a well-specified task. The critic said `done` and was in fact right — **but we cannot tell whether it verified or got lucky**: there is no correctness instrument (F1). This is exactly why the deterministic `spec_gate` (F6) exists instead of trusting an LLM critic.
- **Hallucination / robustness.** No API hallucination in code, but **protocol-signal noise** appeared: the planner leaked a `build` signal and the writer leaked a `fix` signal (stray well-formed H2C blocks), and H2C maps *every field* to a signal (`id`, `fw`, `lib`, `lang` all became signals). On weak models this pollutes the routing namespace — a latent misrouting risk that "skip malformed blocks" does not catch (the stray blocks are well-formed).

## Headline, demonstrated
The run reported **completion** (`[STATE:DONE]`), not **correctness**, and nothing measured whether that verdict was trustworthy. That is F1 in a single screenshot: token/stability are observable; precision/hallucination are not — until the eval harness (issue 01) lands.

---

## F3 demo — RAG relevance floor (via the nomic embedder)

Cosine similarity, query *"How do I implement an is_prime function in Python?"*:

| chunk | cosine | relevant? |
|---|---:|---|
| "Primality testing: check divisibility up to the square root of n." | 0.593 | yes |
| "The Eiffel Tower is a wrought-iron lattice tower in Paris…" | 0.482 | **no** |
| "A sourdough starter is a fermented mix of flour and water…" | 0.429 | **no** |

- With `ScoreThreshold = 0.0` (Agora default) all three clear the floor → with `top_k = 6` over a small corpus, the off-topic chunks get injected as context (wasted tokens + grounding noise). **F3 confirmed.**
- The margin relevant−irrelevant is only **0.11**: nomic packs everything into a narrow 0.43–0.59 band. So not only is `0.0` wrong — a *flat constant* threshold is fragile (a 0.5 cutoff barely separates the on-topic chunk and is model-specific). **Strengthens issue 02:** prefer a normalized/relative or top-margin cutoff over one magic number.

## F7 demo — loop token growth: NOT triggered, and that is the finding

Three graph runs (lenient e2b critic; strict e4b critic; strict critic + explicit bool-trap warning on a task with a genuine bool gap) — **the critic emitted `done` on the first pass every time; 0 loops fired.**

- The writer was, in fact, competent: given explicit requirements it produced correct code, including the bool guard `if isinstance(n, bool): raise TypeError`. **No precision failure could be manufactured** — the local 4B writer kept getting it right.
- But the critic gate proved **unreliable as a control mechanism**: even told to be strict and warned about the exact trap, it rubber-stamped `done`. This is the live case for F1/F6 — you cannot drive routing off LLM self-assessment; the deterministic `spec_gate` exists for exactly this.
- Token-growth mechanism (code-level certainty): `State.Inbox` concatenates *every* message addressed to a node; on a loop the writer's inbox = planner payload + each critic fix message → grows per iteration. Demonstrated in the *linear* form — per-node input ballooned **246→276→660**, **275→315→911**, **298→363→874** across the three runs; the critic node was **53–57%** of all tokens purely from ingesting upstream output. On a loop this compounds.

## New findings surfaced by round C

- **F12 [MEDIUM] — signal-namespace pollution.** `H2cInterpreter` maps every field (and any stray well-formed block) to a routing signal. Weak models leaked signals `build`, `fix`, and even a full sentence — *"The implementation meets all requirements"* — as a signal key. "Skip malformed blocks" does not catch these (they are well-formed). Latent misrouting risk: a field value colliding with an edge `when:` would route accidentally.
- **F13 [LOW] — configured timeout not propagated to the HTTP client.** A slow local generation hit the OpenAI client's default `NetworkTimeout` (100s) *before* Agora's `timeout: 180`, and `RetryPolicy` retried into the same wall (2× wasted partial generations). `defaults.timeout` is ineffective on the actual network call.
