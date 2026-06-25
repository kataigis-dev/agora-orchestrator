# 14 — Extract `SpecStoreFactory` from Runtime (resolver-seam consistency)

Status: ready-for-agent

**Addresses:** architecture review candidate 5 · **Axes:** locality, consistency · **Effort:** S

## Problem

The core→backend resolver seam is **already unified**: `IAgentBackend` is the single interface, with three
`TryCreate*` methods (default-null = "the core handles this type") and one production adapter
(`AgentFrameworkBackend`) plus the test default. The hardcoded core-type lists (`fake`/`memory`/`file`) are
not really "smeared" — the core must know which types it can build itself to decide whether to delegate.

The genuine friction is narrower: the core-side dispatch is **inconsistent by location**.
`RagFactory.BuildEmbedder`/`BuildStore` live in a dedicated core factory, but the equivalent spec-store
dispatch lived **inside `Runtime`** (`BuildSpecStore`), so `Runtime` owned resource construction the other
two resources delegated away.

## Design (locked via `/grilling`, 2026-06-24)

| # | Decision | Choice |
|---|----------|--------|
| 1 | **Scope** | **Surgical**: extract `Runtime.BuildSpecStore` into a core `SpecStoreFactory.Build(config, configDir, backend)`, parallel to `RagFactory`. All config-driven resource construction now lives in dedicated core factories that delegate non-core types outward; `Runtime` owns none. (Rejected: a central registry / generic `Resolve<T>` mechanism — disproportionate for 4 non-core types, and the per-resource pre-processing (key/path/server) still differs, so it wouldn't collapse. The seam is already unified, so no `IAgentBackend` change.) |
| 2 | **No behaviour change** | The dispatch body is moved verbatim (`_config`→`config`, `_configDir`→`configDir`, `_backend`→`backend`). |

## Acceptance criteria

- [x] `SpecStoreFactory.Build(config, configDir, backend)` exists in core (`Agora.Specs.Concretes`); `Runtime` calls it and no longer defines `BuildSpecStore`.
- [x] `IAgentBackend` and the three `TryCreate*` resolvers are unchanged.
- [x] Behaviour preserved (file store core-built; non-file delegates to the backend; same error messages).
- [x] Focused `SpecStoreFactoryTests` added; existing `RuntimeTraceabilityTests` (spec-via-Runtime) still green.

## Notes

Candidate 5 (marked *Worth exploring*). On inspection it was largely already solved; the one real win is
co-locating spec-store dispatch with the other resource factories, which also slims `Runtime` ahead of
candidate 6 (`AgentFactory`). Explicitly avoided the registry over-engineering the explore pass floated.

## Comments

- 2026-06-24 — Design locked via `/grilling` under `/improve-codebase-architecture`, then **implemented**.
  New `src/Agora/Specs/Concretes/SpecStoreFactory.cs`; `Runtime` ctor calls `SpecStoreFactory.Build(...)`
  and the `BuildSpecStore` method is gone. Docs updated (execution-flow table, class reference). New
  `SpecStoreFactoryTests` (3 cases). Full suite green (**331**: 323 Agora.Tests + 8 Agora.Api.Tests).
