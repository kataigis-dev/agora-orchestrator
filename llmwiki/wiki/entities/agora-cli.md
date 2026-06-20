---
type: entity
title: Agora CLI
tags: [cli, tool, dotnet]
related: [agora-orchestrator, agora-api, guided-config, checkpointing, streaming]
created: 2026-06-17
updated: 2026-06-20
---

# Agora CLI

CLI executable (`Agora.Cli`) exposing the framework's main commands.

## Commands

| Command | Description |
|---------|-------------|
| `init` | Builds a YAML config interactively — see [[guided-config]] |
| `run` | Runs a single agent or a graph (opt. `--stream`/`--checkpoint`/`--run-id`) |
| `resume` | Resumes a graph run from a checkpoint — see [[checkpointing]] |
| `validate` | Validates a YAML configuration file |
| `ingest` | Indexes sources for RAG |
| `eval` | Runs a deterministic eval scenario (scripted replay) |

## `init` options

| Option | Description |
|--------|-------------|
| `--output <file>` | Proposed destination path (default `./agora.yaml`) |

## `run` options

| Option | Description |
|--------|-------------|
| `--config <file>` | Path to the YAML file |
| `--input <text>` | User input / task |
| `--agent <id>` | Agent to run (single-agent mode) |
| `--graph` | Run the graph defined in the config |
| `--stream` | Stream tokens to stdout — see [[streaming]] |
| `--checkpoint <dir>` | Save per-step checkpoints (enables `resume`) — see [[checkpointing]] |
| `--run-id <id>` | Run id for checkpoints (default: generated) |

## Usage examples

```bash
# Guided config (generates ./agora.yaml)
dotnet run --project src/Agora.Cli -- init

# Single agent
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Write a note"

# Multi-agent graph
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Build a todo app" --graph

# Validate config
dotnet run --project src/Agora.Cli -- validate --config examples/agora.yaml

# RAG ingest
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml
```

## Approval handler

In CLI mode it uses `ConsoleApprovalHandler` — shows the approval request and waits for keyboard
input. Knowledge-base write conflicts use `ConsoleConflictResolver`.
