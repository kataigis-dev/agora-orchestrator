# Agora Orchestrator

Versione: 0.0.1

Agora Orchestrator è un framework di orchestrazione multi-agente per .NET 10, senza dipendenze esterne nel core. Permette di definire grafi di agenti AI, ciascuno configurato con un modello linguistico diverso (OpenAI, Ollama, endpoint compatibili) e orchestrarli su un grafo diretto con archi sequenziali, handoff e condizionali.

## Scopo

Creare pipeline multi-agente autonome dove agenti specializzati collaborano su task complessi: generazione codice, analisi documenti, revisione, approvazione umana, ingest RAG.

## Principi

- Framework-free: il core `Agora` non ha dipendenze esterne
- Config-driven: tutto è dichiarato in YAML (provider, modelli, agenti, grafo, MCP, skills, RAG)
- Estensibile: provider chat, strumenti MCP, skills, e custom graph executors
- Due modalità di comunicazione: H2C (formale) e natural (linguaggio naturale + segnali)

## Struttura directory

```
src/
├── Agora/                   # Core library (senza dipendenze esterne)
│   ├── Orchestration/       # Motore del grafo (GraphRunner, nodi, edge)
│   ├── Agents/              # Modelli di agente (base Agent)
│   ├── Commands/            # Comandi CLI interni
│   ├── Configuration/       # Parsing YAML e modelli di configurazione
│   ├── Communication/       # Protocolli H2C e natural
│   ├── Providers/           # Interfacce provider chat
│   ├── Mcp/                 # Interfacce MCP
│   ├── RAG/                 # Pipeline RAG (ingest, chunking, embedding, search)
│   └── Resilience/          # Retry, circuit breaker
├── Agora.AgentFramework/    # Implementazioni concrete con Microsoft.Extensions.AI
│   ├── ChatProviders/       # OpenAI, Ollama, custom OpenAI-compatibili
│   └── Mcp/                 # Client MCP stdio/HTTP
├── Agora.Api/               # Server REST API ASP.NET
└── Agora.Cli/               # Eseguibile CLI
tests/
├── Agora.Tests/             # Test unitari core
└── Agora.Api.Tests/         # Test integrazione API
examples/
├── agora.yaml               # Config minimale
├── agora-h2c.yaml           # H2C con grafo condizionale
├── agora-tools.yaml         # Skills + MCP
├── agora-rag.yaml           # RAG pipeline
├── agora-hitl.yaml          # Human-in-the-loop
├── agora-llama.yaml         # Modello locale via llama studio
└── agora-generate-api.yaml  # Generazione codice con MCP + ciclo di revisione
docs/
└── (wiki documentazione)
```

## Avvio rapido

```bash
# Build
dotnet build

# Esecuzione agente singolo
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Scrivi una nota"

# Esecuzione grafo
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Crea una todo app" --graph

# Validazione config
dotnet run --project src/Agora.Cli -- validate --config examples/agora.yaml

# Ingest RAG
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml

# Test
dotnet test tests/Agora.Tests
dotnet test tests/Agora.Api.Tests
```

## Componenti principali

| Componente | Descrizione |
|---|---|
| **GraphExecutor** | Esegue un grafo diretto di agenti, gestendo stati, messaggi e transizioni |
| **Agent** | Agente base: prompt + configurazione + eventuali tools/skills |
| **ChatProvider** | Interfaccia per provider chat (OpenAI, Ollama, custom) |
| **H2cParser** | Interpreta il protocollo H2C (blocchi strutturati tra `[H2C]...[/H2C]`) |
| **McpClient** | Connessione a server MCP via stdio o HTTP |
| **RagPipeline** | Ingest, chunking, embedding, vector search |
| **ResiliencePolicy** | Retry, circuit breaker per chiamate API |
