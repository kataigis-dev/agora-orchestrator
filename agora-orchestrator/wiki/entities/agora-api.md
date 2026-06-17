---
type: entity
title: Agora API
tags: [api, rest, aspnetcore, dotnet]
related: [agora-orchestrator, agora-cli]
created: 2026-06-17
updated: 2026-06-17
---

# Agora API

Server REST (`Agora.Api`) basato su ASP.NET Core Minimal API che espone l'esecuzione di agenti e grafi tramite HTTP.

## Componenti principali

| File | Ruolo |
|------|-------|
| `Program.cs` | Bootstrap del server, registrazione endpoint |
| `RunEndpoints.cs` | Definizione endpoint HTTP |
| `RunExecutor.cs` | Esecuzione asincrona di un run |
| `RunQueue.cs` | Coda interna per l'elaborazione dei run |
| `AgoraRuntimeFactory.cs` | Istanzia il `Runtime` dalla configurazione ricevuta |
| `Dtos.cs` | DTO per request/response HTTP |

## Endpoint principali

Vedi `examples/agora-api.http` per esempi di chiamate.

## Test

I test di integrazione si trovano in `tests/Agora.Api.Tests/`:
- `HealthAndAgentsTests.cs` — health check e listing agenti
- `RunLifecycleTests.cs` — ciclo di vita di un run
- `ApprovalFlowTests.cs` — test del flusso HITL
- `IngestTests.cs` — ingest RAG via API
