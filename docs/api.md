# REST API

## Start

```bash
dotnet run --project src/Agora.Api -- --config examples/agora.yaml
```

The config is resolved server-side from `--config <path>` or the `AGORA_CONFIG` env var (it is
**not** part of requests). Runs are executed asynchronously by a hosted `RunExecutor` over a queue;
state is kept in an in-memory run store.

## Endpoints

### GET /health

```json
{ "status": "ok" }
```

### GET /agents

Lists configured agents:

```json
[
  { "id": "planner", "role": "You decompose the task.", "tools": ["rag_search"], "approvals": [] }
]
```

### POST /runs

Starts a run. `mode` is `"agent"` or `"graph"`; `agent` is required for agent mode:

```json
// Request (StartRunRequest)
{ "mode": "graph", "agent": null, "input": "Build a todo app" }

// Response — 202 Accepted, Location: /runs/{id}
{ "id": "..." }
```

### GET /runs/{id}

Run status and output, plus any pending approvals:

```json
{
  "id": "...",
  "status": "Running",
  "mode": "graph",
  "agentId": null,
  "output": "...",
  "error": null,
  "pending": [
    { "id": "...", "agentId": "ops", "functionName": "write_file", "arguments": "path=out.txt" }
  ]
}
```

### POST /runs/{id}/approvals

Resolves pending HITL approvals:

```json
// Request (ApprovalsRequest)
{ "approvals": [ { "id": "...", "approved": true } ] }

// Response
{ "resolved": 1 }
```

### POST /ingest

Runs the configured RAG ingest (requires an enabled `rag` section):

```json
{ "count": 12 }
```

## Run lifecycle

A run is queued, then executed: it may pause **waiting for approval** (HITL tool calls), then
**complete** or **fail**. Poll `GET /runs/{id}` for status and to discover pending approvals.

## Approvals (HITL)

There is no per-agent `require_approval` flag — approvals are configured per tool via the agent's
`approvals` list (see the agents/configuration pages). When such a tool is called, the run exposes
a pending approval that the client resolves with `POST /runs/{id}/approvals`.
