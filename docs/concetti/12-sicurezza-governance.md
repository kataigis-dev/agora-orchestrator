# 12 — Sicurezza e governance

Gli agenti **agiscono**: invocano strumenti e modificano dati **senza istruzioni umane esplicite a ogni
passo**, su decisioni **non-deterministiche** ([01](01-fondamenti-llm.md)). Questo introduce dimensioni
di rischio che il software tradizionale non ha. Tutte le fonti convergono su un'idea: la sicurezza va
**progettata fin dall'inizio**, non aggiunta dopo.

## Perché gli agenti sono diversi (AWS)

AWS, nella sua *Well-Architected Agentic AI Lens*, elenca le caratteristiche che la progettazione deve
affrontare esplicitamente
([AWS, Agentic AI — Generative AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/agentic-ai.html)):

- gli agenti **ragionano attraverso molte chiamate** di inferenza e invocazioni di strumenti;
- **invocano strumenti e modificano dati** senza istruzione umana a ogni passo;
- le decisioni LLM sono **intrinsecamente non-deterministiche**.

La conseguenza progettuale: "ogni agente opera entro **confini di scopo esplicitamente definiti**, con
**guardrail** che vincolano il comportamento indipendentemente dagli input ricevuti".

## AWS Well-Architected Agentic AI Lens

La lens organizza le buone pratiche attorno ai **sei pilastri** del Well-Architected Framework, adattati
agli agenti
([AWS, Agentic AI Lens](https://docs.aws.amazon.com/wellarchitected/latest/agentic-ai-lens/agentic-ai-lens.html)):

| Pilastro | Per gli agenti significa |
|----------|--------------------------|
| Operational Excellence | gestione del ciclo di vita dei prompt, monitoraggio comportamentale |
| Security | confini di scopo, *least privilege*, guardrail, audit |
| Reliability | gestione del non-determinismo, condizioni di stop, recupero dagli errori |
| Performance Efficiency | uso efficiente di token e chiamate |
| Cost Optimization | controllo del costo per run (token, numero di passi) |
| Sustainability | uso responsabile delle risorse di calcolo |

### Least privilege per i workflow agentici

Pratica cardine (GENSEC05-BP01): "i confini dei permessi dovrebbero fornire accesso **solo** ai sistemi
e alle fonti dati necessari a generare una risposta", e i ruoli vanno costruiti con il **privilegio
minimo**
([AWS, GENSEC05-BP01](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/gensec05-bp01.html)).
In pratica: allow-list di strumenti per agente, credenziali ristrette, nessun accesso "per ogni
evenienza".

### Osservabilità e auditabilità by design

AWS raccomanda di costruirle nell'architettura fin dall'inizio (es. tracciamento di latenza, uso dei
token, tassi di errore). Vedi [10 — Valutazione e osservabilità](10-valutazione-osservabilita.md).

## Google: Secure AI Framework (SAIF)

SAIF affronta i rischi lungo **l'intero ciclo di vita** dell'IA, non solo il modello, con l'obiettivo di
sistemi **sicuri per impostazione predefinita** ([saif.google](https://saif.google/)). I principali
rischi specifici dell'IA che SAIF identifica
([SAIF, *Top Risks*](https://saif.google/secure-ai-framework/risks)):

| Rischio | Descrizione |
|---------|-------------|
| **Prompt injection** | Istruzioni malevole "iniettate" nel prompt sfruttano il confine sfumato tra *istruzioni* e *dati*, alterando il comportamento del modello |
| **Data poisoning** | Avvelenamento dei dati prima/durante l'ingestione, il training o lo storage |
| **Model theft** | Appropriazione non autorizzata del modello (furto di IP/funzionalità) |
| **Model source tampering** | Manomissione di codice, framework o pesi (attacchi alla supply chain) |
| **Vulnerable integrations** | Vulnerabilità in plugin/librerie/app che interagiscono col modello |

La **prompt injection** è particolarmente rilevante per gli agenti che leggono contenuti esterni (web,
documenti, output di strumenti): quel contenuto può contenere istruzioni ostili. Mitigazioni: trattare
l'input esterno come **dato non fidato**, isolare istruzioni e dati, validare gli output, e non dare
all'agente più potere di quanto serva (*least privilege*).

## Microsoft: Responsible AI

Microsoft inquadra la governance attorno a **sei principi** di IA responsabile
([Microsoft, *Responsible AI*](https://www.microsoft.com/en-us/ai/principles-and-approach);
[Microsoft Learn](https://learn.microsoft.com/en-us/azure/machine-learning/concept-responsible-ai)):

1. **Fairness** — trattare le persone in modo equo, evitare bias;
2. **Reliability & Safety** — operare in modo sicuro e affidabile, rilevare e mitigare esiti dannosi;
3. **Privacy & Security** — proteggere i dati e difendersi da attacchi;
4. **Inclusiveness** — coinvolgere e dare potere a tutti;
5. **Transparency** — rendere comprensibili comportamento e limiti;
6. **Accountability** — chi progetta/distribuisce risponde del sistema; gli esseri umani mantengono
   "**controllo significativo** sui sistemi altamente autonomi" (il fondamento dell'
   [HITL](11-human-in-the-loop.md)).

## Guardrail: una sintesi operativa

Mettendo insieme le quattro fonti, i **guardrail** di un sistema agentico includono:

- **Confini di scopo** espliciti per ogni agente (cosa può e non può fare);
- **Least privilege**: allow-list di strumenti e credenziali minime;
- **Input non fidato**: trattare web/documenti/output strumenti come potenzialmente ostili
  (anti prompt-injection);
- **Verifica deterministica** invece di fiducia nella narrazione (vedi [SDD](09-spec-driven-development.md));
- **Esecuzione sicura dei comandi**: nessuna shell arbitraria, comandi solo da allow-list, argomenti
  passati come token separati, timeout e kill dell'albero dei processi;
- **HITL** sui passi ad alto rischio o irreversibili;
- **Osservabilità e audit** fin dalla progettazione;
- **Condizioni di stop** chiare per evitare loop infiniti e consumo incontrollato.

## Il legame con il progetto

Agora Orchestrator applica diversi di questi guardrail: allow-list di strumenti per agente,
approvazioni HITL, e — per l'esecuzione dei controlli — comandi solo da configurazione, **senza shell**,
con sostituzione token-per-token e timeout. Vedi [`../spec.md`](../spec.md) e
[15 — Mappatura su Agora](15-mappatura-agora.md).

---

Precedente: [11 — Human-in-the-loop](11-human-in-the-loop.md) · Prossimo:
[13 — Standard e protocolli](13-standard-protocolli.md).
