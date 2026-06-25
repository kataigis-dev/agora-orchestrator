# 13 — Concentrate CLI verb wiring on `ConfigState`

Status: ready-for-agent

**Addresses:** architecture review candidate 4 · **Axes:** locality, maintainability · **Effort:** S

## Problem

Each CLI verb re-wired the same setup. The provider guard
(`state.Provider ?? throw "no chat provider supplied"`) was copied across `run`, `resume`, `ingest`, and
`eval-quality` (4×), and the `Runtime.FromConfig(Require("config"), provider, backend:, approvalHandler:,
conflictResolver:, checkpointStore:)` wiring was copied across `run`, `resume`, `ingest` (3×). Option access
went through a free static `Require(options, name)`. A change to how a runtime is wired (a new edge
dependency, say) meant editing three verbs identically.

## Design (locked via `/grilling`, 2026-06-24)

| # | Decision | Choice |
|---|----------|--------|
| 1 | **Deepened module** | **`ConfigState` becomes the module the verbs talk to.** It already carries every edge dependency + I/O stream, so it gains the wiring: `Require(name)`, `RequireProvider()`, and `BuildRuntime(checkpoints?)`. Verbs ask the state for a ready runtime and keep only their own run/format logic. (Rejected: a full `VerbRunner` pipeline template — run/format genuinely diverge per verb, so it degrades to "build runtime, call your delegate", i.e. this plus indirection; and a free static helper, which scatters the helpers across two types.) |
| 2 | **Scope (honest)** | Only the genuinely repeated wiring is concentrated. The divergent run/format per verb stays in each verb — no forced template. |

## Acceptance criteria

- [x] `ConfigState.BuildRuntime(checkpoints?)` is the single place a verb gets a wired runtime; the 3 `FromConfig` copies are gone.
- [x] `ConfigState.RequireProvider()` replaces the 4 copied provider guards.
- [x] `ConfigState.Require(name)` replaces the free static `Require(options, name)` across all verbs.
- [x] Behaviour unchanged: missing `--config` → "missing required option --config"; missing provider → "no chat provider supplied".
- [x] CLI guard tests added (through `CliRunner.Run`); full suite green.

## Notes

Candidate 4 from the architecture review (marked *Worth exploring*, with the explicit caveat that CLI
dispatch is often legitimately repetitive). The deepening is deliberately surgical — concentrate the
runtime-construction wiring + guards on the state object, not the whole orchestration. Tests go through the
public `CliRunner.Run` (the interface is the test surface), not `ConfigState`'s internals.

## Comments

- 2026-06-24 — Design locked via `/grilling` under `/improve-codebase-architecture`, then **implemented**.
  `ConfigState` gained `Require`/`RequireProvider`/`BuildRuntime`; `run`/`resume`/`ingest` now call
  `state.BuildRuntime(...)`, and `eval`/`eval-quality`/`validate` use `state.Require(...)`. The free static
  `Require` was deleted. Two CLI guard tests added in `CliRunnerTests`. Full suite green (**328**: 320
  Agora.Tests + 8 Agora.Api.Tests).
