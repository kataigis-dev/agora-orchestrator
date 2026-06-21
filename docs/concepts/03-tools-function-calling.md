# 03 — Tools and function calling

## Why tools

An LLM on its own can only generate text. **Tools** give it the ability to **observe** (read files,
query a database, search the web) and **act** (write files, call APIs, run commands). They are what
turns a model into an agent capable of operating in the world
([Anthropic, *Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)).

## Function calling: how it works

The standard mechanism is called **function calling** (or *tool use*):

1. The developer gives the model, along with the prompt, the **schema** of each available tool: name,
   description, parameters (usually in JSON Schema).
2. The model, instead of replying in text, can emit a **call request**: the tool name and the arguments.
3. The **application** (not the model) runs the tool and returns the result.
4. The model incorporates the observation and continues (see the ReAct loop in [02](02-agents.md)).

Crucial point for security: **it is the application, not the model, that executes the code**. The model
proposes *what* to do; the orchestrator decides *whether and how* to do it, applying validation,
allow-lists and human approvals.

## The agent-computer interface (ACI)

Anthropic introduces the concept of the **Agent-Computer Interface (ACI)**: just as you take care of the
human-machine interface (UI), you must take care of the interface through which the agent uses tools.
Poorly described or ambiguous tools produce unreliable agents. Best practices
([Anthropic, *Writing tools for agents*](https://www.anthropic.com/engineering/writing-tools-for-agents)):

- **Clear, complete descriptions**: the model picks the tool *only* from its name and description.
  Document what it does, when to use it, what it returns.
- **Unambiguous parameters**: explicit names, formats indicated with examples.
- **Useful, concise output**: return what the next step needs, not huge dumps (which saturate the
  context window).
- **Informative errors**: a comprehensible error message lets the model self-correct.
- **Test and iterate**: treat tools as production code, with tests and a *sandbox*.

## Categories of tools

| Category | Examples |
|----------|----------|
| **Read/observe** | semantic search (RAG), reading files, DB queries, web search |
| **Write/act** | writing files, state-changing API calls, running commands |
| **Communication** | asking another agent, asking a human |
| **Verification** | running tests/builds and reading their outcome (deterministic checks) |

The distinction between **read-only** tools and tools that **change state** is central to governance:
action tools require tighter limits (see [12 — Security and governance](12-security-governance.md)).

## Human approval (gating)

For risky or irreversible actions, a tool can be **gated** by a human approval: the agent proposes the
call, but execution is blocked until a person confirms. It is the **Human-in-the-Loop** pattern applied
at the tool level (see [11](11-human-in-the-loop.md)). Google lists it among the fundamental design
patterns, to be used for "high-stakes or subjective tasks"
([Google Cloud](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

## Least privilege

AWS insists on the **least-privilege** principle for tools too: "permission boundaries should only
provide access to the systems and data sources necessary to generate a response", and roles should be
built with *least privilege* in mind
([AWS, GENSEC05-BP01](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/gensec05-bp01.html)).
In practice: an agent should have access **only** to the tools it needs, and nothing else (a per-agent
allow-list).

## Standardizing tool access: MCP

Historically every tool↔model integration was custom. Anthropic's **Model Context Protocol (MCP)**
standardizes this connection, so a tool exposed once is usable by any compatible application. It is the
subject of the next chapter.

---

Previous: [02 — Agents](02-agents.md) · Next: [04 — Model Context Protocol](04-mcp.md).
