# YAML configuration

A single YAML file describes everything. Generate one interactively with `agora init`.

## Top-level structure

```yaml
version: "1"                 # schema version
communication: h2c           # "h2c" (default) or "natural"
handoff: true                # pass only the declared handoff to the next agent (default false)
language: English            # language agents must use in generated documents (optional)

defaults:                    # defaults applied to every agent
  model: balanced
  temperature: 0.2
  max_tokens: 4096
  timeout: 120
  retries: 2
  retry_base_delay: 0.5

providers:                   # available chat providers
  openai:
    api_key_env: OPENAI_API_KEY
    base_url: https://api.openai.com/v1   # optional (OpenAI-compatible endpoints)
  ollama:
    base_url: http://localhost:11434

models:                      # model aliases referenced by agents
  fast:     { provider: openai, model: gpt-4o-mini }
  balanced: { provider: openai, model: gpt-4o }

agents:                      # at least one
  planner:
    model: balanced          # model alias (falls back to defaults.model)
    role: "You decompose the task."
    system_prompt: "..."     # optional, used instead of role
    system_prompt_file: ./prompts/planner.md  # optional, read from file
    timeout: 120             # optional per-agent override
    skills: [summarize]      # skill names (see skills section)
    tools: [rag_search, rag_write, ask_agent, read_file, write_file]
    approvals: [write_file]   # subset of tools requiring human approval (HITL)
```

## Optional sections

### graph

```yaml
graph:
  entry: planner
  edges:
    - { from: planner, to: writer,  type: handoff }
    - { from: writer,  to: critic,  type: sequential }
    - { from: critic,  to: writer,  type: conditional, when: fix, max_loops: 5 }
    - { from: critic,  to: END,     type: conditional, when: done }
```

Edge `type`: `sequential`, `handoff`, `conditional` (with `when` signal + optional `max_loops`),
`route` (LLM picks the branch from each edge's `when` description), `parallel` (fork branches that
converge on one join node). See the graph and edge-types pages.

### rag

```yaml
rag:
  enabled: true
  refine:
    strategy: none           # "none" | "llm"
    model: balanced          # model alias for the "llm" refiner
  retrieval:
    embedder:
      type: fake             # "fake" | "openai" | "ollama"
      provider: openai       # provider for key/base when not "fake"
      model: text-embedding-3-small
    vector_store:
      type: file             # "memory" | "file" | "qdrant"
      path: ./kb.json        # for "file"
      url: http://localhost:6334   # for "qdrant"
      collection: agora      # for "qdrant"
    top_k: 6
    score_threshold: 0.0
  ingest:
    sources: [ ./docs ]
    chunk_size: 800
    chunk_overlap: 120
```

### memory

```yaml
memory:
  enabled: true
  top_k: 5                   # relevant entries recalled per agent
  max_chars: 0               # cap on recalled context (0 = unlimited)
  remember_outputs: false    # also remember (truncated) outputs, not only declared artifacts
```

### skills

```yaml
skills:
  directories: [./skills]    # folders scanned for SKILL.md files
```

### mcp

```yaml
mcp:
  servers:
    filesystem:
      command: npx           # stdio transport
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
    remote:
      url: http://localhost:3000/mcp   # HTTP transport
```

### spec

Structured spec-driven development: persists a machine-checkable `SpecDocument` (requirements +
tasks) and exposes the `spec_*` tools. See [spec.md](spec.md) for the full reference.

```yaml
spec:
  enabled: true
  require_criteria: true       # reject requirements with no acceptance criterion
  store:
    type: file                 # canonical JSON source of truth
    path: ./spec.json
    # type: mcp                # or persist via a RAG store over MCP:
    # server: memory           # an entry under mcp.servers
    # write_tool: rag_write
    # read_tool: rag_search
    # write_arg: text
    # query_arg: query
    # key: "agora:spec-document"
```

### checks

Real build/test execution: a fixed allow-list of named commands that `run_check` and `spec_verify`
may invoke. Commands run with no shell; `{key}` placeholders in arguments are substituted at call
time. See [spec.md](spec.md#verifying-against-reality).

```yaml
checks:
  workdir: .                 # relative to the config dir
  timeout: 300               # seconds, per check
  commands:
    build: { command: dotnet, args: [build] }
    test:  { command: dotnet, args: [test, --filter, "{filter}"] }
```

## Built-in tools

Available to agents that list them in `tools` (no MCP server needed):
`read_file`, `write_file`, `search_files`, `list_directory` (filesystem, sandboxed to the config
directory — paths escaping it via `..`, an absolute path, or a different drive are rejected);
`rag_search`, `rag_write`
(shared knowledge base); `ask_agent` (ask another agent); `spec_get`, `spec_gate`,
`spec_propose_requirement`, `spec_bind_check`, `spec_set_status`, `spec_add_task`, `spec_link_task`
(structured spec — requires the `spec` section); `run_check`, `spec_verify` (real check execution —
requires the `checks` section).
Tools named in `approvals` are gated through a human (HITL).

## Natural communication

With `communication: natural`, agents use natural language and emit routing tokens:

- `<<signal done>>` / `<<signal fix>>` / `<<signal approved>>` — routing signals (any name)
- `<<artifact key=value>>` — shared artifact (the `handoff` key is the handoff payload)

## Full example

See `agora.yaml` (an 8-agent SDLC pipeline) and the `examples/` folder.
