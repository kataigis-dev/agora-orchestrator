# Concepts and foundations of agentic systems

This folder gathers the **conceptual documentation** of the Agora Orchestrator project. It is a
self-contained knowledge base: it starts from the basic definitions (what a *large language model* is,
what an *agent* is, what *RAG* is) and works up to the **standards and patterns for agentic workflows**
adopted by the industry.

The notions are drawn and synthesized from reliable primary sources — **Anthropic, Google, Microsoft and
Amazon (AWS)** — plus the open specifications (Model Context Protocol, OpenTelemetry). Each chapter cites
its specific sources; the complete list is in [99-fonti.md](99-fonti.md).

> This folder is **conceptual and independent of the code**. For Agora Orchestrator's technical
> documentation (YAML configuration, CLI, code architecture) see the [`../`](../index.md) folder. Chapter
> [15-mappatura-agora.md](15-mappatura-agora.md) connects every concept to its concrete implementation in
> the project.

## Index

| # | Chapter | Content |
|---|---------|---------|
| 01 | [Foundations: LLMs and prompting](01-fondamenti-llm.md) | Language models, tokens, context window, prompts, temperature, hallucinations, embeddings |
| 02 | [Agents](02-agenti.md) | Definition of an agent, the augmented LLM, components, reasoning loop (ReAct), autonomy, *workflow vs agent* |
| 03 | [Tools and function calling](03-strumenti-function-calling.md) | Tool use, function calling, agent-computer interface (ACI), tool design and approval |
| 04 | [Model Context Protocol (MCP)](04-mcp.md) | Open standard for connecting agents to data and tools: architecture, primitives, transports |
| 05 | [RAG — Retrieval-Augmented Generation](05-rag.md) | The full pipeline (ingest, chunking, embedding, vector store, retrieval, generation), variants, evaluation |
| 06 | [Memory and context engineering](06-memoria-contesto.md) | Context management, short/long-term memory, compaction, structured note-taking |
| 07 | [Agentic workflow patterns](07-workflow-pattern.md) | Anthropic's patterns: prompt chaining, routing, parallelization, orchestrator-workers, evaluator-optimizer |
| 08 | [Multi-agent orchestration](08-orchestrazione-multi-agente.md) | Microsoft and Google multi-agent patterns, the complexity spectrum, the A2A protocol |
| 09 | [Spec-driven development](09-spec-driven-development.md) | Machine-checkable specifications, deterministic gates, requirement↔task↔test traceability |
| 10 | [Evaluation and observability](10-valutazione-osservabilita.md) | Evals, deterministic metrics vs LLM-as-judge, traces, OpenTelemetry, reliability metrics |
| 11 | [Human-in-the-loop](11-human-in-the-loop.md) | Human oversight, approvals, checkpoints |
| 12 | [Security and governance](12-sicurezza-governance.md) | AWS Well-Architected Agentic AI Lens, least privilege, guardrails, SAIF, Responsible AI |
| 13 | [Standards and protocols](13-standard-protocolli.md) | MCP, A2A, OpenTelemetry GenAI — a recap of the emerging standards |
| 14 | [Glossary](14-glossario.md) | Quick definitions of the terms |
| 15 | [Mapping onto Agora Orchestrator](15-mappatura-agora.md) | Where each concept lives in the project |
| 16 | [Microsoft Agent Framework](16-microsoft-agent-framework.md) | Microsoft's open-source framework (agents + workflows), the basis of Agora's integration layer |
| 17 | [Execution flow](17-flusso-di-esecuzione.md) | How the app works step by step, from command to result — for absolute beginners |
| 18 | [Class reference](18-riferimento-classi.md) | Every class/interface/record in the code and what it is for, grouped by folder |
| 99 | [Sources](99-fonti.md) | Full bibliography with URLs |

## How to read this

- Starting from zero: read the chapters in order (01 → 15).
- If you already know LLMs: jump to [02 Agents](02-agenti.md).
- If you care about **orchestration patterns**: chapters [07](07-workflow-pattern.md) and
  [08](08-orchestrazione-multi-agente.md) are the heart of the agentic-workflow standards.
- If you want the link to the code: [15-mappatura-agora.md](15-mappatura-agora.md).
- If you want to understand **how the app runs** and what each class does: [17 — Execution flow](17-flusso-di-esecuzione.md) and [18 — Class reference](18-riferimento-classi.md).

## A note on terminology

The field is young and the terms are not yet fully standardized: the same pattern has different names
depending on the vendor (for example *routing*, *handoff*, *triage* and *dispatch* often denote the same
idea). Where useful, we note the synonyms. The practical rule, repeated by every source, is to **start
with the simplest solution** and add complexity only when a measurable gain justifies it.
