# 03 — Strumenti e function calling

## Perché gli strumenti

Un LLM da solo può solo generare testo. Gli **strumenti** (*tools*) gli danno la capacità di
**osservare** (leggere file, interrogare un database, cercare sul web) e **agire** (scrivere file,
chiamare API, eseguire comandi). Sono ciò che trasforma un modello in un agente capace di operare nel
mondo ([Anthropic, *Building Effective Agents*](https://www.anthropic.com/research/building-effective-agents)).

## Function calling: come funziona

Il meccanismo standard si chiama **function calling** (o *tool use*):

1. Allo sviluppatore fornisce al modello, insieme al prompt, lo **schema** di ogni strumento
   disponibile: nome, descrizione, parametri (di solito in JSON Schema).
2. Il modello, anziché rispondere in testo, può emettere una **richiesta di chiamata**: il nome dello
   strumento e gli argomenti.
3. L'**applicazione** (non il modello) esegue lo strumento e restituisce il risultato.
4. Il modello incorpora l'osservazione e prosegue (vedi il loop ReAct in [02](02-agenti.md)).

Punto cruciale per la sicurezza: **è l'applicazione, non il modello, a eseguire il codice**. Il modello
propone *cosa* fare; l'orchestratore decide *se e come* farlo, applicando validazione, allow-list e
approvazioni umane.

## L'interfaccia agente-computer (ACI)

Anthropic introduce il concetto di **Agent-Computer Interface (ACI)**: così come si cura l'interfaccia
uomo-macchina (UI), va curata l'interfaccia con cui l'agente usa gli strumenti. Strumenti mal descritti
o ambigui producono agenti inaffidabili. Le buone pratiche
([Anthropic, *Writing tools for agents*](https://www.anthropic.com/engineering/writing-tools-for-agents)):

- **Descrizioni chiare e complete**: il modello sceglie lo strumento *solo* in base a nome e
  descrizione. Documentare cosa fa, quando usarlo, cosa restituisce.
- **Parametri non ambigui**: nomi espliciti, formati indicati con esempi.
- **Output utile e conciso**: restituire ciò che serve al passo successivo, non dump enormi (che
  saturano la finestra di contesto).
- **Errori informativi**: un messaggio di errore comprensibile permette al modello di correggersi.
- **Testare e iterare**: trattare gli strumenti come codice di produzione, con test e *sandbox*.

## Categorie di strumenti

| Categoria | Esempi |
|-----------|--------|
| **Lettura/osservazione** | ricerca semantica (RAG), lettura file, query DB, ricerca web |
| **Scrittura/azione** | scrittura file, chiamate API che modificano lo stato, esecuzione comandi |
| **Comunicazione** | chiedere a un altro agente, chiedere a un umano |
| **Verifica** | eseguire test/build e leggerne l'esito (controlli deterministici) |

La distinzione tra strumenti **read-only** e strumenti che **modificano lo stato** è centrale per la
governance: gli strumenti di azione richiedono limiti più stretti (vedi
[12 — Sicurezza e governance](12-sicurezza-governance.md)).

## Approvazione umana (gating)

Per le azioni rischiose o irreversibili, lo strumento può essere **gated** da un'approvazione umana:
l'agente propone la chiamata, ma l'esecuzione si blocca finché una persona non conferma. È il pattern
**Human-in-the-Loop** applicato a livello di strumento (vedi [11](11-human-in-the-loop.md)). Google lo
elenca tra i pattern di progettazione fondamentali, da usare per "compiti ad alto rischio o soggettivi"
([Google Cloud](https://docs.cloud.google.com/architecture/choose-design-pattern-agentic-ai-system)).

## Least privilege

AWS insiste sul principio del **privilegio minimo** anche per gli strumenti: "i confini dei permessi
dovrebbero fornire accesso solo ai sistemi e alle fonti dati necessari a generare una risposta" e i
ruoli vanno costruiti con il *least privilege* in mente
([AWS, GENSEC05-BP01](https://docs.aws.amazon.com/wellarchitected/latest/generative-ai-lens/gensec05-bp01.html)).
In pratica: un agente dovrebbe avere accesso **solo** agli strumenti che gli servono, e nient'altro
(allow-list per agente).

## Standardizzare l'accesso agli strumenti: MCP

Storicamente ogni integrazione strumento↔modello era custom. Il **Model Context Protocol (MCP)** di
Anthropic standardizza questo collegamento, così uno strumento esposto una volta è utilizzabile da
qualunque applicazione compatibile. È l'argomento del prossimo capitolo.

---

Precedente: [02 — Agenti](02-agenti.md) · Prossimo: [04 — Model Context Protocol](04-mcp.md).
