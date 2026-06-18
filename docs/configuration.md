# Configurazione YAML

## Struttura generale

```yaml
version: "1"                          # Versione schema (obbligatorio)
communication: natural                # "h2c" (default) o "natural"
defaults:                             # Default applicati a tutti gli agenti
  model: fast
  temperature: 0.2
  max_tokens: 4096
  timeout: 120
providers:                            # Provider chat disponibili (obbligatorio)
  openai:
    api_key_env: OPENAI_API_KEY
    base_url: https://api.openai.com/v1
  ollama:
    base_url: http://localhost:11434
    api_key_env: ~
  llamastudio:
    base_url: http://127.0.0.1:1234/v1
    api_key_env: ~
models:                               # Modelli referenziabili dagli agenti (obbligatorio)
  fast:
    provider: openai
    model: gpt-4o-mini
    temperature: 0.3
  local:
    provider: ollama
    model: llama3.1
agents:                               # Definizione agenti (obbligatorio)
  planner:
    model: fast
    role: Sei un planner esperto.
    system_prompt: ...                # Opzionale, sovrascrive role
    tools: [read_file, write_file]    # Tools MCP da abilitare
    skills: [summarize]               # Skills da caricare
    max_retries: 3
    require_approval: true            # Human-in-the-loop
    temperature: 0.5                  # Sovrascrive default/modello
    max_tokens: 2048
```

## Sezioni opzionali

### Graph

```yaml
graph:
  entry: generator
  edges:
    - { from: generator, to: reviewer, type: handoff }
    - { from: reviewer, to: generator, type: conditional, when: fix, max_loops: 5 }
    - { from: reviewer, to: END, type: conditional, when: done }
```

### MCP

```yaml
mcp:
  approval: none                       # "none" (default), "always", "auto"
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
    github:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-github"]
    custom:
      type: http                       # "stdio" (default) o "http"
      url: http://localhost:3000/mcp
```

### RAG

```yaml
rag:
  chunk_size: 1000
  chunk_overlap: 200
  embeddings:
    provider: openai
    model: text-embedding-3-small
  vector_store:
    type: memory                        # "memory" o "chroma"
  knowledge:
    - source: files                     # "files", "web", "directory"
      path: examples/knowledge/
    - source: web
      url: https://example.com/docs
```

### Skills

```yaml
skills:
  summarize:
    location: examples/skills/summarize/SKILL.md
```

### Approvazione

```yaml
approval:
  provider: console                    # "console" (default) o "api"
  timeout: 300                         # secondi prima di timeout
```

## Comunicazione Natural

Con `communication: natural`, gli agenti parlano in linguaggio naturale. I segnali di routing si scrivono come:

- `<<signal done>>` — task completato
- `<<signal fix>>` — richiedi revisione
- `<<signal approve>>` — approvato
- `<<signal deny>>` — rifiutato
- `<<signal escalate>>` — escalation umana

## Esempio completo

Vedi `examples/agora-generate-api.yaml` per un esempio completo con grafo, MCP, e revisione.
