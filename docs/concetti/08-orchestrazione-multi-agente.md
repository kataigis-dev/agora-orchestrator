# 08 — Orchestrazione multi-agente

Quando un singolo agente non basta — perché il problema è cross-dominio, perché gli strumenti sono
troppi, o perché servono confini di sicurezza distinti — si passa a **più agenti specializzati che
coordinano**. Questo capitolo raccoglie i pattern di orchestrazione multi-agente di **Microsoft** e
**Google**, complementari ai workflow di Anthropic visti nel capitolo [07](07-workflow-pattern.md).

## Prima: serve davvero il multi-agente?

Microsoft propone di valutare uno **spettro di complessità** e usare il livello più basso che soddisfa i
requisiti, perché ogni livello aggiunge "overhead di coordinamento, latenza e costi"
([Microsoft, *AI Agent Orchestration Patterns*](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns)):

| Livello | Cos'è | Quando |
|--------|-------|--------|
| Singola chiamata LLM | un prompt, nessuno strumento | compiti banali |
| Agente singolo | un LLM con strumenti e loop | la maggior parte dei casi |
| **Orchestrazione multi-agente** | più agenti che coordinano | problemi cross-dominio, confini di sicurezza distinti, specializzazione parallela |

Google concorda: "le prestazioni di un agente singolo degradano con l'aumentare degli strumenti e della
complessità del compito; considera i sistemi multi-agente per i workflow complessi"
([Google Cloud](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

## I pattern di Microsoft

### Sequential (sequenziale)
Concatena gli agenti in **ordine lineare predefinito**: l'output di uno è l'input del successivo
(pipeline di trasformazioni specializzate). La scelta del prossimo agente è **deterministica**, non
lasciata all'agente. *Sinonimi: pipeline, prompt chaining, linear delegation.*

### Concurrent (concorrente)
Più agenti lavorano **simultaneamente sullo stesso input** da prospettive diverse; i risultati sono poi
aggregati (voto, merge pesato, sintesi via LLM). Gli agenti **non si passano** risultati tra loro.
*Sinonimi: parallel, fan-out/fan-in, scatter-gather, map-reduce.*

### Group chat (chat di gruppo)
Più agenti collaborano in un **thread di conversazione condiviso**, coordinati da un *chat manager* che
decide chi parla. Tipicamente gli agenti sono in **sola lettura** (non modificano sistemi). Ottimo per
brainstorming, dibattito, *quality gate* e supervisione umana. Microsoft consiglia di **limitarsi a tre
o meno agenti** per mantenere il controllo. *Sinonimi: roundtable, multiagent debate, council.*

> **Maker-checker loop**: caso particolare di group chat. Un agente *maker* produce, un agente *checker*
> valuta secondo criteri definiti; se trova lacune rimanda al maker con feedback. Si ripete finché il
> checker approva o si raggiunge il limite di iterazioni. È l'equivalente multi-agente dell'
> *evaluator-optimizer* di Anthropic.

### Handoff (passaggio di consegne)
Ogni agente valuta il compito e decide se **gestirlo o trasferirlo** a un agente più adatto, in modo
dinamico. Solo un agente alla volta opera sull'input; la catena produce un singolo risultato.
*Sinonimi: routing, triage, transfer, dispatch, delegation.*

### Magentic (orchestrazione dinamica)
Per problemi **aperti senza piano predeterminato**. Un *magentic manager* costruisce e raffina
dinamicamente un **task ledger** (registro di obiettivi e sotto-obiettivi) collaborando con agenti
specializzati che usano strumenti per modificare sistemi esterni. Itera, fa *backtracking* e delega
finché il piano è completo, verificando regolarmente se l'obiettivo è raggiunto o in stallo. È
un'estensione del group chat orientata all'azione. *Sinonimi: dynamic orchestration, task-ledger-based,
adaptive planning.*

## I pattern di Google (oltre ai precedenti)

Google ([*Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system))
aggiunge etichette utili:

- **Single-agent**: il punto di partenza — un modello, strumenti definiti, un system prompt completo.
- **Loop**: agenti che ripetono finché una condizione di terminazione è soddisfatta (logica predefinita,
  senza consultare il modello per l'orchestrazione). ⚠️ rischio di loop infiniti.
- **Coordinator**: un agente centrale **decompone e instrada dinamicamente** i sotto-compiti (usa il
  modello per il routing, a differenza del parallel). ≈ orchestrator-workers di Anthropic.
- **Hierarchical task decomposition**: gerarchia a **più livelli**; agenti padre delegano a sotto-agenti
  su più strati. Per problemi ambigui che richiedono pianificazione estesa.
- **Swarm**: agenti che collaborano con comunicazione **all-to-all**, costruendo l'uno sul lavoro
  dell'altro. È "il pattern multi-agente più complesso e costoso"; massima qualità, massimo rischio di
  loop improduttivi.
- **ReAct** e **Human-in-the-loop**: già trattati in [02](02-agenti.md) e [11](11-human-in-the-loop.md).
- **Custom logic**: orchestrazione su misura con logica condizionale, quando nessun pattern standard
  calza.

## Come gli agenti comunicano

Google individua tre meccanismi di interazione tra agenti:

1. **Stato di sessione condiviso** (*shared session state*): gli agenti leggono/scrivono uno stato
   comune;
2. **Delega guidata dal modello** (*model-driven delegation*): il modello instrada i compiti;
3. **Invocazione esplicita**: un agente chiama un altro agente come fosse una funzione/strumento.

A livello di **interoperabilità tra agenti di sistemi diversi** sta emergendo il protocollo **A2A
(Agent-to-Agent)**, complementare a MCP (che collega gli agenti agli strumenti). Vedi
[13 — Standard e protocolli](13-standard-protocolli.md).

## Vantaggi e rischi del multi-agente

| Vantaggi | Rischi |
|----------|--------|
| Specializzazione e contesti più piccoli per agente | Overhead di coordinamento, latenza, costo (più chiamate) |
| Confini di sicurezza distinti per agente | Nuove modalità di fallimento (deadlock, loop) |
| Parallelismo | Difficoltà di debug e osservabilità |
| Riuso di agenti su più workflow | Propagazione di errori tra agenti |

Tutte le fonti convergono: il multi-agente si giustifica **solo** quando un agente singolo non riesce in
modo affidabile, per complessità del prompt, sovraccarico di strumenti o requisiti di sicurezza.

## Tabella riassuntiva dei pattern multi-agente

| Pattern (fonte) | Coordinamento | Tipico uso |
|-----------------|---------------|------------|
| Sequential (MS/Google) | lineare deterministico | pipeline a stadi |
| Concurrent / Parallel (MS/Google) | parallelo + aggregazione | analisi multi-prospettiva |
| Group chat (MS) | thread condiviso + chat manager | dibattito, quality gate |
| Handoff (MS) | trasferimento dinamico | triage/instradamento |
| Magentic (MS) | task ledger dinamico | problemi aperti orientati all'azione |
| Coordinator (Google) | routing dinamico | decomposizione adattiva |
| Hierarchical (Google) | gerarchia multi-livello | pianificazione estesa |
| Swarm (Google) | all-to-all | massima qualità, massima complessità |

---

Precedente: [07 — Pattern di workflow](07-workflow-pattern.md) · Prossimo:
[09 — Spec-driven development](09-spec-driven-development.md).
