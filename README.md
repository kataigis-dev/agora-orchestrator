# Agora Orchestrator

A reusable, framework-free multi-agent orchestration framework for .NET 10.

Define a graph of AI agents, each backed by a configurable language model (OpenAI, Ollama, or any OpenAI-compatible endpoint), and orchestrate them over a directed graph with sequential, handoff, and conditional edges.

## Quick start

```bash
# Build
dotnet build

# Generate a config interactively
dotnet run --project src/Agora.Cli -- init

# Run a single agent
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Write a note"

# Run a graph
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Build a todo app" --graph

# Validate a config
dotnet run --project src/Agora.Cli -- validate --config examples/agora.yaml

# Ingest RAG knowledge
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml
```

## CLI usage

```
agora <init|run|validate|ingest> [options]
```

| Command | Description |
|---------|-------------|
| `init` | Build a config file interactively (guided wizard) |
| `run` | Run a single agent or a graph |
| `validate` | Validate a config file |
| `ingest` | Ingest RAG knowledge sources |

### `init` options

| Option | Description |
|--------|-------------|
| `--output <file>` | Destination path (defaults to `./agora.yaml`) |

The wizard asks for the communication mode, providers, models and agents, plus the
optional sections (skills, MCP tool servers, RAG, and per-agent tools/approvals for
HITL) and — for multi-agent setups — the execution graph, then writes a validated
config.

### `run` options

| Option | Description |
|--------|-------------|
| `--config <file>` | Path to the YAML config file |
| `--input <text>` | User input / task description |
| `--agent <id>` | Agent to run (required for single-agent mode) |
| `--graph` | Run in graph mode (uses the graph defined in config) |

## Configuration

A single YAML file defines providers, models, agents, and optionally a graph.

```yaml
version: "1"
communication: natural                       # "h2c" (default) or "natural"
defaults: { model: fast, temperature: 0.2 }
providers:
  openai: { api_key_env: OPENAI_API_KEY }
  ollama: { base_url: http://localhost:11434 }
models:
  fast:     { provider: openai, model: gpt-4o-mini }
  local:    { provider: ollama, model: llama3.1 }
agents:
  assistant:
    model: fast
    role: "You are a helpful assistant."
```

### Local models (llama studio / LM Studio)

```yaml
providers:
  llamastudio:
    base_url: http://127.0.0.1:1234/v1
    api_key_env: ~
models:
  local: { provider: llamastudio, model: google/gemma-4-e4b }
```

The `/v1` suffix is required — the OpenAI SDK constructs paths relative to the base URL.

### Graph with conditional loops

```yaml
graph:
  entry: generator
  edges:
    - { from: generator, to: reviewer, type: handoff }
    - { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END,    type: conditional, when: done }
```

Agents signal routing with `<<signal done>>` (natural mode) or `[STATE:DONE]` (H2C mode).

### Tools via MCP

```yaml
mcp:
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
agents:
  builder:
    tools: [write_file, read_file]
```

## Examples

| File | Features |
|------|----------|
| `examples/agora.yaml` | Minimal: 2 agents, no graph |
| `examples/agora-tools.yaml` | Skills + MCP tools |
| `examples/agora-rag.yaml` | RAG pipeline |
| `examples/agora-hitl.yaml` | Human-in-the-loop approvals |
| `examples/agora-h2c.yaml` | H2C protocol with conditional graph |
| `examples/agora-llama.yaml` | Local model via llama studio |
| `examples/agora-generate-api.yaml` | Code generation with MCP + review loop |

## Project structure

```
src/
├── Agora/                    # Core library (no external deps)
├── Agora.AgentFramework/     # OpenAI, Ollama, MCP integration
├── Agora.Api/                # REST API server
└── Agora.Cli/                # CLI executable
tests/
├── Agora.Tests/              # Core library tests
└── Agora.Api.Tests/          # API integration tests
```

## Wiki

Documentazione completa in [`docs/`](docs/index.md):

| Documento | Contenuto |
|---|---|
| [docs/index.md](docs/index.md) | Panoramica, struttura directory, avvio rapido |
| [docs/architecture.md](docs/architecture.md) | Architettura, GraphExecutor, ciclo di vita agente |
| [docs/configuration.md](docs/configuration.md) | Riferimento completo configurazione YAML |
| [docs/agents.md](docs/agents.md) | Agenti, tools MCP, skills, approvazione umana |
| [docs/graph.md](docs/graph.md) | Grafo: nodi, edges, segnali, loop, visualizzazione |
| [docs/cli.md](docs/cli.md) | CLI: comandi run/validate/ingest, opzioni, exit code |
| [docs/api.md](docs/api.md) | REST API: endpoint, cicli di vita, health check |
| [docs/providers.md](docs/providers.md) | Provider chat: OpenAI, Ollama, custom, resilience |
| [docs/mcp.md](docs/mcp.md) | MCP: server stdio/HTTP, tools discovery, approval gates |
| [docs/rag.md](docs/rag.md) | RAG: pipeline ingest, chunking, embeddings, vector store |
| [docs/h2c.md](docs/h2c.md) | H2C protocol: blocchi, tipi, interpreter |
| [examples/README.md](examples/README.md) | Descrizione di ogni esempio con comandi |

## Build & test

```bash
dotnet build
dotnet test tests/Agora.Tests
dotnet test tests/Agora.Api.Tests
```

## License

Apache 2.0
