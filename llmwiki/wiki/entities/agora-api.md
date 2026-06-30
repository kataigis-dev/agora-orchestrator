---
type: entity
title: Retired Agora API
tags: [api, rest, aspnetcore, dotnet, retired]
related: [agora-orchestrator, agora-cli, human-in-the-loop]
created: 2026-06-17
updated: 2026-06-30
---

# Retired Agora API

`Agora.Api` was the former ASP.NET Core Minimal API surface for running agents and graphs over HTTP.
It has been retired from the current solution. The active product surface is the CLI (`Agora.Cli`) plus
local read-only MCP stdio exposure for `rag_search`.

The retirement keeps writable RAG (`rag_write`) on the CLI + human path, where conflict resolution can
block synchronously for a present human instead of exposing headless mutation endpoints.

## Main components

| File | Role |
|------|------|
| Current replacement | Role |
|------|------|
| `Agora.Cli` | Runs agents/graphs, validates configs, ingests RAG sources, resumes checkpoints |
| `serve-mcp` | Local read-only MCP stdio server exposing `rag_search` only |
| `purge-kb-log` | CLI retention path for the KB mutation audit log |

## Endpoints

No HTTP endpoints are part of the current baseline.

## Tests

The active test suite lives in `tests/Agora.Tests/`.
