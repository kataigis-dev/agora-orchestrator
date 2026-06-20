---
type: concept
title: Human-in-the-Loop (HITL)
tags: [hitl, approval, human, safety]
related: [agora-orchestrator, agora-cli, agora-api, shared-knowledge-base]
created: 2026-06-17
updated: 2026-06-19
---

# Human-in-the-Loop (HITL)

Meccanismo di approvazione umana per azioni critiche. Permette di mettere in pausa l'esecuzione di un agente e attendere conferma prima di procedere.

## Configurazione

```yaml
agents:
  deployer:
    approvals: [deploy_to_production, delete_database]
```

L'elenco `approvals` definisce i nomi di tool (function-calling) che richiedono approvazione umana esplicita prima di essere eseguiti.

## Interfaccia

```csharp
// Agora/HumanInTheLoop/IApprovalHandler.cs
public sealed record ApprovalRequest
{
    public required string AgentId { get; init; }
    public required string FunctionName { get; init; }
    public string Arguments { get; init; } = "";
}

public interface IApprovalHandler
{
    Task<bool> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
}
```

## Implementazioni

| Classe | Contesto | Comportamento |
|--------|----------|---------------|
| `ConsoleApprovalHandler` | CLI | Stampa la richiesta e attende `y/n` da tastiera |
| `FakeApprovalHandler` | Test | Approva o rifiuta automaticamente (configurabile) |
| `PendingApprovalHandler` | API | Accumula richieste in attesa di risposta tramite `ApprovalGate` |

## Flusso

1. L'agente (tramite `IToolAgentFactory`) sta per eseguire un tool
2. Se il tool name è nella lista `approvals` dell'agente, viene chiamato `IApprovalHandler.RequestAsync(ApprovalRequest)`
3. Se l'handler restituisce `true` → il tool viene eseguito
4. Se restituisce `false` → il tool non viene eseguito (nessuna eccezione, la chiamata viene saltata)

## Test

`tests/Agora.Api.Tests/ApprovalFlowTests.cs` — test di integrazione del flusso HITL via API.

## Risoluzione conflitti (secondo canale HITL)

Oltre all'approvazione sì/no dei tool, esiste un canale HITL dedicato ai conflitti
della knowledge base scrivibile: `IConflictResolver` con esiti a tre vie
(`KeepExisting` / `KeepNew` / `Merge`). Si attiva quando un agente non riesce a
risolvere da solo un conflitto in scrittura sul RAG. Vedi [[shared-knowledge-base]].

## Note di sicurezza

- Il HITL è l'unico meccanismo built-in per prevenire azioni distruttive automatizzate
- In produzione, implementare `IApprovalHandler` con notifica via webhook, Slack, o UI dedicata
- Non dipendere esclusivamente da `FakeApprovalHandler` in ambienti non-test
