# Agenti

## Tipi di agente

### Agent (core)

Definito in `src/Agora/Agents/Agent.cs`. Agente base con:
- `Id` — identificatore unico
- `Model` — riferimento al modello configurato
- `Role` / `SystemPrompt` — istruzioni di sistema
- `Tools` — lista di nomi di tools MCP abilitati
- `Skills` — skills da caricare
- `MaxRetries` — tentativi massimi in caso di errore
- `RequireApproval` — se true, richiede conferma umana prima di eseguire

### AgentFrameworkAgent

Definito in `src/Agora.AgentFramework/AgentFrameworkAgent.cs`. Estende Agent con:
- Integrazione `Microsoft.Extensions.AI.IChatClient` per chiamate ai modelli
- Gestione tool calls (built-in filesystem + MCP)
- Supporto `StreamingChatClient` per risposte in streaming
- Memorizzazione cronologia conversazione

## Tools

Gli agenti possono usare strumenti built-in (filesystem) o esterni tramite MCP (Model Context Protocol).

### Built-in filesystem tools

I tool `read_file`, `write_file`, `search_files`, `list_directory` sono implementati nativamente via `System.IO` e non richiedono server MCP:

```yaml
agents:
  builder:
    tools: [read_file, write_file, search_files, list_directory]
```

### MCP tools

Per tool non filesystem (GitHub, Brave Search, puppeteer, server custom), serve un server MCP:

```yaml
mcp:
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
agents:
  builder:
    tools: [write_file, read_file, search_files]
```

Entrambi i tipi di tool si abilitano con la stessa lista `tools:` nell'agente. I built-in vengono caricati prima degli MCP.

### Discovery automatico (MCP)

All'avvio, il sistema:
1. Avvia i server MCP configurati
2. Scopre i tools disponibili tramite `tools/list`
3. Filtra in base alla lista `tools:` dell'agente
4. Espone i tools filtrati al modello

## Skills

Le skills sono prompt specializzati caricati da file `.md`:

```yaml
skills:
  summarize:
    location: examples/skills/summarize/SKILL.md
agents:
  summarizer:
    skills: [summarize]
```

Il contenuto dello SKILL.md viene preposto al system prompt dell'agente.

## Approvazione umana (HITL)

Quando `require_approval: true`, l'agente:

1. Genera una risposta
2. La mette in pausa in attesa di approvazione
3. Può essere approvata (`approve`) o rifiutata (`deny`) tramite CLI o API
4. Se rifiutata, l'agente riceve feedback e rielabora

### Console approval

In CLI, l'approvazione avviene interattivamente:

```
? Azione proposta: scrivere file output.txt
  Approvare? (y/n): y
```

### API approval

Tramite REST API:

```
POST /runs/{runId}/approve
{"approved": true, "feedback": "Ok"}
```

## Passaggio di contesto

Gli agenti in un grafo condividono il contesto tramite `AgentContext`:
- `Messages` — cronologia della conversazione
- `State` — stato corrente (running, waiting_approval, completed, failed)
- `Output` — output dell'ultima esecuzione
- `Metadata` — dizionario chiave-valore per dati arbitrari
