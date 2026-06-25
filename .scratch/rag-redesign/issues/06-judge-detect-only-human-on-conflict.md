# 06 — Judge detects+proposes; remove auto-apply; human on every conflict

Status: ready-for-human

**Decision:** D3 · **Depends:** 05 · **Effort:** M

## Problem

`KnowledgeBase.WriteAsync` applies a `Resolved` verdict **autonomously**: it deletes the conflicting
entries and stores the LLM's reconciled text with no human (`WriteOutcome.AutoResolved`). The LLM thus
has delete/replace authority over curated knowledge — the opposite of the agreed policy.

## Proposal

- Collapse the `Resolved` branch into the human path: on **any** conflict verdict
  (`Resolved` or `Unresolved`), route to the `IConflictResolver`.
- Pass the judge's proposed merge text to the resolver as the **default suggestion** for the `Merge`
  option (so the LLM stays useful as a suggester, not a decider).
- Remove the `AutoResolved` outcome (or repurpose it to mean "human accepted the suggested merge").
- `NoConflict` (confident) still admits without a human; a clean add (no neighbours / below
  threshold) is unchanged.

## Acceptance criteria

- [ ] No KB entry is ever deleted or replaced without a human decision.
- [ ] A `Resolved` verdict reaches the resolver with the proposed merge pre-filled.
- [ ] `NoConflict` and plain adds still proceed without a human (no regression).
- [ ] Tests cover: resolved→human(merge suggested), unresolved→human, no-conflict→auto-add.

## Notes

Behavioural change to the KB write gate; depends on a resolver always being present, wired in
issue 07.

## Comments

**2026-06-25 — implemented.** `KnowledgeBase.WriteAsync` no longer has a `Resolved` auto-apply branch:
both `Resolved` and `Unresolved` fall through to `EscalateAsync` (human). `WriteOutcome.AutoResolved` is
removed. `EscalateAsync` now presents the human only the judge-named candidates (`assessment.Conflicting`,
not the broad `neighbors`), and passes the judge's reconciliation as `ConflictResolutionRequest.SuggestedMerge`
(new field); on `Merge` it defaults to that suggestion via `FirstNonBlank` when the human supplies none.
With no resolver, a conflict is `Rejected` (defensive) — never auto-applied. `ConsoleConflictResolver`
prints the suggested merge and accepts it on an empty line. `NoConflict` and plain adds are unchanged.
Tests: replaced `Resolved_ReplacesConflictWithMergedText` with
`Resolved_RoutesToHuman_WithSuggestedMergePrefilled`; added `Resolved_NoResolver_Rejected_NothingDeleted`;
existing unresolved/efficiency tests unchanged. `RagTools` only stringifies the outcome, so dropping
`AutoResolved` is safe. Suite green (344). Uncommitted.
