# Research Log

## 2026-06-20 — Miglioramenti: embedder da config, conflict-judge efficiente, memory, eval

Avviato il programma di miglioramenti (10 voci tracciate). Completate finora:
- **Embedder da config**: `type: openai|ollama` selezionabile da YAML via resolver iniettato
  dal bordo (`AgentFrameworkEmbedders`); chiavi risolte dal provider come per il chat client
- **Conflict-judge efficiente**: prefiltro per similarità (`conflictThreshold`) + cache degli esiti
- **Context memory evoluta**: `max_chars` (budget recall) + `remember_outputs` (salva anche gli output)
- **Eval/replay harness**: `agora eval --config --scenario` con risposte scriptate (Fake provider),
  run deterministico e check su output/segnali; `Eval/Scenario` + `ScenarioRunner`
- **Routing intelligente**: edge `type: route` con `IRouter`/`LlmRouter` — un LLM sceglie il
  branch in base alle descrizioni `when`, più robusto del routing a signal-token
- **Esecuzione parallela** (fork/join): edge `type: parallel`, branch concorrenti via
  `Task.WhenAll` (mutazione di `State` seriale, nessuna race), convergenza su un join.
  `GraphExecutor.FanOutAsync`; concept `parallel-execution`, esempio `agora-parallel.yaml`
- **Checkpointing & resume**: `StateSnapshot` (JSON, signals tipizzati) + `ICheckpointStore`
  (`InMemory`/`File`); checkpoint dopo ogni nodo; `GraphExecutor.RunAsync(resumeFrom:)` +
  `Runtime.ResumeAsync`; CLI `run --checkpoint`/`--run-id` e comando `resume`. Concept `checkpointing`
- **Token streaming**: `IStreamingChatProvider` (capability, non rompe `IChatProvider`) su
  Fake/AgentFramework/Resilient; sink `Action<string>` propagato Runtime→executor→agente;
  CLI `run --stream`. Tool agent e branch paralleli esclusi in v1. Concept `streaming`
- **`IVectorStore` async**: `UpsertAsync`/`QueryAsync`/`DeleteAsync`; tolto il sync-over-async
  di `QdrantVectorStore`. Aggiornati InMemory/File/Qdrant + RagPipeline/Ingestor/KnowledgeBase/ContextMemory
  e tutti i test. 209 core verdi
- **Wiki sync**: allineati `overview` (concetti, architettura, stato) e `index` (entità CLI) alle
  nuove capacità; aggiunte le opzioni `--stream`/`--checkpoint`/`--run-id` all'entità `agora-cli`

## 2026-06-20 — Context memory (compressione contesto via RAG)

- Modalità opt-in `memory: { enabled, top_k }`: invece di iniettare tutti gli artifact,
  si **salvano** gli artifact dichiarati e si **recuperano le top-K rilevanti** per agente
- `ContextMemory` (append-only, no conflict-check; source `memory:` per distinguerle dalla KB;
  recall filtrato). `GraphExecutor` usa recall al posto di `ArtifactSummary`; `Runtime` riusa
  embedder/store del RAG
- Nuova pagina concept `context-memory`, esempio `examples/agora-memory.yaml`; +5 test (182 core verdi)
- Fix isolamento: `TracingTests` in collection non parallelizzabile (recorder globale)

## 2026-06-20 — Adapter Qdrant (DB vettoriale su container)

- `QdrantVectorStore` (`Agora.AgentFramework`): `IVectorStore` su server Qdrant via **gRPC**
  (client ufficiale `Qdrant.Client` 1.18.1). Collezione creata lazy al primo upsert (cosine)
- Seam di estensione: `RagFactory`/`Runtime` accettano uno `storeResolver` iniettato dal bordo;
  `AgentFrameworkVectorStores.TryCreate` risolve `type: qdrant` (`url`/`collection`). Core resta framework-free
- `VectorStoreConfig.Url` aggiunto; cablaggio fino a `Program.cs`
- Test del seam + resolver (177 core, 8 API verdi). Integrazione contro un Qdrant reale rimandata (no container qui)

## 2026-06-19 — Wizard `init`: handoff, vector store, tool RAG

- `agora init` ora chiede la modalità `handoff`, il tipo di vector store
  (`memory`/`file` + path) nella sezione RAG, e propone i tool per agente quando
  RAG/handoff/skills/MCP sono configurati (inclusi `rag_search`/`rag_write`/`ask_agent`)
- RAG riordinato prima degli agenti; aggiornata pagina `guided-config`; 172 core verdi

- Gli agenti accedono alla KB condivisa via tool built-in (`Agora.AgentFramework`):
  `rag_search` (lettura), `rag_write` (scrittura via `KnowledgeBase`), abilitati dalla
  allow-list `tools` — `RagTools`
- `ask_agent(target, question)`: il ricevente interpella un altro agente dopo `rag_search`.
  Tool sincrono che riesegue il target in "answer-mode" (senza `ask_agent`, niente ricorsione) — `AskAgentTool`
- `Runtime` costruisce la `KnowledgeBase` riusando `Embedder`/`Store` del `RagPipeline`
  (un solo `IVectorStore` per lettura+scrittura); `IConflictResolver` cablato fino a `Program.cs`
- `AgentBuildContext` porta `Rag`/`KnowledgeBase`/`AskAgent`; +12 test (171 core, 8 API)
- Aggiornate `shared-knowledge-base`, `handoff-context`, `mcp-tools`

## 2026-06-19 — Contesto handoff, Fase 2

- Modalità opt-in `handoff: true`: su un hop si passa al successivo **solo** l'artifact
  `handoff` del mittente (o niente), non l'output completo. Default invariato
- `GraphExecutor` param `handoff`; l'artifact `handoff` è canale mirato, escluso dal
  sommario globale degli artifact; `H2cInterpreter` estrae il campo `handoff` in h2c
- `HandoffPreamble` iniettato quando attivo; nuova pagina concept `handoff-context`,
  aggiornato `agent-graph`, esempio `examples/agora-handoff.yaml`
- +3 test (160 core verdi)

## 2026-06-19 — RAG scrivibile, Fase 1 (knowledge base condivisa)

- Avviata l'evoluzione del RAG da sola-lettura a **knowledge base scrivibile** con
  rilevamento e risoluzione conflitti. Creata pagina concept `shared-knowledge-base`
- `IVectorStore` esteso: `Chunk.Id` stabile + `Delete(ids)` + upsert-by-id → la
  risoluzione conflitti **sostituisce** la voce superata invece di accodarla
- Nuovo `FileVectorStore` (persistente su disco, JSON); `vector_store: { type: file }`
- `KnowledgeBase` (embed → vicini → judge → write/escalate); `IConflictJudge` +
  `LlmConflictJudge` (protocollo a marker con `CONFLICTS_WITH`) + `NoOpConflictJudge`
- `IConflictResolver` (KeepExisting/KeepNew/Merge) + `ConsoleConflictResolver` / `FakeConflictResolver`
- Accesso al RAG dietro interfaccia: disco ora, DB su container (Qdrant/pgvector) in futuro
- +17 test (157 core, 8 API verdi). Non ancora collegata agli agenti (Fase 3)
- Aggiornate `rag-pipeline`, `human-in-the-loop`, indice

## 2026-06-19 — Comando `init` (config guidata)

- Aggiunto comando CLI `agora init`: wizard interattivo che costruisce la config YAML
  (communication, providers, models, agents e grafo multi-agente con edge condizionali)
- Nuovo `ConfigWriter` (serializzazione YAML, controparte di `ConfigLoader`); prima
  YamlDotNet era usato solo in lettura
- `ConfigWizard` con I/O iniettabili → unit-testabile (`ConfigWizardTests`)
- Creata pagina concept `guided-config`; aggiornata entity `agora-cli` e l'indice
- **Estensione "completa"**: il wizard copre ora anche le sezioni opzionali — skills
  (directory), MCP (server stdio/http), RAG (ingest/chunk/top_k/refine, embedder
  `fake` + store `memory`) e HITL (`approvals` ⊆ `tools` per agente). Le sezioni
  opzionali sono gate dietro sì/no con default no; test esteso a 4 casi

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
