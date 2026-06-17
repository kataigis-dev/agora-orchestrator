---
type: concept
title: Human-in-the-Loop (HITL)
tags: [hitl, approval, human, safety]
related: [agora-orchestrator, agora-cli, agora-api]
created: 2026-06-17
updated: 2026-06-17
---

# Human-in-the-Loop (HITL)

Meccanismo di approvazione umana per azioni critiche. Permette di mettere in pausa l'esecuzione di un agente e attendere conferma prima di procedere.

## Configurazione

```yaml
agents:
  deployer:
    approvals: [deploy_to_production, delete_database]
```

L'elenco `approvals` definisce le azioni che richiedono approvazione esplicita prima di essere eseguite.

## Interfaccia

```csharp
// Agora/HumanInTheLoop/IApprovalHandler.cs
public interface IApprovalHandler
{
    Task<bool> RequestApprovalAsync(string action, string context);
}
```

## Implementazioni

| Classe | Contesto | Comportamento |
|--------|----------|---------------|
| `ConsoleApprovalHandler` | CLI | Stampa l'azione e attende `y/n` da tastiera |
| `FakeApprovalHandler` | Test | Approva o rifiuta automaticamente (configurabile) |

## Flusso

1. L'agente sta per eseguire un'azione in `approvals`
2. `IApprovalHandler.RequestApprovalAsync(action, context)` viene invocato
3. Se l'handler restituisce `true` → l'azione viene eseguita
4. Se restituisce `false` → l'azione viene saltata / l'esecuzione si ferma

## Test

`tests/Agora.Api.Tests/ApprovalFlowTests.cs` — test di integrazione del flusso HITL via API.

## Note di sicurezza

- Il HITL è l'unico meccanismo built-in per prevenire azioni distruttive automatizzate
- In produzione, implementare `IApprovalHandler` con notifica via webhook, Slack, o UI dedicata
- Non dipendere esclusivamente da `FakeApprovalHandler` in ambienti non-test
