---
type: concept
title: Guided Config (init wizard)
tags: [cli, configuration, yaml, wizard, dx]
related: [agora-cli, agent-graph, edge-types, communication-modes, handoff-context, rag-pipeline, shared-knowledge-base, skills, mcp-tools, human-in-the-loop, agora-orchestrator]
created: 2026-06-19
updated: 2026-06-19
---

# Guided Config (`init` wizard)

Il comando `agora init` costruisce un file di configurazione YAML in modo
**interattivo e guidato**, evitando di scrivere a mano provider, modelli, agenti,
grafo e le sezioni opzionali. È pensato per ridurre l'attrito iniziale: l'utente
risponde a una serie di domande e ottiene un file già validato e pronto per `run`.

## Flusso

Il wizard procede a step, ognuno con un `[default]` accettabile con Invio:

1. **Communication mode** — `h2c` (default) o `natural`. Vedi [[communication-modes]].
2. **Handoff mode** — sì/no (default no): passa al successivo solo l'handoff esplicito.
   Vedi [[handoff-context]].
3. **Providers** — uno o più (es. `openai`, `anthropic`, `ollama`); per ciascuno
   `api_key_env` e `base_url` opzionali. Almeno uno è obbligatorio.
4. **Models** — alias → `provider` + nome modello concreto (es. `balanced` →
   `openai`/`gpt-4o`). Almeno uno.
5. **Default model** — alias usato dagli agenti che non specificano un modello
   proprio (popola `defaults.model`).
6. **Skills** (opzionale) — directory dei file `SKILL.md`. Vedi [[skills]].
7. **MCP** (opzionale) — server di tool esterni, via `stdio` (`command` + `args`)
   o `http` (`url`). Vedi [[mcp-tools]].
8. **RAG** (opzionale) — abilita la pipeline retrieval: tipo di vector store
   (`memory` o `file` + path), sorgenti di ingest, `chunk_size`/`chunk_overlap`,
   `top_k` e strategia di refine (`none`/`llm`). Usa l'embedder `fake` built-in.
   Vedi [[rag-pipeline]] e [[shared-knowledge-base]].
9. **Agents** — `id`, alias modello e `role` (system prompt). Almeno uno. Se sono
   state configurate skill/MCP/RAG o l'handoff, per ogni agente si possono elencare
   `skills`, `tools` (inclusi i built-in `rag_search`/`rag_write`/`ask_agent`) e gli
   `approvals` (sottoinsieme dei tool che richiede approvazione umana —
   [[human-in-the-loop]]).
10. **Graph** — proposto solo con **2+ agenti**. Si sceglie l'`entry` e si
    aggiungono gli edge (`from` → `to`, con `END` per terminare); per gli edge
    `conditional` vengono chiesti `when` e `max_loops`. Vedi [[agent-graph]] e
    [[edge-types]].
11. **Output** — path del file (default `./agora.yaml`), con conferma di
    sovrascrittura se esiste già.

Le sezioni opzionali (skills, MCP, RAG) sono gate dietro un sì/no con default
**no**: chi vuole una config minimale le salta con un Invio. Con un solo agente
il grafo viene saltato e la config si esegue in single-agent mode
(`run --agent <id>`).

## Garanzie

- Le scelte vincolate (communication mode, alias provider/modello, `entry`,
  target degli edge) sono validate **durante** il wizard: non è possibile
  digitare un riferimento inesistente.
- Al termine il file viene **riletto con `ConfigLoader`**: se la validazione
  fallisce il comando esce con errore, quindi un `init` riuscito produce sempre
  una config caricabile.
- A fine flusso stampa i comandi `validate` e `run` pronti da copiare.

## Implementazione

| Componente | Ruolo |
|------------|-------|
| `Cli/ConfigWizard.cs` | Driver delle domande; costruisce un `AgoraConfig` |
| `Configuration/ConfigWriter.cs` | Serializza `AgoraConfig` → YAML (controparte di `ConfigLoader`) |
| `Cli/CliRunner.cs` | Dispatch del comando `init`; opzione `--output` |

`ConfigWizard.Run(TextReader, TextWriter, TextWriter, string?)` riceve input e
output iniettati, perciò il flusso è interamente **unit-testabile** pilotando
uno stdin scriptato (vedi `ConfigWizardTests`). `ConfigWriter` usa il
`SerializerBuilder` di YamlDotNet con `OmitNull | OmitEmptyCollections`, così il
file generato contiene solo le sezioni effettivamente popolate.

## Esempio

```bash
agora init                       # scrive ./agora.yaml in modo guidato
agora init --output team.yaml    # pre-imposta il path di destinazione
```

## Note

- Per RAG il wizard imposta l'embedder `fake` (unico built-in nel core via
  `RagFactory`); un embedder reale va iniettato in codice (es.
  `AgentFrameworkEmbedder`). Il vector store è scegliibile tra `memory` e `file`.
- Quando la config include RAG, il footer suggerisce anche `agora ingest`.
