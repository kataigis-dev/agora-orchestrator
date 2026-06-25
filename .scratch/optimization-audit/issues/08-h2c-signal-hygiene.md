# 08 — H2C signal hygiene (scope routing signals)

Status: ready-for-agent

**Addresses:** F12 (MEDIUM) · **Axes:** precision (routing correctness), robustness · **Impact:** ★★ · **Effort:** S–M

## Problem

`H2cInterpreter` maps **every** field (`key:value`) and every parsed block subtype to a routing signal. Observed live (round C) with weak models: stray well-formed blocks leaked signals `build` and `fix` from nodes that should not emit them, and a metadata field produced a signal whose key was a whole sentence — *"The implementation meets all requirements"*. Because the stray blocks are well-formed, "skip malformed blocks" does not catch them. If a leaked field value ever collides with an edge's `when:` label, the graph routes accidentally — a correctness/robustness risk on exactly the weak models H2C targets.

## Proposal

- Separate **routing signals** (block subtypes / an explicit signal channel) from **data fields** (artifacts/metadata) so arbitrary `key:value` pairs do not become routable signals.
- Optionally restrict routing to a node's *declared* outgoing `when:` labels, ignoring unrelated emitted signals.
- Keep field data available as artifacts/context (don't lose it — just stop treating it as control flow).

## Acceptance criteria

- [ ] Arbitrary metadata fields no longer become routing signals (test with a block carrying noise fields).
- [ ] Routing considers only signals relevant to the node's outgoing edges.
- [ ] Existing H2C routing tests still pass.

## Notes

Surfaced empirically in round C (`roundc/results.md`). Reference: H2C `H2cInterpreter`/`SignalParser` in `src/Agora/Communication/`.

## Comments
