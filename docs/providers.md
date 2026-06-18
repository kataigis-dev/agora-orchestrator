# Provider Chat

## Interfaccia IChatProvider

Definita in `src/Agora/Providers/IChatProvider.cs`:

```csharp
public interface IChatProvider
{
    IAsyncEnumerable<ChatMessage> GetResponseAsync(
        ChatRequest request,
        CancellationToken ct = default);
}
```

## Provider supportati

### OpenAI

```yaml
providers:
  openai:
    api_key_env: OPENAI_API_KEY
    base_url: https://api.openai.com/v1   # opzionale, default
```

Usa `Microsoft.Extensions.AI.OpenAI` con `OpenAIClient`. Supporta:
- `gpt-4o`, `gpt-4o-mini`, `gpt-4-turbo`, `o1`, `o3-mini`
- Streaming delle risposte
- Tool calls nativi

### Ollama

```yaml
providers:
  ollama:
    base_url: http://localhost:11434
    api_key_env: ~
```

Usa `Microsoft.Extensions.AI.Ollama`. Supporta:
- Modelli locali (llama3.1, mistral, phi4, qwen2.5)
- API compatibile OpenAI
- Nessuna API key necessaria

### Custom / OpenAI-compatible

```yaml
providers:
  llamastudio:
    base_url: http://127.0.0.1:1234/v1
    api_key_env: ~
```

Usa `OpenAIClient` con `ApiKeyCredential` e `OpenAIClientOptions.Endpoint` custom.

Compatibile con:
- **llama studio** / LM Studio
- **vLLM**
- **TGI** (Text Generation Inference)
- **LocalAI**
- **Azure OpenAI** (con base_url Azure)

## Configurazione modello

```yaml
models:
  fast:
    provider: openai
    model: gpt-4o-mini
    temperature: 0.3
    max_tokens: 4096
    top_p: 0.9
    frequency_penalty: 0
    presence_penalty: 0
    stop: ["```"]              # Token di stop opzionali
```

## Meccanismo di risoluzione

`ChatClientFactory` in `src/Agora.AgentFramework/`:

1. Riceve `ModelSpec` (model id + optional overrides)
2. Cerca il modello in `models:` del config
3. Risolve il provider referenziato
4. Crea il `ChatProvider` appropriato
5. Applica parametri (temperature, max_tokens, etc.)
6. Applica resilience (retry, circuit breaker)
7. Restituisce `IChatProvider` pronto all'uso

## Resilience

| Parametro | Default | Descrizione |
|---|---|---|
| `max_retries` | 3 | Tentativi massimi |
| `retry_delay_ms` | 1000 | Ritardo base tra tentativi |
| `circuit_breaker_threshold` | 5 | Fallimenti prima di aprire il circuito |
| `circuit_breaker_duration_s` | 30 | Durata apertura circuito |
| `timeout_s` | 120 | Timeout richiesta |
