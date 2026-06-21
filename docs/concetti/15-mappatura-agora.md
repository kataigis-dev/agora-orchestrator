# 15 — Mappatura su Agora Orchestrator

Questo capitolo collega ogni concetto della cartella alla sua **implementazione concreta** in Agora
Orchestrator e alla documentazione tecnica in [`../`](../index.md).

Agora Orchestrator è un framework di orchestrazione multi-agente **framework-free** per .NET 10: il core
`Agora` non ha dipendenze esterne (a parte YamlDotNet), mentre l'integrazione con i modelli, MCP e il
*tooling* vive in `Agora.AgentFramework`, costruita su **Microsoft.Extensions.AI** e
**Microsoft.Agents.AI** (vedi [16 — Microsoft Agent Framework](16-microsoft-agent-framework.md)).

## Mappa concetto → implementazione

| Concetto (capitolo) | In Agora | Doc tecnica |
|---------------------|----------|-------------|
| **LLM / provider** ([01](01-fondamenti-llm.md)) | `IChatProvider` (OpenAI, Ollama, endpoint compatibili), `ModelSpec`, alias di modello in YAML | [providers.md](../providers.md) |
| **Agente** ([02](02-agenti.md)) | `Agent` / `AgentFrameworkAgent`, definiti in YAML con ruolo, modello, strumenti | [agents.md](../agents.md) |
| **Strumenti / function calling** ([03](03-strumenti-function-calling.md)) | Strumenti built-in (`read_file`, `rag_search`, `ask_agent`, `spec_*`, `run_check`...), allow-list `tools:` per agente | [agents.md](../agents.md), [configuration.md](../configuration.md) |
| **MCP** ([04](04-mcp.md)) | `McpToolSession` (stdio/HTTP), sezione `mcp.servers`, scoperta strumenti | [mcp.md](../mcp.md) |
| **RAG** ([05](05-rag.md)) | `RagPipeline` (ingest, chunking, embedding, vector store file/memory/Qdrant), `rag_search`/`rag_write` | [rag.md](../rag.md) |
| **Memoria / context engineering** ([06](06-memoria-contesto.md)) | `ContextMemory` (recall top-K per agente), handoff dell'artefatto, `ArtifactSummary` | [architecture.md](../architecture.md) |
| **Pattern di workflow** ([07](07-workflow-pattern.md)) | Archi del grafo: `sequential`, `handoff`, `conditional` (loop con `max_loops`), `route` (router LLM) | [graph.md](../graph.md) |
| **Orchestrazione multi-agente** ([08](08-orchestrazione-multi-agente.md)) | `GraphExecutor` con archi `parallel` (fork/join), routing condizionale, `ask_agent` per invocazione esplicita | [graph.md](../graph.md), [architecture.md](../architecture.md) |
| **Spec-driven development** ([09](09-spec-driven-development.md)) | `Specs/` (`SpecDocument`, `SpecValidator`, `ISpecStore` file/MCP), `Verification/` (`ProcessCheckRunner`, `AcceptanceVerifier`, `TraceabilityValidator`), strumenti `spec_*`/`run_check`/`spec_verify`/`spec_gate` | [spec.md](../spec.md) |
| **Valutazione / osservabilità** ([10](10-valutazione-osservabilita.md)) | `IExecutionObserver`, `MetricsExecutionObserver` → `RunMetrics` (passi, rework, token/cache, tracciabilità) | [observability.md](../observability.md) |
| **Human-in-the-loop** ([11](11-human-in-the-loop.md)) | Interfacce di approvazione/risoluzione conflitti, strumenti marcati `approvals:` | [agents.md](../agents.md) |
| **Sicurezza / governance** ([12](12-sicurezza-governance.md)) | Allow-list strumenti per agente; esecuzione check **senza shell**, comandi solo da config, sostituzione token-per-token, timeout + kill dell'albero processi | [spec.md](../spec.md) |
| **Standard / protocolli** ([13](13-standard-protocolli.md)) | MCP via `ModelContextProtocol.Core`; spec store opzionale su RAG-over-MCP | [mcp.md](../mcp.md), [spec.md](../spec.md) |

## Due comunicazioni: H2C e naturale

Agora supporta due modalità di comunicazione tra agenti, entrambe basate sui pattern di
[routing](07-workflow-pattern.md):

- **Naturale**: gli agenti usano linguaggio naturale e token `<<signal nome>>` / `<<artifact k=v>>` per
  il routing e la condivisione.
- **H2C**: protocollo compresso `[TYPE:SUBTYPE]` con verdetti nel sottotipo (es. `[STATE:DONE]`,
  `[TEST:PASS]`).

Vedi [h2c.md](../h2c.md) e [graph.md](../graph.md).

## Il filo conduttore: "i gate affermano la realtà"

La scelta architetturale distintiva di Agora — coerente con tutte le fonti di questa cartella — è
spostare le decisioni di completamento dalla **narrazione del modello** ai **controlli deterministici**:
i check eseguono build/test reali, `spec_verify` avanza un requisito a *verificato* solo su un exit code
di successo, e `spec_gate`/`TraceabilityValidator` calcolano un verdetto COMPLETO/INCOMPLETO sulla
tracciabilità. È la materializzazione pratica dello [spec-driven development](09-spec-driven-development.md)
e dei [gate programmatici](07-workflow-pattern.md) di Anthropic.

---

Precedente: [14 — Glossario](14-glossario.md) · Prossimo:
[16 — Microsoft Agent Framework](16-microsoft-agent-framework.md).
