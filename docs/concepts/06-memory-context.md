# 06 — Memory and context engineering

## From prompt engineering to context engineering

When systems were single LLM calls, the art was writing the right **prompt**. With agents, which
accumulate history, tool results and memory over many steps, the problem becomes broader: **managing the
whole context window over time**. Anthropic calls this discipline **context engineering**:

> "Context engineering is the art and science of filling the context window with *exactly the right
> information* at each step of an agent's trajectory."
> — [Anthropic, *Effective context engineering for AI agents*](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)

The guiding principle: context is **finite and with diminishing marginal returns**. The goal is to find
"the smallest set of high-signal tokens that maximizes the likelihood of the desired outcome". More text
is not better: beyond a threshold, the model gets confused (*context rot*).

## How to structure the context

Anthropic recommends a precise ordering, because "the arrangement and quality of the context determine
the agent's performance more than any other factor":

1. **System instructions** (role, constraints, format);
2. **Relevant memory** (only what is needed now);
3. **Tool definitions**;
4. **Conversation history**.

## Memory: short and long term

| Type | What it holds | Where it lives |
|------|---------------|----------------|
| **Working / short-term memory** | The state of the current trajectory (history, recent observations) | in the context window |
| **Long-term memory** | Facts, decisions, artifacts to keep across steps and across sessions | in an **external store** (file, DB, vector store) retrieved as needed |

Long-term memory often relies on the same technologies as [RAG](05-rag.md): you write to a store and
retrieve, by similarity, only the relevant subset, compressing tokens.

## Context-management strategies

From Anthropic's guidance
([*Effective context engineering*](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents),
[Claude Cookbook](https://platform.claude.com/cookbook/tool-use-context-engineering-context-engineering-tools)):

- **Compaction**: when the history grows, summarize it into a denser form, keeping the key decisions and
  artifacts and discarding the noise.
- **Structured note-taking**: the agent writes notes to an external store (e.g. a `NOTES.md` file) and
  re-reads them when needed, "keeping only what is necessary in working memory". It lets the agent track
  long-running work without keeping everything in active context.
- **Context editing / pruning**: remove from the context, according to rules, what is no longer needed
  (e.g. the output of already-consumed tools).
- **Context awareness**: give the agent a signal about the remaining context capacity, so it can decide
  when to compact.
- **Selective (just-in-time) retrieval**: instead of pre-loading everything, the agent retrieves the
  information *when* it needs it, assembling understanding "layer by layer".

## The link to multi-agent systems

A structural reason for moving to multiple agents is precisely **context management**: splitting a
problem among specialized agents means each works with a smaller, focused context window, instead of a
single agent drowning in a huge context. Microsoft explicitly cites *prompt complexity* and *tool
overload* as reasons to move to multi-agent
([Microsoft](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns)).
The flip side is that agents must **exchange only the minimum necessary context** so as not to reproduce
the same problem at the system level (see [08](08-multi-agent-orchestration.md)).

---

Previous: [05 — RAG](05-rag.md) · Next: [07 — Workflow patterns](07-workflow-patterns.md).
