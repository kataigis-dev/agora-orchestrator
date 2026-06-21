# 16 — Microsoft Agent Framework

Questo capitolo approfondisce un **framework concreto**, il **Microsoft Agent Framework**, sia perché è
una delle implementazioni di riferimento dei concetti visti finora, sia perché Agora Orchestrator vi si
appoggia (lo strato `Agora.AgentFramework` usa **Microsoft.Extensions.AI** e **Microsoft.Agents.AI**).

## Cos'è

Il **Microsoft Agent Framework** è un **SDK e runtime open source** per costruire **agenti** e
**workflow multi-agente** in **.NET e Python**
([Microsoft Learn, *Agent Framework Overview*](https://learn.microsoft.com/en-us/agent-framework/overview/)).
È il **successore diretto** di due progetti Microsoft precedenti, creato dagli stessi team:

- **Semantic Kernel** — le fondamenta *enterprise*: gestione dello stato basata su sessioni, type
  safety, *middleware*/filtri, telemetria, ampio supporto a modelli ed embedding;
- **AutoGen** (Microsoft Research) — le astrazioni semplici per pattern single- e multi-agente e
  l'orchestrazione.

In sintesi (parole di Microsoft): "Agent Framework combina le astrazioni semplici di AutoGen con le
funzionalità enterprise di Semantic Kernel — gestione dello stato per sessione, type safety, middleware,
telemetria — e aggiunge **workflow basati su grafo** per l'orchestrazione multi-agente esplicita"
([Microsoft Foundry blog](https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/)).

## Le due categorie di capacità

Il framework offre due categorie principali, che ricalcano esattamente la distinzione
[workflow vs agente](02-agenti.md):

| Categoria | Cos'è |
|-----------|-------|
| **Agents** | Singoli agenti che usano un LLM per elaborare input, chiamare [strumenti](03-strumenti-function-calling.md) e [server MCP](04-mcp.md), e generare risposte. Provider supportati: Microsoft Foundry, Azure OpenAI, OpenAI, Anthropic, Ollama e altri |
| **Workflows** | Workflow **basati su grafo** che collegano agenti e funzioni per compiti multi-step, con **routing type-safe**, **checkpointing** e supporto **human-in-the-loop** |

### Quando usare un agente e quando un workflow

La guida ufficiale è netta (e coincide con il capitolo [02](02-agenti.md)):

| Usa un **agente** quando… | Usa un **workflow** quando… |
|---------------------------|-----------------------------|
| il compito è aperto o conversazionale | il processo ha passi ben definiti |
| serve uso autonomo di strumenti e pianificazione | serve controllo esplicito sull'ordine di esecuzione |
| basta una singola chiamata LLM (con strumenti) | più agenti o funzioni devono coordinarsi |

> Massima di progettazione di Microsoft, perfettamente allineata al principio di **semplicità** di
> Anthropic: *"se puoi scrivere una funzione per gestire il compito, fallo invece di usare un agente
> IA."*

## Blocchi di costruzione fondamentali

Oltre ad agenti e workflow, il framework fornisce
([Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/overview/)):

- **Model client** (chat completions e responses);
- **Agent session** per la gestione dello **stato**;
- **Context provider** per la **memoria** dell'agente (vedi [06](06-memoria-contesto.md));
- **Middleware** per **intercettare** le azioni dell'agente (logging, guardrail, filtri);
- **MCP client** per l'integrazione degli strumenti (vedi [04](04-mcp.md)).

## Pattern di orchestrazione supportati

Il framework implementa i pattern multi-agente descritti nel capitolo [08](08-orchestrazione-multi-agente.md)
([Microsoft Foundry blog](https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/)):

- **Sequential** (sequenziale);
- **Concurrent** (concorrente / parallelo);
- **Group chat** (chat di gruppo, incl. *maker-checker*);
- **Handoff** (passaggio dinamico);
- **Magentic** (manager con *task ledger*).

## Connettività, interoperabilità e robustezza

| Area | Funzionalità |
|------|--------------|
| **Strumenti** | **MCP** per la scoperta dinamica degli strumenti |
| **Inter-agente** | **A2A** (*Agent-to-Agent*) per la collaborazione cross-runtime (vedi [13](13-standard-protocolli.md)) |
| **Integrazione** | design *OpenAPI-first*; connettori enterprise (Azure AI Foundry, Microsoft Graph, SharePoint, ecc.) |
| **Memoria** | memoria *pluggable* su più backend (Redis, Postgres, Elasticsearch, …) |
| **Osservabilità** | **OpenTelemetry** integrato (vedi [10](10-valutazione-osservabilita.md)) |
| **Durabilità** | esecuzioni long-running con **checkpointing** e **pause/resume** |
| **Governance** | workflow di **approvazione human-in-the-loop** (vedi [11](11-human-in-the-loop.md)) |

## Relazione con Azure AI Foundry

Il framework si integra con **Azure AI Foundry Agent Service** per l'hosting cloud sicuro (integrazione
con virtual network, controllo accessi basato sui ruoli, compliance enterprise). Azure AI Foundry usa
Semantic Kernel come motore di orchestrazione interno, di cui Agent Framework è l'evoluzione.

## Esempio minimo (.NET)

```csharp
using Microsoft.Agents.AI;
// ...
AIAgent agent = new AIProjectClient(endpoint, new AzureCliCredential())
    .AsAIAgent(model: "gpt-5.4-mini",
               instructions: "You are a friendly assistant. Keep your answers brief.");

Console.WriteLine(await agent.RunAsync("What is the largest city in France?"));
```

## Una nota su responsabilità e sistemi di terze parti

Microsoft avverte esplicitamente: usando Agent Framework con server, agenti, codice o modelli **non
Azure** ("Third-Party Systems"), lo si fa **a proprio rischio**; tali sistemi sono governati dai
rispettivi termini di licenza. È responsabilità dello sviluppatore rivedere i dati scambiati, gestire i
confini di compliance/geografici e implementare le proprie mitigazioni di **IA responsabile**
(metaprompt, content filter, sistemi di sicurezza). Vedi anche
[12 — Sicurezza e governance](12-sicurezza-governance.md).

## Relazione con Agora Orchestrator

Agora **non è** il Microsoft Agent Framework, ma vi si appoggia nel suo strato di integrazione:

- il core `Agora` resta **framework-free** (solo BCL + YamlDotNet): modelli, interfacce, grafo,
  validatori e verifica vivono qui;
- `Agora.AgentFramework` costruisce gli agenti concreti su **Microsoft.Extensions.AI** /
  **Microsoft.Agents.AI** e i client MCP su **ModelContextProtocol.Core**, mantenendo il resto del
  sistema indipendente dal framework.

Questa separazione è una scelta di design: si beneficia dell'ecosistema Microsoft (astrazioni di agente,
function calling, client MCP) senza accoppiare il motore di orchestrazione a un singolo fornitore. Vedi
[15 — Mappatura su Agora](15-mappatura-agora.md) e [architecture.md](../architecture.md).

---

Precedente: [15 — Mappatura su Agora](15-mappatura-agora.md) · Prossimo: [99 — Fonti](99-fonti.md).
