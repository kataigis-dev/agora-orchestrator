# 10 — Evaluation and observability

An agentic system is not judged by a single successful answer: it must be **measured** systematically
(evaluation) and **observed** in production (observability). They are two faces of the same need — knowing
whether the system is *reliable*, not just *capable*.

## Evaluation (evals)

### Two families of metrics

The fundamental distinction, reiterated by Microsoft and the evals literature
([Microsoft, *AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)):

| Type | When to use it | Examples |
|------|----------------|----------|
| **Deterministic metrics** | Exact, objective checks | is the tool call correct? does the test pass? is the JSON valid? is the task complete? |
| **LLM-as-judge** | Criteria requiring judgment or context | is the answer relevant? faithful to the sources? appropriate in tone? |

Practical rule: **always prefer deterministic checks** where possible; resort to the LLM-judge only for
what is not machine-verifiable. It is the same principle as
[spec-driven development](09-spec-driven-development.md).

### LLM-as-judge

**LLM-as-judge** uses a model to evaluate another model's outputs: it lets you automate quality checks
that would otherwise require human review, evaluating thousands of outputs in minutes and flagging
hallucinations or off-topic answers. Best practices
([Microsoft](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/), and related
sources):

- **clear rubrics** and structured prompts for the judge;
- **constrained output** (e.g. JSON) to reduce ambiguity;
- *score smoothing* and calibration against human labels;
- do not use it for what an exact check can verify better.

### What to evaluate in an agent

Beyond the quality of the final answer, agents require specific metrics:

- **Tool-use correctness** (*tool calling*): did it call the right tool with the right arguments?
- **Task completion**: was the goal achieved?
- **Reasoning quality** and **trajectory**: were the intermediate steps sensible?
- **Trace-based evaluation**: you evaluate the whole trajectory, not just the result (this is where the
  idea of *agent-as-a-judge* fits — an agent evaluating another by observing its intermediate steps).

## Observability

The goal is to **instrument** the agent's code so it emits **traces** and **metrics** that an
observability platform can collect. **OpenTelemetry** is emerging as the industry standard for LLM
observability
([Microsoft, *AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).

AWS stresses that observability and auditability must be **designed from the start**, not added later:
Amazon Bedrock AgentCore Observability, for example, enables real-time monitoring tracking **latency,
token usage and error rates**
([AWS, Agentic AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html)).

### Typical reliability metrics of an agentic run

| Signal | What it indicates |
|--------|-------------------|
| **Completion** | did the run reach the end or abort? |
| **Rework / loops** | how many times the graph went back to redo work (a proxy for instability) |
| **Work distribution** | where effort concentrated (visits per node) |
| **Cost** | input/output tokens, prompt-cache hits |
| **Verification** | how much of the scope was actually verified (see SDD) |

These metrics let you **compare configurations** (a leaner graph vs the full pipeline with gates; memory
on vs off) instead of judging a run on the final answer alone.

## Link to the project

Agora Orchestrator separates execution from presentation via *observers* and aggregates events into run
metrics (steps, rework, token/cache, spec traceability). Details in
[`../observability.md`](../observability.md).

---

Previous: [09 — Spec-driven development](09-spec-driven-development.md) · Next:
[11 — Human-in-the-loop](11-human-in-the-loop.md).
