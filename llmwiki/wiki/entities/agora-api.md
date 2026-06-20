---
type: entity
title: Agora API
tags: [api, rest, aspnetcore, dotnet]
related: [agora-orchestrator, agora-cli, human-in-the-loop]
created: 2026-06-17
updated: 2026-06-20
---

# Agora API

REST server (`Agora.Api`) built on ASP.NET Core Minimal APIs, exposing agent and graph execution
over HTTP. The config is loaded server-side (`--config` / `AGORA_CONFIG`); runs execute
asynchronously over a queue, with state in an in-memory run store.

## Main components

| File | Role |
|------|------|
| `Program.cs` | Server bootstrap, endpoint registration (`/health`, `/agents`) |
| `RunEndpoints.cs` | HTTP endpoints (`/runs`, `/runs/{id}`, `/runs/{id}/approvals`, `/ingest`) |
| `RunExecutor.cs` | Hosted service that executes queued runs |
| `RunQueue.cs` | Internal queue for run processing |
| `AgoraRuntimeFactory.cs` | Builds a fresh `Runtime` per run from the shared config |
| `Dtos.cs` | DTOs for HTTP requests/responses |

## Endpoints

- `GET /health`, `GET /agents`
- `POST /runs` (mode `agent`/`graph` + input → 202 with run id), `GET /runs/{id}`
- `POST /runs/{id}/approvals` (resolve pending HITL approvals)
- `POST /ingest` (run the configured RAG ingest)

See `examples/agora-api.http` for example calls.

## Tests

Integration tests live in `tests/Agora.Api.Tests/`:
- `HealthAndAgentsTests.cs` — health check and agent listing
- `RunLifecycleTests.cs` — run lifecycle
- `ApprovalFlowTests.cs` — HITL flow
- `IngestTests.cs` — RAG ingest via API
