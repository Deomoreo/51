# Rapporto di stabilizzazione per la prima versione pubblica (08/10)

Decisioni dell'utente dell'08/10: **D11 approvato** (fatto, sotto); **D12** = l'architettura B (piccolo servizio .NET autorevole) è la
direzione futura, ma prototipo P0+P1 e migrazione **non approvati**; **D13** = nessun acquisto (server, VPS, abbonamenti). Priorità: pubblicare
51. Niente nuova infrastruttura, deploy, build iOS, aumenti di versione o commit senza il via dell'utente.

Stato del codice: 51.js `test.js` e `concorrenza.js` (300 giri) passano; Unity EditMode 380 passati, 7 esclusi di proposito
(`Explicit`), 0 falliti. Nessuna build fatta.

## 0. D11, fatto in locale

- Un solo tetto di **100 monete al giorno** in comune per i premi che il server non può verificare: allenamento (e ogni partita senza prova
  di altre persone, compresa la partita rapida coi soli bot), app vecchie senza biglietto (anche il loro premio pieno), risultati senza
  biglietto del telefono nuovo, partecipazione alle partite non confermate. Regolabile da Title Data:
  `Economia.partita.tettoNonVerificabili` (se manca vale 100). Resta dentro il tetto generale di 400.
- **Le partite confermate non cambiano**: niente tetto D11; la partecipazione già data si scala dal premio pieno, quella tagliata dal tetto no
  (prova: tetto pieno, partita confermata = 40 interi).
- **Statistiche separate**: `PartiteOggi.nonVerificabili` = monete di oggi per tipo (`allenamento`, `appVecchia`, `senzaBiglietto`,
  `partecipazione`). XP non toccati.
- **Richieste insieme**: tutto avviene sotto il lucchetto del giocatore. In più i contatori del giorno (`PartiteOggi`, `RisultatiOggi`,
  `SenzaBiglietto`) ora si scrivono **nella stessa chiamata della consegna**. Prima erano nei dati interni, scritti dopo l'accredito: uno script
  fermato in mezzo lasciava monete fuori dai contatori e, per i risultati senza biglietto, poteva pagare due volte lo stesso id. Ora sono
  dati Read Only: il telefono li vede ma non li scrive.
- Prove: `test.js` (tetto comune fra i quattro tipi, statistiche per tipo, confermata piena, partecipazione tagliata poi confermata, giorno
  nuovo, tetto configurato a 30, partite fittizie 80 al giorno = 100 monete). `concorrenza.js`: richieste insieme di allenamento, senza biglietto
  e app vecchia, con guasti a caso. Controlla che le monete consegnate restino ≤ 100 e siano uguali ai contatori. Prova di mutazione: senza
  lucchetto, o coi contatori scritti dopo, fallisce ("TETTO D11 SUPERATO: 140").
- Il telefono non cambia: col tetto pieno riceve `tetto: true` e mostra già "Tetto di monete di oggi raggiunto". Risposta nuova (per ora non
  usata dal telefono): `tettoNonVerificabili`.
- **Effetti da sapere.** (a) Il giorno del deploy i contatori ripartono da zero, perché prima erano nei dati interni: al massimo un giorno con
  tetti nuovi per chi aveva già giocato. (b) Un tester con l'app vecchia (TestFlight) prende al massimo 100 monete al giorno anche nelle partite
  vere: senza biglietto non si possono distinguere. (c) Ritorno alla revisione 19: legge i suoi vecchi contatori interni, al massimo un giorno
  di tetti azzerati.

## 1. Modifiche locali non pubblicate

Tutto dopo il commit `ab9eb1c` (build iOS 2, versione 1.0.0, Android 264) è **solo in locale**: nessun commit, nessun deploy.

| Area | Cosa | Dove è descritto |
|---|---|---|
| Client Unity | Build 3 B1–B38: input e turni, rientro, sessione e account, amici, premi e posta, tastiera, Accuso, Scope, Matta, tutorial scriptato, audio, vibrazione. Giri Android #106–#137. Premi solo confermati dal server (#118–#121). Biglietto di partita, risultati in sospeso e dichiarazioni (#141–#150) | `STATO_BACKLOG.md` |
| File nuovi non tracciati (da includere nel commit, o la Cloud Build non compila o resta muta) | `Core/TutorialScript.cs`, `UI/UI51WalletPills.cs`, test `SourceEncodingTests` e `TurnControllerInputGateTests`, sprite `BackgroundHome/Flame/`, **`Assets/Audio/51_Audio_v07_FINAL_CANDIDATE/` (17 MB)**: la `SoundLibrary.asset` modificata punta già lì | git status |
| Mazzi | Cartelle vecchie `51_*_PNG_Unity` e `Resources/Cards` cancellate (338 file), mazzi in `Deck_<nome>` | memoria mazzi 07/10 |
| CloudScript | In uso: **revisione 19** (consegne col lucchetto). In locale anche: #139 consegna ferma, #141–#143 e #148–#150 biglietti e Fase A, webhook `RoomLeft`, `XPConcordato`, D11. `51.carica.js` rigenerato ora, pronto per l'upload | `PIANO_RISULTATI_AUTOREVOLI.md` |
| PlayFab (pannello) | Aggregazione "Last" anche per `XPConcordato` (#102); `Economia` in Title Data facoltativa | #102 |
| Photon (pannello) | PathLeave = `RoomLeft<segreto>` (#103) | §3 sotto |
| Strumenti | `Server/QA/` (qa.js, LEGGIMI), `concorrenza.js`, `docs/build3/`. `.gitignore` aggiunge `Server/QA/qa.json` (chiave segreta: mai nel commit) | |
| Roslyn | `Assets/Plugins/Roslyn/` (13 MB, strumenti dell'Editor per Unity-MCP): **solo Editor dall'08/10 notte (#153)**. La compilazione del player Android e iOS non lo contiene più | `CHECKLIST_RILASCIO.md` |

## 2. Cosa va aggiornato insieme (client ↔ CloudScript)

Il **CloudScript nuovo è compatibile con le app vecchie**: ramo `senzaBiglietto` per la revisione 19, acceso finché non lo spegni. Un
**client nuovo con la revisione 19, invece, non funziona bene**: chiama `inizioPartita`, che lì non esiste. Ogni partita finirebbe senza
biglietto e la revisione 19 non conosce `senzaBiglietto`. Quindi l'ordine è obbligatorio: **prima lo script, poi le app**, mai al contrario.

| Il client nuovo richiede lo script nuovo | Perché |
|---|---|
| Fine partita (biglietto, dichiarazioni, risultati in sospeso, avvisi "in verifica" e "confermata") | `inizioPartita`, stati e campi nuovi di `premioPartita` |
| Partita rapida coi soli bot pagata a metà, uscite definitive viste | `b<posto>` scritto dal server + webhook `RoomLeft` |
| Consegna ferma che non blocca le altre (#139) | Solo server, ma provata solo insieme al client nuovo |

Già compatibili con la revisione 19, perché c'erano già: sessione unica (`sessione`), amicizie reciproche, saldi nelle risposte,
premio giornaliero, Posta e tutorial.

**Versioni miste: risolto (#154, 08/10 notte).** Correzione di quanto scritto prima: in locale le build erano già separate da TestFlight 2
(`1.0.0-r2` contro `1.0.0`). Ora la separazione segue solo il **protocollo multiplayer** `p3` (`PhotonAuthConnector.ProtocolVersion`), non
la versione commerciale (resta 1.0.0). Viene impostato prima della prima scena, quindi prima dell'autenticazione Photon: verificato dal vivo,
il client si è collegato al Master come `p3_2.52`. Vale per la ricerca partita, le stanze per codice, il rientro e Photon Chat. Build:
iOS 3, Android 265.

## 3. Webhook Photon e app vecchie: sono pronti?

**Sì nel codice, no in produzione.**

- Codice: `RoomCreated`, `RoomJoined`, `RoomLeft` (col segreto), `RoomClosed`, `PathBeforeJoin` vuoto. Il ramo delle app vecchie è
  provato in `test.js`: premio pieno solo col record Photon (un altro account seduto, oppure uscito per l'abbandono, con i suoi tetti), uno al
  minuto, 20 al giorno, evento PlayStream `partita_senza_biglietto`, e ora il tetto D11.
- Mancano (fuori dal repository, li fai tu, in quest'ordine, `PIANO_RISULTATI_AUTOREVOLI.md` §3):
  1. Upload + Deploy di `51.carica.js` (#101).
  2. Subito dopo, nel pannello Photon: PathLeave `RoomLeft<segreto>` (il nome lo stampa `carica.js`), PathBeforeJoin vuoto; controlla che
     l'accesso anonimo sia spento (#103). Senza PathLeave le uscite non si vedono: la partita resta `incompleta` (solo partecipazione), mai
     pagata per errore.
  3. Prova a due telefoni con l'APK nuovo, poi la build TestFlight nuova.
  4. `Economia.partita.senzaBiglietto.attiva = false` solo quando l'evento `appVecchia` in PlayStream è quasi a zero.
- Ritorno indietro: la revisione 19 resta nel Game Manager.
- Non provato dal vivo: un'app vecchia contro un'app nuova, con lo script nuovo (ultimo punto della lista di prove in `STATO_BACKLOG.md`).

## 4. Bug davvero bloccanti nel backlog

**Nessun bug aperto e confermato nel codice blocca il lancio.** Bloccano però queste **condizioni**:

1. **#101 + #103**: deploy dello script e PathLeave. Senza, il client nuovo non riceve i premi delle partite (§2).
2. ~~Versioni miste~~ e ~~Roslyn nelle build~~: risolti (#153, #154). Restano le scelte A-C in `CHECKLIST_RILASCIO.md` (pacchetti non usati,
   configurazione Android per lo store).
4. **Prove su telefono delle voci critiche ancora DA VERIFICARE**: economia (#139–#143, #148–#150), passaggio fra account (#10, #77), rientro
   (#5, #6). Sono state corrette nel codice, ma "corretto" non vuol dire "provato" (§5).

**Cambia classe:** **#151 carte nascoste**. Prima era un blocco per il lancio (D9), ora è un **rischio importante, noto e accettato per ora**
(D12): la prima versione non ha competizioni con premi di valore. Il multiplayer **non** è protetto dai trucchi e non va presentato
così, né nello store né nei testi. Controllo previsto: eventi `partita_incongruenza` e `partita_senza_biglietto` in PlayStream, registro
`Incongruenze`, segnalazioni dei giocatori. Si rivaluta se emergono abusi, ripartendo dal piano B già scritto (§6 del piano, misure e
prototipo conservati, nessun avvio).

Aperti ma non bloccanti: #21 icone dei trofei (serve uno screenshot dal telefono), #64 grafica ridotta (serve il Profiler), #104 "Cirulla"
nello store e in TitleData (tuo).

## 5. Prove indispensabili prima dello store

Sempre con **build identiche** sui telefoni, e lo script nuovo caricato. Con `qa.js stato` prima e dopo le prove sui premi.

**Android e iOS (entrambi):**
1. Primo avvio pulito → Benvenuto → tutorial completo (+200 una volta sola) → Home.
2. Registrazione, Esci, Accedi con un altro account, ospite. Nessun dato che passa da un account all'altro (#10, #77, #100, #26b).
3. Stesso account su due telefoni: il secondo login viene rifiutato (#76).
4. Premio giornaliero e Posta con tocchi rapidi (Q2 e Q5 di `Server/QA/LEGGIMI.md` §4); saldo in alto subito giusto.
5. Allenamento: +20/+10, poi il tetto D11 raggiunto (5 vittorie = 100) con la scritta "Tetto di monete di oggi raggiunto".
6. 1v1 online fra due telefoni: `confermata` 40/20. Uno esce a metà: `abbandono`. Rete tolta a fine partita: `incompleta`, poi confermata
   alla riapertura.
7. Rientro: app in background, chiusa e riaperta, rete tolta e rimessa durante il proprio turno e durante quello dell'altro (#5, #6, #71–#75).
8. Passaggio Wi-Fi ↔ rete mobile durante una partita (#69, #70).
9. Tastiera in Login, Registrazione, Recupero e ricerca amici (#89).
10. Amici: richiesta, accettazione, invito, partita (#16, #115).
11. Cancellazione dell'account solo per gli account veri (requisito Apple), mai per gli ospiti.

**Solo iOS (TestFlight dalla Cloud Build):** lettere accentate corrette (#28, file ricodificati); safe area e notch su un iPhone piccolo e uno
grande; vibrazione; ritorno dal background dopo minuti (iOS chiude la connessione).
**Solo Android:** tasto Indietro di sistema in partita e nei pannelli; tablet e telefono piccolo; prestazioni su un telefono economico.

Una sola volta: **app vecchia (TestFlight 2) e app nuova non devono mai finire allo stesso tavolo**; TestFlight 2 da sola deve ancora ricevere i premi.

## 6. Cosa si può rimandare senza rischi per gameplay, account o acquisti

Nell'app **non ci sono acquisti** (nessun pacchetto IAP, nessuna pubblicità nel progetto): oggi non c'è niente da proteggere. Prima di
aprire un negozio servono la verifica delle ricevute sul server e la consegna "una volta sola" già in uso.

Si possono rimandare:
- Servizio di partita autorevole (#151, D12), Fase B del replay, filtro dello stato.
- 2v2 con scelta del compagno (#17), cronologia e giocatori recenti, schede AMICI/RICHIESTE/RECENTI, +AMICO a fine partita, settimanale a 9
  giorni, nuovi avatar, Scopa e Briscola (#90–#96).
- Audio v07: approvazione e mescolata (#57, #130, #131). Il pack resta così com'è.
- Grafica: icona del forziere nella Posta (#146), baule aperto (#147), icone dei trofei (#21, salvo deformazioni evidenti dallo screenshot),
  grafica ridotta (#64).
- Tabella dei premi della pagina Premi copiata dal server (#145): basta non cambiare `Economia.settimana` senza aggiornare l'app.
- Recupero dei premi vecchi (`RECUPERO_CONSEGNE.md`): solo con prove certe, a mano.
- Mostrare `tettoNonVerificabili` con un testo dedicato. Oggi basta "tetto raggiunto".
- Spegnere il ramo delle app vecchie: solo quando PlayStream lo permette.

Da **non** rimandare oltre il lancio: il controllo settimanale di PlayStream (`partita_incongruenza`, `partita_senza_biglietto`) e del
registro `Incongruenze` con `qa.js`. Senza questo controllo, il rischio #151 accettato diventa un rischio non sorvegliato.
