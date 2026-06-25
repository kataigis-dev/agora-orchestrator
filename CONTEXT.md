# Context — Agora Orchestrator glossary

Domain vocabulary for the orchestrator. Use these terms (not synonyms) in issue titles, refactor
proposals, test names, and docs. Created lazily by `/domain-modeling`; extend it as terms get resolved.

## Communication

**Agent instructions** — everything that shapes how an agent communicates, owned by `AgentInstructions`
(`src/Agora/Communication/`). It has two sides that must agree: the **system-prompt prefix** prepended to
an agent's own instructions, and the **output interpreter** that reads the agent's reply back. Built once
from config; the prefix is assembled per agent (it varies only by *answer-mode*).

**Communication protocol** — how agents encode replies. Two variants, each pairing a preamble with an
interpreter: **natural** (`<<signal>>` / `<<artifact>>` tokens, `SignalInterpreter`) and **H2C** (the
token-compressed `[TYPE:SUBTYPE]` block grammar, `H2cInterpreter`). The protocol is the only multi-variant
axis of agent instructions; keep its preamble and interpreter co-located so they cannot drift apart.

**Handoff directive** — an instruction layer (config `handoff: true`) telling an agent to pass only the
essential context to the next agent, not its full output. Phrased per protocol. Skipped in *answer-mode*.

**Answer-mode** — an agent re-invoked to answer another agent's `ask_agent` question rather than to advance
the graph. The handoff directive (and the `ask_agent` tool) are omitted so it cannot ask back.

**Output-language directive** — an instruction layer (config `language`) fixing the language of generated
documents and responses. Independent of the protocol.
