---
type: concept
title: Handoff Context (minimal inter-agent context)
tags: [graph, handoff, context, orchestration, artifacts]
related: [agent-graph, communication-modes, shared-knowledge-base, signal, agora-orchestrator]
created: 2026-06-19
updated: 2026-06-19
---

# Handoff Context

Modalità opzionale di passaggio del contesto tra agenti: invece di consegnare al
nodo successivo **l'intero output** del precedente, si passa **solo il contesto
necessario** che il mittente dichiara esplicitamente.

> **Stato:** implementata. Il fallback "il ricevente cerca nel RAG prima di chiedere
> all'agente di partenza" è disponibile tramite i tool `rag_search`/`ask_agent` —
> vedi [[shared-knowledge-base]].

## Attivazione

Opt-in a livello globale; di default resta il comportamento attuale (output completo):

```yaml
handoff: true   # default: false
```

## Comportamento

Su un hop `current → next` ([[agent-graph]]):

| `handoff` | Cosa riceve il nodo successivo nell'inbox |
|-----------|-------------------------------------------|
| `false` (default) | L'**output completo** del mittente |
| `true` | **Solo** l'artifact `handoff` del mittente; se assente, **niente** |

In modalità handoff l'artifact `handoff` è un **canale mirato** verso il prossimo
agente: non viene aggiunto al sommario globale degli artifact condivisi
(`State.ArtifactSummary`), per non duplicarlo né allargarne la visibilità.

Se il mittente non dichiara un handoff, il ricevente parte senza contesto in inbox
(riceve comunque `UserInput` e gli artifact condivisi) — in Fase 3 ricorrerà al RAG.

## Come si dichiara l'handoff

| Modalità ([[communication-modes]]) | Sintassi |
|------------------------------------|----------|
| `natural` | `<<artifact handoff=...>>` (token già supportato da [[signal]]) |
| `h2c` | un campo `handoff:<testo>` dentro un blocco, es. `[CTX:UPDATE]`\n`handoff:auth usa JWT` |

Quando `handoff: true`, a ogni agente viene iniettato un breve preambolo
(`HandoffPreamble`) che lo istruisce a emettere il payload di handoff, perché il
successivo **non vede** il suo output completo.

## Implementazione

| Componente | Ruolo |
|------------|-------|
| `AgoraConfig.Handoff` | Flag di configurazione (`bool?`) |
| `GraphExecutor` (param `handoff`) | Passa il payload di handoff o niente; esclude `handoff` dagli artifact globali |
| `H2cInterpreter` | Estrae il campo `handoff` come artifact in modalità h2c |
| `Communication/HandoffPreamble` | Istruzione iniettata quando la modalità è attiva |

## Esempio

`examples/agora-handoff.yaml` — pipeline natural con `handoff: true`.

## Note

- Modalità ortogonale a `communication`: funziona sia con `h2c` sia con `natural`.
- Pensata per ridurre il "context bloat" e i token passati a valle; il prezzo è che
  gli agenti devono dichiarare cosa condividere (o appoggiarsi al RAG condiviso).
