# Grafo

## Concetti base

Un grafo definisce il flusso di esecuzione tra agenti. È dichiarato nella sezione `graph` del config YAML:

```yaml
graph:
  entry: generator
  edges:
    - { from: generator, to: reviewer, type: handoff }
    - { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END, type: conditional, when: done }
```

## Nodi

Ogni nodo del grafo corrisponde a un agente definito nella sezione `agents:`. Il nodo speciale `END` termina l'esecuzione.

## Archi (edges)

### sequential

Esegue i nodi nell'ordine in cui sono definiti:

```yaml
edges:
  - { from: planner, to: executor, type: sequential }
```

### handoff

Passa il controllo a un nodo specifico, portando il contesto:

```yaml
edges:
  - { from: generator, to: reviewer, type: handoff }
```

L'agente ricevente vede la cronologia della conversazione.

### conditional

Decide dinamicamente il prossimo nodo in base al segnale dell'agente:

```yaml
edges:
  - { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
  - { from: reviewer, to: END, type: conditional, when: done }
```

- `when` — il valore del segnale che attiva questo edge
- `max_loops` — numero massimo di iterazioni (previene loop infiniti)

## Segnali

Gli agenti comunicano il routing tramite segnali.

### In modalità Natural

```
<<signal done>>
<<signal fix>>
<<signal approve>>
```

### In modalità H2C

```
[STATE:DONE]
[STATE:FIX]
[STATE:APPROVE]
```

## Esecuzione

### GraphExecutor.RunAsync()

1. Legge il nodo `entry` dal config
2. Prepara `GraphState` con messaggi iniziali
3. Loop:
   a. Esegue l'agente corrente con `ExecuteAgentAsync()`
   b. Analizza la risposta per estrarre il segnale
   c. Determina il prossimo nodo in base agli edges
   d. Se `END`, termina
   e. Se conditional senza match, va al nodo successivo
4. Restituisce `GraphResult` con tutti i messaggi prodotti

### Visualizzazione real-time (CLI)

Con `--graph` il CLI mostra:

```
━━━ Agent Graph Execution ━━━
  Graph: generator ──handoff→ reviewer ──conditional→ generator ──conditional→ END
  ▶ [1] Agent: generator
  └─ input: Crea una todo app API
  └─ signals: none
  ──▶ handoff → reviewer
  ▶ [2] Agent: reviewer
  └─ input: Crea una todo app API
  └─ signals: fix
  └─ artifact language: C#            (se l'agente ha scritto <<artifact language=C#>>)
  ──▶ condition:fix (1/5) → generator
━━━ Execution Complete ━━━
```

## Artifacts condivisi

Oltre ai messaggi, gli agenti condividono dati strutturati via `State.Artifacts` (dizionario `Dictionary<string, string>`).

- Un agente scrive un artifact con `<<artifact key=value>>` nel suo output
- `GraphExecutor` lo estrae, lo rimuove dall'output visibile, e lo aggiunge a `State.Artifacts`
- Prima di ogni esecuzione, tutti gli artifacts vengono serializzati nel contesto dell'agente
- Ogni agente può leggere e sovrascrivere qualsiasi chiave

Esempio — il primo agente imposta:

```
Il progetto sarà in <<artifact language=C#>>
```

Il secondo agente riceve nel contesto:

```
━━━ Shared Artifacts ━━━
  language: C#

(messaggi precedenti...)
```

## Stato del grafo

`State` (blackboard) mantiene:
- `Messages` — tutti i messaggi scambiati (filtrati per destinatario via `Inbox()`)
- `Outputs` — ultimo output per ogni agente (sovrascritto)
- `Signals` — segnali di routing dall'ultimo agente
- `LoopCounters` — contatori per ogni edge condizionale
- `Artifacts` — dati strutturati condivisi tra tutti gli agenti
- `LastAgent` — ID dell'ultimo agente eseguito
