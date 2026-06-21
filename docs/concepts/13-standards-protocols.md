# 13 — Standards and protocols

The agentic ecosystem is converging on a few **open standards** that reduce *lock-in* and let components
from different vendors interoperate. Three are the most relevant.

## MCP — Model Context Protocol

**Problem solved**: connecting an agent to **tools and data**.

An open standard from Anthropic (Nov 2024), a client–server architecture over JSON-RPC 2.0, with three
primitives (*tools*, *resources*, *prompts*). It has become the de-facto industry standard for giving
agents access to external capabilities. Covered in detail in chapter [04 — MCP](04-mcp.md).
Source: [modelcontextprotocol.io](https://modelcontextprotocol.io/),
[Anthropic](https://www.anthropic.com/news/model-context-protocol).

## A2A — Agent-to-Agent

**Problem solved**: letting **different agents talk to each other**, even if built with different
frameworks or *runtimes*.

While MCP connects the agent to tools, **A2A** standardizes **agent↔agent** collaboration: discovering an
agent's capabilities, exchanging messages and delegating tasks across system boundaries. It is supported,
among others, by Google and Microsoft (which integrates it into its Agent Framework and Semantic Kernel)
([Microsoft, *Building Multi-Agent Solutions with Semantic Kernel and A2A*](https://devblogs.microsoft.com/agent-framework/guest-blog-building-multi-agent-solutions-with-semantic-kernel-and-a2a-protocol/)).
MCP and A2A are **complementary**: the former for tools, the latter for agents.

## OpenTelemetry (GenAI)

**Problem solved**: uniform **observability** of traces and metrics.

OpenTelemetry is the industry standard for traces, metrics and logs; its **semantic conventions for
GenAI** are defining how to uniformly represent model calls, token usage and tool invocations. Microsoft
points to it as the emerging standard for LLM observability
([Microsoft, *AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).
See [10 — Evaluation and observability](10-evaluation-observability.md).

## Overall picture

```
        ┌─────────────────────────────────────────────┐
        │                  AGENT / RUNTIME             │
        │                                              │
   A2A  │   ◀── other agents (cross-runtime)           │
  ◀────▶│                                              │
        │   MCP ──▶ external tools / data              │
        │                                              │
        │   OpenTelemetry ──▶ traces, metrics, logs    │
        └─────────────────────────────────────────────┘
```

| Standard | Connects | Promoted by |
|----------|----------|-------------|
| **MCP** | agent ↔ tools/data | Anthropic (+ industry adoption) |
| **A2A** | agent ↔ agent | Google, Microsoft, etc. |
| **OpenTelemetry GenAI** | system ↔ observability | CNCF / industry |

## Why adopt open standards

- **Interoperability**: components from different vendors work together.
- **Reuse**: a tool/agent exposed once serves many applications.
- **Reduced lock-in**: you swap model or framework without rewriting the integrations.
- **Governance**: the standardized boundaries (client–server, A2A messages) are natural places for
  permissions and auditing ([12](12-security-governance.md)).

---

Previous: [12 — Security and governance](12-security-governance.md) · Next:
[14 — Glossary](14-glossary.md).
