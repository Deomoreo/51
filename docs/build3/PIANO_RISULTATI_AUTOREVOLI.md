# #141, #142 Fase A, #143, #148-#150: risultati concordati fra i telefoni (fatto in locale, NON caricato) e architettura autorevole (#151)

Stato al 08/10 dopo le decisioni D1-D7. Tutto è nel codice e nelle prove, **niente è online**: CloudScript Live = revisione 19,
nessun deploy, nessuna build, versione invariata, nessun commit. #151 (carte nascoste) ha il suo documento:
[AUDIT_151_CARTE_NASCOSTE.md](AUDIT_151_CARTE_NASCOSTE.md).

## 1. Come funziona adesso

**Biglietto (#141).** A ogni partita il telefono chiede `inizioPartita` (alla prima distribuzione, e di nuovo dopo un rientro). L'id
lo genera il server. Online la chiave è stanza + id del record Photon; in allenamento è l'id della partita sul telefono, quindi un
biglietto per partita. Il telefono manda in `dopo` i biglietti già finiti (in sospeso e ultimi 10, salvati per account: valgono anche
dopo un riavvio). **Il server non restituisce mai un biglietto già usato** e ne dà al massimo uno nuovo al minuto (risposta `attendi`
con i secondi). Questo chiude #148 (rivincita dopo un riavvio) e i biglietti in parallelo.

**Presenza (#143, D5).** Nessuna soglia di tempo. Conta come persona presente chi ha chiesto il biglietto **di quella partita mentre
sedeva** nel record della stanza (`b<posto>`, lo scrive il server). Un account che entra ed esce prima della distribuzione non ce l'ha.
Le uscite definitive arrivano dal nuovo webhook PathLeave (`RoomLeft<segreto>` → `u<posto>`); una disconnessione con possibile rientro
(`IsInactive`) non conta. A fine partita il record resta (segnato `Chiusa`) per 8 giorni se ha biglietti, per il riesame.

**Fase A (D3, D4).** Ogni persona dichiara il risultato: punti finali di ogni posto o squadra, smazzate, vincitore, proprio posto,
squadre, giocatori, versione dell'app e rientri. Il server lo scrive **una volta** nel proprio biglietto e nel record della stanza.
Poi confronta le dichiarazioni delle persone presenti:

| Esito | Quando | Monete | XP e statistiche | `XPConcordato` (mai per premi) |
|---|---|---|---|---|
| `confermata` | tutte le persone rimaste hanno dichiarato lo stesso risultato, coerente col proprio posto | piene | sì | sì, solo se nessuno è uscito e i giocatori sono tutti persone (D6) |
| `abbandono` | le altre persone sono uscite per sempre senza dichiarare | piene, entro i tetti degli abbandoni (3 al giorno, 1 per avversario) se vinta | sì | no |
| `inVerifica` | manca una dichiarazione, attesa in corso (`attesaMinuti`, default 10) | niente per ora | no | no |
| `incompleta` | l'attesa è finita e manca ancora qualcuno | solo **partecipazione** (`partecipazione`, default 10), una volta | no | no |
| `contestata` | dichiarazioni diverse o incoerenti | solo partecipazione, una volta | no | no |
| `allenamento` | nessuna altra persona con il biglietto (bot, allenamento, record mancante) | metà | sì (metà) | no |

- **Nessun vincitore dichiarato da un solo telefono viene pagato** per il fatto che l'attesa scade (D3). Una incompleta il telefono
  la ritenta per 7 giorni (Home): se la dichiarazione mancante arriva diventa `confermata` e paga **la differenza** (es. 40 − 10).
  Dopo 7 giorni si chiude con la sola partecipazione.
- **Contestata (D4)**: nessuno è pagato come vincitore né come perdente. La partecipazione è separata dal risultato competitivo, che
  resta non verificato. L'**intervento amministrativo** è `qa.js esito <utente> <partita> vinta|persa|nulla` (nuovo): paga la
  differenza come consegna, una volta, senza XP né statistiche.
- **Registro, nessuna sanzione automatica.** Ogni incompleta, contestata o da riconciliare va nei dati interni `Incongruenze` (ultime
  50, con tutte le dichiarazioni) e in PlayStream (`partita_incongruenza`). La categoria è indicativa: `bug` (versioni diverse),
  `rete` (rientri o assenti), `sospetta` (vincitore diverso senza rientri, o dichiarazione incoerente). Si legge con `qa.js stato`.

**Limite di 10 (D1).** Server: oltre 10 biglietti aperti il più vecchio (una partita mai finita) diventa `scaduta`. Se poi il suo
risultato arriva lo stesso, viene registrato come `daRiconciliare` in `Incongruenze` e non è mai pagato da solo né perso in silenzio
(`qa.js esito`). Telefono: al massimo 10 risultati in sospeso. Al limite esce prima la incompleta più vecchia (il server ha già la
sua dichiarazione), poi la più vecchia con biglietto (registrata sul server), con un avviso. Se sono tutte senza biglietto il risultato
nuovo non si salva e lo si dice. Esiti finali: ultimi 30 nel Read Only, poi 150 nell'archivio interno `ArchivioPartite`.

**Senza biglietto (#149, #150, D2).**
- Telefono nuovo senza biglietto per la rete: risultato salvato con l'id del telefono e ritentato. Il server paga metà (come
  l'allenamento), un id una volta sola.
- App vecchie (revisione 19, nessun biglietto): ramo di compatibilità **senza data di scadenza**, acceso finché
  `Economia.partita.senzaBiglietto.attiva` non diventa `false`. Premio pieno solo se il record della stanza mostra un altro account
  ancora seduto (o uscito, per l'abbandono, con i suoi tetti). Il campo `allenamento` del telefono non conta.
- Entrambi: uno al minuto, al massimo `senzaBiglietto.giorno` (default 20) al giorno, evento PlayStream `partita_senza_biglietto`
  (`tipo`: `appVecchia` o `rete`) per il controllo.

**Bot (D6).** Partite con soli bot: metà premio, XP normali della metà, **mai** in `XPConcordato`. Partite competitive con un bot
sostitutivo (qualcuno uscito, o giocatori dichiarati diversi dalle persone presenti): pagate ma fuori da `XPConcordato`.

**Concordata non vuol dire verificata.** La Fase A confronta quello che dichiarano i telefoni: è un **accordo fra client**, non una
validazione autorevole. Due account d'accordo ottengono una `confermata`, e ogni telefono conosce già tutte le carte (#151). Per
questo la statistica si chiama `XPConcordato` (prima `XPVerificato`, rinominata il 08/10 prima di qualsiasi deploy) e **non** deve
fare da base a classifiche con premi: quelle useranno solo i risultati del server di partita (sezione 6).

## 2. Limiti che restano (detti chiaramente)

- **Due account dello stesso giocatore** che restano seduti, giocano e dichiarano lo stesso risultato ottengono una `confermata`.
  Nessuna prova del server li distingue da due persone. Lo limitano tetti giornalieri (400 monete, 60 partite), il minuto fra due
  biglietti e il registro per coppie di account. **La Fase A non è sicura contro la collusione** e non lo sarà nessuna verifica
  concordata. Il server di partita (sezione 6) toglie i risultati inventati, ma due account dello stesso giocatore possono sempre
  giocare davvero fra loro e lasciarsi vincere: contro questo restano solo tetti, statistiche per coppia di account e controlli a mano.
- **Partecipazione e partite fittizie (verificato l'08/10, prova in `test.js`).** Per la partecipazione servono due account diversi,
  entrambi seduti nel record della stanza con il loro biglietto, e almeno un minuto di partita. Due account che si contestano apposta
  80 volte in un giorno prendono 10 monete a partita (meno delle 20 di una sconfitta concordata), solo per i primi 60 risultati, fino
  a 400 monete, senza XP né statistiche. La partecipazione **non** è una strada migliore dell'accordo fra account.
- **Il buco più grande sta altrove (preesistente, anche nella revisione 19).** L'allenamento contro i bot si gioca solo sul telefono:
  il server può verificare solo "biglietto chiesto + 60 s". Un client modificato, con **un solo account e senza giocare**, chiede un
  biglietto al minuto e dichiara vittorie: 20 monete a volta, quindi il tetto di 400 monete in circa 20 minuti, e gli XP dimezzati
  fino a 60 risultati al giorno (che contano anche nella classifica settimanale senza premi). Lo stesso vale per i risultati senza
  biglietto. Nessun server di partita lo risolve, a meno di giocare anche l'allenamento sul server. Proposta (D11): un tetto
  giornaliero separato e più basso per i premi non verificabili (allenamento, senza biglietto, partecipazione), regolabile in
  `Economia.partita`.
- **Un perdente che non dichiara** blocca la vittoria piena dell'altro finché non esce per sempre (allora è `abbandono`, entro i suoi
  tetti) o finché non chiude l'app (PathLeave alla fine dei 60 s del rientro: da confermare nella prova a 2 telefoni, punto 3). Se resta seduto fermo, l'altro ha la partecipazione e il riesame.
- **Un imbroglione che dichiara il falso** non vince niente, ma toglie la vittoria piena all'altro: contestata, registro con categoria
  `sospetta`, decisione a mano. È voluto (D4).
- **Carte nascoste (#151)**: oggi ogni telefono le ha tutte. Vedi il documento a parte.
- **Il dato "uscito per sempre" dipende dal webhook PathLeave**: va configurato nel pannello Photon (sotto). Senza, nessuna uscita viene
  vista e le partite con un perdente sparito restano `incompleta` (mai pagate per intero per errore).

## 3. Ordine del passaggio (D2), quando decidi tu

1. Carica il CloudScript nuovo (`node Server/CloudScript/carica.js`, che stampa anche i nomi dei webhook) e pubblicalo. Le app vecchie
   continuano a essere premiate (ramo di compatibilità acceso).
2. Pannello Photon: **PathLeave = `RoomLeft<segreto>`** (come stampato da `carica.js`), PathBeforeJoin vuoto, accesso anonimo spento (#103).
3. Prove con l'APK nuovo (2 telefoni: confermata, abbandono, contestata a mano, rete tolta a fine partita).
4. Build TestFlight nuova.
5. Quando le versioni vecchie non si vedono più (PlayStream `partita_senza_biglietto` con `tipo: appVecchia` vicino a zero):
   `Aggiornamento.minima` alla nuova versione. Richiede di riattivare gli aumenti di versione (oggi in pausa).
6. Solo dopo: `Economia.partita.senzaBiglietto.attiva = false`.

Ritorno indietro: la revisione 19 resta nel Game Manager. I dati nuovi (`Partite`, `Incongruenze`, `b/u/d` nel record) non le danno
fastidio.

## 4. Prove

`node Server/CloudScript/test.js` copre: partita vera, biglietto inventato e di un altro, rivincita, biglietti in parallelo,
dichiarazioni discordanti (anche `bug`), una persona disconnessa (incompleta → confermata, differenza 30) e una che non torna (chiusa
dopo 7 giorni), due account d'accordo (limite noto), 2v2 con 4 persone, 1v3 con bot, solo bot, sospeso, 10 biglietti
(scaduta → daRiconciliare), 30 giorni offline ancora pagata, allenamento senza riuso, archivio, senza biglietto, app vecchia con
ramo acceso e spento, record tenuto dopo la chiusura, uscita finta prima dell'inizio, uscita `IsInactive`. Chiamate API misurate sotto il tetto di 25
(fine partita al massimo 18). `concorrenza.js`: 300 giri. Unity EditMode: tutte passate (più la prova che fotografa #151).

Non coperto da prove automatiche: il percorso sul telefono (risultato in sospeso, ritentativi a 10 s e 30 s, riga "in verifica"). Va
provato a mano al punto 3.

## 5. Fase B (non fatta): analisi di sicurezza del replay sul server

| Domanda | Risposta |
|---|---|
| Chi genera e conosce il seme | Il server, derivato dal record della stanza (`HMAC(segreto, id record)`): non sceglibile dal telefono. Oggi l'host mescola con `System.Random` a seme d'orologio e manda a tutti mazzo e mani (#151). |
| Evitare che un client ricostruisca le carte nascoste | Con il replay da solo non si può: chi ha il seme ricostruisce il mazzo. Serve lo stato filtrato (#151, livello 1) e un generatore crittografico; l'host vede comunque tutto finché non c'è un server di gioco (#151, strada B). |
| Fonte affidabile delle mosse | Registri delle mosse dichiarati da tutte le persone e confrontati (uguali, perché tutti ricevono le stesse mosse `AllViaServer`). Il webhook per ogni mossa costerebbe 200-400 esecuzioni per partita. |
| Dati dai webhook Photon | Creazione, entrata, uscita (con `IsInactive` e motivo), chiusura. UserId = PlayFabId autenticato. Il contenuto di eventi inoltrati è del client. |
| Dati ancora dichiarati dai client | Oggi: punteggi e vincitore (confrontati), scope, accusi. Dopo la Fase B: solo il registro delle mosse. Restano del client le mosse dei bot e quelle forzate. |
| Bot, abbandoni, rientri | Bot scelti dall'host: partite con bot fuori da `XPConcordato` (fatto). Abbandoni certificati da PathLeave (fatto). Rientri: registro continuo con `TurnId`. |
| Sequenza inventata | Seme del server + mosse legali + registri uguali fra tutte le persone + durata plausibile. Resta solo l'accordo fra account. |
| Modifiche | Generatore con seme uguale in C# e JS, registro delle mosse in `TurnController`, copia JS di `Rules51` + `MatchScore` (~500 righe) e prova di parità. |

**Prima del lancio pubblico (D9):** server di partita autorevole (sezione 6), che rende superflue la Fase B e il filtro dello stato.
**Prima degli acquisti:** i premi di partita non cambiano (tetto 400 al giorno); servono la verifica delle ricevute sul server e, per
"esattamente una volta" sugli accrediti, Economy v2 (#138).

## 6. Architettura autorevole per #151 (D8-D10 dell'08/10): confronto e raccomandazione, niente avviato

Decisioni: **D8 = C temporaneamente.** Nessun filtro dello stato adesso: rientro e cambio di Master restano come sono. **D9:** #151 va
risolta **prima del lancio pubblico**: le mani avversarie e il mazzo futuro non devono arrivare a nessun client. **D10:** si valuta ora
l'architettura autorevole, senza implementarla e senza comprare servizi. La migrazione parte solo dopo la tua approvazione.

### Cosa si riusa del codice di oggi (misurato)

- **Regole: si riusano tali e quali.** `Card`, `Suit`, `Move`, `MoveType`, `GameState`, `PlayerState`, `Rules51`, `RoundManager`,
  `MatchScore`, `AccusiChecker`, `PunteggioManager`, `CirullaAI`, `GameStateSerializer`: circa 1.750 righe di C# senza Unity. Prova
  dell'08/10 in un progetto .NET 10 a parte (fuori dal repository): questi file, **non modificati**, compilano e giocano 4.000 smazzate
  (1v1 e 4 giocatori, IA compresa) in circa 4 microsecondi a mossa. Solo `MatchRules` e `GameFormat` stanno in `MatchConfig.cs`, che
  usa Unity: vanno spostati in un file loro (nessun cambio di comportamento).
- **Da estrarre: il flusso del turno.** Ordine dei turni, finestre degli accusi, Tre assi, fine smazzata, partita successiva, timer
  da 30 + 10 s, mosse forzate, bot dopo 3 mosse forzate: oggi stanno in `TurnController` (2.077 righe, MonoBehaviour) e in
  `NetworkGameController`. Per non avere due copie va estratto in un `MatchEngine` in C# puro, usato sia dal server sia dal telefono
  (allenamento offline). Stima: 400-600 righe spostate, non riscritte.
- **Parità delle regole.** Un'unica copia del codice: il server compila gli stessi file di `Assets/Scripts/Core` (collegati, non
  copiati), quindi una regola cambiata in Unity cambia anche sul server. In più una **prova di parità**: un file di partite con seme
  (mosse e impronta dello stato dopo ogni mossa) prodotto dal server e rigiocato nelle prove EditMode di Unity, e viceversa. Fallisce
  se le due esecuzioni divergono, per esempio per differenze di runtime fra Unity e .NET.

### Confronto

| | **A. PlayFab/CloudScript come arbitro** (Photon per lobby e presenza) | **B. Piccolo servizio di partita dedicato** (.NET, WebSocket; Photon e PlayFab per il resto) | **C. Plugin Photon Server** |
|---|---|---|---|
| Codice riusato | A1 (CloudScript JS di oggi): **regole riscritte in JS**, seconda copia da tenere allineata con la prova di parità. A2 (CloudScript su Azure Functions, C#): regole riusate | Regole riusate tali e quali (provato) + `MatchEngine` condiviso col telefono | Regole riusate (plugin in C#; versione di .NET supportata da verificare) |
| Mazzo segreto | generato dal server (generatore crittografico), salvato nei dati del server fra una chiamata e l'altra | generato e tenuto in memoria dal servizio, con una copia su disco a ogni mossa per i riavvii | nel plugin, in memoria della stanza |
| Carte private | **a richiesta**: ogni telefono chiede la propria mano a ogni giro; il server non può mandare niente da solo | ogni posto riceve solo la propria mano sulla propria connessione; gli altri solo il numero di carte | eventi mirati del plugin al singolo giocatore |
| Verifica delle mosse | una chiamata per mossa; gli altri telefoni devono sapere della mossa: o la richiedono al server (una chiamata in più a testa) o la ricevono via Photon con una **firma** del server da verificare (crittografia da scrivere in ES5 in A1) | il servizio controlla ogni mossa (`Rules51.GetValidMoves`) e la manda a tutti | il plugin intercetta e controlla ogni mossa |
| Bot e timer | nessun timer sul server: un telefono deve "svegliarlo" a tempo scaduto (il server controlla l'ora); i bot giocano nella stessa chiamata | timer e bot sul servizio (`CirullaAI`); l'arbitro di riserva sparisce | timer del plugin; bot nel plugin |
| Rientro e cambio di Master | stato sul server: il rientro rilegge la propria vista; il Master non conta più per la partita | rientro = nuova connessione e vista filtrata, anche dopo un riavvio dell'app; il Master Photon non conta più per la partita | rientro Photon normale; il Master non conta più |
| 1v1, 2v2, 1v3 | nessun client vede carte altrui, nemmeno l'host; bot non più in mano all'host | uguale; compagno in 2v2 compreso | uguale |
| Latenza (stima, **da misurare**) | PlayFab risponde dagli Stati Uniti: circa 0,3-0,8 s a mossa più le richieste degli altri; Azure Functions a consumo ha avvii a freddo di secondi, salvo un piano a pagamento sempre attivo | server in UE: un giro di andata e ritorno, circa 30-100 ms su 4G, come oggi via Photon | come oggi (server Photon UE) |
| Limiti API e costi | 50-100 esecuzioni per partita 1v1, 150-250 a 4 giocatori; tetto di 25 chiamate e 10 s per esecuzione; limiti di frequenza del titolo e il lucchetto con Shared Group (chiamate in più). Il costo in denaro delle esecuzioni è basso (decine di centesimi per milione secondo il listino PlayFab, da verificare), il problema è la latenza | PlayFab solo per il controllo del login (1 chiamata a connessione) e il risultato (1-2 per partita). Hosting: un piccolo server in UE **circa 5-20 € al mese** (prezzi Hetzner aumentati nel 2026, da verificare) o un container gestito circa 10-40 € al mese; certificato gratuito | **Enterprise Cloud: prezzo su richiesta** (riferimento pubblico Photon Industries: 2.000 $ al mese per un cloud dedicato). In alternativa Photon Server sul proprio server: licenza circa 500 $ al mese per 500 CCU (minimo 3 mesi) o 1.500 $ al mese senza limite, più il server e la gestione |
| Manutenzione | logica divisa fra CloudScript e telefono, debug solo da log; in A1 due copie delle regole | un servizio nostro: aggiornamenti, rilasci, monitoraggio, un punto di guasto (le partite online si fermano se cade; lobby e resto restano) | in cloud: una sola versione del plugin per AppId, debug limitato. In proprio: server Photon da gestire |
| Migrazione | alta: tutto il gioco online diventa domanda e risposta, con richieste continue o firme | media-alta: `MatchEngine`, servizio, trasporto della partita nel client (Photon resta per stanze, emoticon e presenza) | media: il modello resta Photon (eventi), il plugin prende il ruolo dell'host |
| Rischio di bug nuovi | alto (richieste continue, concorrenza fra chiamate, firme) | medio: il punto delicato è l'estrazione del flusso del turno, coperta dalla prova di parità e dalle prove a più telefoni | medio |
| Tempo indicativo | 5-8 settimane, con esperienza di gioco peggiore | prototipo 1v1 isolato 1-1,5 settimane; tutto (1v1, 2v2, 1v3, bot, timer, rientro, premi) 4-6 settimane più le prove sui telefoni | 3-5 settimane, ma con il costo fisso sopra |

Scartati: server di gioco open source come Nakama o Colyseus. Hanno partite autorevoli, ma la logica si scrive in Go, TypeScript o
Lua, quindi tornerebbe la seconda copia delle regole.

### Raccomandazione unica: B, un piccolo servizio di partita autorevole in .NET

- **Perché.** Toglie davvero le carte nascoste a tutti i client, host compreso. Riusa le regole senza copiarle (provato). Costa poco
  e non lega a un fornitore. Lascia Photon (stanze, ricerca partita, presenza, emoticon) e PlayFab (account, monete, statistiche)
  dove sono già.
- **Costo indicativo.** Circa 10-30 € al mese per un server piccolo in UE con monitoraggio di base, più il dominio (circa 10 € l'anno).
  I piani Photon e PlayFab non cambiano. Da confermare con i prezzi del giorno.
- **Rischi eliminati.**
  - Mani avversarie e mazzo futuro letti da un client modificato, in 1v1, 2v2 (compagno compreso) e 1v3.
  - Mazzo prevedibile (generatore crittografico sul server).
  - Mosse illegali o fuori turno.
  - Risultati e sequenze inventati: il risultato lo scrive il server, e la Fase A resta solo per le versioni vecchie.
  - Vantaggio dell'host sui bot.
  - Fragilità del cambio di Master e arbitro di riserva.
  - Rientro anche dopo un riavvio dell'app.
- **Rischi che restano.**
  - Collusione: due account o due persone allo stesso tavolo che si passano informazioni o si lasciano vincere.
  - Premi dell'allenamento offline (D11).
  - Disponibilità del servizio, unico punto di guasto: riavvio automatico, copia su disco a ogni mossa, avviso di manutenzione con il
    `ServiceGate` che esiste già.
  - "Esattamente una volta" sugli accrediti (#138) e verifica delle ricevute per gli acquisti.
  - Un client modificato può ancora automatizzare le **proprie** mosse (un bot che gioca al posto suo) senza vedere carte altrui.
- **Cosa diventa superfluo.** Il filtro dello stato (livello 1 di #151) e la Fase B (replay sul server) per le partite giocate sul
  servizio.

### Primo prototipo (isolato, nessun cambio al multiplayer di oggi)

1. **P0, mezza giornata.** `MatchRules` e `GameFormat` in un file loro dentro Core (nessun cambio di comportamento: le prove EditMode lo
   confermano). Progetto `Server/MatchServer` (.NET) che compila i file di Core collegati, più la prova di parità di base (partite con
   seme, impronta dello stato a ogni mossa, uguale in Unity e in .NET).
2. **P1, circa una settimana.** Partita **1v1** autorevole in memoria, su WebSocket in locale:
   - mazzo crittografico, vista filtrata per posto, mosse controllate;
   - timer di 30 + 10 s, mossa forzata, bot dopo 3 mosse forzate;
   - rientro con la vista filtrata;
   - login controllato con il ticket di sessione PlayFab, posti presi dal record della stanza.
   Due client di prova in .NET giocano partite intere. Un terzo "attaccante" prova a:
   - leggere le carte altrui;
   - giocare una carta che non ha;
   - giocare fuori turno;
   - usare il posto di un altro;
   - chiedere lo stato dell'altro.
   Deve fallire in tutti i casi (prove automatiche).
3. **P2, dopo P1 e solo se approvato.** Una scena di prova nell'app (solo build QA, mai nello store) per misurare la latenza vera da
   telefono su 4G e Wi-Fi verso un server di prova in UE.

Il prototipo non tocca `NetworkGameController`, `TurnController`, il cambio di Master né il CloudScript, e non va online: niente deploy,
niente build dello store, versione invariata. Poi la migrazione vera, a passi: `MatchEngine` estratto e usato dall'allenamento, 1v1 sul
servizio, poi 2v2 e 1v3, premi dal risultato del server, infine via il vecchio percorso Photon per le partite.

### Decisioni (08/10, finali)

- **D11 approvato e fatto in locale** (#152): tetto comune di 100 monete al giorno, regolabile.
- **D12**: B è la direzione futura per consolidare il multiplayer. Prototipo P0+P1 e migrazione **non approvati**: piano, misure e strategia di
  condivisione della logica C# restano qui. #151 = rischio importante, noto e accettato per una prima versione senza premi di valore; il
  multiplayer non è dichiarato protetto dai trucchi; controllo e rivalutazione se emergono abusi.
- **D13**: nessun acquisto. Quando si deciderà: prima un prototipo locale, poi la misura della latenza su un server di prova.

Testo originale delle domande:

- **D11** Tetto giornaliero separato e più basso per i premi non verificabili (allenamento, senza biglietto, partecipazione)? Proposta:
  100 monete al giorno, regolabile.
- **D12** Approvi B come architettura e il prototipo P0 + P1 (isolato, senza deploy)?
- **D13** Il server di prova per P2: dove (un piccolo server in UE a tuo nome) e quando comprarlo. Nessun acquisto prima del tuo via.

Fonti dei prezzi (08/10, da riconfermare): [Photon Plugins](https://doc.photonengine.com/server/current/plugins/manual) (solo
Enterprise Cloud o Photon Server in proprio), [prezzi Photon Industries](https://www.photonengine.com/industries/hosting),
[prezzi PlayFab](https://developer.microsoft.com/en-us/games/products/playfab/pricing/),
[CloudScript su Azure Functions](https://learn.microsoft.com/en-us/xbox/playfab/live-service-management/service-gateway/automation/cloudscript-af/),
[aumento prezzi Hetzner 2026](https://heise.de/-11333037).
