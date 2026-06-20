---
type: entity
title: Agora CLI
tags: [cli, tool, dotnet]
related: [agora-orchestrator, agora-api, guided-config]
created: 2026-06-17
updated: 2026-06-19
---

# Agora CLI

Eseguibile CLI (`Agora.Cli`) che espone i comandi principali del framework.

## Comandi

| Comando | Descrizione |
|---------|-------------|
| `init` | Costruisce una config YAML in modo guidato — vedi [[guided-config]] |
| `run` | Esegue un singolo agente o un grafo (opz. `--checkpoint`/`--run-id`) |
| `resume` | Riprende un run grafo da checkpoint — vedi [[checkpointing]] |
| `validate` | Valida un file di configurazione YAML |
| `ingest` | Indicizza sorgenti per RAG |
| `eval` | Esegue uno scenario di eval deterministico (replay con risposte scriptate) |

## Opzioni di `init`

| Opzione | Descrizione |
|---------|-------------|
| `--output <file>` | Path di destinazione proposto (default `./agora.yaml`) |

## Opzioni di `run`

| Opzione | Descrizione |
|---------|-------------|
| `--config <file>` | Path al file YAML |
| `--input <text>` | Input utente / task |
| `--agent <id>` | Agente da eseguire (single-agent mode) |
| `--graph` | Esegui il grafo definito in config |
| `--stream` | Streaming dei token su stdout — vedi [[streaming]] |
| `--checkpoint <dir>` | Salva i checkpoint per step (abilita `resume`) — vedi [[checkpointing]] |
| `--run-id <id>` | Id del run per i checkpoint (default: generato) |

## Esempi di utilizzo

```bash
# Configurazione guidata (genera ./agora.yaml)
dotnet run --project src/Agora.Cli -- init

# Singolo agente
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Scrivi una nota"

# Grafo multi-agente
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Costruisci un todo app" --graph

# Validazione config
dotnet run --project src/Agora.Cli -- validate --config examples/agora.yaml

# Ingest RAG
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml
```

## Approval handler

In modalità CLI usa `ConsoleApprovalHandler` — mostra all'utente la richiesta di approvazione e attende input da tastiera.
