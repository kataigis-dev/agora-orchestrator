# Chat providers

## IChatProvider

Defined in `src/Agora/Providers/IChatProvider.cs`:

```csharp
public interface IChatProvider
{
    Task<CompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, CancellationToken cancellationToken = default);
}
```

Streaming is an optional capability:

```csharp
public interface IStreamingChatProvider : IChatProvider
{
    Task<CompletionResult> StreamAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default);
}
```

## Supported providers

### OpenAI

```yaml
providers:
  openai:
    api_key_env: OPENAI_API_KEY
    base_url: https://api.openai.com/v1   # optional
```

Uses `Microsoft.Extensions.AI` over the OpenAI client. Supports streaming and tool calls.

### Ollama

```yaml
providers:
  ollama:
    base_url: http://localhost:11434
```

Local models (llama3.1, mistral, qwen2.5, …); no API key needed.

### Custom / OpenAI-compatible

```yaml
providers:
  llamastudio:
    base_url: http://127.0.0.1:1234/v1
```

Any OpenAI-compatible endpoint (LM Studio, vLLM, TGI, LocalAI, Azure OpenAI with an Azure base_url),
via the OpenAI client with a custom `Endpoint`.

### GitHub Copilot

```yaml
providers:
  github-copilot:
    api_key_env: GITHUB_COPILOT_OAUTH_TOKEN   # GitHub OAuth token; optional (see below)
models:
  copilot: { provider: github-copilot, model: gpt-4o }
```

Targets the Copilot editor endpoint (`https://api.githubcopilot.com`). It speaks the OpenAI
chat-completions format but needs the editor headers (`Editor-Version`, `Copilot-Integration-Id`)
and a short-lived **session token**. Agora handles that automatically: it takes a long-lived GitHub
**OAuth token** and exchanges it at `https://api.github.com/copilot_internal/v2/token`, caching the
session token and refreshing it before it expires (`CopilotChatClient` / `CopilotTokenProvider`).

The OAuth token is resolved in this order:

1. The provider's `api_key_env` (an env var holding the OAuth token), if set.
2. Otherwise your editor's Copilot sign-in file: `~/.config/github-copilot/apps.json` (or
   `hosts.json`) — i.e. if you're already signed in to Copilot in VS Code / Neovim, no config needed.

Requires an active GitHub Copilot subscription. Aliases: `github-copilot` and `copilot`.
Embeddings are not provided over this endpoint — use another provider for `rag`.

## Models

A model alias maps to a provider + concrete model:

```yaml
models:
  fast:     { provider: openai, model: gpt-4o-mini }
  balanced: { provider: openai, model: gpt-4o }
```

Generation parameters (temperature, max_tokens, timeout, retries) come from `defaults` (and the
per-agent `timeout` override), resolved into a `ModelSpec`.

## Resolution

`ModelResolver.Resolve(config)` builds a `ModelSpec` per alias: provider, model, the `defaults`
parameters, the API key (read from the provider's `api_key_env`) and base URL.
`Agora.AgentFramework.ChatClients.Build(spec)` then creates the concrete client. The provider is
wrapped in `ResilientChatProvider`.

## Prompt caching

Providers expose prompt caching through **different mechanisms**, so Agora abstracts the *intent*
rather than any one API. The core marks stable content; each provider adapter translates it.

**The hint.** `ChatMessage` carries a `CacheStable` flag. The agent runners mark the system prompt /
role instructions (stable across a run) as cacheable; the user turn is left volatile. The core stays
provider-agnostic — it never names a caching API.

**The mechanisms** (`Agora.Providers.PromptCaching.For(provider)`):

| Mode | Providers | How the hint is translated |
|---|---|---|
| `Implicit` | OpenAI, OpenAI-compatible, Ollama | No-op. Caching is automatic on a stable prefix; the lever is keeping that prefix first and byte-identical (which the runners already do). |
| `Breakpoint` | Anthropic | The adapter tags the message with `cache_control: { type: ephemeral }`. |
| `Resource` | Google Gemini | Reserved: manage a `CachedContent` handle for the stable segment (not yet implemented). |
| `None` | unknown / null | Ignored. |

**Measuring it.** Cache usage is reported back through `CompletionResult.CacheReadTokens` /
`CacheWriteTokens` (mapped from `UsageDetails.CachedInputTokenCount` and provider `AdditionalCounts`)
and aggregated into `RunMetrics.CacheHitRate` — so you can verify caching is actually taking effect.
See [observability.md](observability.md).

> **Prerequisite for native Anthropic caching.** `ChatClients.Build` currently routes `anthropic`
> through the OpenAI-compatible path, which does **not** carry `cache_control` to the wire. The
> abstraction (marker + translation + usage accounting) is in place, but emitting the breakpoint
> end-to-end needs a dedicated Anthropic `IChatClient` branch. Implicit caching (OpenAI/local) and
> the usage accounting work today.

## Resilience

`RetryPolicy` wraps each call:

| Parameter (`defaults`) | Default | Description |
|---|---|---|
| `retries` | 2 | Max retries |
| `retry_base_delay` | 0.5 | Base delay (s) for exponential backoff |
| `timeout` | 120 | Per-call timeout (s) |
