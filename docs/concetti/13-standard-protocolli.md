# 13 — Standard e protocolli

L'ecosistema agentico sta convergendo su alcuni **standard aperti** che riducono il *lock-in* e
permettono a componenti di fornitori diversi di interoperare. Tre sono i più rilevanti.

## MCP — Model Context Protocol

**Problema risolto**: collegare un agente a **strumenti e dati**.

Standard aperto di Anthropic (nov. 2024), architettura client–server su JSON-RPC 2.0, con tre primitive
(*tools*, *resources*, *prompts*). Diventato di fatto lo standard del settore per dare agli agenti
accesso a capacità esterne. Trattato in dettaglio nel capitolo [04 — MCP](04-mcp.md).
Fonte: [modelcontextprotocol.io](https://modelcontextprotocol.io/),
[Anthropic](https://www.anthropic.com/news/model-context-protocol).

## A2A — Agent-to-Agent

**Problema risolto**: far comunicare **agenti diversi tra loro**, anche se costruiti con framework o
*runtime* differenti.

Mentre MCP collega l'agente agli strumenti, **A2A** standardizza la collaborazione **agente↔agente**:
scoperta delle capacità di un agente, scambio di messaggi e delega di compiti attraverso confini di
sistema. È supportato, tra gli altri, da Google e da Microsoft (che lo integra nel suo Agent Framework e
in Semantic Kernel)
([Microsoft, *Building Multi-Agent Solutions with Semantic Kernel and A2A*](https://devblogs.microsoft.com/agent-framework/guest-blog-building-multi-agent-solutions-with-semantic-kernel-and-a2a-protocol/)).
MCP e A2A sono **complementari**: il primo per gli strumenti, il secondo per gli agenti.

## OpenTelemetry (GenAI)

**Problema risolto**: **osservabilità** uniforme di tracce e metriche.

OpenTelemetry è lo standard di settore per tracce, metriche e log; le sue **convenzioni semantiche per
la GenAI** stanno definendo come rappresentare in modo uniforme chiamate ai modelli, uso dei token e
invocazioni di strumenti. Microsoft lo indica come standard emergente per l'osservabilità degli LLM
([Microsoft, *AI Agents in Production*](https://microsoft.github.io/ai-agents-for-beginners/10-ai-agents-production/)).
Vedi [10 — Valutazione e osservabilità](10-valutazione-osservabilita.md).

## Quadro d'insieme

```
        ┌─────────────────────────────────────────────┐
        │                  AGENTE / RUNTIME            │
        │                                              │
   A2A  │   ◀── altri agenti (cross-runtime)           │
  ◀────▶│                                              │
        │   MCP ──▶ strumenti / dati esterni           │
        │                                              │
        │   OpenTelemetry ──▶ tracce, metriche, log    │
        └─────────────────────────────────────────────┘
```

| Standard | Collega | Promosso da |
|----------|---------|-------------|
| **MCP** | agente ↔ strumenti/dati | Anthropic (+ adozione di settore) |
| **A2A** | agente ↔ agente | Google, Microsoft, ecc. |
| **OpenTelemetry GenAI** | sistema ↔ osservabilità | CNCF / settore |

## Perché adottare standard aperti

- **Interoperabilità**: componenti di fornitori diversi lavorano insieme.
- **Riuso**: uno strumento/agente esposto una volta serve molte applicazioni.
- **Riduzione del lock-in**: si cambia modello o framework senza riscrivere le integrazioni.
- **Governance**: i confini standardizzati (client–server, messaggi A2A) sono punti naturali per
  permessi e audit ([12](12-sicurezza-governance.md)).

---

Precedente: [12 — Sicurezza e governance](12-sicurezza-governance.md) · Prossimo:
[14 — Glossario](14-glossario.md).
