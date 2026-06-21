# 12 — Security and governance

Agents **act**: they invoke tools and modify data **without explicit human instruction at every step**,
on **non-deterministic** decisions ([01](01-fondamenti-llm.md)). This introduces risk dimensions that
traditional software does not have. All the sources converge on one idea: security must be **designed from
the start**, not added later.

## Why agents are different (AWS)

In its *Well-Architected Agentic AI Lens*, AWS lists the characteristics that design must address
explicitly
([AWS, Agentic AI — Generative AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/agentic-ai.html)):

- agents **reason through many calls** of inference and tool invocations;
- they **invoke tools and modify data** without human instruction at every step;
- LLM decisions are **inherently non-deterministic**.

The design consequence: "every agent operates within **explicitly defined scope boundaries**, with
**guardrails** that constrain behavior regardless of the inputs received".

## AWS Well-Architected Agentic AI Lens

The lens organizes the best practices around the **six pillars** of the Well-Architected Framework,
adapted to agents
([AWS, Agentic AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html)):

| Pillar | For agents it means |
|--------|---------------------|
| Operational Excellence | prompt-lifecycle management, behavioral monitoring |
| Security | scope boundaries, *least privilege*, guardrails, auditing |
| Reliability | handling non-determinism, stop conditions, error recovery |
| Performance Efficiency | efficient use of tokens and calls |
| Cost Optimization | controlling per-run cost (tokens, number of steps) |
| Sustainability | responsible use of compute resources |

### Least privilege for agentic workflows

A cornerstone practice (GENSEC05-BP01): "permission boundaries should provide access **only** to the
systems and data sources necessary to generate a response", and roles must be built with **least
privilege**
([AWS, GENSEC05-BP01](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/gensec05-bp01.html)).
In practice: a per-agent tool allow-list, restricted credentials, no "just-in-case" access.

### Observability and auditability by design

AWS recommends building them into the architecture from the start (e.g. tracking latency, token usage,
error rates). See [10 — Evaluation and observability](10-valutazione-osservabilita.md).

## Google: Secure AI Framework (SAIF)

SAIF addresses risks across **the entire lifecycle** of AI, not just the model, aiming for systems that
are **secure by default** ([saif.google](https://saif.google/)). The main AI-specific risks SAIF
identifies ([SAIF, *Top Risks*](https://saif.google/secure-ai-framework/risks)):

| Risk | Description |
|------|-------------|
| **Prompt injection** | Malicious instructions "injected" into the prompt exploit the blurry boundary between *instructions* and *data*, altering the model's behavior |
| **Data poisoning** | Poisoning the data before ingestion, during training, or in storage |
| **Model theft** | Unauthorized appropriation of the model (theft of IP/functionality) |
| **Model source tampering** | Tampering with code, frameworks or weights (supply-chain attacks) |
| **Vulnerable integrations** | Vulnerabilities in plugins/libraries/apps that interact with the model |

**Prompt injection** is especially relevant for agents that read external content (web, documents, tool
outputs): that content may contain hostile instructions. Mitigations: treat external input as **untrusted
data**, isolate instructions and data, validate outputs, and don't give the agent more power than it
needs (*least privilege*).

## Microsoft: Responsible AI

Microsoft frames governance around **six principles** of responsible AI
([Microsoft, *Responsible AI*](https://www.microsoft.com/en-us/ai/principles-and-approach);
[Microsoft Learn](https://learn.microsoft.com/en-us/azure/machine-learning/concept-responsible-ai)):

1. **Fairness** — treat people fairly, avoid bias;
2. **Reliability & Safety** — operate safely and reliably, detect and mitigate harmful outcomes;
3. **Privacy & Security** — protect data and defend against attacks;
4. **Inclusiveness** — engage and empower everyone;
5. **Transparency** — make behavior and limits understandable;
6. **Accountability** — whoever designs/deploys answers for the system; humans keep "**meaningful
   control** over highly autonomous systems" (the foundation of [HITL](11-human-in-the-loop.md)).

## Guardrails: an operational summary

Combining the four sources, the **guardrails** of an agentic system include:

- **Explicit scope boundaries** for each agent (what it can and cannot do);
- **Least privilege**: tool allow-lists and minimal credentials;
- **Untrusted input**: treat web/documents/tool outputs as potentially hostile (anti prompt-injection);
- **Deterministic verification** instead of trusting the narration (see [SDD](09-spec-driven-development.md));
- **Safe command execution**: no arbitrary shell, commands only from an allow-list, arguments passed as
  separate tokens, timeouts and killing the process tree;
- **HITL** on high-risk or irreversible steps;
- **Observability and auditing** from the design stage;
- **Clear stop conditions** to avoid infinite loops and uncontrolled consumption.

## Link to the project

Agora Orchestrator applies several of these guardrails: per-agent tool allow-lists, HITL approvals, and —
for check execution — commands only from configuration, **with no shell**, with token-by-token
substitution and timeouts. See [`../spec.md`](../spec.md) and
[15 — Mapping onto Agora](15-mappatura-agora.md).

---

Previous: [11 — Human-in-the-loop](11-human-in-the-loop.md) · Next:
[13 — Standards and protocols](13-standard-protocolli.md).
