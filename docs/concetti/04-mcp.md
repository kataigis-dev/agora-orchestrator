# 04 — Model Context Protocol (MCP)

## Cos'è

Il **Model Context Protocol (MCP)** è uno **standard aperto** introdotto da Anthropic a novembre 2024
per standardizzare il modo in cui i sistemi di IA (gli LLM e gli agenti) si integrano e scambiano dati
con strumenti, sistemi e fonti di dati esterni
([Anthropic, *Introducing MCP*](https://www.anthropic.com/news/model-context-protocol)).

L'analogia diffusa è quella di una **"porta USB-C per l'IA"**: invece di scrivere un'integrazione
custom per ogni coppia modello–servizio, si espone il servizio una volta tramite MCP e qualunque
applicazione compatibile può collegarsi. Questo sostituisce integrazioni frammentate con un unico
protocollo, più semplice e affidabile.

Dal lancio, l'adozione è stata rapida: la comunità ha costruito migliaia di server MCP, sono disponibili
SDK per tutti i principali linguaggi e il protocollo è diventato di fatto lo standard per collegare
agenti a strumenti e dati ([modelcontextprotocol.io](https://modelcontextprotocol.io/)).

## Architettura: client–server

MCP definisce un'architettura **client–server** semplice, con messaggi **JSON-RPC 2.0**:

```
┌────────────────────┐         JSON-RPC 2.0          ┌────────────────────┐
│   Host / MCP client │ ◀───────────────────────────▶ │     MCP server      │
│  (l'app con l'LLM)  │                                │ (proprietario dati) │
└────────────────────┘                                └────────────────────┘
         │                                                       │
   un client per ogni                                  espone strumenti, risorse,
   server a cui si collega                              prompt verso una fonte dati
```

- **Host / Client**: l'applicazione che contiene l'agente/LLM e che *consuma* le capacità. Apre una
  connessione (un client) verso ciascun server.
- **Server**: un processo che *espone* le capacità di una fonte dati o di un servizio (file system,
  GitHub, un database, un knowledge base...).

Esistono server ufficiali e di comunità per Google Drive, Slack, GitHub, database come Postgres/SQLite,
browser web e molto altro.

## Le primitive di MCP

Un server MCP annuncia un insieme standardizzato di capacità, divise in tre **primitive**
([Wikipedia, *Model Context Protocol*](https://en.wikipedia.org/wiki/Model_Context_Protocol);
[modelcontextprotocol.io](https://modelcontextprotocol.io/)):

| Primitiva | Cosa è | Esempio |
|-----------|--------|---------|
| **Tools** (strumenti) | Funzioni invocabili che compiono azioni o calcoli | `write_file`, `query_db`, `search` |
| **Resources** (risorse) | Endpoint di dati in sola lettura, indirizzabili da un URI | il contenuto di un file, una riga di DB |
| **Prompts** | Template di istruzioni pre-scritti, riutilizzabili | un prompt "riassumi questo ticket" |

Gli **strumenti** sono la primitiva più usata dagli agenti, perché si mappano direttamente sul
[function calling](03-strumenti-function-calling.md): il client scopre gli strumenti del server e li
presenta al modello come funzioni chiamabili.

## Trasporti

MCP è agnostico rispetto al canale di trasporto. I due più comuni:

- **stdio**: il client avvia il server come processo locale e comunica su standard input/output. Ideale
  per strumenti locali (file system, CLI).
- **HTTP** (con streaming): il client si collega a un server remoto via HTTP. Ideale per servizi
  condivisi o in cloud.

## Perché conta per gli agenti

1. **Interoperabilità**: lo stesso server è usabile da più agenti e da più applicazioni, senza riscrivere
   integrazioni.
2. **Separazione delle responsabilità**: chi possiede i dati espone un server; chi costruisce l'agente
   consuma capacità senza conoscere i dettagli interni.
3. **Sicurezza e governance**: il confine client–server è un punto naturale dove applicare permessi,
   allow-list e audit (vedi [12](12-sicurezza-governance.md)).

## Protocolli affini

MCP risolve il collegamento **agente ↔ strumenti/dati**. Un problema diverso è il collegamento
**agente ↔ agente**: per quello sta emergendo il protocollo **A2A (Agent-to-Agent)**, trattato nel
capitolo [13 — Standard e protocolli](13-standard-protocolli.md).

---

Precedente: [03 — Strumenti](03-strumenti-function-calling.md) · Prossimo: [05 — RAG](05-rag.md).
