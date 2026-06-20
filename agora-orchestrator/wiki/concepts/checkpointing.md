---
type: concept
title: Checkpointing & Resume
tags: [durability, checkpoint, resume, state, orchestration]
related: [agent-graph, parallel-execution, agora-cli, agora-orchestrator]
created: 2026-06-20
updated: 2026-06-20
---

# Checkpointing & Resume

Esecuzione **durevole**: lo `State` del grafo viene salvato a ogni step in uno store di
checkpoint pluggable, così un run interrotto (crash, kill) può essere **ripreso** da dove
si era fermato, senza rieseguire i nodi già completati. Vedi [[agent-graph]].

## Modello

- **`StateSnapshot`** — snapshot JSON-serializzabile dello `State` (nodo corrente, step,
  input, messaggi, output, artifact, signals, loop counter, last agent). I `signals`
  (mappa `string|bool`) fanno round-trip via JSON e vengono normalizzati al ricaricamento.
- **`ICheckpointStore`** — `Save(runId, snapshot)` / `Load(runId)`. Implementazioni:
  `InMemoryCheckpointStore` (test) e `FileCheckpointStore` (un JSON per run id).

## Semantica

Il checkpoint viene salvato **dopo** il completamento di ogni nodo (con `Current` = nodo
successivo). Quindi:

- I nodi completati vengono preservati (eseguiti **una volta**).
- Solo il nodo che stava per partire viene rieseguito al resume (at-least-once).
- Per il fan-out ([[parallel-execution]]) il checkpoint avviene dopo il join.

Al resume, `GraphExecutor.RunAsync(resumeFrom: snapshot)` ricostruisce lo `State`, riparte
dal nodo `Current` e **salta** il seed RAG (già presente nei messaggi salvati).

## CLI

```bash
# esegue salvando i checkpoint; stampa il run-id su stderr
agora run --config app.yaml --input "..." --graph --checkpoint ./checkpoints

# riprende il run interrotto
agora resume --config app.yaml --checkpoint ./checkpoints --run-id <id>
```

`Runtime.RunAsync(input, runId?)` genera un run id quando il checkpointing è attivo;
`Runtime.ResumeAsync(runId)` carica lo snapshot e continua.

## Limiti / note

- Se il crash avviene **durante** una chiamata al modello, quel nodo riparte da capo
  (la sua chiamata LLM viene rifatta) — l'esecuzione dei nodi non è transazionale.
- È la base per un **HITL durevole** (pausa per approvazione che sopravvive ai riavvii):
  estensione futura sopra a questo meccanismo. Vedi [[human-in-the-loop]].
