# CLI

## Usage

```bash
dotnet run --project src/Agora.Cli -- <command> [options]
```

## Commands

### init

Builds a config interactively (guided wizard):

```bash
dotnet run --project src/Agora.Cli -- init [--output agora.yaml]
```

### run

Runs a single agent or a graph:

```bash
# Single agent
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Write a note"

# Graph
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Build a todo app" --graph
```

#### Options

| Option | Description |
|---|---|
| `--config <file>` | Path of the YAML file (required) |
| `--input <text>` | User input / task description (required) |
| `--agent <id>` | Agent to run (required for single-agent mode) |
| `--graph` | Run in graph mode (uses the graph defined in the config) |
| `--stream` | Stream tokens to stdout as they are generated |
| `--checkpoint <dir>` | Persist a per-step checkpoint (enables `resume`) |
| `--run-id <id>` | Run id for checkpoints (default: generated) |

### resume

Resumes a graph run from its checkpoint:

```bash
dotnet run --project src/Agora.Cli -- resume --config examples/agora.yaml --checkpoint ./cp --run-id <id>
```

### validate

Validates a configuration file without running it:

```bash
dotnet run --project src/Agora.Cli -- validate --config examples/agora.yaml
```

Returns `OK` or the validation error.

### ingest

Runs the RAG pipeline to index knowledge:

```bash
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml
```

Processes the configured sources (files, directories) and populates the vector store.

### eval

Runs a deterministic eval scenario (scripted replay, no real model calls):

```bash
dotnet run --project src/Agora.Cli -- eval --config examples/agora.yaml --scenario scenario.json
```

Prints `PASS` or `FAIL` with the failed expectations.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Error (invalid config, unknown agent, execution error) |

## Option parsing

`CliRunner.ParseOptions` handles the command line with support for:
- Boolean flags (`--graph`, `--stream`)
- Values with spaces (`--input "long text"`)
- Paths with spaces
