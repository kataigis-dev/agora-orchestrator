# CLI

## Uso

```bash
dotnet run --project src/Agora.Cli -- <comando> [options]
```

## Comandi

### run

Esegue un agente singolo o un grafo:

```bash
# Agente singolo
dotnet run --project src/Agora.Cli -- run --config examples/agora.yaml --agent planner --input "Scrivi una nota"

# Grafo
dotnet run --project src/Agora.Cli -- run --config examples/agora-h2c.yaml --input "Crea una todo app" --graph
```

#### Options

| Opzione | Descrizione |
|---|---|
| `--config <file>` | Path del file YAML (obbligatorio) |
| `--input <text>` | Input utente / descrizione task (obbligatorio) |
| `--agent <id>` | Agente da eseguire (obbligatorio per modalità singola) |
| `--graph` | Esegue in modalità grafo (usa il graph definito nel config) |

### validate

Valida un file di configurazione senza eseguire:

```bash
dotnet run --project src/Agora.Cli -- validate --config examples/agora.yaml
```

Restituisce la struttura parsata o errori di validazione.

### ingest

Esegue la pipeline RAG per indicizzare knowledge:

```bash
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml
```

Processa le fonti configurate (file, directory, web) e popola il vector store.

## Exit codes

| Codice | Significato |
|---|---|
| 0 | Successo |
| 1 | Errore generico |
| 2 | Config non valido |
| 3 | Agente non trovato |
| 4 | Errore durante esecuzione |

## Parsing opzioni

`CliRunner.ParseOptions` gestisce la linea di comando con supporto per:
- Flag booleani (`--graph`, `--verbose`)
- Valori con spazio (`--input "testo lungo"`)
- Path con spazi
- Default values
