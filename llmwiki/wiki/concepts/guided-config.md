---
type: concept
title: Guided Config (init wizard)
tags: [cli, configuration, yaml, wizard, dx]
related: [agora-cli, agent-graph, edge-types, communication-modes, handoff-context, rag-pipeline, shared-knowledge-base, skills, mcp-tools, human-in-the-loop, agora-orchestrator]
created: 2026-06-19
updated: 2026-06-20
---

# Guided Config (`init` wizard)

The `agora init` command builds a YAML configuration file **interactively**, avoiding hand-writing
providers, models, agents, graph and the optional sections. It reduces the initial friction: the
user answers a series of questions and gets a file that is already validated and ready for `run`.

## Flow

The wizard proceeds step by step, each with a `[default]` accepted with Enter:

1. **Communication mode** — `h2c` (default) or `natural`. See [[communication-modes]].
2. **Handoff mode** — yes/no (default no): pass only the explicit handoff to the next agent.
   See [[handoff-context]].
3. **Providers** — one or more (e.g. `openai`, `anthropic`, `ollama`); for each, optional
   `api_key_env` and `base_url`. At least one is required.
4. **Models** — alias → `provider` + concrete model name (e.g. `balanced` → `openai`/`gpt-4o`).
   At least one.
5. **Default model** — alias used by agents that don't specify their own (populates `defaults.model`).
6. **Skills** (optional) — directories of `SKILL.md` files. See [[skills]].
7. **MCP** (optional) — external tool servers, via `stdio` (`command` + `args`) or `http` (`url`).
   See [[mcp-tools]].
8. **RAG** (optional) — enables the retrieval pipeline: vector store type (`memory`/`file` + path),
   ingest sources, `chunk_size`/`chunk_overlap`, `top_k`, and refine strategy (`none`/`llm`). Uses
   the built-in `fake` embedder. See [[rag-pipeline]] and [[shared-knowledge-base]].
9. **Agents** — `id`, model alias and `role` (system prompt). At least one. If skills/MCP/RAG or
   handoff were configured, each agent can list `skills`, `tools` (including the built-in
   `rag_search`/`rag_write`/`ask_agent`) and `approvals` (subset of tools requiring human approval
   — [[human-in-the-loop]]).
10. **Graph** — offered only with **2+ agents**. Pick the `entry` and add edges (`from` → `to`, with
    `END` to terminate); for `conditional` edges it asks `when` and `max_loops`. See [[agent-graph]]
    and [[edge-types]].
11. **Output** — file path (default `./agora.yaml`), with overwrite confirmation if it exists.

The optional sections (skills, MCP, RAG) are gated behind a yes/no defaulting to **no**: a minimal
config skips them with one Enter. With a single agent the graph is skipped and the config runs in
single-agent mode (`run --agent <id>`).

## Guarantees

- Constrained choices (communication mode, provider/model aliases, `entry`, edge targets) are
  validated **during** the wizard: you cannot type a non-existent reference.
- At the end the file is **re-read with `ConfigLoader`**: if validation fails the command exits with
  an error, so a successful `init` always produces a loadable config.
- It prints ready-to-copy `validate` and `run` commands at the end.

## Implementation

| Component | Role |
|-----------|------|
| `Cli/ConfigWizard.cs` | Drives the questions; builds an `AgoraConfig` |
| `Configuration/ConfigWriter.cs` | Serializes `AgoraConfig` → YAML (counterpart of `ConfigLoader`) |
| `Cli/CliRunner.cs` | Dispatches the `init` command; `--output` option |

`ConfigWizard.Run(TextReader, TextWriter, TextWriter, string?)` takes injected input/output, so the
flow is fully **unit-testable** by driving a scripted stdin (see `ConfigWizardTests`). `ConfigWriter`
uses YamlDotNet's `SerializerBuilder` with `OmitNull | OmitEmptyCollections`, so the generated file
contains only the populated sections.

## Example

```bash
agora init                       # writes ./agora.yaml interactively
agora init --output team.yaml    # pre-set the destination path
```

## Notes

- For RAG the wizard sets the `fake` embedder (the only core built-in via `RagFactory`); a real
  embedder is selectable via config (`type: openai`/`ollama`). The vector store is `memory` or `file`.
- When the config includes RAG, the footer also suggests `agora ingest`.
