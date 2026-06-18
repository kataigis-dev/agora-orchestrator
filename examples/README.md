# Esempi

## Configurazioni

### agora.yaml — Configurazione minimale

Due agenti (planner, executor) con provider OpenAI, comunicazione H2C, senza grafo.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Pianifica un progetto"
```

### agora-h2c.yaml — Grafo condizionale con H2C

Grafo: planner → coder → reviewer → (fix loop o done). Usa H2C `[STATE:DONE/FIX]`.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Crea una todo list API" --graph
```

### agora-llama.yaml — Modello locale

Usa llama studio / LM Studio con `google/gemma-4-e4b`.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-llama.yaml --agent assistant --input "Ciao"
```

### agora-generate-api.yaml — Generazione codice con revisione

Grafo: generator → reviewer (conditional: fix/done). Usa MCP filesystem per leggere/scrivere file, comunicazione natural con segnali `<<signal done/fix>>`.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-generate-api.yaml --input "Crea una Web API per gestione libri" --graph
```

### agora-tools.yaml — Skills + MCP tools

Agente con skill `summarize` e accesso a MCP filesystem.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-tools.yaml --agent summarizer --input "Leggi e riassumi questo file"
```

### agora-rag.yaml — RAG pipeline

Pipeline di ingest e query con knowledge da file.

```bash
# Ingest
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml

# Query
dotnet run --project src/Agora.Cli -- run --config examples/agora-rag.yaml --agent assistant --input "Cosa sa il progetto su X?"
```

### agora-hitl.yaml — Human-in-the-loop

Agente che richiede approvazione prima di eseguire azioni.

```bash
dotnet run --project src/Agora.Cli -- run --config examples/agora-hitl.yaml --agent reviewer --input "Approva questa modifica"
```

## Progetti generati

### generated-api/

Progetto .NET Web API generato autonomamente dall'agente tramite `examples/agora-generate-api.yaml`. Contiene:

- `Api.csproj` — progetto .NET 10
- `Program.cs` — Web API con 7 endpoint (CRUD libri), middleware, in-memory store
