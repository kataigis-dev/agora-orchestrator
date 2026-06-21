# 16 — Microsoft Agent Framework

This chapter goes deeper into a **concrete framework**, the **Microsoft Agent Framework**, both because it
is one of the reference implementations of the concepts seen so far, and because Agora Orchestrator relies
on it (the `Agora.AgentFramework` layer uses **Microsoft.Extensions.AI** and **Microsoft.Agents.AI**).

## What it is

The **Microsoft Agent Framework** is an **open-source SDK and runtime** for building **agents** and
**multi-agent workflows** in **.NET and Python**
([Microsoft Learn, *Agent Framework Overview*](https://learn.microsoft.com/en-us/agent-framework/overview/)).
It is the **direct successor** of two earlier Microsoft projects, created by the same teams:

- **Semantic Kernel** — the *enterprise* foundations: session-based state management, type safety,
  *middleware*/filters, telemetry, broad support for models and embeddings;
- **AutoGen** (Microsoft Research) — the simple abstractions for single- and multi-agent patterns and the
  orchestration.

In short (Microsoft's words): "Agent Framework combines AutoGen's simple abstractions with Semantic
Kernel's enterprise features — session-based state management, type safety, middleware, telemetry — and
adds **graph-based workflows** for explicit multi-agent orchestration"
([Microsoft Foundry blog](https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/)).

## The two categories of capability

The framework offers two main categories, which mirror exactly the [workflow vs agent](02-agenti.md)
distinction:

| Category | What it is |
|----------|-----------|
| **Agents** | Individual agents that use an LLM to process inputs, call [tools](03-strumenti-function-calling.md) and [MCP servers](04-mcp.md), and generate responses. Supported providers: Microsoft Foundry, Azure OpenAI, OpenAI, Anthropic, Ollama and more |
| **Workflows** | **Graph-based** workflows connecting agents and functions for multi-step tasks, with **type-safe routing**, **checkpointing** and **human-in-the-loop** support |

### When to use an agent and when a workflow

The official guidance is clear (and matches chapter [02](02-agenti.md)):

| Use an **agent** when… | Use a **workflow** when… |
|------------------------|--------------------------|
| the task is open-ended or conversational | the process has well-defined steps |
| you need autonomous tool use and planning | you need explicit control over execution order |
| a single LLM call (with tools) suffices | multiple agents or functions must coordinate |

> Microsoft's design maxim, perfectly aligned with Anthropic's **simplicity** principle: *"if you can
> write a function to handle the task, do that instead of using an AI agent."*

## Foundational building blocks

Beyond agents and workflows, the framework provides
([Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/overview/)):

- **Model clients** (chat completions and responses);
- **Agent session** for **state** management;
- **Context providers** for an agent's **memory** (see [06](06-memoria-contesto.md));
- **Middleware** to **intercept** the agent's actions (logging, guardrails, filters);
- **MCP clients** for tool integration (see [04](04-mcp.md)).

## Supported orchestration patterns

The framework implements the multi-agent patterns described in chapter
[08](08-orchestrazione-multi-agente.md)
([Microsoft Foundry blog](https://devblogs.microsoft.com/foundry/introducing-microsoft-agent-framework-the-open-source-engine-for-agentic-ai-apps/)):

- **Sequential**;
- **Concurrent** (parallel);
- **Group chat** (incl. *maker-checker*);
- **Handoff** (dynamic transfer);
- **Magentic** (a manager with a *task ledger*).

## Connectivity, interoperability and robustness

| Area | Feature |
|------|---------|
| **Tools** | **MCP** for dynamic tool discovery |
| **Inter-agent** | **A2A** (*Agent-to-Agent*) for cross-runtime collaboration (see [13](13-standard-protocolli.md)) |
| **Integration** | *OpenAPI-first* design; enterprise connectors (Azure AI Foundry, Microsoft Graph, SharePoint, etc.) |
| **Memory** | *pluggable* memory across multiple backends (Redis, Postgres, Elasticsearch, …) |
| **Observability** | **OpenTelemetry** integrated (see [10](10-valutazione-osservabilita.md)) |
| **Durability** | long-running executions with **checkpointing** and **pause/resume** |
| **Governance** | **human-in-the-loop** approval workflows (see [11](11-human-in-the-loop.md)) |

## Relationship with Azure AI Foundry

The framework integrates with **Azure AI Foundry Agent Service** for secure cloud hosting (virtual
network integration, role-based access control, enterprise compliance). Azure AI Foundry uses Semantic
Kernel as its internal orchestration engine, of which Agent Framework is the evolution.

## Minimal example (.NET)

```csharp
using Microsoft.Agents.AI;
// ...
AIAgent agent = new AIProjectClient(endpoint, new AzureCliCredential())
    .AsAIAgent(model: "gpt-5.4-mini",
               instructions: "You are a friendly assistant. Keep your answers brief.");

Console.WriteLine(await agent.RunAsync("What is the largest city in France?"));
```

## A note on responsibility and third-party systems

Microsoft explicitly warns: using Agent Framework with **non-Azure** servers, agents, code or models
("Third-Party Systems") is done **at your own risk**; such systems are governed by their own license
terms. It is the developer's responsibility to review the data exchanged, manage compliance/geographic
boundaries, and implement their own **responsible AI** mitigations (metaprompt, content filters, safety
systems). See also [12 — Security and governance](12-sicurezza-governance.md).

## Relationship with Agora Orchestrator

Agora **is not** the Microsoft Agent Framework, but relies on it in its integration layer:

- the `Agora` core stays **framework-free** (BCL + YamlDotNet only): models, interfaces, the graph,
  validators and verification live here;
- `Agora.AgentFramework` builds the concrete agents on **Microsoft.Extensions.AI** /
  **Microsoft.Agents.AI** and the MCP clients on **ModelContextProtocol.Core**, keeping the rest of the
  system framework-independent.

This separation is a design choice: you benefit from the Microsoft ecosystem (agent abstractions,
function calling, MCP clients) without coupling the orchestration engine to a single vendor. See
[15 — Mapping onto Agora](15-mappatura-agora.md) and [architecture.md](../architecture.md).

---

Previous: [15 — Mapping onto Agora](15-mappatura-agora.md) · Next: [99 — Sources](99-fonti.md).
