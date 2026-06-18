# REST API

## Avvio

```bash
dotnet run --project src/Agora.Api
```

Il server si avvia su `http://localhost:5000` per default.

## Endpoint

### POST /runs

Avvia una nuova esecuzione agente o grafo:

```json
// Request
{
  "config": "examples/agora.yaml",
  "agent": "planner",
  "input": "Scrivi una nota",
  "graph": false
}

// Response
{
  "runId": "run_abc123",
  "status": "running",
  "output": "...",
  "messages": [...]
}
```

### GET /runs/{runId}

Recupera lo stato e l'output di un'esecuzione:

```json
{
  "runId": "run_abc123",
  "status": "completed",
  "agent": "planner",
  "input": "Scrivi una nota",
  "output": "Ecco la nota...",
  "messages": [...],
  "createdAt": "2026-06-18T12:00:00Z",
  "completedAt": "2026-06-18T12:00:05Z"
}
```

### GET /runs

Lista tutte le esecuzioni:

```json
{
  "runs": [
    {
      "runId": "run_abc123",
      "status": "completed",
      "agent": "planner",
      "createdAt": "2026-06-18T12:00:00Z"
    }
  ]
}
```

### POST /runs/{runId}/approve

Approvazione umana per agenti con `require_approval: true`:

```json
{
  "approved": true,
  "feedback": "Procedi pure"
}
```

### GET /agents

Lista agenti configurati:

```json
{
  "agents": [
    {
      "id": "planner",
      "model": "gpt-4o-mini",
      "tools": ["read_file"],
      "status": "idle"
    }
  ]
}
```

### GET /health

Health check:

```json
{
  "status": "healthy",
  "version": "0.0.1",
  "uptime": "01:23:45"
}
```

## Ciclo di vita di un run

1. **pending** — ricevuta richiesta, in coda
2. **running** — in esecuzione
3. **waiting_approval** — in attesa di approvazione umana
4. **completed** — eseguito con successo
5. **failed** — errore durante esecuzione
6. **cancelled** — cancellato dall'utente

## Configurazione

L'API cerca il file `appsettings.json` o `appsettings.{ENVIRONMENT}.json`:

```json
{
  "Agora": {
    "DefaultConfig": "examples/agora.yaml",
    "DefaultAgent": "planner"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" }
    }
  }
}
```
