# 12 — Communication instruction composer (`AgentInstructions`)

Status: ready-for-agent

**Addresses:** architecture review candidate 3 · **Axes:** locality, maintainability · **Impact:** ★★ · **Effort:** S

## Problem

How an agent is instructed to communicate was scattered. The protocol's two sides — its system-prompt
preamble and its `IOutputInterpreter` — were chosen in two different places (`Runtime` ctor picked the
interpreter; `BuildAgent` prepended the preamble), so they could silently drift (an `H2cInterpreter` with no
`H2cPreamble`, or vice versa). On top of that, `BuildAgent` hand-assembled three preambles (H2C, handoff,
language) with ordering and the `handoff`/`answerMode` interplay inline. Understanding or extending "how an
agent communicates" meant bouncing across the ctor, `BuildAgent`, and 5 classes.

## Design (locked via `/grilling`, 2026-06-23)

| # | Decision | Choice |
|---|----------|--------|
| 1 | **Module scope** | **Broad instruction composer** (`AgentInstructions`): one module owns the entire system-prompt prefix (protocol preamble + handoff + language, in order) AND the interpreter; the protocol (natural/H2C) is an internal detail. `BuildAgent` calls one method. Strong deletion test — removing it scatters all assembly + interpreter selection back. (Rejected: a narrow protocol-only `CommunicationMode`, which leaves language/ordering in `BuildAgent`; and a minimal preamble↔interpreter co-location, which leaves assembly scattered.) |
| 2 | **Interface** | `IOutputInterpreter Interpreter { get; }` (stable per run) + `string Prefix(bool answerMode)` (the only per-call variation). `handoff`/`language`/protocol are held from config, not passed per call → small surface. |
| 3 | **Existing classes** | **Absorb** the preamble text: `H2cPreamble`, `HandoffPreamble`, `LanguagePreamble` deleted, their text inlined as private members of `AgentInstructions`. The interpreters (`SignalInterpreter`, `H2cInterpreter`) **stay** (they are the parsers; the composer just selects one). |
| 4 | **Runtime** | Drops the `_h2c` and `_interpreter` fields (encapsulated in the composer); keeps `_handoff` (the `GraphExecutor` needs it for routing, not just the preamble). |

## Acceptance criteria

- [x] `AgentInstructions.For(config)` owns prefix assembly (language → protocol → handoff, in order) and interpreter selection; `BuildAgent` calls it.
- [x] The protocol preamble and its interpreter cannot drift — both come from the same module.
- [x] `H2cPreamble`/`HandoffPreamble`/`LanguagePreamble` removed; no dangling references (code + docs).
- [x] Handoff is suppressed in answer-mode; existing language/H2C behaviour preserved (`RuntimeLanguageTests`/`RuntimeH2cTests` green).
- [x] New `AgentInstructionsTests` cover natural/H2C, handoff on/off + answer-mode, language, and ordering; full suite green.

## Notes

Candidate 3 from the architecture review, and the natural home for the still-open **F12** (H2C
signal-namespace pollution, issue 08) — the interpreter selection now lives beside the protocol so a
hardened `H2cInterpreter` plugs in one place. New domain term `AgentInstructions` recorded in `CONTEXT.md`
(created lazily). Handoff/language are instruction *layers*, not protocols, so they stay non-polymorphic.

## Comments

- 2026-06-23 — Design locked via `/grilling` under `/improve-codebase-architecture` (4 decisions).
- 2026-06-24 — **Implemented.** New `src/Agora/Communication/AgentInstructions.cs` (composer: `Interpreter`
  + `Prefix(answerMode)`, protocol preamble/handoff/language inlined). `Runtime` drops `_h2c`/`_interpreter`,
  builds `AgentInstructions.For(config)`, and `BuildAgent` prepends `Prefix(answerMode)`. Deleted the three
  preamble classes; docs updated (`h2c.md`, execution-flow, class reference); `CONTEXT.md` created with the
  communication glossary. New `AgentInstructionsTests` (5 cases). Full suite green (**326**: 318 Agora.Tests
  + 8 Agora.Api.Tests).
