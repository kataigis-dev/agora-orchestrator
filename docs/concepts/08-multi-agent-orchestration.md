# 08 — Multi-agent orchestration

When a single agent is not enough — because the problem is cross-domain, because there are too many
tools, or because distinct security boundaries are needed — you move to **multiple specialized agents
that coordinate**. This chapter gathers the multi-agent orchestration patterns from **Microsoft** and
**Google**, complementary to Anthropic's workflows from chapter [07](07-workflow-patterns.md).

## First: do you really need multi-agent?

Microsoft proposes evaluating a **complexity spectrum** and using the lowest level that meets the
requirements, because each level adds "coordination overhead, latency and cost"
([Microsoft, *AI Agent Orchestration Patterns*](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns)):

| Level | What it is | When |
|-------|-----------|------|
| Single LLM call | one prompt, no tools | trivial tasks |
| Single agent | one LLM with tools and a loop | most cases |
| **Multi-agent orchestration** | multiple agents coordinating | cross-domain problems, distinct security boundaries, parallel specialization |

Google agrees: "a single agent's performance degrades as tools and task complexity grow; consider
multi-agent systems for complex workflows"
([Google Cloud](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

## Microsoft's patterns

### Sequential
Chains the agents in a **predefined linear order**: each one's output is the next one's input (a pipeline
of specialized transformations). The choice of the next agent is **deterministic**, not left to the
agent. *Synonyms: pipeline, prompt chaining, linear delegation.*

### Concurrent
Multiple agents work **simultaneously on the same input** from different perspectives; the results are
then aggregated (voting, weighted merge, LLM-synthesized summary). The agents **do not hand off** results
to each other. *Synonyms: parallel, fan-out/fan-in, scatter-gather, map-reduce.*

### Group chat
Multiple agents collaborate in a **shared conversation thread**, coordinated by a *chat manager* that
decides who speaks. Typically the agents are **read-only** (they do not modify systems). Great for
brainstorming, debate, *quality gates* and human oversight. Microsoft recommends **limiting to three or
fewer agents** to keep control. *Synonyms: roundtable, multiagent debate, council.*

> **Maker-checker loop**: a special case of group chat. A *maker* agent produces, a *checker* agent
> evaluates against defined criteria; if it finds gaps it sends back to the maker with feedback. It
> repeats until the checker approves or the iteration limit is reached. It is the multi-agent equivalent
> of Anthropic's *evaluator-optimizer*.

### Handoff
Each agent assesses the task and decides whether to **handle it or transfer it** to a more appropriate
agent, dynamically. Only one agent at a time works on the input; the chain produces a single result.
*Synonyms: routing, triage, transfer, dispatch, delegation.*

### Magentic (dynamic orchestration)
For **open-ended problems with no predetermined plan**. A *magentic manager* dynamically builds and
refines a **task ledger** (a register of goals and subgoals) collaborating with specialized agents that
use tools to modify external systems. It iterates, *backtracks* and delegates until the plan is complete,
regularly checking whether the goal is reached or stalled. It is an action-oriented extension of group
chat. *Synonyms: dynamic orchestration, task-ledger-based, adaptive planning.*

## Google's patterns (beyond the above)

Google ([*Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system))
adds useful labels:

- **Single-agent**: the starting point — one model, defined tools, a complete system prompt.
- **Loop**: agents that repeat until a termination condition is met (predefined logic, without consulting
  the model for orchestration). ⚠️ risk of infinite loops.
- **Coordinator**: a central agent that **dynamically decomposes and routes** subtasks (it uses the model
  for routing, unlike parallel). ≈ Anthropic's orchestrator-workers.
- **Hierarchical task decomposition**: a **multi-level** hierarchy; parent agents delegate to subagents
  across layers. For ambiguous problems requiring extensive planning.
- **Swarm**: agents collaborating with **all-to-all** communication, each building on the other's work.
  It is "the most complex and costly multi-agent pattern"; maximum quality, maximum risk of unproductive
  loops.
- **ReAct** and **Human-in-the-loop**: already covered in [02](02-agents.md) and
  [11](11-human-in-the-loop.md).
- **Custom logic**: bespoke orchestration with conditional logic, when no standard pattern fits.

## How agents communicate

Google identifies three interaction mechanisms between agents:

1. **Shared session state**: agents read/write a common state;
2. **Model-driven delegation**: the model routes tasks;
3. **Explicit invocation**: an agent calls another agent as if it were a function/tool.

For **interoperability between agents from different systems**, the **A2A (Agent-to-Agent)** protocol is
emerging, complementary to MCP (which connects agents to tools). See
[13 — Standards and protocols](13-standards-protocols.md).

## Benefits and risks of multi-agent

| Benefits | Risks |
|----------|-------|
| Specialization and smaller contexts per agent | Coordination overhead, latency, cost (more calls) |
| Distinct security boundaries per agent | New failure modes (deadlock, loops) |
| Parallelism | Harder debugging and observability |
| Reuse of agents across workflows | Error propagation between agents |

All sources converge: multi-agent is justified **only** when a single agent cannot reliably succeed, due
to prompt complexity, tool overload or security requirements.

## Summary table of multi-agent patterns

| Pattern (source) | Coordination | Typical use |
|------------------|--------------|-------------|
| Sequential (MS/Google) | deterministic linear | staged pipeline |
| Concurrent / Parallel (MS/Google) | parallel + aggregation | multi-perspective analysis |
| Group chat (MS) | shared thread + chat manager | debate, quality gate |
| Handoff (MS) | dynamic transfer | triage/routing |
| Magentic (MS) | dynamic task ledger | open-ended, action-oriented problems |
| Coordinator (Google) | dynamic routing | adaptive decomposition |
| Hierarchical (Google) | multi-level hierarchy | extensive planning |
| Swarm (Google) | all-to-all | maximum quality, maximum complexity |

---

Previous: [07 — Workflow patterns](07-workflow-patterns.md) · Next:
[09 — Spec-driven development](09-spec-driven-development.md).
