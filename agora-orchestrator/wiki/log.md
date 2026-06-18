# Research Log

## 2026-06-17

- Project created
- Wiki inizializzata con conoscenza del progetto Agora Orchestrator
- Creati 4 entity pages: `agora-orchestrator`, `agora-cli`, `agora-api`, `agora-agent-framework`
- Creati 8 concept pages: `agent-graph`, `edge-types`, `h2c-protocol`, `signal`, `communication-modes`, `rag-pipeline`, `skills`, `mcp-tools`, `human-in-the-loop`
- Aggiornati `purpose.md`, `wiki/overview.md`, `wiki/index.md`

## 2026-06-18 — Correzione pagine wiki

- **signal.md**: aggiunta sintassi `<<artifact key=value>>`, firma `Extract` a 3-tuple, esempio artifact
- **agent-graph.md**: aggiunti `Artifacts`, `LoopCounters`, `Inbox()`, `ArtifactSummary()` allo stato condiviso
- **mcp-tools.md**: aggiunta sezione Built-in Filesystem Tools (read/write/search/list)
- **communication-modes.md**: aggiunta colonna artifact nella tabella comparativa
- **overview.md**: `IProvider` → `IChatProvider` nell'architettura
- **agora-orchestrator.md**: corrette dipendenze esterne (Microsoft.Extensions.AI.OpenAI, ModelContextProtocol.Core, OllamaSharp)
- **human-in-the-loop.md**: corretto `IApprovalHandler` (firma con `ApprovalRequest` + `CancellationToken`), flusso tool-based, `PendingApprovalHandler` API
- **rag-pipeline.md**: corretta struttura YAML annidata (retrieval.embedder, ingest.sources, ecc.)
- **skills.md**: `directory` → `directories` (lista)
- **edge-types.md**: fallback a primo edge non-condizionale (non errore)
