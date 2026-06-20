---
type: concept
title: Skills
tags: [skills, tools, prompt, reusable]
related: [agora-orchestrator, agora-agent-framework, mcp-tools]
created: 2026-06-17
updated: 2026-06-20
---

# Skills

Skills are **reusable prompt files** exposed as a tool the agent can call. They encapsulate
specialized logic (e.g. summarization, translation, formatting) in Markdown files.

## Skill structure

A skill is a folder containing a `SKILL.md` file:

```
skills/
  summarize/
    SKILL.md
```

`SKILL.md` contains the skill's instructions (prompt, expected format, etc.).

## Example

`examples/skills/summarize/SKILL.md` — a summarization skill.

## YAML configuration

```yaml
skills:
  directories: [examples/skills]

agents:
  writer:
    skills: [summarize]
```

## Components

| Class | Role |
|-------|------|
| `SkillLoader` | Loads skills from the configured directories |
| `SkillRegistry` | Registry of available skills |
| `Skill` | Record with the skill's name, description and prompt |
| `SkillTools` (AgentFramework) | Exposes skills as a `load_skill` tool via LLM function-calling |

## Difference from MCP tools

| Aspect | Skills | MCP Tools |
|--------|--------|-----------|
| Implementation | Markdown prompt | External process (stdio) |
| Deploy | File in the skills directory | Separate MCP server |
| Complexity | Low | High |
| Power | Limited to text | Filesystem, network, etc. |

## Notes

- Skills are exposed to the agent as a tool (`load_skill`, progressive disclosure).
- An agent can have both skills and MCP tools at once (`tools` + `skills` in `AgentConfig`).
