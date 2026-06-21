# 02 — Agenti

## Cosa è un agente

Non esiste una definizione unica, ma le fonti convergono. **Anthropic** distingue nettamente due cose
([*Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)):

- **Workflow**: "sistemi in cui LLM e strumenti sono orchestrati attraverso *percorsi di codice
  predefiniti*". Il controllo del flusso è scritto dallo sviluppatore.
- **Agente**: "sistemi in cui gli LLM *dirigono dinamicamente i propri processi e l'uso degli
  strumenti*, mantenendo il controllo su come portano a termine i compiti". In forma essenziale, un
  agente è "*solo un LLM che usa strumenti in un loop sulla base del feedback dell'ambiente*".

**Google** lo formula in modo complementare: gli agenti sono sistemi che "risolvono problemi aperti,
che possono richiedere decisioni autonome e la gestione di workflow complessi multi-step" ed "eccellono
nel risolvere problemi in tempo reale usando dati esterni"
([Google Cloud, *Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

La differenza chiave rispetto a una semplice chiamata a un LLM è la **agency**: l'agente decide *quali*
passi compiere, *quali* strumenti usare e *quando* fermarsi, anziché seguire uno script fisso.

## Il "LLM aumentato": il mattone di base

Anthropic individua come blocco fondamentale l'**augmented LLM**: un modello potenziato con
**retrieval, strumenti e memoria**. Il modello moderno usa attivamente queste capacità — genera da sé
le query di ricerca, sceglie gli strumenti appropriati e decide cosa conservare in memoria.

```
            ┌──────────────────────────────┐
   input ──▶│          LLM aumentato        │──▶ output
            │  ┌──────────┐  ┌────────────┐ │
            │  │ retrieval│  │  strumenti  │ │
            │  └──────────┘  └────────────┘ │
            │         ┌──────────┐          │
            │         │  memoria │          │
            │         └──────────┘          │
            └──────────────────────────────┘
```

## I componenti di un agente

Mettendo insieme Google e Anthropic, un agente si compone di:

| Componente | Ruolo | Approfondimento |
|------------|-------|-----------------|
| **Modello (AI Model)** | Fornisce ragionamento e capacità decisionale | [01](01-fondamenti-llm.md) |
| **System prompt** | Definisce comportamento, persona e vincoli operativi | [01](01-fondamenti-llm.md) |
| **Strumenti (Tools)** | Risorse esterne per raccogliere informazioni o compiere azioni | [03](03-strumenti-function-calling.md) |
| **Memoria** | Conserva informazioni tra i passi e tra le sessioni | [06](06-memoria-contesto.md) |
| **Loop di orchestrazione/ragionamento** | Gestisce il ciclo iterativo *pensa → agisci → osserva* | sotto |

## Il loop di ragionamento (ReAct)

Il pattern canonico con cui un singolo agente opera è **ReAct** (*Reason + Act*): l'agente alterna
in un ciclo iterativo
([Google Cloud, *Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)):

1. **Thought (ragionamento)** — riflette su cosa fare;
2. **Action (azione)** — sceglie uno strumento e ne formula gli argomenti (oppure produce la risposta
   finale);
3. **Observation (osservazione)** — riceve il risultato dello strumento e lo incorpora nel contesto;

ripetendo fino a una **condizione di uscita**. Da qui derivano due esigenze critiche che tutte le fonti
sottolineano: **strumenti progettati con cura** e **condizioni di stop chiare** (altrimenti l'agente
cicla all'infinito o consuma risorse senza concludere).

## Livelli di autonomia: workflow vs agente

Microsoft descrive uno **spettro di complessità**: si va dalla singola chiamata LLM, alla pipeline
deterministica (workflow), fino all'orchestrazione multi-agente realmente autonoma. La regola, condivisa
da tutti, è: **usare il livello di complessità più basso che soddisfa in modo affidabile i requisiti**
([Microsoft, *AI Agent Orchestration Patterns*](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns)).

| | Workflow | Agente autonomo |
|---|----------|-----------------|
| Controllo del flusso | codice predefinito | il modello decide |
| Prevedibilità | alta | minore |
| Adattabilità | bassa | alta |
| Quando preferirlo | compiti ben definiti, ripetibili | problemi aperti, passi non noti a priori |

Anthropic è esplicita: "i workflow spesso offrono migliore prevedibilità e consistenza per compiti ben
definiti", mentre l'autonomia va introdotta solo quando serve flessibilità su larga scala.

## I tre principi di progettazione (Anthropic)

1. **Semplicità** — molte implementazioni di successo sono "*solo una singola chiamata LLM ottimizzata
   con retrieval ed esempi*". Aggiungere agenti e framework ha un costo (latenza, token, debug).
2. **Trasparenza** — mostrare esplicitamente i passi di pianificazione dell'agente.
3. **Cura dell'interfaccia agente-computer (ACI)** — documentare e testare gli strumenti con la stessa
   attenzione che si dedicherebbe a un'interfaccia per esseri umani (vedi
   [03](03-strumenti-function-calling.md)).

> **Da ricordare.** "Inizia semplice; aggiungi complessità solo quando un miglioramento dimostrato la
> giustifica." Questo principio attraversa tutti i capitoli seguenti.

---

Precedente: [01 — Fondamenti](01-fondamenti-llm.md) · Prossimo:
[03 — Strumenti e function calling](03-strumenti-function-calling.md).
