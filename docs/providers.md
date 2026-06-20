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

## Resilience

`RetryPolicy` wraps each call:

| Parameter (`defaults`) | Default | Description |
|---|---|---|
| `retries` | 2 | Max retries |
| `retry_base_delay` | 0.5 | Base delay (s) for exponential backoff |
| `timeout` | 120 | Per-call timeout (s) |
