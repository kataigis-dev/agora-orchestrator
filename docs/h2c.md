# H2C — Human-to-Computer Protocol

## Cos'è

H2C è un protocollo di comunicazione strutturata tra umani e agenti AI, basato su blocchi delimitati. Permette agli agenti di esprimere intenti, stati e dati in modo formalmente parsabile.

## Sintassi

```
[H2C:TIPO:SOTTOTIPO]
... contenuto ...
[/H2C]
```

### Esempio

```
[H2C:ACTION:CODE_GENERATE]
namespace MyApp;
class Program { ... }
[/H2C]

[H2C:STATE:DONE]
Task completato.
[/H2C]
```

## Tipi

| Tipo | Descrizione |
|---|---|
| `STATE` | Stato dell'agente (DONE, FIX, APPROVE, DENY, ESCALATE) |
| `ACTION` | Azione richiesta (CODE_GENERATE, FILE_WRITE, FILE_READ, API_CALL) |
| `INFO` | Informazione strutturata (RESULT, ERROR, PROGRESS) |
| `QUERY` | Richiesta all'utente (CLARIFY, CONFIRM, APPROVAL) |

## Sottotipi STATE

| Sottotipo | Significato | Routing |
|---|---|---|
| `DONE` | Task completato con successo | Edge: done → END |
| `FIX` | Richiesta modifica | Edge: fix → loop |
| `APPROVE` | Approvato | Edge: approve → next |
| `DENY` | Rifiutato | Edge: deny → loop o END |
| `ESCALATE` | Richiede intervento umano | Pausa per approvazione |

## Interprete H2C

`H2cParser` in `src/Agora/Communication/H2cParser.cs`:

1. Riceve il testo della risposta dell'agente
2. Cerca pattern `[H2C:...]...[/H2C]` tramite regex
3. Estrae tipo, sottotipo e contenuto
4. Per blocchi STATE, converte in segnali di routing
5. Restituisce lista di `H2cBlock` parsati

## Modalità H2C vs Natural

| Aspetto | H2C | Natural |
|---|---|---|
| Formato | Blocchi strutturati | Linguaggio naturale |
| Routing | `[STATE:DONE]` | `<<signal done>>` |
| Parsing | Regex formale | Regex su segnali |
| Quando usare | Sistemi che richiedono parsing preciso | Interazioni più fluide |
