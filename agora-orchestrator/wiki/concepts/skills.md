---
type: concept
title: Skills
tags: [skills, tools, prompt, reusable]
related: [agora-orchestrator, agora-agent-framework, mcp-tools]
created: 2026-06-17
updated: 2026-06-17
---

# Skills

Le Skill sono **prompt file riutilizzabili** che vengono esposti come tool chiamabili dagli agenti. Permettono di incapsulare logica specializzata (es. summarizzazione, traduzione, formattazione) in file Markdown.

## Struttura di una Skill

Una skill è una cartella con un file `SKILL.md`:

```
skills/
  summarize/
    SKILL.md
```

Il `SKILL.md` contiene le istruzioni per la skill (prompt, formato atteso, ecc.).

## Esempio

`examples/skills/summarize/SKILL.md` — skill di summarizzazione.

## Configurazione YAML

```yaml
skills:
  directories: [examples/skills]

agents:
  writer:
    skills: [summarize]
```

## Componenti

| Classe | Ruolo |
|--------|-------|
| `SkillLoader` | Carica le skill dalla directory configurata |
| `SkillRegistry` | Registro delle skill disponibili |
| `Skill` | Record con nome, descrizione e prompt della skill |
| `SkillTools` (AgentFramework) | Converte le skill in tool chiamabili tramite LLM function-calling |

## Differenza con MCP Tools

| Aspetto | Skills | MCP Tools |
|---------|--------|-----------|
| Implementazione | Prompt Markdown | Processo esterno (stdio) |
| Deploy | File nella directory skills | Server MCP separato |
| Complessità | Bassa | Alta |
| Potenza | Limitata al testo | Accesso al filesystem, rete, ecc. |

## Note

- Le skill vengono passate all'agente come tool nel contesto di function calling
- Un agente può avere sia skill che MCP tool contemporaneamente (`tools` + `skills` in `AgentConfig`)
