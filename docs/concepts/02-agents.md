# 02 — Agents

## What an agent is

There is no single definition, but the sources converge. **Anthropic** draws a sharp distinction between
two things
([*Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)):

- **Workflow**: "systems where LLMs and tools are orchestrated through *predefined code paths*". The
  control flow is written by the developer.
- **Agent**: "systems where LLMs *dynamically direct their own processes and tool usage*, maintaining
  control over how they accomplish tasks". In essence, an agent is "*just an LLM using tools in a loop
  based on environmental feedback*".

**Google** frames it in a complementary way: agents are systems that "solve open-ended problems, which
might require autonomous decision-making and complex multi-step workflow management" and "excel at
solving problems in real-time by using external data"
([Google Cloud, *Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

The key difference from a simple LLM call is **agency**: the agent decides *which* steps to take,
*which* tools to use and *when* to stop, instead of following a fixed script.

## The "augmented LLM": the basic building block

Anthropic identifies the **augmented LLM** as the foundational block: a model enhanced with
**retrieval, tools and memory**. The modern model actively uses these capabilities — it generates its
own search queries, picks the appropriate tools, and decides what to keep in memory.

```
            ┌──────────────────────────────┐
   input ──▶│         Augmented LLM         │──▶ output
            │  ┌──────────┐  ┌────────────┐ │
            │  │ retrieval│  │   tools     │ │
            │  └──────────┘  └────────────┘ │
            │         ┌──────────┐          │
            │         │  memory  │          │
            │         └──────────┘          │
            └──────────────────────────────┘
```

## The components of an agent

Combining Google and Anthropic, an agent is made of:

| Component | Role | Deep dive |
|-----------|------|-----------|
| **Model (AI Model)** | Provides reasoning and decision-making | [01](01-llm-foundations.md) |
| **System prompt** | Defines behavior, persona and constraints | [01](01-llm-foundations.md) |
| **Tools** | External resources to gather information or take actions | [03](03-tools-function-calling.md) |
| **Memory** | Keeps information across steps and across sessions | [06](06-memory-context.md) |
| **Orchestration/reasoning loop** | Manages the iterative *think → act → observe* cycle | below |

## The reasoning loop (ReAct)

The canonical pattern by which a single agent operates is **ReAct** (*Reason + Act*): the agent
alternates in an iterative cycle
([Google Cloud, *Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)):

1. **Thought** — it reflects on what to do;
2. **Action** — it picks a tool and formulates its arguments (or produces the final answer);
3. **Observation** — it receives the tool's result and incorporates it into the context;

repeating until an **exit condition** is met. From this come two critical needs that every source
stresses: **carefully designed tools** and **clear stop conditions** (otherwise the agent loops forever
or burns resources without finishing).

## Levels of autonomy: workflow vs agent

Microsoft describes a **complexity spectrum**: from a single LLM call, to a deterministic pipeline
(workflow), up to truly autonomous multi-agent orchestration. The rule, shared by all, is: **use the
lowest level of complexity that reliably meets the requirements**
([Microsoft, *AI Agent Orchestration Patterns*](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns)).

| | Workflow | Autonomous agent |
|---|----------|------------------|
| Control flow | predefined code | the model decides |
| Predictability | high | lower |
| Adaptability | low | high |
| When to prefer it | well-defined, repeatable tasks | open-ended problems, steps not known up front |

Anthropic is explicit: "workflows often provide better predictability and consistency for well-defined
tasks", while autonomy should be introduced only when large-scale flexibility is needed.

## The three design principles (Anthropic)

1. **Simplicity** — many successful implementations are "*just a single optimized LLM call with
   retrieval and examples*". Adding agents and frameworks has a cost (latency, tokens, debugging).
2. **Transparency** — show the agent's planning steps explicitly.
3. **Care for the agent-computer interface (ACI)** — document and test tools with the same attention you
   would give to a human interface (see [03](03-tools-function-calling.md)).

> **Remember.** "Start simple; add complexity only when a demonstrated improvement justifies it." This
> principle runs through all the following chapters.

---

Previous: [01 — Foundations](01-llm-foundations.md) · Next:
[03 — Tools and function calling](03-tools-function-calling.md).
