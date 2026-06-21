# 11 — Human-in-the-loop (HITL)

## Cos'è

Il pattern **Human-in-the-loop (HITL)** inserisce un essere umano in **punti di controllo** del
workflow agentico: l'agente si **ferma** e attende che una persona riveda il lavoro, **approvi**,
**corregga** o **fornisca input** prima di proseguire
([Google Cloud, *Choose a design pattern*](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

È il riconoscimento che, per quanto autonomo, un agente che opera su decisioni non-deterministiche
([01](01-fondamenti-llm.md)) non dovrebbe agire **senza supervisione** quando la posta in gioco è alta.

## Quando usarlo

Google e AWS indicano gli stessi scenari: compiti **ad alto rischio o soggettivi** —

- transazioni finanziarie;
- validazione di documenti sensibili;
- approvazioni di *compliance*;
- azioni irreversibili o che modificano sistemi di produzione.

## Forme di HITL

| Forma | Descrizione |
|-------|-------------|
| **Approvazione di azione (gating)** | L'agente propone una chiamata a strumento rischiosa; l'esecuzione è bloccata finché un umano non conferma. Vedi [03 — Strumenti](03-strumenti-function-calling.md). |
| **Revisione di output** | L'agente produce un risultato che un umano deve approvare prima della pubblicazione. |
| **Chat manager umano** | In un'orchestrazione *group chat* ([08](08-orchestrazione-multi-agente.md)), una persona può assumere il ruolo di coordinatore, guidando la discussione tra agenti. |
| **Risoluzione di conflitti** | Quando più agenti producono risultati incompatibili, un umano (o una policy) dirime. |

## Compromessi

Google è esplicita sul trade-off: l'HITL "**migliora sicurezza e affidabilità**" ma "richiede di
costruire e mantenere sistemi di interazione esterni", aggiungendo complessità architetturale. Va quindi
applicato **selettivamente**, ai soli punti dove il rischio lo giustifica, non a ogni passo (altrimenti
si perde il vantaggio dell'automazione).

## HITL e governance

L'HITL è uno dei **guardrail** discussi nel capitolo sulla [sicurezza](12-sicurezza-governance.md): è il
punto in cui il giudizio umano entra come confine esplicito al comportamento autonomo dell'agente.
Combinato con il *least privilege* (l'agente può proporre azioni rischiose ma non eseguirle da solo),
costituisce una difesa in profondità.

## Il legame con il progetto

Agora Orchestrator espone interfacce di approvazione e di risoluzione conflitti, e permette di marcare
singoli strumenti come *approvals* (gated) per agente. Dettagli in [`../agents.md`](../agents.md).

---

Precedente: [10 — Valutazione e osservabilità](10-valutazione-osservabilita.md) · Prossimo:
[12 — Sicurezza e governance](12-sicurezza-governance.md).
