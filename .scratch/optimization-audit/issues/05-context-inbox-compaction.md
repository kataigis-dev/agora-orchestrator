# 05 — Context & inbox compaction

Status: ready-for-agent

**Addresses:** F7, F8 (MEDIUM) · **Axes:** token · **Impact:** ★★ · **Effort:** M

## Problem

Two unbounded-context paths inflate tokens on long/looping runs:

- **F7 — inbox.** `State.Inbox(agentId)` concatenates *every* message ever addressed to a node. On a conditional loop (`reviewer↔generator`, `max_loops:5`) each pass appends another full output → monotonic growth. `GraphExecutor.BuildContextAsync` only swaps `ArtifactSummary()` for top-K recall, so context memory does **not** compress the inbox. H2C defines `COMPACT`/`PRUNE` subtypes that nothing acts on.
- **F8 — artifacts.** With `memory` off (the default), `State.ArtifactSummary()` dumps *all* artifacts into every node's context, growing with run length. The compression lever exists but is opt-in and used in one example.

## Proposal

- Bound/compact the inbox: keep last-N or summarize older entries on loops; optionally honor H2C `COMPACT`/`PRUNE`.
- Apply a context budget (chars/tokens) to the assembled node context, not just to memory recall.
- Consider making `memory` (or a default trimming policy) the out-of-the-box behavior so the default path isn't the un-optimized one.

## Acceptance criteria

- [ ] Inbox/context size is bounded across loop iterations (test on a `max_loops` graph asserts non-monotonic growth).
- [ ] A context budget caps assembled node context regardless of memory mode.
- [ ] Token reduction on a looping graph is demonstrated via `RunMetrics` before/after.

## Notes

LangGraph's `trim_messages`/state-reducer helpers are the reference. Reliability guardrail: trimming must not drop content the next agent provably needs — pair any aggressive trimming with the issue-01 quality eval to catch precision regressions.

## Comments
