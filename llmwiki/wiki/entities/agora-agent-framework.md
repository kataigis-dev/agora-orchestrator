---
type: entity
title: Agora.AgentFramework
tags: [integration, openai, ollama, mcp, qdrant, dotnet]
related: [agora-orchestrator, mcp-tools, rag-pipeline, shared-knowledge-base, streaming]
created: 2026-06-17
updated: 2026-06-20
---

# Agora.AgentFramework

Satellite project providing the concrete integrations with LLM providers and external tools. The
`Agora` core depends only on interfaces — `AgentFramework` implements them.

## Main classes

| Class | Role |
|-------|------|
| `AgentFrameworkAgent` | `IAgent` implementation with tool-calling and HITL approvals |
| `AgentFrameworkChatProvider` | `IChatProvider`/`IStreamingChatProvider` over OpenAI/Ollama |
| `AgentFrameworkEmbedder` | `IEmbedder` for RAG — generates embeddings via the provider |
| `AgentFrameworkEmbedders` | Resolves config-driven embedders (`type: openai`/`ollama`) |
| `AgentFrameworkVectorStores` | Resolves config-driven vector stores (`type: qdrant`) |
| `QdrantVectorStore` | `IVectorStore` over a Qdrant server (gRPC) |
| `AgentFrameworkToolAgentFactory` | `IToolAgentFactory` — builds tool-enabled agents |
| `BuiltInFileTools`, `RagTools`, `AskAgentTool` | Built-in tools (filesystem, rag_search/rag_write, ask_agent) |
| `McpToolSession` | MCP session with an external server (stdio/HTTP) |
| `SkillTools` | Exposes skills as a `load_skill` tool |
| `ChatClients.cs` | Factory for OpenAI / Ollama / compatible clients |

## Supported providers

| Provider | Config key | Notes |
|----------|-----------|-------|
| OpenAI | `openai` | Requires `api_key_env` |
| Ollama | `ollama` | Requires `base_url` (default `http://localhost:11434`) |
| LM Studio / llama.cpp | any | `base_url` with `/v1` suffix |
| Any OpenAI-compatible | any | Same schema as Ollama |

## Notes

- The `/v1` suffix in `base_url` is required for OpenAI-compatible providers (the SDK builds paths
  relative to the base URL).
- Real embedders and the Qdrant store are injected from the edge via resolvers, so the core stays
  framework-free.
