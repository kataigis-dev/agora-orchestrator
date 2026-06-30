# Wiki Index

## Entities

- [[agora-orchestrator]] — .NET 10 core library for multi-agent orchestration, framework-free
- [[agora-cli]] — CLI executable: `init`, `run`, `resume`, `ingest`, `validate`, `eval`
- [[agora-api]] — retired REST server (historical note; CLI + MCP are current)
- [[agora-agent-framework]] — OpenAI, Ollama, MCP integration (satellite project)

## Concepts

- [[agent-graph]] — Directed graph of LLM agents with signal-based routing
- [[edge-types]] — Edge types: `sequential`, `handoff`, `conditional`, `route`, `parallel`
- [[parallel-execution]] — Concurrent branch execution (fork/join)
- [[checkpointing]] — Durable execution: per-step state checkpoint and resume (`run --checkpoint`, `resume`)
- [[streaming]] — Token streaming (`IStreamingChatProvider`, `run --stream`)
- [[h2c-protocol]] — Structured `[TYPE:SUBTYPE]` protocol for agent→orchestrator communication
- [[signal]] — `<<signal name>>` tokens for conditional routing in natural mode
- [[communication-modes]] — Comparison of `h2c` (default) and `natural` modes
- [[handoff-context]] — Minimal context passing between agents (`handoff: true`)
- [[rag-pipeline]] — Retrieval-Augmented Generation pipeline: ingest, embed, retrieve, inject
- [[shared-knowledge-base]] — Writable RAG: `KnowledgeBase`, LLM conflict-judge, HITL resolution
- [[context-memory]] — Context compression via RAG: save and recall top-K (`memory: enabled`)
- [[skills]] — Reusable Markdown prompt files as agent tools
- [[mcp-tools]] — External tools via the Model Context Protocol (stdio)
- [[human-in-the-loop]] — Human approval for critical actions (HITL)
- [[guided-config]] — `agora init` command: interactive YAML config building

## Sources

## Queries

## Comparisons

## Synthesis
