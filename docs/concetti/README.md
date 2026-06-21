# Concetti e fondamenti dei sistemi agentici

Questa cartella raccoglie la **documentazione concettuale** del progetto Agora Orchestrator, in
italiano. È una base di conoscenza autonoma: parte dalle definizioni di base (cosa è un *large language
model*, cosa è un *agente*, cosa è il *RAG*) e arriva agli **standard e ai pattern per i workflow
agentici** adottati dall'industria.

Le nozioni sono tratte e sintetizzate da fonti primarie affidabili — **Anthropic, Google, Microsoft e
Amazon (AWS)** — oltre alle specifiche aperte (Model Context Protocol, OpenTelemetry). Ogni capitolo
cita le fonti puntuali; l'elenco completo è in [99-fonti.md](99-fonti.md).

> Questa cartella è **concettuale e indipendente dal codice**. Per la documentazione tecnica di Agora
> Orchestrator (configurazione YAML, CLI, architettura del codice) vedi la cartella
> [`../`](../index.md). Il capitolo [15-mappatura-agora.md](15-mappatura-agora.md) collega ogni concetto
> alla sua implementazione concreta nel progetto.

## Indice

| # | Capitolo | Contenuto |
|---|----------|-----------|
| 01 | [Fondamenti: LLM e prompting](01-fondamenti-llm.md) | Modelli linguistici, token, finestra di contesto, prompt, temperatura, allucinazioni, embedding |
| 02 | [Agenti](02-agenti.md) | Definizione di agente, LLM aumentato, componenti, loop di ragionamento (ReAct), autonomia, *workflow vs agente* |
| 03 | [Strumenti e function calling](03-strumenti-function-calling.md) | Tool use, function calling, interfaccia agente-computer (ACI), progettazione e approvazione degli strumenti |
| 04 | [Model Context Protocol (MCP)](04-mcp.md) | Standard aperto per collegare agenti a dati e strumenti: architettura, primitive, trasporti |
| 05 | [RAG — Retrieval-Augmented Generation](05-rag.md) | Pipeline completa (ingest, chunking, embedding, vector store, retrieval, generazione), varianti, valutazione |
| 06 | [Memoria e context engineering](06-memoria-contesto.md) | Gestione del contesto, memoria a breve/lungo termine, compattazione, note strutturate |
| 07 | [Pattern di workflow agentici](07-workflow-pattern.md) | I pattern di Anthropic: prompt chaining, routing, parallelizzazione, orchestrator-workers, evaluator-optimizer |
| 08 | [Orchestrazione multi-agente](08-orchestrazione-multi-agente.md) | Pattern multi-agente di Microsoft e Google, livello di complessità, protocollo A2A |
| 09 | [Spec-driven development](09-spec-driven-development.md) | Specifiche macchina-verificabili, gate deterministici, tracciabilità requisito↔task↔test |
| 10 | [Valutazione e osservabilità](10-valutazione-osservabilita.md) | Eval, metriche deterministiche vs LLM-as-judge, tracce, OpenTelemetry, metriche di affidabilità |
| 11 | [Human-in-the-loop](11-human-in-the-loop.md) | Supervisione umana, approvazioni, checkpoint |
| 12 | [Sicurezza e governance](12-sicurezza-governance.md) | AWS Well-Architected Agentic AI Lens, least privilege, guardrail, SAIF, Responsible AI |
| 13 | [Standard e protocolli](13-standard-protocolli.md) | MCP, A2A, OpenTelemetry GenAI — riepilogo degli standard emergenti |
| 14 | [Glossario](14-glossario.md) | Definizioni rapide dei termini |
| 15 | [Mappatura su Agora Orchestrator](15-mappatura-agora.md) | Dove ogni concetto vive nel progetto |
| 16 | [Microsoft Agent Framework](16-microsoft-agent-framework.md) | Il framework open source di Microsoft (agenti + workflow), su cui poggia lo strato di integrazione di Agora |
| 99 | [Fonti](99-fonti.md) | Bibliografia completa con URL |

## Come leggere

- Se parti da zero: leggi i capitoli in ordine (01 → 15).
- Se conosci già gli LLM: salta al capitolo [02 Agenti](02-agenti.md).
- Se ti interessano i **pattern di orchestrazione**: capitoli [07](07-workflow-pattern.md) e
  [08](08-orchestrazione-multi-agente.md) sono il cuore degli standard per i workflow agentici.
- Se vuoi vedere il legame con il codice: [15-mappatura-agora.md](15-mappatura-agora.md).

## Una nota sulla terminologia

Il settore è giovane e i termini non sono ancora del tutto standardizzati: lo stesso pattern ha nomi
diversi a seconda del fornitore (ad esempio *routing*, *handoff*, *triage* e *dispatch* indicano spesso
la stessa idea). Dove utile, riportiamo i sinonimi. La regola pratica, ripetuta da tutte le fonti, è
**iniziare dalla soluzione più semplice** e aggiungere complessità solo quando un guadagno misurabile
la giustifica.
