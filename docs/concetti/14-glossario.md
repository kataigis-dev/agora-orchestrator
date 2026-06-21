# 14 — Glossario

Definizioni rapide dei termini ricorrenti. Per gli approfondimenti, i rimandi ai capitoli.

| Termine | Definizione |
|---------|-------------|
| **Agente** (*agent*) | Sistema in cui un LLM dirige dinamicamente i propri passi e l'uso di strumenti in un loop. → [02](02-agenti.md) |
| **Agentic RAG** | RAG in cui il recupero è uno strumento che l'agente decide quando usare. → [05](05-rag.md) |
| **Allucinazione** | Affermazione fluente ma falsa generata da un LLM. → [01](01-fondamenti-llm.md) |
| **A2A** (*Agent-to-Agent*) | Protocollo per la comunicazione tra agenti di runtime diversi. → [13](13-standard-protocolli.md) |
| **ACI** (*Agent-Computer Interface*) | L'interfaccia con cui un agente usa gli strumenti; va curata come una UI. → [03](03-strumenti-function-calling.md) |
| **Chunking** | Spezzare i documenti in frammenti per l'indicizzazione RAG. → [05](05-rag.md) |
| **Context engineering** | Disciplina di riempire la finestra di contesto con esattamente le informazioni giuste. → [06](06-memoria-contesto.md) |
| **Context window** (*finestra di contesto*) | Numero massimo di token gestibili in una chiamata; risorsa finita. → [01](01-fondamenti-llm.md) |
| **Embedding** | Rappresentazione vettoriale del significato di un testo. → [01](01-fondamenti-llm.md), [05](05-rag.md) |
| **Evaluator-optimizer** | Pattern: un LLM genera, un altro valuta e dà feedback, si itera. → [07](07-workflow-pattern.md) |
| **Function calling** (*tool use*) | Meccanismo con cui il modello richiede l'esecuzione di uno strumento. → [03](03-strumenti-function-calling.md) |
| **Gate** | Controllo (idealmente deterministico) tra i passi di un workflow. → [09](09-spec-driven-development.md) |
| **Guardrail** | Vincolo che limita il comportamento di un agente indipendentemente dall'input. → [12](12-sicurezza-governance.md) |
| **Handoff** | Trasferimento dinamico del compito a un agente più adatto (≈ routing). → [08](08-orchestrazione-multi-agente.md) |
| **HITL** (*Human-in-the-loop*) | Supervisione umana inserita in punti di controllo del workflow. → [11](11-human-in-the-loop.md) |
| **LLM** | Modello linguistico di grandi dimensioni; predice il token successivo. → [01](01-fondamenti-llm.md) |
| **LLM-as-judge** | Usare un LLM per valutare gli output di un altro. → [10](10-valutazione-osservabilita.md) |
| **Magentic** | Orchestrazione dinamica guidata da un *task ledger* costruito da un manager. → [08](08-orchestrazione-multi-agente.md) |
| **Maker-checker** | Loop in cui un agente produce e un altro verifica secondo criteri. → [08](08-orchestrazione-multi-agente.md) |
| **MCP** (*Model Context Protocol*) | Standard aperto per collegare agenti a strumenti/dati. → [04](04-mcp.md) |
| **Orchestrator-workers** | Un LLM scompone dinamicamente il compito e delega a worker. → [07](07-workflow-pattern.md) |
| **Prompt** | Testo in ingresso al modello (system + utente + contesto). → [01](01-fondamenti-llm.md) |
| **Prompt chaining** | Concatenazione sequenziale di chiamate LLM con gate. → [07](07-workflow-pattern.md) |
| **Prompt injection** | Attacco che inietta istruzioni ostili nei dati letti dall'agente. → [12](12-sicurezza-governance.md) |
| **RAG** (*Retrieval-Augmented Generation*) | Ancorare la generazione a una base di conoscenza esterna. → [05](05-rag.md) |
| **ReAct** (*Reason + Act*) | Loop pensa→agisci→osserva di un singolo agente. → [02](02-agenti.md) |
| **Reranking** | Riordinare i risultati del retrieval per pertinenza. → [05](05-rag.md) |
| **Routing** | Instradare l'input al processo specializzato adatto. → [07](07-workflow-pattern.md) |
| **Spec-driven development** | Specifiche macchina-verificabili con gate deterministici. → [09](09-spec-driven-development.md) |
| **System prompt** | Istruzioni persistenti che definiscono ruolo e vincoli. → [01](01-fondamenti-llm.md) |
| **Temperature** | Parametro che controlla la casualità della generazione. → [01](01-fondamenti-llm.md) |
| **Token** | Unità con cui il modello legge/scrive; base di costo e latenza. → [01](01-fondamenti-llm.md) |
| **Tool** (*strumento*) | Funzione che l'agente può invocare per osservare o agire. → [03](03-strumenti-function-calling.md) |
| **Tracciabilità** | Legame requisito↔task↔test verificabile a macchina. → [09](09-spec-driven-development.md) |
| **Vector store** | Database di embedding per la ricerca semantica. → [05](05-rag.md) |
| **Workflow** | Sistema in cui il flusso è orchestrato da codice predefinito. → [02](02-agenti.md), [07](07-workflow-pattern.md) |

---

Precedente: [13 — Standard e protocolli](13-standard-protocolli.md) · Prossimo:
[15 — Mappatura su Agora](15-mappatura-agora.md).
