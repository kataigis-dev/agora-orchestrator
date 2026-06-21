# 04 — Model Context Protocol (MCP)

## What it is

The **Model Context Protocol (MCP)** is an **open standard** introduced by Anthropic in November 2024 to
standardize the way AI systems (LLMs and agents) integrate and exchange data with external tools, systems
and data sources
([Anthropic, *Introducing MCP*](https://www.anthropic.com/news/model-context-protocol)).

The common analogy is a **"USB-C port for AI"**: instead of writing a custom integration for every
model–service pair, you expose the service once via MCP and any compatible application can connect. This
replaces fragmented integrations with a single, simpler and more reliable protocol.

Since launch, adoption has been rapid: the community has built thousands of MCP servers, SDKs are
available for all major languages, and the protocol has become the de-facto standard for connecting
agents to tools and data ([modelcontextprotocol.io](https://modelcontextprotocol.io/)).

## Architecture: client–server

MCP defines a simple **client–server** architecture, with **JSON-RPC 2.0** messages:

```
┌────────────────────┐         JSON-RPC 2.0          ┌────────────────────┐
│   Host / MCP client │ ◀───────────────────────────▶ │     MCP server      │
│   (the app + LLM)   │                                │   (data owner)      │
└────────────────────┘                                └────────────────────┘
         │                                                       │
   one client per                                      exposes tools, resources,
   server it connects to                               prompts toward a data source
```

- **Host / Client**: the application that contains the agent/LLM and *consumes* the capabilities. It
  opens a connection (a client) to each server.
- **Server**: a process that *exposes* the capabilities of a data source or service (a file system,
  GitHub, a database, a knowledge base...).

There are official and community servers for Google Drive, Slack, GitHub, databases such as
Postgres/SQLite, web browsers and much more.

## MCP's primitives

An MCP server announces a standardized set of capabilities, divided into three **primitives**
([Wikipedia, *Model Context Protocol*](https://en.wikipedia.org/wiki/Model_Context_Protocol);
[modelcontextprotocol.io](https://modelcontextprotocol.io/)):

| Primitive | What it is | Example |
|-----------|-----------|---------|
| **Tools** | Invocable functions that perform actions or computations | `write_file`, `query_db`, `search` |
| **Resources** | Read-only data endpoints, addressable by a URI | the contents of a file, a DB row |
| **Prompts** | Pre-written, reusable instruction templates | a "summarize this ticket" prompt |

**Tools** are the primitive most used by agents, because they map directly onto
[function calling](03-tools-function-calling.md): the client discovers the server's tools and
presents them to the model as callable functions.

## Transports

MCP is agnostic about the transport channel. The two most common:

- **stdio**: the client launches the server as a local process and communicates over standard
  input/output. Ideal for local tools (file system, CLI).
- **HTTP** (with streaming): the client connects to a remote server over HTTP. Ideal for shared or
  cloud services.

## Why it matters for agents

1. **Interoperability**: the same server is usable by multiple agents and multiple applications, without
   rewriting integrations.
2. **Separation of concerns**: whoever owns the data exposes a server; whoever builds the agent consumes
   capabilities without knowing the internal details.
3. **Security and governance**: the client–server boundary is a natural place to apply permissions,
   allow-lists and auditing (see [12](12-security-governance.md)).

## Related protocols

MCP solves the **agent ↔ tools/data** connection. A different problem is the **agent ↔ agent**
connection: for that the **A2A (Agent-to-Agent)** protocol is emerging, covered in chapter
[13 — Standards and protocols](13-standards-protocols.md).

---

Previous: [03 — Tools](03-tools-function-calling.md) · Next: [05 — RAG](05-rag.md).
