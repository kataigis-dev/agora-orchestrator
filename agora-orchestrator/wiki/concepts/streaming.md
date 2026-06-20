---
type: concept
title: Token Streaming
tags: [streaming, tokens, provider, cli, ux]
related: [agora-orchestrator, agora-cli, agent-graph]
created: 2026-06-20
updated: 2026-06-20
---

# Token Streaming

Gli output del modello possono essere **trasmessi token-per-token** mentre vengono
generati, invece di attendere la risposta completa — utile per la reattività in CLI/API.

## Architettura

Lo streaming è una **capacità opzionale** che non cambia `IChatProvider`:

```csharp
public interface IStreamingChatProvider : IChatProvider
{
    Task<CompletionResult> StreamAsync(
        IReadOnlyList<ChatMessage> messages, ModelSpec spec, Action<string> onChunk,
        CancellationToken cancellationToken = default);
}
```

Ogni chunk arriva a `onChunk`; alla fine si ottiene comunque il `CompletionResult`
completo, quindi il parsing di signal/artifact resta invariato.

| Componente | Streaming |
|------------|-----------|
| `FakeChatProvider` | emette la risposta parola-per-parola (test deterministici) |
| `AgentFrameworkChatProvider` | usa `IChatClient.GetStreamingResponseAsync` (OpenAI/compatibili) |
| `ResilientChatProvider` | delega allo `_inner` se è streaming-capable (retry attorno allo stream) |

## Propagazione

Un sink `Action<string>? onChunk` viene passato lungo lo stack:
`Runtime.RunAsync/RunAgentAsync` → `GraphExecutor` → `IAgent.RunAsync` → provider.
L'`Agent` core usa lo streaming quando il sink è presente **e** il provider è
`IStreamingChatProvider`; altrimenti usa `CompleteAsync`.

## CLI

```bash
agora run --config app.yaml --input "..." --agent writer --stream
agora run --config app.yaml --input "..." --graph --stream
```

Con `--stream` i token vengono scritti su stdout man mano che arrivano (l'output finale
non viene ristampato).

## Limiti (v1)

- I **tool agent** (`AgentFrameworkAgent`, con loop tool/approval) non fanno ancora
  streaming: ignorano il sink.
- Nei **branch paralleli** ([[parallel-execution]]) lo streaming è disattivato (evita
  l'interleaving); fa streaming solo il percorso principale del grafo.
