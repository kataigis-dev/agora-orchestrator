# 07 — Synchronous conflict gate + always-present resolver; refuse non-interactive

Status: ready-for-human

**Decision:** D4, D6 · **Depends:** 06 · **Effort:** M

## Problem

The `rag_write` conflict path needs a human, but:

- the CLI must guarantee a resolver is always wired (`ConsoleConflictResolver`);
- a non-interactive context (no TTY: CI/CD, piped stdin) has no human → today the API path leaves
  `IConflictResolver` null and `EscalateAsync` silently `Rejected`s the write. The product is
  **CLI-only, human always present** (D6), so this state must be **refused**, not silently degraded.

## Proposal

- CLI: always construct the runtime with `ConsoleConflictResolver`; the `rag_write` tool blocks
  synchronously until the human answers (reuse the `ApprovalGate` pattern already used for tool
  approvals).
- At runtime/build, if any agent can reach `rag_write` and the process is **not interactive**
  (`Console.IsInputRedirected`/no TTY), **refuse to run** with a clear error directing the user to run
  interactively. Best-effort enforcement of "no CI/CD" (cannot be made cryptographically impossible).
- Headless without a resolver is no longer a normal path; the null-resolver `Rejected` branch becomes a
  defensive guard, not an expected outcome.

## Acceptance criteria

- [ ] A `rag_write` conflict blocks the run until the console resolver answers.
- [ ] Running a config where `rag_write` is reachable in a non-interactive process fails fast with a
      clear message.
- [ ] Interactive CLI runs resolve conflicts as before.

## Notes

Completes the fail-loud principle: when the required human can't be present, refuse rather than drop
data. Ties to issue 10 (retiring the headless REST surface).

## Comments

**2026-06-25 — implemented.** The CLI already wires `ConsoleConflictResolver` (`Program.cs`) and the
gate is synchronous (the resolver blocks on `ReadLine`). New: `CliRunner.Run` takes `bool? interactive`
(default `!Console.IsInputRedirected`, overridable for tests) and threads it into `ConfigState.Interactive`.
`ConfigState.RequireInteractiveForRagWrite(config)` throws a `ConfigException` when the process is
non-interactive and any agent allow-lists `rag_write`; the `run` and `resume` verbs call it right after
`BuildRuntime` (so it fails fast before any agent runs). `Execute` turns the throw into a clear
`ERROR: …` + exit 1. Headless-without-resolver stays a defensive `Rejected` branch in `KnowledgeBase`
(issue 06). Tests (guard tested directly via internal `ConfigState`, decoupled from the embedder build
path so it survives issue 08): `RagWriteInteractivityTests` (refused / allowed-when-interactive /
allowed-without-rag_write) and `ConsoleConflictResolverTests` (reads decision; Enter accepts the judge's
suggested merge; typed text overrides; suggestion shown). Suite green (353). Uncommitted.
