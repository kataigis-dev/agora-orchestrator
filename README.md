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

> The **H2C** structured-communication protocol is by [LuPaLa-Coder/H2C](https://github.com/LuPaLa-Coder/H2C);
> Agora implements it as one of its two communication modes.

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
| `examples/agora-spec.yaml` | Structured spec-driven development (spec store + `spec_*` tools) |

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

Full documentation in [`docs/`](docs/index.md):

| Document | Content |
|---|---|
| [docs/index.md](docs/index.md) | Overview, directory structure, quick start |
| [docs/architecture.md](docs/architecture.md) | Architecture, GraphExecutor, agent lifecycle |
| [docs/configuration.md](docs/configuration.md) | Complete YAML configuration reference |
| [docs/agents.md](docs/agents.md) | Agents, MCP tools, skills, human approval |
| [docs/graph.md](docs/graph.md) | Graph: nodes, edges, signals, loops, visualization |
| [docs/cli.md](docs/cli.md) | CLI: run/validate/ingest commands, options, exit codes |
| [docs/api.md](docs/api.md) | REST API: endpoints, lifecycles, health check |
| [docs/providers.md](docs/providers.md) | Chat providers: OpenAI, Ollama, custom, prompt caching, resilience |
| [docs/observability.md](docs/observability.md) | Execution observers, run metrics (steps, rework, token/cache usage) |
| [docs/mcp.md](docs/mcp.md) | MCP: stdio/HTTP servers, tool discovery, approval gates |
| [docs/rag.md](docs/rag.md) | RAG: ingest pipeline, chunking, embeddings, vector store |
| [docs/spec.md](docs/spec.md) | Structured specs: requirement/task model, store (file/MCP), `spec_*` tools, validation, real check execution (`run_check`/`spec_verify`), and the traceability completion gate (`spec_gate`) |
| [docs/h2c.md](docs/h2c.md) | H2C protocol: blocks, types, interpreter |
| [docs/concepts/](docs/concepts/README.md) | Conceptual documentation: agents, RAG, MCP, agentic-workflow patterns, security, execution flow & class reference — with sources from Anthropic/Google/Microsoft/AWS |
| [examples/README.md](examples/README.md) | Description of each example with commands |

## Build & test

```bash
dotnet build
dotnet test tests/Agora.Tests
dotnet test tests/Agora.Api.Tests
```

## Acknowledgments

- [@s4ndr0ne](https://github.com/s4ndr0ne) — for contributions to this repository.
- [LuPaLa-Coder/H2C](https://github.com/LuPaLa-Coder/H2C) — author of the H2C structured-communication
  protocol, implemented here as one of Agora's two communication modes.

## License

Apache 2.0
