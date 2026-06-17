---
type: concept
title: Communication Modes
tags: [communication, h2c, natural, protocol]
related: [h2c-protocol, signal]
created: 2026-06-17
updated: 2026-06-17
---

# Communication Modes

Agora supporta due modalità di comunicazione tra agente e orchestratore, configurabili a livello globale.

## Configurazione

```yaml
communication: h2c      # default
# oppure
communication: natural
```

## Confronto

| Aspetto | `h2c` | `natural` |
|---------|-------|-----------|
| Formato output | `[STATE:DONE]` + campi | `<<signal done>>` inline nel testo |
| Struttura | Blocchi tipizzati con campi chiave-valore | Testo libero con token speciali |
| Robustezza | Alta (modelli meno capaci) | Media (richiede che il modello rispetti la sintassi) |
| Leggibilità output | Tecnica | Naturale |
| System prompt injection | `H2cPreamble` iniettato automaticamente | Istruzioni signal nel role |
| Parsing | `H2cParser` + `H2cInterpreter` | `SignalParser` |

## Quando usare `h2c`

- Modelli con capacità di instruction-following moderate (es. modelli locali piccoli)
- Pipeline dove la struttura dell'output è critica
- Quando servono campi aggiuntivi oltre al segnale (es. `reason`, `count`)

## Quando usare `natural`

- Modelli capaci (GPT-4, Claude, modelli > 13B)
- Pipeline dove l'output deve essere leggibile dall'utente finale
- Prototipazione rapida

## Esempi di file config

- `examples/agora-h2c.yaml` — pipeline con H2C e loop condizionale
- `examples/agora.yaml` — minimal senza grafo (natural implicito)
