# Ambiente QA dei premi

Serve a ripetere 20 volte gli stessi test dei premi con 1–2 account di prova, senza creare account nuovi e senza aspettare il giorno dopo.
Tutto gira sul PC: nell'app non ci sono pulsanti segreti, reset o accrediti.

## 1. Prove automatiche (sempre, prima di ogni deploy)

```
node Server/CloudScript/test.js
node Server/CloudScript/concorrenza.js
```

`test.js` usa un finto PlayFab e copre: premio disponibile e già riscattato, doppio riscatto e spam, Posta singola e "Raccogli tutto",
tutorial una volta sola, accredito rifiutato da PlayFab (anche per sempre: valuta che non c'è), risposta persa dopo l'accredito, script
fermato durante l'accredito, lucchetto occupato e scaduto, registro del terzo giro, tutorial segnato dagli script vecchi (incerto, mai
pagato da solo), guasti finti solo sul titolo di sviluppo.

`concorrenza.js` fa girare 2–4 chiamate insieme, ognuna nel suo thread, con orologi a cavallo di un cambio di minuto e guasti a caso
(rifiuti, risposte perse, script morti). Controlla che nessun premio venga consumato due volte, che nessun accredito arrivi due volte e
che alla fine ogni premio sia arrivato oppure sia segnato "incerto". 300 giri di default; `concorrenza.js 1500 1000` = 1500 giri dal
seme 1000. Un errore stampa il seme: si rifà uguale con lo stesso seme.

## 2. Preparazione (una volta)

1. **Titolo di sviluppo (consigliato).** Nel Game Manager di PlayFab: nuovo titolo nello stesso studio. In Economy (legacy) → Currencies crea
   `CO` (monete) e `GE` (gemme). In Title settings → Secret Keys crea una chiave.
2. **Configurazione.** Crea `Server/QA/qa.json` (è fuori da git):
   ```json
   { "titleId": "ID_DEL_TITOLO", "secretKey": "CHIAVE_SEGRETA", "accountProva": [] }
   ```
3. Sul titolo di sviluppo:
   ```
   node Server/QA/qa.js deploy
   ```
   (pubblica 51.js coi guasti finti accesi: `QA = true` solo nella copia caricata sul titolo di sviluppo).
4. **APK di prova per il titolo di sviluppo.** Serve anche un'app Photon di sviluppo con l'autenticazione verso il nuovo titolo. Quando
   ci sono i due id li metto io in una build QA (simbolo `QA51`) che non può uscire come build di store. Finché non ci sono, vedi il punto 5.
5. **Senza titolo di sviluppo (subito).** `qa.json` con il titolo dell'app e i tuoi account di prova in `accountProva`
   (es. `["qa_1", "qa_2"]`). Sul titolo dell'app lo strumento può solo leggere e preparare quegli account. Niente guasti finti né deploy:
   gli errori e i timeout li coprono le prove automatiche.

## 3. Comandi

| Comando | Cosa fa |
|---|---|
| `node Server/QA/qa.js diagnosi` | Valute CO/GE presenti, revisione CloudScript pubblicata (consegna sicura sì/no), TitleData Economia. Solo lettura |
| `node Server/QA/qa.js stato qa_1` | Saldo, premio di oggi, tutorial, posta da riscattare, consegne (`attesa`, `incerto`, `fermo`, segno vecchio del tutorial) con cosa fare, partite (aperte, in riesame, chiuse), incongruenze con le dichiarazioni, guasto |
| `node Server/QA/qa.js prepara qa_1 [1-7]` | Premio del giorno N da riscattare (7 = forziere viola), tutorial +200 da riscattare, 2 messaggi con premio (monete+gemme, forziere verde), nessun guasto |
| `node Server/QA/qa.js riscattato qa_1` | Premio di oggi già riscattato |
| `node Server/QA/qa.js guasto qa_1 rifiuto` | Solo titolo di sviluppo. PlayFab rifiuta l'accredito: premio preso, consegna in attesa |
| `node Server/QA/qa.js guasto qa_1 incerto` | Accredito fatto ma risposta persa: consegna `incerto`, mai ritentata da sola (niente doppio) |
| `node Server/QA/qa.js guasto qa_1 timeout` | Lo script si ferma dopo aver preso il premio: il telefono vede un errore, la consegna diventa `incerto` |
| `node Server/QA/qa.js guasto qa_1 lento` | Risposta dopo 3 s: per i tocchi ripetuti e la chiusura della schermata durante l'attesa |
| `node Server/QA/qa.js guasto qa_1 nessuno` | Toglie il guasto |
| `node Server/QA/qa.js riconcilia qa_1 <chiave> CO=si\|no [GE=si\|no] [--reale]` | Chiude una consegna incerta o ferma (sezione 5) |
| `node Server/QA/qa.js esito qa_1 <partita> vinta\|persa\|nulla [--reale]` | Partita che il server non ha potuto verificare (#142 Fase A): contestata, incompleta già chiusa, da riconciliare. Prima leggi le dichiarazioni con `stato` (incongruenze). `vinta`/`persa` = monete piene meno la partecipazione già data, alla prossima apertura dell'app; `nulla` = solo registrata. Niente XP né statistiche (le classifiche restano solo sulle partite verificate). Una volta per partita |

## 4. Giro di prova sul telefono (ripetibile)

Prima di ogni prova: `prepara qa_1`, poi `stato qa_1` e annota il saldo.

| # | Prova | Atteso |
|---|---|---|
| Q1 | Premi → RISCATTA | Animazione, saldo +50 in TopBar subito; `stato` uguale al telefono |
| Q2 | Q1 con 5–10 tocchi rapidi | Un solo accredito |
| Q3 | Chiudi e riapri Premi, poi esci e rientra nell'app | Resta riscattato, saldo uguale |
| Q4 | `riscattato qa_1`, apri Premi | Nessun RISCATTA, conto alla rovescia |
| Q5 | Posta → RISCATTA su "monete e gemme", poi "Raccogli tutto" | +100 monete, +5 gemme, poi il forziere verde. Tocchi ripetuti: un solo accredito |
| Q6 | Rifai il tutorial fino alla fine | +200 una volta; rifatto di nuovo: "già dato" |
| Q7 | `prepara qa_1 7` | Forziere viola: animazione completa |
| Q8 | `guasto qa_1 rifiuto` + RISCATTA | Avviso "Premio preso: arriva appena il server conferma", RISCATTA non torna, saldo fermo. `guasto qa_1 nessuno`, riavvia l'app: avviso "premio in sospeso arrivato", saldo giusto |
| Q9 | `guasto qa_1 incerto` + RISCATTA, poi `nessuno` e riavvio | Saldo +50 una volta sola, niente recupero; `stato`: consegna `incerto` |
| Q10 | `guasto qa_1 timeout` + RISCATTA | "RIPROVA PIÙ TARDI"; `nessuno` e riavvio: niente accredito, `stato`: consegna `incerto` |
| Q11 | `guasto qa_1 lento` + RISCATTA e chiudi subito la pagina | Nessun blocco; riaprendo: riscattato |
| Q12 | Esci, accedi con qa_2, poi torna a qa_1 | Saldi e stati di ciascun account, niente mescolato |

## 5. Un giocatore dice "il premio non è arrivato" (procedura)

Serve solo `Server/QA/qa.json` con il titolo dell'app (vedi sopra). Non si accredita mai a mano dal Game Manager: si passa sempre da
`riconcilia`, che fa pagare il server una volta sola e tiene l'elenco di cosa è stato fatto.

**Perché esiste "incerto".** `AddUserVirtualCurrency` (Economy legacy) non ha un id di transazione: se la risposta si perde nessuna API
dice se l'accredito è arrivato, e il saldo non lo prova. Il server garantisce "al massimo una volta" e scrive ogni dubbio. Non esiste un
"esattamente una volta" con Economy legacy: serve Economy v2.

1. **Trova il giocatore**: nome utente o PlayFabId (Game Manager → Players). `node Server/QA/qa.js stato <nome>`.
2. **Leggi le consegne** (chiavi `Cons_<id>`, una per premio; ogni riga dice cosa era, quando in ora italiana, quanto e cosa fare):

   | Stato | Significato | Cosa fai |
   |---|---|---|
   | nessuna consegna | Ogni premio consumato è arrivato (o non è mai stato riscattato) | Niente. Se il giocatore insiste: monete di fine partita non arrivate per rete persa non lasciano traccia (vedi `STATO_BACKLOG.md`, audit economia) |
   | `ATTESA` | PlayFab ha detto "no" con certezza: non è arrivato | Niente: arriva da solo alla prossima apertura dell'app |
   | `INCORSO` | Accredito in corso adesso, o script fermato | Aspetta un minuto e rilancia `stato`: diventa `INCERTO` alla prossima chiamata dei premi |
   | `INCERTO` | Esito sconosciuto della valuta indicata (`dubbio`) | Punto 3 |
   | `FERMO` | Rifiutato 10 volte di fila: di sicuro non è arrivato, ma qualcosa lo blocca (valuta cancellata, limite) | Togli la causa (`qa.js diagnosi`), poi `riconcilia <nome> <chiave>` |
   | `Tutorial (segno vecchio)` | Tutorial finito con gli script fino all'08/10: forse 200 monete mai arrivate | Punto 3 con la chiave `Tutorial` |

3. **Incerto: guarda PlayStream.** Game Manager → Players → il giocatore → PlayStream: cerca `player_virtual_currency_balance_changed`
   della valuta in `dubbio` (CO = monete, GE = gemme) con l'aumento uguale all'importo, attorno all'ora della consegna (anche qualche
   secondo dopo). Se l'evento è troppo vecchio per la pagina, Data Explorer; se non è sicuro che ci sia tutto (campionamento), **non
   decidere**: lascia incerta.
   - C'è: `node Server/QA/qa.js riconcilia <nome> <chiave> CO=si` (o `GE=si`).
   - Non c'è: `... CO=no`. La consegna torna in attesa e il server la paga alla prossima apertura dell'app.
   - `dubbio` con due valute (`CO,GE`): servono tutte e due (`CO=si GE=no`). Le valute fuori da `dubbio` non sono mai state tentate:
     restano da pagare da sole.
   - Sul titolo dell'app, per un giocatore che non è in `accountProva`, aggiungi `--reale`.
4. **Controlla**: dopo che il giocatore ha aperto l'app, `stato <nome>`: la consegna non c'è più, il saldo è salito una volta.

**Niente doppi.** `riconcilia` rifiuta una consegna già riconciliata (elenco `Riconciliazioni` nei dati interni del giocatore, con data,
esito e com'era prima), una consegna in `ATTESA`/`INCORSO` (la sta già facendo il server) e una chiave che non c'è più (già pagata). Il
saldo non si usa mai come prova. Una `FERMO` si può rimettere in attesa più volte: non è mai arrivata.

**Recupero automatico vs manuale.** Automatico: tutto ciò che PlayFab ha rifiutato con certezza (`ATTESA`, fino a 10 tentativi, una
consegna vecchia per chiamata, prima la meno rifiutata). Manuale: `INCERTO`, `FERMO`, segno vecchio del tutorial, premi persi prima
del 09/10 (`docs/build3/RECUPERO_CONSEGNE.md`).
