# 11 — Per-provider cache adapter (one seam for prompt-cache mark + usage)

Status: ready-for-agent

**Addresses:** architecture review candidate 2 · **Axes:** token (cost visibility/correctness), locality · **Impact:** ★★ · **Effort:** S–M

## Problem

Prompt-caching is spread across the provider stack with poor locality. The genuinely thin/smeared part
is the cross-class handshake: `CacheTranslation.MarkStable` tags `cache_control` onto a Microsoft.Extensions.AI
message's `AdditionalProperties`, and `AnthropicChatClient.BuildRequestJson` reads that exact key back to
serialize the wire — `CacheStable (bool)` laundered through MEAI `AdditionalProperties` purely to reach the
hand-rolled client. The provider→mechanism policy (`PromptCaching`/`CachingMode`) and the usage
normalisation (`UsageMapping`) float **outside** the providers, so "how does provider X cache?" is answered
in several files. `CachingMode.Resource` is even declared but implemented nowhere (a dead path).

(Honest scope note: `ChatMessage.CacheStable` is just a flag, and `PromptCaching`/`UsageMapping` are small
deep modules on their own. The win here is *locality* — one place per provider — not deleting a god-object.)

## Design (locked via `/grilling`, 2026-06-23)

| # | Decision | Choice |
|---|----------|--------|
| 1 | **Seam shape** | **New `ICacheAdapter` interface** (option A): `void Mark(MEAI.ChatMessage)` + `(int Input,int Output,int CacheRead,int CacheWrite) MapUsage(UsageDetails?)`. Groups marking + usage normalisation per provider behind one seam. Two real adapters justify it (breakpoint vs implicit). The wire serialize/parse stays in `AnthropicChatClient` (accepted trade-off of A); the `AdditionalProperties` handshake therefore remains, relocated from `CacheTranslation` into `AnthropicCacheAdapter.Mark`. |
| 2 | **Adapters** | `AnthropicCacheAdapter` (breakpoint): `Mark` sets `cache_control:{type:ephemeral}`; `MapUsage` reads the common fields + the `cache_creation_input_tokens` write key. `ImplicitCacheAdapter` (default/none): `Mark` is a no-op; `MapUsage` reads the common fields only. |
| 3 | **Resolver / enum fate** | **Delete `CachingMode` + `PromptCaching` (core) and `CacheTranslation` (framework)** (option β). A single framework resolver `CacheAdapters.For(ModelSpec) → ICacheAdapter` (`anthropic`/`claude` → Anthropic, default → Implicit) is the one place mapping provider → caching behaviour. `ChatMessage.CacheStable` **stays in core** (the `Agent` marks the system prompt without knowing the provider). |
| 4 | **UsageMapping fate** | **Delete `UsageMapping`.** Its single-usage mapping moves into the adapters; its multi-message aggregation (tool path, `AgentFrameworkAgent`) becomes a small local sum over `adapter.MapUsage` (resolving `CacheAdapters.For(spec)` there too). |

## Flow (after)

`Agent` marks the system prompt `CacheStable` (unchanged) → `AgentFrameworkChatProvider` resolves
`CacheAdapters.For(spec)` once, calls `adapter.Mark(msg)` while mapping messages and
`adapter.MapUsage(response.Usage)` for the result → for Anthropic, `AnthropicChatClient` still owns the wire
(reads the `cache_control` marker, parses `cache_*_input_tokens`). `RunMetrics.CacheHitRate` is unchanged.

## Acceptance criteria

- [x] `ICacheAdapter` exists with `AnthropicCacheAdapter` + `ImplicitCacheAdapter`; `CacheAdapters.For` resolves by provider.
- [x] `CachingMode`, `PromptCaching`, `CacheTranslation`, `UsageMapping` (and `PromptCachingTests`) are removed; no dangling references (incl. doc `<see cref>`s and the prose docs).
- [x] Anthropic still serialises `cache_control` and maps `cache_read`/`cache_creation` tokens (existing `AnthropicChatClientTests` green, retargeted to the adapter).
- [x] Tool-agent usage aggregation still works (no regression on the `AgentFrameworkAgent` path — adapter resolved once, used for both mark and the per-message usage sum).
- [x] New adapter tests cover Mark (breakpoint vs no-op), MapUsage (both), and the resolver; full suite green.

## Notes

Candidate 2 from the architecture review. The chosen path keeps the existing per-provider `IChatClient`
boundary (Anthropic hand-rolled, OpenAI via MEAI) and adds the `ICacheAdapter` seam over it rather than
restructuring provider dispatch (the rejected, more invasive native-`AnthropicChatProvider` option C).

## Comments

- 2026-06-23 — Design locked via `/grilling` as a side effect of `/improve-codebase-architecture`
  (3 decisions). Chosen: `ICacheAdapter` (A) + single `CacheAdapters.For` resolver with the core enum
  deleted (β). Honest caveat recorded: `MapUsage` varies little across providers, so the adapters lean on
  `Mark` for their real depth.
- 2026-06-23 — **Implemented.** New `CacheAdapters.cs` (`ICacheAdapter` + `AnthropicCacheAdapter` +
  `ImplicitCacheAdapter` + `CacheAdapters.For`). `AgentFrameworkChatProvider` and `AgentFrameworkAgent`
  resolve the adapter once and use it for both marking and usage (the tool path sums `MapUsage` per
  message). Deleted `CacheTranslation`, `UsageMapping`, `PromptCaching`/`CachingMode`, and
  `PromptCachingTests`; `AnthropicChatClientTests` retargeted to `AnthropicCacheAdapter.MapUsage`; docs
  updated (`providers.md`, class reference, execution-flow). New `CacheAdaptersTests` (7 cases). Full suite
  green (**321**: 313 Agora.Tests + 8 Agora.Api.Tests).
