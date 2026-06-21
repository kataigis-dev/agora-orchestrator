# 09 — Spec-driven development for agents

## The problem: trusting the narration

An LLM agent tends to **declare** success ("I implemented the function, the tests pass") even when it is
not true, because it optimizes for the plausibility of the text, not the truth (see
[01](01-llm-foundations.md)). If a workflow's *gates* trust this narration, the system becomes unreliable.
**Spec-driven development (SDD)** is the answer: turning control decisions from "*the model says it's
done*" into "*a deterministic check asserts reality*".

This principle is consistent with the recommendations of all the sources:

- Anthropic: insert **programmatic gates** between the steps of a workflow to verify progress
  ([*Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)).
- Microsoft: "use **deterministic metrics** for exact checks like tool-call correctness, and LLM-as-judge
  only for what requires judgment"
  ([*AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).
- AWS: an LLM's decisions are non-deterministic, so you need **guardrails** and explicit boundaries
  ([Agentic AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html)).

## What a machine-checkable specification is

Instead of a free-text specification (buried in prompts or fuzzy RAG entries), SDD uses a **structured**
specification with these properties:

- **Requirements with stable identifiers** (e.g. `R1`, `R2`), priorities and a lifecycle status
  (proposed → approved → implemented → verified).
- **Acceptance criteria** for each requirement: what defines "done", in a **machine-checkable** form (a
  test to run, a command, the existence of a file).
- **Implementation tasks** that **trace** back to the requirements they satisfy.
- A **validator** that enforces the invariants on every write (unique ids, no task without a requirement,
  no dangling references).

## The three pillars of SDD

### 1. Executable acceptance criteria
Each criterion is bound to a **check** that can actually be run: a test, a build command, the existence
of an artifact. The verdict comes from the process's **exit code**, not from the model's judgment.

### 2. Real check execution
Builds and tests are run as **real processes** (with the proper security precautions: a command
allow-list, no shell, timeouts — see [12](12-security-governance.md)). A requirement moves to "verified"
**only** when its checks pass deterministically.

### 3. Traceability and a completion gate
**Requirement ↔ task ↔ test** traceability is enforced: no task without a requirement, no approved
requirement without a task covering it, no requirement "verified" without a real check backing it. The
**completion** of the work is a deterministic verdict (COMPLETE/INCOMPLETE) computed over the
traceability matrix, not a declaration by the agent.

```
Requirement R1 ──covered by──▶ Task T1 ──evidence──▶ check "test" (exit 0) ✓ verified
Requirement R2 ──covered by──▶ Task T2 ──evidence──▶ check "build" (exit 1) ✗ NOT verified  ⟹ INCOMPLETE
```

## Why it is a reliability pattern

SDD combines two patterns already seen, in a "hardened" form:

- the **evaluator-optimizer** / **maker-checker** ([07](07-workflow-patterns.md),
  [08](08-multi-agent-orchestration.md)), but with a **deterministic** evaluator (a process, not an
  LLM);
- the **programmatic gates** of prompt chaining, applied to the whole specification.

The result is an agentic workflow in which "*the gates assert reality*": quality does not depend on the
model's self-assessment.

## Link to the project

Agora Orchestrator implements SDD in three phases (structured schema → real verification → traceability
gate). The technical details are in [`../spec.md`](../spec.md); the conceptual mapping is in
[15 — Mapping onto Agora](15-agora-mapping.md).

---

Previous: [08 — Multi-agent orchestration](08-multi-agent-orchestration.md) · Next:
[10 — Evaluation and observability](10-evaluation-observability.md).
