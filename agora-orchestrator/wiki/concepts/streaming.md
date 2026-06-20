---
type: concept
title: Token Streaming
tags: [streaming, tokens, provider, cli, ux]
related: [agora-orchestrator, agora-cli, agent-graph]
created: 2026-06-20
updated: 2026-06-20
---

# Token Streaming

Model output can be **streamed token-by-token** as it is generated, instead of waiting for the full
response — useful for responsiveness in CLI/API.

## Architecture

Streaming is an **optional capability** that does not change `IChatProvider`:

```csharp
public interface IStreamingChatProvider : IChatProvider
{
    Task<CompletionResult> StreamAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default);
}
```

Each chunk is delivered to `onChunk`; the full `CompletionResult` is still returned at the end, so
signal/artifact parsing is unchanged.

| Component | Streaming |
|-----------|-----------|
| `FakeChatProvider` | emits the response word-by-word (deterministic tests) |
| `AgentFrameworkChatProvider` | uses `IChatClient.GetStreamingResponseAsync` (OpenAI/compatible) |
| `ResilientChatProvider` | delegates to `_inner` if streaming-capable (retry around the stream) |

## Propagation

An `Action<string>? onChunk` sink is threaded down the stack:
`Runtime.RunAsync/RunAgentAsync` → `GraphExecutor` → `IAgent.RunAsync` → provider. The core `Agent`
uses streaming when the sink is present **and** the provider is `IStreamingChatProvider`; otherwise
it uses `CompleteAsync`.

## CLI

```bash
agora run --config app.yaml --input "..." --agent writer --stream
agora run --config app.yaml --input "..." --graph --stream
```

With `--stream` tokens are written to stdout as they arrive (the final output is not reprinted).

## Limits (v1)

- **Tool agents** (`AgentFrameworkAgent`, with the tool/approval loop) do not stream yet: they
  ignore the sink.
- In **parallel branches** ([[parallel-execution]]) streaming is disabled (to avoid interleaving);
  only the main graph path streams.
