# 99 — Fonti

Bibliografia delle fonti primarie usate in questa cartella. Tutte appartengono a fornitori e progetti di
riferimento: **Anthropic, Google, Microsoft, Amazon (AWS)** e le specifiche aperte. Ultima verifica:
giugno 2026.

## Anthropic

- *Building Effective Agents* — workflow vs agenti, LLM aumentato, i cinque pattern di workflow, agenti
  autonomi, principi di progettazione (semplicità, trasparenza, ACI).
  <https://www.anthropic.com/research/building-effective-agents>
- *Effective context engineering for AI agents* — gestione del contesto, struttura del prompt, memoria,
  compattazione, note strutturate.
  <https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents>
- *Writing tools for agents* — buone pratiche di progettazione degli strumenti (ACI).
  <https://www.anthropic.com/engineering/writing-tools-for-agents>
- *Introducing the Model Context Protocol* — annuncio e motivazione di MCP.
  <https://www.anthropic.com/news/model-context-protocol>

## Google

- *Choose a design pattern for your agentic AI system* — definizione di agente, componenti, e la
  tassonomia completa dei pattern (single-agent, sequential, parallel, loop, review-and-critique,
  iterative refinement, coordinator, hierarchical, swarm, ReAct, HITL, custom logic).
  <https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system>
- *Choose your agentic AI architecture components* — componenti architetturali.
  <https://docs.cloud.google.com/architecture/choose-agentic-ai-architecture-components>
- *Developer's guide to multi-agent patterns in ADK* — pattern multi-agente nell'Agent Development Kit.
  <https://developers.googleblog.com/developers-guide-to-multi-agent-patterns-in-adk/>
- *Secure AI Framework (SAIF)* — rischi specifici dell'IA (prompt injection, data poisoning, model
  theft, source tampering, vulnerable integrations) e controlli.
  <https://saif.google/> · <https://saif.google/secure-ai-framework/risks>

## Microsoft

- *AI Agent Orchestration Patterns* (Azure Architecture Center) — spettro di complessità e i cinque
  pattern: sequential, concurrent, group chat (maker-checker), handoff, magentic.
  <https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/ai-agent-design-patterns>
- *Microsoft Agent Framework — Overview* — agenti e workflow, blocchi fondamentali, quando usare agente
  vs workflow, rapporto con Semantic Kernel e AutoGen.
  <https://learn.microsoft.com/en-us/agent-framework/overview/>
- *Introducing Microsoft Agent Framework* (Foundry blog) — SDK/runtime open source, pattern di
  orchestrazione, MCP/A2A, OpenTelemetry, durabilità/checkpointing, HITL, connettori.
  <https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/>
- *AI Agents in Production: Observability & Evaluation* — metriche deterministiche vs LLM-as-judge,
  output strutturato, tracce, OpenTelemetry.
  <https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/>
- *Responsible AI — Principles and Approach* — i sei principi di IA responsabile.
  <https://www.microsoft.com/en-us/ai/principles-and-approach> ·
  <https://learn.microsoft.com/en-us/azure/machine-learning/concept-responsible-ai>
- *Multi-Agent Solutions with Semantic Kernel and A2A* — protocollo A2A.
  <https://devblogs.microsoft.com/agent-framework/guest-blog-building-multi-agent-solutions-with-semantic-kernel-and-a2a-protocol/>

## Amazon (AWS)

- *Well-Architected Agentic AI Lens* — sei pilastri adattati agli agenti, confini di scopo, guardrail,
  osservabilità.
  <https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html>
- *Agentic AI — Generative AI Lens* — caratteristiche degli agenti (molte inferenze, azioni autonome,
  non-determinismo).
  <https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/agentic-ai.html>
- *GENSEC05-BP01 — least privilege per i workflow agentici*.
  <https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/gensec05-bp01.html>
- *What is RAG?* — definizione di RAG, chunking, embedding, vector store, funzionamento.
  <https://aws.amazon.com/what-is/retrieval-augmented-generation/>
- *Amazon Bedrock Agents*.
  <https://aws.amazon.com/bedrock/agents/>

## Specifiche e standard aperti

- *Model Context Protocol* — sito e specifica.
  <https://modelcontextprotocol.io/> · <https://modelcontextprotocol.io/specification/2025-11-25>
- *Model Context Protocol* (voce enciclopedica con sintesi di architettura e primitive).
  <https://en.wikipedia.org/wiki/Model_Context_Protocol>

## Altre referenze

- NVIDIA — *What Is Retrieval-Augmented Generation aka RAG* (sintesi divulgativa del flusso RAG).
  <https://blogs.nvidia.com/blog/what-is-retrieval-augmented-generation/>

---

Torna all'[indice](README.md).
