# 07 — Pattern di workflow agentici

Questo è uno dei due capitoli centrali sugli **standard per i workflow agentici**. Qui trattiamo i
pattern in cui il flusso è **orchestrato da codice predefinito** (workflow), seguendo la tassonomia di
riferimento di **Anthropic**
([*Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)). Il
capitolo [08](08-orchestrazione-multi-agente.md) tratta invece l'orchestrazione *multi-agente*.

> Promemoria di metodo (Anthropic): **inizia dalla soluzione più semplice**. Molti casi si risolvono con
> una singola chiamata LLM aumentata con retrieval ed esempi. Introduci i pattern seguenti solo quando
> servono davvero.

## 1. Prompt chaining (concatenazione di prompt)

Scompone un compito in **passi sequenziali**, dove ogni chiamata LLM elabora l'output della precedente.
Tra un passo e l'altro si possono inserire **gate programmatici** che verificano il progresso.

```
input ─▶ LLM 1 ─▶ [gate] ─▶ LLM 2 ─▶ [gate] ─▶ LLM 3 ─▶ output
```

- **Quando**: il compito si scompone in modo pulito in sotto-compiti fissi.
- **Compromesso**: scambia *latenza* per *accuratezza* (più passi, ma ciascuno più semplice e
  controllabile).
- Microsoft chiama lo stesso schema **sequential orchestration** / *pipeline* quando applicato ad agenti.

## 2. Routing

Un classificatore **instrada** l'input verso il processo specializzato più adatto.

```
                 ┌─▶ gestore A
input ─▶ router ─┼─▶ gestore B
                 └─▶ gestore C
```

- **Quando**: esistono categorie distinte che è meglio gestire separatamente, e la classificazione è
  affidabile.
- **Vantaggio**: separazione delle responsabilità e prompt ottimizzati per ciascun caso.
- Sinonimi nelle altre fonti: *handoff*, *triage*, *dispatch*, *coordinator*.

## 3. Parallelization (parallelizzazione)

Esegue più chiamate **in parallelo** e ne aggrega i risultati. Due varianti:

- **Sectioning**: si suddivide il compito in sotto-compiti *indipendenti* eseguiti simultaneamente.
- **Voting**: si esegue *lo stesso* compito più volte per ottenere prospettive diverse e poi si sceglie
  (es. voto di maggioranza), aumentando la confidenza.

```
            ┌─▶ LLM ─┐
input ─▶ split ─▶ LLM ─┼─▶ aggregazione ─▶ output
            └─▶ LLM ─┘
```

- **Quando**: per guadagnare velocità (sectioning) o affidabilità (voting).
- Microsoft lo chiama **concurrent orchestration** (*fan-out/fan-in*, *scatter-gather*, *map-reduce*);
  l'aggregazione può essere voto, merge pesato o sintesi via LLM.

## 4. Orchestrator–workers (orchestratore-lavoratori)

Un LLM **orchestratore** scompone *dinamicamente* il compito e delega a LLM **worker**, poi ne sintetizza
i risultati.

```
                 ┌─▶ worker ─┐
input ─▶ orchestratore ─▶ worker ─┼─▶ sintesi ─▶ output
                 └─▶ worker ─┘
```

- **Differenza dalla parallelizzazione**: i sotto-compiti **non sono predefiniti**; è l'orchestratore a
  determinarli in base all'input specifico. Questo lo rende più flessibile (e meno prevedibile).
- **Quando**: compiti la cui scomposizione non è nota a priori (es. modifiche a un numero variabile di
  file).
- Equivalenti multi-agente: **coordinator** (Google) e **hierarchical** quando i livelli sono più di
  uno.

## 5. Evaluator–optimizer (valutatore-ottimizzatore)

Un LLM **genera** una risposta; un secondo LLM la **valuta** e fornisce feedback; il primo **rivede**.
Il ciclo si ripete finché un criterio è soddisfatto (o si raggiunge un limite di iterazioni).

```
                ┌───────── feedback ─────────┐
                ▼                             │
input ─▶ generatore ─▶ risposta ─▶ valutatore ─┴─▶ (ok) ─▶ output
```

- **Quando**: esistono **criteri di valutazione chiari** e le risposte migliorano dimostrabilmente con il
  feedback.
- Nelle altre fonti: **maker-checker loop** / **review-and-critique** (Microsoft, Google),
  **iterative refinement**.
- ⚠️ Richiede una **condizione di uscita** (qualità soglia o *max iterazioni*) per non ciclare
  all'infinito.

## Workflow vs agente autonomo (di nuovo)

I cinque pattern sopra sono **workflow**: il percorso è cablato nel codice. Oltre questi c'è l'**agente
autonomo**, che decide da sé la sequenza di azioni nel loop ReAct (vedi [02](02-agenti.md)). Anthropic
ribadisce: per compiti ben definiti i workflow offrono migliore prevedibilità; l'autonomia si introduce
quando la flessibilità è davvero necessaria.

## Tabella riassuntiva

| Pattern | Flusso | Quando | Sinonimi |
|---------|--------|--------|----------|
| Prompt chaining | sequenziale fisso | scomposizione pulita | pipeline, sequential |
| Routing | ramificazione su classificazione | categorie distinte | handoff, triage, dispatch |
| Parallelization | parallelo + aggregazione | velocità o confidenza | concurrent, fan-out/fan-in |
| Orchestrator–workers | delega dinamica | sotto-compiti non noti a priori | coordinator, hierarchical |
| Evaluator–optimizer | generazione + critica iterativa | criteri chiari di qualità | maker-checker, review-critique |

---

Precedente: [06 — Memoria e contesto](06-memoria-contesto.md) · Prossimo:
[08 — Orchestrazione multi-agente](08-orchestrazione-multi-agente.md).
