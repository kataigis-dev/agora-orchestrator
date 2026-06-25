# 09 — Propagate configured timeout to the HTTP client

Status: ready-for-human

**Addresses:** F13 (LOW) · **Axes:** token (waste), robustness · **Impact:** ★ · **Effort:** S

## Problem

`defaults.timeout` (e.g. 180s) is enforced by `RetryPolicy`, but the underlying OpenAI client has its own default `NetworkTimeout` (~100s). On a slow/large generation (observed live with a local model in round C) the client times out at 100s **before** Agora's 180s, and `RetryPolicy` retries into the same wall — burning 2× partial generations. The configured `timeout` lever is therefore ineffective on the actual network call.

## Proposal

- Pass the resolved per-call timeout into the client (`ClientPipelineOptions.NetworkTimeout`, or the equivalent on the Ollama/Copilot paths) when building the chat client, so the client and `RetryPolicy` agree.
- Consider not retrying on a *timeout* (vs a transient network error) for very long generations, to avoid duplicated cost.

## Acceptance criteria

- [x] A configured `timeout` greater than the client default actually allows the call to run that long.
- [ ] A timed-out long generation is not blindly retried into the same wall (or the retry budget accounts for it). *(still open — retry-on-timeout semantics not changed.)*

## Notes

`ready-for-human`: touches client construction across providers and the retry policy — worth a maintainer decision on retry-on-timeout semantics. Surfaced in `roundc/results.md`.

## Comments

- 2026-06-23 — **Timeout propagation fixed** on both client paths: `ChatClients.BuildOpenAI` now sets
  `OpenAIClientOptions.NetworkTimeout` from `spec.Timeout` (OpenAI + all OpenAI-compatible endpoints incl.
  LM Studio), and the new `AnthropicChatClient` sets `HttpClient.Timeout` from the spec. Confirmed live in
  round D: the 3-node graph case that previously died at the ~100s default now completes under the
  configured `timeout: 180` (`roundd-eval-harness.md`). **Still open:** the retry-on-timeout decision —
  `RetryPolicy` currently re-issues a timed-out long generation; whether to skip retry on a genuine timeout
  (vs a transient error) is the remaining maintainer call, so this stays partially done.
