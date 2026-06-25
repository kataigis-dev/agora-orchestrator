# 06 — Anthropic prompt caching end-to-end

Status: ready-for-human

**Addresses:** F9 (MEDIUM, already documented) · **Axes:** token · **Impact:** ★ · **Effort:** M
**Blocks:** 01 (the eval harness wants a native-Claude strong judge; see issue 01 decision #13)

## Problem

`CacheTranslation.MarkStable` tags Breakpoint-provider messages with `cache_control: {type: ephemeral}`, and both agent paths mark the system prompt stable. But `ChatClients.Build` (`src/Agora.AgentFramework/Providers/ChatClients.cs`) has **no `anthropic` branch** — it falls through to the OpenAI client, which never serializes `cache_control` to Anthropic's wire format. So the one provider that needs explicit breakpoints (Claude, the most cache-lucrative) gets **zero** caching. The marker + usage-accounting abstraction is in place; only the wire path is missing. Known and documented in `docs/providers.md`.

## Proposal

- Add a dedicated Anthropic `IChatClient` branch in `ChatClients.Build` that carries `cache_control` to the wire (per `docs/providers.md`'s noted prerequisite).
- Verify `cache_creation_input_tokens` / `cache_read_input_tokens` flow back through `UsageMapping` into `RunMetrics.CacheHitRate`.

## Acceptance criteria

- [x] An `anthropic`/`claude` provider routes through a client that emits `cache_control`.
- [ ] A real-or-recorded run shows `CacheHitRate > 0` for Anthropic with a stable system prefix. *(unit-proven via `ParseResponse`/`UsageMapping`; live confirmation pending a real Anthropic-key run.)*
- [x] `docs/providers.md` updated to drop the "not yet wired" caveat.

## Notes

Dependency decision (was `ready-for-human`): resolved by **hand-rolling** `AnthropicChatClient` over the Messages API rather than adding an SDK — zero new dependencies, matching the repo's existing custom-client precedent (`CopilotChatClient`). It coexists with the OpenAI-compatible path as a dedicated `switch` branch.

## Comments

- 2026-06-23 — **Implemented.** New `src/Agora.AgentFramework/Providers/AnthropicChatClient.cs` (custom
  `IChatClient`): lifts system messages to the top-level `system` field, serializes per-block
  `cache_control: {type: ephemeral}` for cache-marked messages, and maps `cache_creation_input_tokens` /
  `cache_read_input_tokens` into `UsageDetails` (InputTokenCount = total prompt so cache-read is a subset,
  matching OpenAI semantics). Wired `"anthropic" or "claude"` into `ChatClients.Build`; the existing
  `CacheTranslation` (Breakpoint) + `UsageMapping` already did their parts. Also sets the HTTP client
  timeout from the spec (pre-empts F13 on this path). `docs/providers.md` caveat replaced. 3 offline tests
  (`tests/Agora.Tests/AgentFramework/AnthropicChatClientTests.cs`) cover request serialization (system
  lift + cache_control) and usage parsing (CacheHitRate > 0); full suite green (314). Text completions
  only — tool/function calling on Anthropic still uses the OpenAI-compat path. **Unblocks issue 01's
  native-Claude judge** (`evals/judge.claude.yaml` added).
