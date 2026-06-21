# 10 — Valutazione e osservabilità

Un sistema agentico non si giudica da una singola risposta riuscita: va **misurato** in modo sistematico
(valutazione) e **osservato** in produzione (osservabilità). Sono due facce della stessa esigenza —
sapere se il sistema è *affidabile*, non solo *capace*.

## Valutazione (evals)

### Due famiglie di metriche

La distinzione fondamentale, ribadita da Microsoft e dalla letteratura sugli eval
([Microsoft, *AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)):

| Tipo | Quando usarla | Esempi |
|------|---------------|--------|
| **Metriche deterministiche** | Controlli esatti, oggettivi | la chiamata a strumento è corretta? il test passa? il JSON è valido? il task è completato? |
| **LLM-as-judge** | Criteri che richiedono giudizio o contesto | la risposta è pertinente? fedele alle fonti? di tono adeguato? |

Regola pratica: **preferire sempre i controlli deterministici** dove possibile; ricorrere all'LLM-judge
solo per ciò che non è verificabile a macchina. È lo stesso principio dello
[spec-driven development](09-spec-driven-development.md).

### LLM-as-judge

L'**LLM-as-judge** usa un modello per valutare gli output di un altro: permette di automatizzare
controlli di qualità che altrimenti richiederebbero revisione umana, valutando migliaia di output in
pochi minuti e segnalando allucinazioni o risposte fuori tema. Buone pratiche
([Microsoft](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/), e fonti
correlate):

- **rubriche chiare** e prompt strutturati per il giudice;
- **output vincolati** (es. JSON) per ridurre l'ambiguità;
- *score smoothing* e calibrazione contro etichette umane;
- non usarlo per ciò che un controllo esatto può verificare meglio.

### Cosa valutare in un agente

Oltre alla qualità della risposta finale, gli agenti richiedono metriche specifiche:

- **Correttezza nell'uso degli strumenti** (*tool calling*): ha chiamato lo strumento giusto con gli
  argomenti giusti?
- **Completamento del compito** (*task completion*): l'obiettivo è stato raggiunto?
- **Qualità del ragionamento** e della **traiettoria**: i passi intermedi erano sensati?
- **Valutazione basata sulla traccia** (*trace-based*): si valuta l'intera traiettoria, non solo il
  risultato (qui si inserisce l'idea di *agent-as-a-judge*, un agente che valuta un altro osservandone i
  passi intermedi).

## Osservabilità

L'obiettivo è **strumentare** il codice dell'agente perché emetta **tracce** e **metriche** raccoglibili
da una piattaforma di osservabilità. **OpenTelemetry** sta emergendo come standard di settore per
l'osservabilità degli LLM
([Microsoft](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).

AWS sottolinea che osservabilità e auditabilità vanno **progettate fin dall'inizio**, non aggiunte dopo:
Amazon Bedrock AgentCore Observability, ad esempio, abilita il monitoraggio in tempo reale tracciando
**latenza, uso dei token e tassi di errore**
([AWS, Agentic AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html)).

### Metriche di affidabilità tipiche di un run agentico

| Segnale | Cosa indica |
|---------|-------------|
| **Completamento** | il run ha raggiunto la fine o si è interrotto? |
| **Rework / loop** | quante volte il grafo è tornato indietro a rifare il lavoro (proxy di instabilità) |
| **Distribuzione del lavoro** | dove si è concentrato lo sforzo (visite per nodo) |
| **Costo** | token in ingresso/uscita, hit di cache del prompt |
| **Verifica** | quanta parte dell'ambito è stata effettivamente verificata (vedi SDD) |

Queste metriche permettono di **confrontare configurazioni** (un grafo più snello vs la pipeline
completa con gate; memoria attiva vs disattiva) invece di giudicare un run dalla sola risposta finale.

## Il legame con il progetto

Agora Orchestrator separa l'esecuzione dalla presentazione tramite *observer* e aggrega gli eventi in
metriche di run (passi, rework, token/cache, tracciabilità della specifica). Dettagli in
[`../observability.md`](../observability.md).

---

Precedente: [09 — Spec-driven development](09-spec-driven-development.md) · Prossimo:
[11 — Human-in-the-loop](11-human-in-the-loop.md).
