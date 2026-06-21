# 11 — Human-in-the-loop (HITL)

## What it is

The **Human-in-the-loop (HITL)** pattern inserts a human at **control points** of the agentic workflow:
the agent **stops** and waits for a person to review the work, **approve**, **correct** or **provide
input** before continuing
([Google Cloud, *Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

It is the recognition that, however autonomous, an agent operating on non-deterministic decisions
([01](01-fondamenti-llm.md)) should not act **without oversight** when the stakes are high.

## When to use it

Google and AWS point to the same scenarios: **high-stakes or subjective** tasks —

- financial transactions;
- validation of sensitive documents;
- *compliance* approvals;
- irreversible actions or those that modify production systems.

## Forms of HITL

| Form | Description |
|------|-------------|
| **Action approval (gating)** | The agent proposes a risky tool call; execution is blocked until a human confirms. See [03 — Tools](03-strumenti-function-calling.md). |
| **Output review** | The agent produces a result a human must approve before publication. |
| **Human chat manager** | In a *group chat* orchestration ([08](08-orchestrazione-multi-agente.md)), a person can take the coordinator role, guiding the discussion among agents. |
| **Conflict resolution** | When multiple agents produce incompatible results, a human (or a policy) arbitrates. |

## Trade-offs

Google is explicit about the trade-off: HITL "**improves safety and reliability**" but "requires building
and maintaining external interaction systems", adding architectural complexity. It should therefore be
applied **selectively**, only at the points where the risk justifies it, not at every step (otherwise you
lose the benefit of automation).

## HITL and governance

HITL is one of the **guardrails** discussed in the [security](12-sicurezza-governance.md) chapter: it is
the point where human judgment enters as an explicit boundary on the agent's autonomous behavior.
Combined with *least privilege* (the agent can propose risky actions but not execute them on its own), it
constitutes defense in depth.

## Link to the project

Agora Orchestrator exposes approval and conflict-resolution interfaces, and lets you mark individual tools
as *approvals* (gated) per agent. Details in [`../agents.md`](../agents.md).

---

Previous: [10 — Evaluation and observability](10-valutazione-osservabilita.md) · Next:
[12 — Security and governance](12-sicurezza-governance.md).
