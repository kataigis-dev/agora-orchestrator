# 99 — Sources

Bibliography of the primary sources used in this folder. All belong to reference vendors and projects —
**Anthropic, Google, Microsoft, Amazon (AWS)** — and the open specifications. Last verified: June 2026.

## Anthropic

- *Building Effective Agents* — workflows vs agents, the augmented LLM, the five workflow patterns,
  autonomous agents, design principles (simplicity, transparency, ACI).
  <https://www.anthropic.com/research/building-effective-agents>
- *Effective context engineering for AI agents* — context management, prompt structure, memory,
  compaction, structured note-taking.
  <https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents>
- *Writing tools for agents* — best practices for tool design (ACI).
  <https://www.anthropic.com/engineering/writing-tools-for-agents>
- *Introducing the Model Context Protocol* — MCP announcement and rationale.
  <https://www.anthropic.com/news/model-context-protocol>

## Google

- *Choose a design pattern for your agentic AI system* — the definition of an agent, components, and the
  full taxonomy of patterns (single-agent, sequential, parallel, loop, review-and-critique, iterative
  refinement, coordinator, hierarchical, swarm, ReAct, HITL, custom logic).
  <https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system>
- *Choose your agentic AI architecture components* — architectural components.
  <https://docs.cloud.google.com/architecture/choose-agentic-ai-architecture-components>
- *Developer's guide to multi-agent patterns in ADK* — multi-agent patterns in the Agent Development Kit.
  <https://developers.googleblog.com/developers-guide-to-multi-agent-patterns-in-adk/>
- *Secure AI Framework (SAIF)* — AI-specific risks (prompt injection, data poisoning, model theft, source
  tampering, vulnerable integrations) and controls.
  <https://saif.google/> · <https://saif.google/secure-ai-framework/risks>

## Microsoft

- *AI Agent Orchestration Patterns* (Azure Architecture Center) — the complexity spectrum and the five
  patterns: sequential, concurrent, group chat (maker-checker), handoff, magentic.
  <https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns>
- *Microsoft Agent Framework — Overview* — agents and workflows, foundational blocks, when to use an
  agent vs a workflow, relationship with Semantic Kernel and AutoGen.
  <https://learn.microsoft.com/en-us/agent-framework/overview/>
- *Introducing Microsoft Agent Framework* (Foundry blog) — the open-source SDK/runtime, orchestration
  patterns, MCP/A2A, OpenTelemetry, durability/checkpointing, HITL, connectors.
  <https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/>
- *AI Agents in Production: Observability & Evaluation* — deterministic metrics vs LLM-as-judge,
  structured output, traces, OpenTelemetry.
  <https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/>
- *Responsible AI — Principles and Approach* — the six principles of responsible AI.
  <https://www.microsoft.com/en-us/ai/principles-and-approach> ·
  <https://learn.microsoft.com/en-us/azure/machine-learning/concept-responsible-ai>
- *Multi-Agent Solutions with Semantic Kernel and A2A* — the A2A protocol.
  <https://devblogs.microsoft.com/agent-framework/guest-blog-building-multi-agent-solutions-with-semantic-kernel-and-a2a-protocol/>

## Amazon (AWS)

- *Well-Architected Agentic AI Lens* — the six pillars adapted to agents, scope boundaries, guardrails,
  observability.
  <https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html>
- *Agentic AI — Generative AI Lens* — agent characteristics (many inferences, autonomous actions,
  non-determinism).
  <https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/agentic-ai.html>
- *GENSEC05-BP01 — least privilege for agentic workflows*.
  <https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/gensec05-bp01.html>
- *What is RAG?* — the definition of RAG, chunking, embedding, vector store, how it works.
  <https://aws.amazon.com/what-is/retrieval-augmented-generation/>
- *Amazon Bedrock Agents*.
  <https://aws.amazon.com/bedrock/agents/>

## Specifications and open standards

- *Model Context Protocol* — site and specification.
  <https://modelcontextprotocol.io/> · <https://modelcontextprotocol.io/specification/2025-11-25>
- *Model Context Protocol* (encyclopedic entry summarizing the architecture and primitives).
  <https://en.wikipedia.org/wiki/Model_Context_Protocol>

## Other references

- NVIDIA — *What Is Retrieval-Augmented Generation aka RAG* (an introductory summary of the RAG flow).
  <https://blogs.nvidia.com/blog/what-is-retrieval-augmented-generation/>

---

Back to the [index](README.md).
