# 07 — Agentic workflow patterns

This is one of the two central chapters on the **standards for agentic workflows**. Here we cover the
patterns in which the flow is **orchestrated by predefined code** (workflows), following **Anthropic's**
reference taxonomy
([*Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)). Chapter
[08](08-orchestrazione-multi-agente.md) covers *multi-agent* orchestration instead.

> Method reminder (Anthropic): **start with the simplest solution**. Many cases are solved with a single
> LLM call augmented with retrieval and examples. Introduce the following patterns only when truly
> needed.

## 1. Prompt chaining

Breaks a task into **sequential steps**, where each LLM call processes the output of the previous one.
Between steps you can insert **programmatic gates** that verify progress.

```
input ─▶ LLM 1 ─▶ [gate] ─▶ LLM 2 ─▶ [gate] ─▶ LLM 3 ─▶ output
```

- **When**: the task decomposes cleanly into fixed subtasks.
- **Trade-off**: trades *latency* for *accuracy* (more steps, but each simpler and more controllable).
- Microsoft calls the same scheme **sequential orchestration** / *pipeline* when applied to agents.

## 2. Routing

A classifier **routes** the input to the most appropriate specialized process.

```
                 ┌─▶ handler A
input ─▶ router ─┼─▶ handler B
                 └─▶ handler C
```

- **When**: distinct categories exist that are better handled separately, and classification is
  reliable.
- **Benefit**: separation of concerns and prompts optimized for each case.
- Synonyms in the other sources: *handoff*, *triage*, *dispatch*, *coordinator*.

## 3. Parallelization

Runs multiple calls **in parallel** and aggregates the results. Two variants:

- **Sectioning**: the task is split into *independent* subtasks run simultaneously.
- **Voting**: *the same* task is run multiple times to get different perspectives, then you choose (e.g.
  majority vote), increasing confidence.

```
            ┌─▶ LLM ─┐
input ─▶ split ─▶ LLM ─┼─▶ aggregation ─▶ output
            └─▶ LLM ─┘
```

- **When**: to gain speed (sectioning) or reliability (voting).
- Microsoft calls it **concurrent orchestration** (*fan-out/fan-in*, *scatter-gather*, *map-reduce*);
  aggregation can be voting, weighted merge or LLM-synthesized summary.

## 4. Orchestrator–workers

An **orchestrator** LLM *dynamically* decomposes the task and delegates to **worker** LLMs, then
synthesizes their results.

```
                 ┌─▶ worker ─┐
input ─▶ orchestrator ─▶ worker ─┼─▶ synthesis ─▶ output
                 └─▶ worker ─┘
```

- **Difference from parallelization**: the subtasks are **not predefined**; the orchestrator determines
  them based on the specific input. This makes it more flexible (and less predictable).
- **When**: tasks whose decomposition is not known up front (e.g. changes to a variable number of files).
- Multi-agent equivalents: **coordinator** (Google) and **hierarchical** when there is more than one
  level.

## 5. Evaluator–optimizer

One LLM **generates** a response; a second one **evaluates** it and provides feedback; the first
**revises**. The cycle repeats until a criterion is met (or an iteration limit is reached).

```
                ┌───────── feedback ─────────┐
                ▼                             │
input ─▶ generator ─▶ response ─▶ evaluator ─┴─▶ (ok) ─▶ output
```

- **When**: there are **clear evaluation criteria** and responses demonstrably improve from feedback.
- In the other sources: **maker-checker loop** / **review-and-critique** (Microsoft, Google),
  **iterative refinement**.
- ⚠️ Requires an **exit condition** (quality threshold or *max iterations*) to avoid looping forever.

## Workflow vs autonomous agent (again)

The five patterns above are **workflows**: the path is hardcoded. Beyond these is the **autonomous
agent**, which decides the sequence of actions itself in the ReAct loop (see [02](02-agenti.md)).
Anthropic reiterates: for well-defined tasks workflows offer better predictability; autonomy is
introduced when flexibility is truly necessary.

## Summary table

| Pattern | Flow | When | Synonyms |
|---------|------|------|----------|
| Prompt chaining | fixed sequential | clean decomposition | pipeline, sequential |
| Routing | branching on classification | distinct categories | handoff, triage, dispatch |
| Parallelization | parallel + aggregation | speed or confidence | concurrent, fan-out/fan-in |
| Orchestrator–workers | dynamic delegation | subtasks not known up front | coordinator, hierarchical |
| Evaluator–optimizer | generation + iterative critique | clear quality criteria | maker-checker, review-critique |

---

Previous: [06 — Memory and context](06-memoria-contesto.md) · Next:
[08 — Multi-agent orchestration](08-orchestrazione-multi-agente.md).
