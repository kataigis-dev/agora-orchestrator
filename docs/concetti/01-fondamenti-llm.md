# 01 — Fondamenti: LLM e prompting

Prima di parlare di agenti serve un vocabolario condiviso sui **modelli linguistici di grandi
dimensioni** (LLM, *Large Language Models*), il mattone su cui tutto il resto è costruito.

## Cos'è un LLM

Un LLM è un modello di rete neurale addestrato su enormi quantità di testo a **predire il token
successivo** data una sequenza di token precedenti. Da questa capacità apparentemente semplice
emergono comportamenti complessi: rispondere a domande, riassumere, tradurre, scrivere codice,
ragionare passo-passo. Un LLM non "consulta un database" quando risponde: genera testo plausibile sulla
base degli schemi statistici appresi durante l'addestramento. Questo ha due conseguenze fondamentali:

1. **Conoscenza congelata.** Il modello conosce solo ciò che era nei dati di addestramento fino a una
   certa data (*knowledge cutoff*). Non conosce eventi successivi né dati privati/aziendali. Questo è il
   problema che il [RAG](05-rag.md) risolve.
2. **Allucinazioni.** Il modello può generare affermazioni fluenti ma false, perché ottimizza la
   plausibilità linguistica, non la verità. Per questo i sistemi seri **verificano** gli output invece
   di fidarsi (vedi [valutazione](10-valutazione-osservabilita.md) e
   [spec-driven development](09-spec-driven-development.md)).

## Token e finestra di contesto

- **Token**: l'unità con cui il modello legge e scrive. Un token è circa 3–4 caratteri in inglese (un
  po' di più in italiano); "orchestrazione" può valere più token. Il costo e la latenza di una chiamata
  dipendono dal numero di token in ingresso e in uscita.
- **Finestra di contesto** (*context window*): il numero massimo di token che il modello può
  considerare in una singola chiamata (prompt + risposta). È una risorsa **finita e con rendimenti
  decrescenti**: oltre una certa quantità, aggiungere testo degrada la qualità invece di migliorarla.
  Gestire bene questo spazio è il tema del [context engineering](06-memoria-contesto.md).

## Il prompt

Il **prompt** è il testo in ingresso. Si distinguono di solito:

- **System prompt** (*istruzioni di sistema*): definisce ruolo, comportamento, vincoli e formato di
  risposta dell'assistente. È persistente per tutta la conversazione.
- **Messaggi utente / assistente**: il dialogo vero e proprio.
- **Contesto aggiuntivo**: documenti recuperati (RAG), risultati di strumenti, memoria.

Anthropic raccomanda un ordine preciso del contesto — **prima le istruzioni di sistema, poi la memoria
rilevante, poi le definizioni degli strumenti, infine la cronologia** — perché "la disposizione e la
qualità di questo contesto determinano le prestazioni dell'agente più di qualsiasi altro fattore"
([Anthropic, *Effective context engineering*](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)).

### Tecniche di prompting comuni

| Tecnica | Idea |
|---------|------|
| *Zero-shot* | Si chiede direttamente, senza esempi |
| *Few-shot* | Si forniscono alcuni esempi del comportamento desiderato |
| *Chain-of-thought* | Si chiede al modello di "ragionare passo per passo" prima di rispondere |
| *Structured output* | Si vincola la risposta a un formato (es. JSON), per renderla verificabile a macchina |

L'output strutturato è particolarmente importante nei sistemi agentici: vincolare il modello a JSON
"rimuove l'ambiguità e permette una valutazione più standardizzata"
([Microsoft, *AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).

## Parametri di generazione

- **Temperature**: controlla la casualità. Valori bassi (es. 0–0.3) rendono le risposte più
  deterministiche e ripetibili; valori alti aumentano la varietà/creatività. Per compiti di precisione
  (estrazione, classificazione, routing) si preferiscono valori bassi.
- **Max tokens**: limite alla lunghezza della risposta.
- **Top-p / top-k**: strategie alternative di campionamento dei token.

## Embedding

Un **embedding** è la rappresentazione di un testo come **vettore numerico** che ne cattura il
significato semantico: testi simili hanno vettori vicini nello spazio. Gli embedding sono il
fondamento della ricerca semantica e quindi del [RAG](05-rag.md): si confronta il vettore della domanda
con i vettori dei documenti per trovare i più pertinenti, anche quando le parole non coincidono
esattamente ([AWS, *What is RAG?*](https://aws.amazon.com/what-is/retrieval-augmented-generation/)).

## Non-determinismo: la sfida architetturale

A differenza del software tradizionale, le decisioni di un LLM sono **intrinsecamente
non-deterministiche**: lo stesso input può produrre output diversi. AWS lo indica come una delle
dimensioni che la progettazione agentica deve affrontare esplicitamente
([AWS, *Agentic AI — Generative AI Lens*](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/agentic-ai.html)).
La conseguenza pratica, ripresa in tutti i capitoli seguenti: **non fidarsi della narrazione del
modello; verificare con controlli deterministici** ogni volta che è possibile.

---

Prossimo: [02 — Agenti](02-agenti.md).
