---
type: concept
title: Parallel Execution (fork/join)
tags: [graph, parallel, fan-out, join, concurrency, orchestration]
related: [agent-graph, edge-types, agora-orchestrator]
created: 2026-06-20
updated: 2026-06-20
---

# Parallel Execution (fork/join)

Esecuzione **concorrente** di più agenti indipendenti tramite un pattern fork→join:
un nodo *fork* dirama su N branch che girano in parallelo, e i loro output confluiscono
in un unico nodo *join* (es. un sintetizzatore). Riduce la latenza quando i branch non
dipendono l'uno dall'altro. Vedi [[agent-graph]] e [[edge-types]].

## Configurazione

Gli edge in uscita dal fork hanno `type: parallel`; i branch convergono su un solo join:

```yaml
graph:
  entry: planner
  edges:
    - { from: planner, to: research_web, type: parallel }
    - { from: planner, to: research_docs, type: parallel }
    - { from: research_web,  to: synthesizer, type: sequential }
    - { from: research_docs, to: synthesizer, type: sequential }
    - { from: synthesizer, to: END, type: sequential }
```

## Semantica

1. Il **fork** esegue normalmente; il suo output viene consegnato all'inbox di **ogni** branch.
2. I **branch** vengono eseguiti **concorrentemente** (`Task.WhenAll`): la concorrenza è
   nelle chiamate al modello (la parte costosa); lo `State` viene mutato **serialmente**
   dopo che tutti hanno finito → nessuna race.
3. Il **join** è l'unico nodo su cui convergono i branch; riceve nell'inbox gli output di
   **tutti** i branch e prosegue il flusso normale. Se i branch convergono su nodi diversi
   è un errore (`GraphError`).
4. Branch che vanno a `END` → nessun join (la pipeline termina dopo i branch).

## Vincoli (v1)

- I branch sono **singoli agenti** (nessun fan-out annidato dentro un branch).
- Tutti i branch di un fork devono convergere su **un solo** nodo join.

## Implementazione

`GraphExecutor.FanOutAsync`: aggiunge i messaggi fork→branch, esegue i branch con
`Task.WhenAll`, calcola il join (target comune dei branch), poi registra gli output e i
messaggi branch→join in modo seriale. I percorsi non-parallel restano invariati.

## Esempio

`examples/agora-parallel.yaml`.

## Note

- Ogni agente usa il proprio chat client/provider, quindi le chiamate concorrenti sono
  indipendenti; lo store/memory durante il fan-out è solo letto (concorrenza sicura).
