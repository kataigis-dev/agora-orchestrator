# 06 — Memoria e context engineering

## Dal prompt engineering al context engineering

Quando i sistemi erano singole chiamate a un LLM, l'arte era scrivere il **prompt** giusto. Con gli
agenti, che accumulano cronologia, risultati di strumenti e memoria su molti passi, il problema diventa
più ampio: **gestire l'intera finestra di contesto nel tempo**. Anthropic chiama questa disciplina
**context engineering**:

> "Il context engineering è l'arte e la scienza di riempire la finestra di contesto con *esattamente le
> informazioni giuste* a ogni passo della traiettoria di un agente."
> — [Anthropic, *Effective context engineering for AI agents*](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)

Il principio guida: il contesto è **finito e con rendimenti marginali decrescenti**. L'obiettivo è
trovare "il più piccolo insieme di token ad alto segnale che massimizza la probabilità del risultato
desiderato". Più testo non è meglio: oltre una soglia, il modello si confonde (*context rot*).

## Come strutturare il contesto

Anthropic raccomanda un ordine preciso, perché "la disposizione e la qualità del contesto determinano le
prestazioni dell'agente più di qualsiasi altro fattore":

1. **Istruzioni di sistema** (ruolo, vincoli, formato);
2. **Memoria rilevante** (solo ciò che serve ora);
3. **Definizioni degli strumenti**;
4. **Cronologia della conversazione**.

## Memoria: breve e lungo termine

| Tipo | Cosa contiene | Dove vive |
|------|---------------|-----------|
| **Memoria di lavoro / breve termine** | Lo stato della traiettoria corrente (cronologia, osservazioni recenti) | nella finestra di contesto |
| **Memoria a lungo termine** | Fatti, decisioni, artefatti da conservare tra i passi e tra le sessioni | in uno **store esterno** (file, DB, vector store) da cui si recupera al bisogno |

La memoria a lungo termine si appoggia spesso alle stesse tecnologie del [RAG](05-rag.md): si scrive in
uno store e si recupera per similarità solo il sottoinsieme pertinente, comprimendo i token.

## Strategie di gestione del contesto

Dalle indicazioni di Anthropic
([*Effective context engineering*](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents),
[Claude Cookbook](https://platform.claude.com/cookbook/tool-use-context-engineering-context-engineering-tools)):

- **Compattazione** (*compaction*): quando la cronologia cresce, riassumerla in una forma più densa,
  conservando le decisioni e gli artefatti chiave e scartando il rumore.
- **Note strutturate** (*structured note-taking*): l'agente scrive note in uno store esterno (es. un
  file `NOTES.md`) e le rilegge quando servono, "mantenendo solo il necessario nella memoria di lavoro".
  Permette di seguire lavori lunghi senza tenere tutto nel contesto attivo.
- **Context editing / pruning**: rimuovere dal contesto, secondo regole, ciò che non serve più (es.
  output di strumenti ormai consumati).
- **Context awareness**: dare all'agente un segnale sulla capacità residua del contesto, così che possa
  decidere quando compattare.
- **Recupero selettivo (just-in-time)**: invece di precaricare tutto, l'agente recupera le informazioni
  *quando* gli servono, assemblando la comprensione "strato per strato".

## Il legame con i sistemi multi-agente

Un motivo strutturale per cui si passa a più agenti è proprio la **gestione del contesto**: suddividere
un problema tra agenti specializzati significa che ciascuno lavora con una finestra di contesto più
piccola e mirata, invece di un unico agente che annega in un contesto enorme. Microsoft cita
esplicitamente il *prompt complexity* e il *tool overload* come ragioni per passare al multi-agente
([Microsoft](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns)).
Il rovescio della medaglia è che gli agenti devono **scambiarsi solo il contesto minimo necessario** per
non riprodurre lo stesso problema a livello di sistema (vedi [08](08-orchestrazione-multi-agente.md)).

---

Precedente: [05 — RAG](05-rag.md) · Prossimo: [07 — Pattern di workflow](07-workflow-pattern.md).
