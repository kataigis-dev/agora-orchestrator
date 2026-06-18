# MCP — Model Context Protocol

## Cos'è

MCP (Model Context Protocol) è un protocollo che permette agli agenti AI di interagire con strumenti e risorse esterne in modo standardizzato.

## Built-in filesystem tools

Agora include **tools filesystem built-in** che non richiedono un server MCP. Gli agenti con tools `read_file`, `write_file`, `search_files`, `list_directory` abilitati li usano automaticamente tramite `System.IO`.

```yaml
agents:
  builder:
    tools: [read_file, write_file, search_files, list_directory]
    # Nessun server MCP necessario!
```

I built-in tools hanno la stessa interfaccia dei corrispondenti tool MCP, quindi puoi passare da MCP a built-in senza cambiare configurazione agente — basta rimuovere il server MCP.

## Architettura (con MCP esterno)

```
Agente AI  ──►  MCP Client  ──►  MCP Server (stdio/HTTP)
                                    │
                                    ├── read_file
                                    ├── write_file
                                    ├── search_files
                                    └── ...
```

## Configurazione

### Server stdio (default)

```yaml
mcp:
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
```

Il processo viene avviato come child process e comunicazione via stdin/stdout in JSON-RPC.

### Server HTTP

```yaml
mcp:
  servers:
    custom:
      type: http
      url: http://localhost:3000/mcp
```

Comunicazione via HTTP POST con body JSON-RPC.

## Tools discovery

Al'avvio del server MCP:

1. Agora invia `tools/list` al server
2. Il server risponde con la lista di tools disponibili (nome, descrizione, parametri)
3. Agora filtra in base alla lista `tools:` dell'agente
4. I tools filtrati vengono esposti al modello come function calling

## Esecuzione tool

Quando il modello richiama un tool:

1. Agora riceve la tool call dal modello
2. Invia `tools/call` al server MCP con i parametri
3. Il server esegue l'operazione e restituisce il risultato
4. Agora inoltra il risultato al modello

## Approval gates

```yaml
mcp:
  approval: auto    # "none" (default) | "always" | "auto"
```

- `none` — i tool vengono eseguiti senza conferma
- `always` — ogni tool call richiede conferma umana
- `auto` — i tool read-only sono automatici, i tool di scrittura richiedono conferma

## Server integrati

| Server | Comando | Tools |
|---|---|---|
| filesystem | `npx @modelcontextprotocol/server-filesystem` | read_file, write_file, search_files, list_directory |
| github | `npx @modelcontextprotocol/server-github` | create_pr, list_issues, search_code |
| puppeteer | `npx @modelcontextprotocol/server-puppeteer` | screenshot, click, type, navigate |
| brave-search | `npx @modelcontextprotocol/server-brave-search` | web_search, news_search |
