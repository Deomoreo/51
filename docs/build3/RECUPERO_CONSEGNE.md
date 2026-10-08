# Recupero dei premi consumati senza accredito (piano, 09/10)

Niente di questo è stato eseguito. Si procede solo con evidenze attendibili, un account alla volta, dopo il tuo via libera sull'elenco.

## Fatto chiave

La valuta `CO` (monete) non esisteva nel titolo `10A53D` fino a quando l'hai creata l'08/10: c'era `C0`. Quindi ogni
`AddUserVirtualCurrency CO` precedente è stato rifiutato da PlayFab con certezza. Per le monete "consumato prima della creazione di `CO`"
vuol dire "mai accreditato". Per le gemme `GE` no: se `GE` esisteva, quegli accrediti possono essere arrivati.

Da confermare prima di tutto: ora esatta di creazione di `CO` (T_CO) ed esistenza di `GE` in quel periodo (`qa.js diagnosi` più la tua
memoria del Game Manager).

## Cosa facevano gli script vecchi quando l'accredito falliva

| Script | Premio giornaliero / Posta | Tutorial | Partita |
|---|---|---|---|
| 02/10 (ultimo nel repository) | Segna consumato, poi accredita `CO` e dopo `GE`: il rifiuto di `CO` ferma lo script, quindi niente monete e niente gemme del premio (le gemme da sole, giorno 3, arrivano se `GE` esisteva) | — | Statistiche scritte, monete no |
| 07/10 (build 3) | come sopra | Segno nei dati interni, poi accredito fallito: +200 consumato senza monete | come sopra |
| 08/10 secondo giro | Rimetteva il premio (non consumato) | ? | ? |
| 08/10 terzo giro | Lista `Consegne` | Lista `Consegne` | Lista `Consegne` |

Le revisioni del 07/10 e del secondo giro non sono nel repository. Il comportamento in tabella viene dal registro (`STATO_BACKLOG.md`):
da verificare sulle revisioni in Game Manager (Automation → CloudScript → Revisions) prima di usarlo.

## Evidenze per tipo di premio

| Premio | Evidenza nei dati | Attendibile? |
|---|---|---|
| Lista `Consegne` (terzo giro) | Voci con `rifiutato` | Sì: le converte da sola la nuova consegna (in attesa = pagate una volta) |
| Tutorial | Segno interno `Tutorial` con data < T_CO | Sì per le monete: 200 `CO` mai pagate. Oggi diventa una consegna `incerto` alla prima chiamata del telefono |
| Premio giornaliero | `Premi` = {giorno N, ultimo D}: la serie dice che i giorni D-N+1…D sono stati riscattati | Solo i giorni con data < giorno di T_CO; importi dalla settimana in vigore (TitleData `Economia` o valori di default) |
| Posta | Messaggi `riscattato: true` con allegati | Solo se il messaggio non poteva essere riscattato dopo T_CO (data del messaggio e scadenza); i forzieri hanno importi casuali mai salvati: valore minimo dell'intervallo |
| Partite | Solo `PartiteOggi` di oggi; le statistiche non dicono quante monete | No: nessuna evidenza per partita. Al massimo un risarcimento forfettario, è una scelta tua |

## Come si recupera (quando c'è il via libera)

1. **Inventario, sola lettura.** Uno script sul PC (come `qa.js`) legge gli account (export del segmento "All Players"), applica la
   tabella e produce un elenco: account, premio, importo, evidenza. Nessuna scrittura.
2. **Revisione dell'elenco**, tua.
3. **Accredito tramite il server**, mai a mano. Per ogni voce approvata si scrive nel giocatore una chiave nuova
   `Cons_rec<id>` = `{"stato":"attesa", "CO": n, "GE": m, "errore": "recupero <evidenza>"}`. È una chiave nuova, quindi non tocca i dati
   che il telefono sta usando. Alla prossima chiamata dei premi la consegna sotto lucchetto la accredita al massimo una volta, e
   `qa.js stato` mostra l'esito. Attenzione: dopo l'accredito la chiave sparisce, quindi riscriverla pagherebbe due volte. Lo script
   scrive prima l'id nell'elenco dei dati interni del giocatore `Recuperi` e salta ogni voce che c'è già.
4. **Prima su un account QA**, poi a gruppi piccoli.
