# RAG — Retrieval-Augmented Generation

## Panoramica

La pipeline RAG permette di indicizzare conoscenza (file, documenti, pagine web) e renderla disponibile agli agenti come contesto arricchito.

## Configurazione

```yaml
rag:
  chunk_size: 1000
  chunk_overlap: 200
  embeddings:
    provider: openai
    model: text-embedding-3-small
  vector_store:
    type: memory           # "memory" (default) o "chroma"
  knowledge:
    - source: files
      path: examples/knowledge/
    - source: web
      url: https://example.com/docs
```

## Pipeline di ingest

```
1. Load       — legge le fonti (file, directory, web scraping)
2. Chunk      — divide in segmenti (configurabile: chunk_size, chunk_overlap)
3. Embed      — genera embedding vettoriali per ogni chunk
4. Store      — salva nel vector store (indicizzato)
```

### Comando

```bash
dotnet run --project src/Agora.Cli -- ingest --config examples/agora-rag.yaml
```

## Chunking

| Parametro | Default | Descrizione |
|---|---|---|
| `chunk_size` | 1000 | Caratteri per chunk |
| `chunk_overlap` | 200 | Sovrapposizione tra chunk consecutivi |
| `separators` | ["\n\n", "\n", " "] | Ordine di priorità per separazione |

## Embeddings

Supporta:
- **OpenAI** — `text-embedding-3-small`, `text-embedding-3-large`, `text-embedding-ada-002`
- **Ollama** — `nomic-embed-text`, `all-minilm`

## Vector store

### Memory (default)

Store in-memory per test e sviluppo. I dati vengono persi al riavvio.

### Chroma

Persistente su disco. Richiede `chroma` installato:

```bash
pip install chromadb
```

## Query

Quando un agente usa RAG:

1. La query dell'utente viene embedded
2. Similarity search nel vector store (k=5 per default)
3. I chunk più simili vengono aggiunti al contesto come `knowledge` source
4. L'agente riceve il contesto arricchito

## Knowledge sources

| Source | Descrizione |
|---|---|
| `files` | Carica file da path locale (.md, .txt, .pdf, .cs, .py) |
| `directory` | Carica ricorsivamente tutti i file in una directory |
| `web` | Web scraping di pagine web |
