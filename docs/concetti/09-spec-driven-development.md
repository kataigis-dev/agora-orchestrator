# 09 — Spec-driven development per agenti

## Il problema: la fiducia nella narrazione

Un agente LLM tende a **dichiarare** il successo ("ho implementato la funzione, i test passano") anche
quando non è vero, perché ottimizza la plausibilità del testo, non la verità (vedi
[01](01-fondamenti-llm.md)). Se i *gate* di un workflow si fidano di questa narrazione, il sistema
diventa inaffidabile. Lo **spec-driven development (SDD)** è la risposta: trasformare le decisioni di
controllo da "*il modello dice fatto*" a "*un controllo deterministico afferma la realtà*".

Questo principio è coerente con le raccomandazioni di tutte le fonti:

- Anthropic: inserire **gate programmatici** tra i passi di un workflow per verificare il progresso
  ([*Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)).
- Microsoft: "usa **metriche deterministiche** per i controlli esatti come la correttezza di una
  chiamata a strumento, e l'LLM-as-judge solo per ciò che richiede giudizio"
  ([*AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).
- AWS: le decisioni dell'LLM sono non-deterministiche, quindi servono **guardrail** e confini espliciti
  ([Agentic AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html)).

## Cosa è una specifica macchina-verificabile

Invece di una specifica in testo libero (sepolta nei prompt o in voci RAG sfumate), l'SDD usa una
specifica **strutturata** con queste proprietà:

- **Requisiti con identificatori stabili** (es. `R1`, `R2`), priorità e stato di ciclo di vita
  (proposto → approvato → implementato → verificato).
- **Criteri di accettazione** per ogni requisito: ciò che definisce "fatto", in forma **verificabile a
  macchina** (un test da eseguire, un comando, l'esistenza di un file).
- **Task di implementazione** che **tracciano** verso i requisiti che soddisfano.
- Un **validatore** che impone le invarianti a ogni scrittura (id unici, nessun task senza requisito,
  nessun riferimento pendente).

## I tre cardini dell'SDD

### 1. Criteri di accettazione eseguibili
Ogni criterio è legato a un **controllo** (*check*) che può essere eseguito davvero: un test, un comando
di build, la verifica dell'esistenza di un artefatto. Il verdetto deriva dall'**exit code** del
processo, non dal giudizio del modello.

### 2. Esecuzione reale dei controlli
Build e test vengono eseguiti come **processi reali** (con le dovute cautele di sicurezza:
allow-list di comandi, nessuna shell, timeout — vedi [12](12-sicurezza-governance.md)). Un requisito
passa a "verificato" **solo** quando i suoi controlli passano in modo deterministico.

### 3. Tracciabilità e gate di completamento
Si impone la **tracciabilità requisito ↔ task ↔ test**: nessun task senza requisito, nessun requisito
approvato senza un task che lo copra, nessun requisito "verificato" senza un controllo reale che lo
sostenga. Il **completamento** del lavoro è un verdetto deterministico (COMPLETO/INCOMPLETO) calcolato
sulla matrice di tracciabilità, non una dichiarazione dell'agente.

```
Requisito R1 ──coperto da──▶ Task T1 ──evidenza──▶ check "test" (exit 0) ✓ verificato
Requisito R2 ──coperto da──▶ Task T2 ──evidenza──▶ check "build" (exit 1) ✗ NON verificato  ⟹ INCOMPLETO
```

## Perché è un pattern di affidabilità

L'SDD combina due pattern già visti in forma "indurita":

- l'**evaluator-optimizer** / **maker-checker** ([07](07-workflow-pattern.md),
  [08](08-orchestrazione-multi-agente.md)), ma con il valutatore **deterministico** (un processo, non un
  LLM);
- i **gate programmatici** del prompt chaining, applicati all'intera specifica.

Il risultato è un workflow agentico in cui "*i gate affermano la realtà*": la qualità non dipende
dall'auto-valutazione del modello.

## Legame con il progetto

Agora Orchestrator implementa l'SDD in tre fasi (schema strutturato → verifica reale → gate di
tracciabilità). I dettagli tecnici sono in [`../spec.md`](../spec.md); la mappatura concettuale è in
[15 — Mappatura su Agora](15-mappatura-agora.md).

---

Precedente: [08 — Orchestrazione multi-agente](08-orchestrazione-multi-agente.md) · Prossimo:
[10 — Valutazione e osservabilità](10-valutazione-osservabilita.md).
