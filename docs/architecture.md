# Architettura

## Panoramica

Agora Orchestrator è progettato con un'architettura a strati:

```
┌─────────────────────┐
│  CLI / API          │  Interfacce utente
├─────────────────────┤
│  GraphExecutor      │  Motore di orchestrazione
├─────────────────────┤
│  Agent / AgentFrameworkAgent  │  Esecutori agente
├─────────────────────┤
│  ChatProvider       │  Provider (OpenAI, Ollama, custom)
│  McpClient          │  Strumenti esterni
│  RagPipeline        │  Ricerca vettoriale
├─────────────────────┤
│  Core (Agora)       │  Modelli, interfacce, resilience
└─────────────────────┘
```

## Motore del grafo (GraphExecutor)

`GraphExecutor` in `src/Agora/Orchestration/GraphExecutor.cs` è il cuore del sistema. Esegue un grafo diretto di agenti:

1. Parte dal nodo `entry` specificato nel config
2. Per ogni nodo, esegue l'agente associato e passa il contesto
3. Determina il prossimo nodo in base al tipo di edge
4. Continua fino a raggiungere `END` o il massimo di loop

### Tipi di edge

| Tipo | Comportamento |
|---|---|
| `sequential` | Passa al prossimo nodo in ordine di definizione |
| `handoff` | Passa il controllo al nodo specificato in `to` |
| `conditional` | Decide il prossimo nodo in base al segnale ricevuto |

### Ciclo di vita di un agente in un grafo

1. Riceve l'input (messaggi precedenti + contesto)
2. Elabora con il modello configurato
3. Può chiamare tools MCP o skills
4. Produce un messaggio di output
5. Se previsto, emette un segnale di routing

## Comunicazione

### H2C (Human-to-Computer)

Protocollo formale con blocchi strutturati:

```
[H2C:TYPE:SUBTYPE]
... contenuto ...
[/H2C]
```

I comandi speciali per il grafo usano `[STATE:DONE]`, `[STATE:FIX]`, `[STATE:APPROVE]`, `[STATE:DENY]`.

### Natural

Gli agenti comunicano in linguaggio naturale e usano segnali `<<signal done>>` per il routing nel grafo.

## Condivisione contesto con Artifacts

Gli agenti possono condividere dati strutturati attraverso **Artifacts**, un dizionario chiave-valore persistente in `State.Artifacts`.

### Scrittura

Un agente scrive un artifact nel suo output:

```
Il progetto usa <<artifact language=C#>> e <<artifact framework=net10>>
```

Il token `<<artifact key=value>>` viene parsato da `SignalParser`, rimosso dall'output visibile, e la coppia chiave-valore viene aggiunta a `State.Artifacts`.

### Lettura

Prima di ogni esecuzione, `GraphExecutor` serializza gli artifacts correnti nel contesto passato all'agente:

```
━━━ Shared Artifacts ━━━
  language: C#
  framework: net10

(messaggi inbox...)
```

### Visibilità

- Gli artifacts sono visibili a **tutti** gli agenti del grafo (non filtrati per destinatario)
- Ogni agente può leggere e sovrascrivere qualsiasi chiave
- Gli artifacts persistono fino al termine dell'esecuzione del grafo
- Utili per decisioni condivise: naming convention, linguaggio, stile, path di output

## Provider e modelli

La risoluzione segue questa gerarchia:

1. Config specifica `providers` e `models`
2. Ogni agente referenzia un modello con `model: <id>`
3. Il modello referenzia un provider con `provider: <id>`
4. Al runtime, `ChatClientFactory` risolve la catena e crea il `ChatProvider` appropriato

## Resilience

`RetryPolicy` e `CircuitBreaker` in `src/Agora/Resilience/` gestiscono fallimenti delle chiamate API:

- Retry con backoff esponenziale (default: 3 tentativi)
- Circuit breaker (default: 5 fallimenti in 30s)
- Timeout configurabile per agente
