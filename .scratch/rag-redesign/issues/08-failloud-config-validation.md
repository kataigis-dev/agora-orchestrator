# 08 — Fail-loud config validation for `rag_write`

Status: ready-for-human

**Decision:** D2 · **Depends:** — · **Effort:** S

## Problem

The conflict-check silently degrades to a no-op:

- embedder `fake` (the example default) → non-semantic vectors → neighbours are noise → no real
  conflict is ever detected;
- `defaults.model` unresolvable → `Retrieval.Build` falls back to `NoOpConflictJudge`, whose
  `AssessAsync` always returns `NoConflict`.

In both cases `rag_write` keeps answering `Added`/`NoConflict` as if reconciliation were working.
Nothing in `validate`/build prevents it.

## Proposal

- If **any** agent allow-lists `rag_write` (writable KB reachable), reject the config at `validate`
  **and** at Runtime/Retrieval build when:
  - the RAG embedder is `fake`/missing, or
  - the conflict-judge model (`defaults.model`) does not resolve to a `ModelSpec`.
- Error messages must name the offending agent/field, e.g. *"agent 'security' uses rag_write but
  rag.retrieval.embedder.type is 'fake' — the conflict check needs a semantic embedder
  (openai/ollama)"*.
- `NoOpConflictJudge` + `fake` remain legal only when no agent writes (read-only KB).

## Acceptance criteria

- [ ] A config with `rag_write` + `fake` embedder fails `validate` with a clear message.
- [ ] A config with `rag_write` + unresolvable judge model fails `validate`.
- [ ] The same checks fail at Runtime build (so the API/CLI can't bypass `validate`).
- [ ] A read-only RAG config (no `rag_write`) with `fake` still validates.

## Notes

Makes the writable KB "either works or doesn't start".

## Comments

**2026-06-25 — implemented.** New `RagWriteValidator.Validate(AgoraConfig)` (in `Agora.Rag.Concretes`):
when any agent allow-lists `rag_write`, it throws `ConfigException` naming the agent/field if the RAG
embedder is `fake`/missing, or if `defaults.model` doesn't resolve via `ModelResolver`. No-op for a
read-only KB. Wired at both required points: `CommandStrategy.Validate` (the `validate` verb) and the
`Runtime` constructor (so a directly-built runtime — API/CLI — can't bypass it). `ConfigLoader` stays
structural. Fixed the flagship `agora.yaml` (was `fake` + rag_write → now `ollama` local embedder +
provider). `examples/agora-spec.yaml` only mentions rag_write in comments (no change). The guided
`ConfigWizard` now asks "Will agents write to the KB?" and prompts for a real embedder (openai/ollama)
when yes, so it can no longer generate an invalid config. Tests: `RagWriteValidatorTests` (fake →
rejected, missing rag → rejected, unresolvable judge → rejected, real embedder+judge → valid, read-only
fake → valid); updated `ConfigWizardTests.Init_FullConfig` script/assertions for the new embedder
prompt. Suite green (358). Uncommitted.
