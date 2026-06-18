---
type: concept
title: MCP Tools
tags: [mcp, tools, model-context-protocol, external]
related: [agora-agent-framework, skills, agora-orchestrator]
created: 2026-06-17
updated: 2026-06-17
---

# MCP Tools (Model Context Protocol)

Integrazione con server MCP esterni per esporre tool al sistema (filesystem, rete, database, ecc.).

## Cos'è MCP

Il **Model Context Protocol** è uno standard aperto che permette agli LLM di invocare tool implementati in processi separati tramite un protocollo stdio/JSON-RPC.

## Configurazione YAML

```yaml
mcp:
  servers:
    filesystem:
      command: npx
      args: ["-y", "@modelcontextprotocol/server-filesystem", "."]
    custom-tool:
      command: python
      args: ["my_tool_server.py"]

agents:
  builder:
    tools: [write_file, read_file, list_directory]
```

## Funzionamento

1. `McpToolSession` avvia il processo esterno tramite stdio
2. Esegue l'handshake MCP (initialize / list_tools)
3. I tool disponibili vengono esposti all'agente come function-calling tools
4. Quando l'agente chiama un tool, `McpToolSession` serializza la chiamata e legge la risposta

## Classe `McpToolSession`

```csharp
// Agora.AgentFramework/McpToolSession.cs
// Gestisce il ciclo di vita di una sessione MCP:
// - Avvio del processo
// - Inizializzazione protocollo
// - Esposizione dei tool disponibili
// - Chiamata sincrona ai tool
```

## Sicurezza

- I tool MCP girano in processi separati — l'accesso al filesystem è limitato al path configurato
- Configurare `args` con attenzione: evitare path assoluti o permessi eccessivi
- Non esporre all'agente tool che permettono esecuzione arbitraria di codice senza approvazione HITL

## Built-in Filesystem Tools

Agora include quattro tool filesystem **built-in** (nessun server MCP esterno necessario), implementati in `BuiltInFileTools.cs` con `System.IO`:

| Tool | Descrizione |
|------|-------------|
| `read_file` | Legge un file dal disco (`File.ReadAllTextAsync`) |
| `write_file` | Scrive un file sul disco (`File.WriteAllTextAsync`) |
| `search_files` | Cerca file per glob pattern (`Directory.EnumerateFiles`) |
| `list_directory` | Elenca il contenuto di una directory (`Directory.EnumerateFileSystemEntries`) |

Vengono caricati nel `AgentFrameworkAgent` prima dei tool MCP. Usano gli stessi nomi dei tool MCP equivalenti per compatibilità drop-in.

Configurazione YAML:
```yaml
agents:
  builder:
    tools: [read_file, write_file, search_files, list_directory]
```

Nessuna configurazione server MCP richiesta.

## Esempi

- `examples/agora-tools.yaml` — pipeline con skill + MCP tools
- `examples/agora-generate-api.yaml` — generazione codice con MCP + review loop
