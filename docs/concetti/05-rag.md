# 05 — RAG: Retrieval-Augmented Generation

## Il problema che risolve

Un LLM conosce solo i dati di addestramento fino al suo *knowledge cutoff* e non può vedere i dati
privati o aggiornati di un'organizzazione (vedi [01](01-fondamenti-llm.md)). Riaddestrare il modello è
costoso e lento. Il **RAG** è la tecnica per dare al modello accesso a una **base di conoscenza
autorevole esterna** *al momento della generazione*, senza modificarne i pesi.

> "Il RAG è il processo di ottimizzare l'output di un LLM facendo sì che faccia riferimento a una base
> di conoscenza autorevole, esterna ai suoi dati di addestramento, prima di generare una risposta."
> — [AWS, *What is RAG?*](https://aws.amazon.com/what-is/retrieval-augmented-generation/)

## Come funziona (ad alto livello)

Quando l'utente pone una domanda
([AWS](https://aws.amazon.com/what-is/retrieval-augmented-generation/),
[NVIDIA](https://blogs.nvidia.com/blog/what-is-retrieval-augmented-generation/)):

1. la domanda viene convertita in un **embedding** (vettore numerico);
2. si cercano nel **vector store** i frammenti (chunk) il cui embedding è più vicino a quello della
   domanda — è la **ricerca semantica**;
3. i frammenti recuperati vengono **iniettati nel prompt** insieme alla domanda;
4. l'LLM genera la risposta combinando la propria capacità linguistica con i dati recuperati, e può
   **citare le fonti**.

Il risultato: risposte aggiornate, ancorate a fonti verificabili, con meno allucinazioni.

## La pipeline RAG in dettaglio

### Fase di indicizzazione (offline / *ingest*)

```
documenti ──▶ chunking ──▶ embedding ──▶ vector store
```

- **Chunking**: spezzare documenti grandi in **frammenti** più piccoli e gestibili. La dimensione del
  chunk e la **sovrapposizione** (*overlap*) tra chunk sono parametri chiave: chunk troppo grandi
  diluiscono la pertinenza, troppo piccoli perdono contesto
  ([AWS](https://aws.amazon.com/what-is/retrieval-augmented-generation/)).
- **Embedding**: trasformare ogni chunk nel suo vettore numerico, che ne cattura il significato
  semantico.
- **Vector store** (o *vector database*/*vector index*): il database che memorizza gli embedding e
  permette ricerche di vicinanza efficienti. Esempi: indici in memoria, su file, o servizi dedicati
  (Qdrant, ecc.).

### Fase di interrogazione (online / *retrieval + generation*)

```
domanda ──▶ embedding ──▶ ricerca (top-k) ──▶ [rerank] ──▶ prompt + contesto ──▶ LLM ──▶ risposta
```

- **Top-k**: quanti frammenti recuperare.
- **Score threshold**: soglia minima di similarità per includere un frammento.
- **Reranking** (opzionale): un secondo modello riordina i risultati per pertinenza prima di passarli
  all'LLM, migliorando la precisione.

## Varianti di RAG

| Variante | Idea |
|----------|------|
| **Naive RAG** | La pipeline base descritta sopra: recupera *k* chunk e genera |
| **Advanced RAG** | Aggiunge pre-elaborazione della query, reranking, fusione di più fonti, riscrittura |
| **Agentic RAG** | Il *retrieval* diventa uno **strumento** che l'agente decide quando e come usare, eventualmente iterando più ricerche e ragionando sui risultati |

L'**Agentic RAG** è il punto di contatto con i capitoli sugli agenti: invece di una pipeline fissa, il
recupero è una capacità che l'agente invoca dinamicamente nel suo loop di ragionamento.

## RAG vs fine-tuning vs context window

- **RAG**: per conoscenza **fattuale, dinamica, privata**. Aggiornabile cambiando i documenti, senza
  riaddestrare. Cita le fonti.
- **Fine-tuning**: per insegnare **stile, formato o competenze**, non fatti che cambiano spesso.
- **Contesto lungo**: se i documenti rilevanti sono pochi e piccoli, a volte basta metterli direttamente
  nel prompt. Ma la finestra di contesto ha rendimenti decrescenti (vedi [06](06-memoria-contesto.md)),
  quindi il RAG resta preferibile su grandi corpora.

## Valutare un sistema RAG

Un RAG va valutato su due fronti distinti:

- **Qualità del retrieval**: i frammenti recuperati sono pertinenti? (metriche di precision/recall sul
  recupero).
- **Qualità della generazione**: la risposta è *fedele* (*faithfulness*) ai frammenti recuperati e
  *pertinente* alla domanda? Qui si usano spesso valutatori automatici (vedi
  [10 — Valutazione](10-valutazione-osservabilita.md)).

## RAG come base di conoscenza *scrivibile*

Nei sistemi multi-agente il vector store non è solo in lettura: può diventare una **memoria condivisa
scrivibile**, in cui gli agenti depositano risultati intermedi che altri agenti recuperano. Questo
collega il RAG ai temi di [memoria](06-memoria-contesto.md) e
[orchestrazione](08-orchestrazione-multi-agente.md).

---

Precedente: [04 — MCP](04-mcp.md) · Prossimo: [06 — Memoria e context engineering](06-memoria-contesto.md).
