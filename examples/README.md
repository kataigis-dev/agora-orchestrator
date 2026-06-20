# Examples

## Configurations

### agora.yaml — Minimal configuration

Two agents (planner, executor) with the OpenAI provider, H2C communication, no graph.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Plan a project"
```

### agora-h2c.yaml — Conditional graph with H2C

Graph: planner → coder → reviewer → (fix loop or done). Uses H2C `[STATE:DONE/FIX]`.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Create a todo list API" --graph
```

### agora-llama.yaml — Local model

Uses llama studio / LM Studio with `google/gemma-4-e4b`.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-llama.yaml --agent assistant --input "Hello"
```

### agora-generate-api.yaml — Code generation with review

Graph: generator → reviewer (conditional: fix/done). Uses MCP filesystem to read/write files,
natural communication with `<<signal done/fix>>` signals.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-generate-api.yaml --input "Create a Web API for managing books" --graph
```

### agora-tools.yaml — Skills + MCP tools

Agent with the `summarize` skill and access to MCP filesystem.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-tools.yaml --agent summarizer --input "Read and summarize this file"
```

### agora-rag.yaml — RAG pipeline

Ingest and query pipeline with knowledge from files.

```bash
# Ingest
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml

# Query
dotnet run --project src/Agora.Cli -- run --config examples/agora-rag.yaml --agent assistant --input "What does the project know about X?"
```

### agora-hitl.yaml — Human-in-the-loop

Agent that requires approval before performing actions.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-hitl.yaml --agent reviewer --input "Approve this change"
```

## Generated projects

### generated-api/

A .NET Web API project generated autonomously by the agent via `examples/agora-generate-api.yaml`.
Contains:

- `Api.csproj` — .NET 10 project
- `Program.cs` — Web API with 7 endpoints (book CRUD), middleware, in-memory store
