# Build 3: diagnosi del backlog QA (07/10)

Analisi in sola lettura del backlog "PROJECT 51 — BUILD 3", fatta prima di toccare il codice. Ogni gruppo di voci è stato indagato da un agente e ricontrollato da un secondo agente che cercava di smentirlo. Le affermazioni più pesanti le ho ricontrollate anche io a mano (regola del 51, sessione ospite, codifica dei file).
Il dettaglio per voce (prove file:riga, correzione proposta, giudizio del verificatore) è in `dettaglio_voci.md`. Le righe si riferiscono al codice del 07/10, con la 2.65 non committata.

---

## 0. Stato dei lavori (aggiornato a ogni blocco)

**Decisioni (07/10):** l'utente ha accettato TUTTI i consigli del §4 come decisioni finali. X1: l'accuso "Cirulla" si chiama "Accuso". Niente cambi di bundleVersion/build fino alla build iOS definitiva (per B2 si usa il suffisso "versione regole").

- Fatto 07/10 fuori ordine: trofei a 0 dopo l'avvio a freddo (UI51TrophySummary, parte di B19/ST2); corsa statoPremi/riscatto e festeggiamento immediato in Premi e Posta con forziere che aspetta il contenuto (parte di B21/B22); tastiera: riquadro nativo nascosto e contenuto "Safe" che sale (B25 senza "Invio passa al campo dopo"); I2 confermato (build da Cloud Build).
- B1 fatto: 6 file ricodificati, test SourceEncodingTests.
- B2 fatto: PhotonAuthConnector.RulesVersion (2) -> AppVersion "1.0.0-r2" per stanze, Chat ed etichetta versione.
- B30 fatto: MatchScore (> traguardo, 51 esatti -> 0 in ContinueMatch), RaceNote, risultati, tutorial, Regole; pillola "OLTRE 51". Testi delle scene corretti via API Editor (builder aggiornati, non rilanciati). Da guardare nel Simulator: larghezza della pillola OLTRE.
- X1 fatto: "Cirulla" -> "Accuso" nei testi visibili, sottotitolo accesso "Unisciti alla sfida del 51", benvenuto "Conosci già il 51?".
- B3 fatto: TurnController.AcceptsLocalInput letto al tocco (CardView.OnMouseDown, CardViewManager click/doppio click), buffer pendingLocalMove eliminato. Test TurnControllerInputGateTests.
- B4 fatto: SelectionCoroutine/HoverCoroutine rileggono la posa a ogni frame (hover con enter && enableHover).
- B5 fatto: un solo momento "tocca a te" (fronte di AcceptsLocalInput in Update): suono, vibrazione leggera, chip "TOCCA A TE" messa dove sta "Gioca adesso" (UI51TableMoments.YourTurn); banner proprio e alone mano seguono lo stesso controllo. Suono v07 rimandato a B34. Da guardare nel Simulator: posizione della chip TOCCA A TE.
- B6 fatto: caduta vista al ritorno dalla pausa conta dalla pausa; dal master server si riprova con RejoinRoom (solo su MasterServer); rientro rifiutato (non 32746) chiude subito; errore "Connessione persa" solo se si era in una stanza; toast in Home se il rientro rinuncia; testi neutri X2 coi nomi dei banner.
- B7 fatto: stato nuovo dal master ferma intro/distribuzione, uccide i tween sulle carte, toglie cartello e fantasmi dell'accuso del mazziere; i risultati si rimostrano solo se cambiano; bonus contati una volta per smazzata, premio una volta per partita (non per stato: un resync divergente non paga due volte).
- B8 fatto: chi rientra resta bloccato ("Collegato · aggiorno il tavolo…") finché non arriva lo stato del master (richiesto ogni 1,5 s); stato completo accettato solo dal master; senza stato -> toast e menu.
- X2 parziale: "· SMAZZATA N", "ATTENDI GLI ALTRI", "Prossima smazzata tra N secondi". Restano "Capp.", "RIMASTE", GetSimpleDisplayName.
- Revisione F1-F2 (deep-reviewer) applicata: overlay tolto prima dell'uscita per inattività, onMaster solo su MasterServer, uscita senza stato con overlay fino al menu, mossa vecchia non parcheggia copie, dedup per partita.
- B9 fatto: ospite nascosto = CustomId solo in memoria, nuovo dopo ogni Esci; HasRealLogin non più salvato sul telefono (vero solo dopo Accedi/Registrati in questa esecuzione); Logout chiama ForgetAllCredentials; via device-id e IsRegistered; flag impostato prima dell'evento del nome (S4).
- B10 fatto: WalletService.Reset, ProfileService.Reset (risposte in volo dell'account vecchio scartate), XP locale cancellata, tutto in AuthBootstrapper.ResetAccountCaches (Accedi ed Esci).
- B11 fatto: Posta ricaricata al profilo del nuovo account (OnProfileLoaded, solo se l'account è cambiato); "letto" per account; un regalo riscattato conta come letto; RewardsService.Reset scarta le risposte vecchie e spegne il pallino Premi.
- B12 fatto: emoticon in uso per account (l'ospite le cambia per la sessione e le porta nell'account se si registra; la Collezione si aggiorna dopo Accedi); "Silenzia emoticon" per account, quello dell'ospite (o verso un ospite) solo in memoria fino alla chiusura; l'ospite al tavolo vede solo "Silenzia"; silenziare un ospite ora funziona (MuteIdAt); eliminazione account cancella le chiavi per account. Le vecchie chiavi globali (Collection.Emoticons, Social.MutedEmoticons, Mail.ReadIds) non si leggono più: scelte e silenziati di prima ripartono da zero una volta.
- B13 fatto: il nome scelto vale subito alla registrazione (MarkRegistered col nome), NickName di Photon aggiornato quando cambia il nome, login che ripara il nome visibile mancante, Registrati senza sessione mostra un avviso invece di bloccare l'app. Rimandato: schermata "nome già preso, scegline un altro" (schermata nuova: da confermare).
- B14 fatto: SessionId nuovo a ogni login vero, mandato in "moderazione"; il server tiene l'ultimo ("Sessione" nei dati interni); l'altro telefono esce con avviso alla Home, prima di una partita online o al ritorno dal background, mai al tavolo. Ospiti e versioni vecchie esclusi. **Da fare dall'utente: ricaricare 51.js su PlayFab (carica.js).**
- B15 fatto: Photon Chat si ricollega da solo dopo una caduta (5 s, poi il doppio fino a 30 s; non dopo uscita voluta o credenziali rifiutate), va offline subito in background e torna al ritorno, fallback TCP, si collega dopo Accedi/registrazione senza aprire Amici (OnProfileLoaded), niente più condizione "ha amici"; "Visto" -> "Ultimo accesso". Da guardare nel Simulator: "Ultimo accesso 3 giorni fa" nella riga amico.
- B16 fatto: "Invitato" vale solo nella stessa stanza per 20 s (la durata dell'avviso), poi INVITA torna; invito fallito dalla pagina Amici ora lo dice.
- B17 fatto: scheda profilo "Tra i tuoi amici" (builder + scena), errori di Aggiungi amico in un avviso (anche "È già tra i tuoi amici."), stato aggiunto/segnalato legato all'account. Rimandato: scheda "Richieste" (resta vuota finché non esistono richieste vere).
- B18 rimandato (scelta squadre 2v2, dopo i bug).
- B19 fatto: un account senza profilo caricato vede "—" (livello, niente barra XP) invece dell'XP del telefono; aprire il Profilo riprova il caricamento (al posto di un pulsante RIPROVA, che richiederebbe un pezzo nuovo nel builder); premio di fine partita fallito -> statistiche rilette (RefreshStatistics); trofei dal livello dell'XP; pagina Trofei si riallinea se il profilo arriva dopo; partita lasciata conta sul server anche col profilo in caricamento. Un caricamento scartato (account cambiato) sblocca comunque chi aspetta (la Home non resta "in caricamento").
- B20 fatto: avatar pubblicato su Photon ("av") e mostrato su banner (anche il proprio), profilo rapido, sorteggio, risultati, momenti, sala privata e ricerca partita; bot, ospiti e versioni vecchie tengono il ritratto del posto; "mai scelto" = primo avatar come in Home. Avatar collegati in GameScene via API Editor (builder aggiornato, non rilanciato). Classifica e Amici: dopo, come deciso.
- B21 fatto: il forziere si apre da solo 0,7 s dopo il pop ("TOCCA PER APRIRE" tolto), RACCOGLI è l'unico tocco, in Premi e in Posta (il riscatto era già immediato dal primo giro).
- B22 fatto: in Posta i pulsanti RISCATTA/Raccogli tutto si spengono a metà opacità durante il riscatto; dopo un no o un errore Posta, Premi e saldo si rileggono dal server; il foglio aperto si riallinea senza riscorrere; il saldo conta solo l'ultima risposta. Server: posta senza data riscattabile (come la mostra il telefono), niente PostaGlobale agli ospiti (test.js). **Ricaricare 51.js.** Rimandato: monete e gemme in alto nelle pagine Posta e Premi (pezzo nuovo nei builder, da fare con verifica nel Simulator).
- B23 fatto: accesso sbagliato = un solo messaggio "Email/nome utente o password non corretti" (anche account inesistente), mai testo inglese di PlayFab; troppi tentativi -> attesa (il tempo detto da PlayFab, altrimenti 60 s) senza chiamare il server, con i secondi mostrati.
- B24 fatto: nome utente max 20 caratteri (builder + scena), solo lettere e cifre senza accenti, e 0,6 s dopo l'ultimo tasto PlayFab dice se è già preso (REGISTRATI spento, messaggio). Senza rete non blocca. Rimandato: schermata "nome già preso" (B13).
- B25 fatto: Invio passa al campo dopo (Accesso e Registrazione); il tasto resta "Fine/Invio" (Unity non permette "Avanti" senza plugin nativo). Sollevamento e riquadro nativo già fatti; si prova solo su telefono.
- B26 fatto: la matta trasformata ha bordo viola e pillola "MATTA" in basso finché vale un'altra carta (mano e mani accusate), anche nell'accuso 2v2/1v3 (cartellino "Matta" sulle carte dell'impatto, builder + scena); "Suggerimenti mosse" ora mette un bordo azzurro netto sulle carte che prendono (resta l'alone oro del turno sulle altre). Bordi separati: non si cancellano con l'oro delle accusate. Verificato in Play (screenshot).
- B27 fatto: le carte accusate degli altri aprono il visore al rilascio del dito (come i pulsanti) e non si sollevano più sotto al velo; tolto il "passa sopra per vedere il 7 vero" (c'è la scritta MATTA). Da provare su telefono: tocco vicino al bordo della carta.
- B28 fatto (parte "dal codice"): il "+N" delle proprie scope non tocca più l'anello di ACCUSA (builder + scena, 266 -> 258). Prova in Play con 6/1/2/5 scope: docs/build3/b28_scope_prova.png (prima del giro sui lati). Lati (sì dell'utente 07/10, scostandosi dal mockup): le scope salgono dritte sopra al banner verso il tavolo, sporgono 20, passo 17, "+N" dopo la quarta, gettone del mazziere nell'angolo esterno (UI51TableBuilder.SideScopeCard/RelayoutSideScope, scena aggiornata).
- B29 fatto: dealer e tre assi usano lo stesso nome dei banner (GameSocialV2.PlayerName via reflection). "Capp." e "RIMASTE" restano: "Cappotto" (Cinzel 14) non entra nei 62 del totale, "CARTE RIMASTE" non entra nella medaglia da 52 (regola "solo se entra").
- B31 già coperto da B30 e X1 (testi di Regole, Benvenuto, Accesso e tutorial corretti nelle scene; nessun "Cirulla" visibile).
- B32 fatto: "premioTutorial" sul server (+200 monete, TitleData Economia.tutorial, una volta per account con la chiave interna "Tutorial", ospiti no) e prove in test.js; il telefono lo chiede a fine guida (schermata "Fatto") e mostra l'avviso; l'ospite legge "Registrati e rifai il tutorial per avere +200 monete". **Da fare dall'utente: Upload + Deploy di 51.carica.js su PlayFab** (preparato con carica.js il 07/10, stesso segreto: pannello Photon invariato; senza, il telefono riceve "CloudScriptNotFound"). "+200" nel Benvenuto (pillola del mockup) e nella schermata finale (scheda del mockup, accesa solo quando il server paga) col builder della Fase 12.
- B33 fatto (scelte dell'utente 07/10): tutorial corto e tutto scriptato. Core: Rules51.ScriptedDeck (mazzo fisso in ordine di pesca, mazziere l'ultimo posto), RoundManager.TotalHands dal mazzo rimasto, TutorialScript (due mazzi ridotti, mosse, totali di partenza 45/49), prova Rules51CoreTests.TutorialScript_PlaysExact51ThenAWin (al posto di quella del seme, tolti TutorialSeed e Reseed). TurnController: TutorialGate (solo la mossa guidata), TutorialBotMove (bot del copione), TutorialHold (Accuso, bot e fine smazzata aspettano Nonna Rosa). UI51TutorialView: 14 passi legati alla partita, tocchi attraverso il buco del velo (ICanvasRaycastFilter), schermata finale dopo i risultati, GIOCA LA PRIMA PARTITA = rivincita vera. Storia: prima smazzata uguale, somma, 15, scopa+settebello del bot, Accuso, l'asso piglia tutto, il bot chiude a 51 esatti e torna a 0 (passo sulla "Corsa al 51"); seconda smazzata scopa col 15 e settebello, ultime due carte libere, vinci a 52+ qualunque scelta. Niente statistiche, XP, monete di partita né abbandoni (MatchResultsV2). Provato nel Simulator (iPhone 12) per intero: circa 100 s di gioco senza lettura, quindi 4-5 minuti veri.
- B34 fatto: SoundLibraryBuilder punta al pacchetto v07 (5 carta giocata, 4 presa, 3 scopa, 3 conferma, musica v07) coi valori del CSV; con lo stesso file restano i volumi ritoccati, con un file nuovo si riparte dal CSV. Libreria ricostruita (20 suoni, nessun file mancante). I file v01-v03 restano in Assets/Audio, non usati. Niente Shuffle (manca l'animazione). Da ascoltare: sincronia di carta giocata e scopa (file molto più corti).
- B35 fatto: musica abbassata di circa 5 dB sotto scopa e accuso e 8 dB sotto vittoria e sconfitta (scende in 0,08 s, risale in 0,8 s dopo la fine dell'effetto; effetti sovrapposti non si sommano); la dissolvenza Home <-> tavolo dura davvero 1,2 s; la prima variante di ogni suono può uscire anche alla prima riproduzione.
- B36 fatto: gli interruttori vibrano (leggero); dopo una sconfitta niente vibrazione per l'XP (restano le particelle; la vittoria vibra già da sé). Turno, scopa, accuso, vittoria vibravano già. Prova solo su telefono.
- Giro 07/10 sera (scelte dell'utente: sì saldo in Posta/Premi, sì RIPROVA, no schermata "nome già preso", Aiuto = Impostazioni → Regole e tutorial, "Capp."/"RIMASTE" abbreviati): carta sollevata fino all'eco del server (CardViewManager.AfterLocalMove, abbassata in ExecuteMoveWithAnimation); ora del server per "Ultimo accesso" e date della Posta (DeviceModeration.UtcNow, SyncClock per tutti); RIPROVA nel Profilo (ProfileService.IsLoading, ProfileViewData.CanRetry, UI51MetaBuilder.ProfileRetry); saldo in testata di Posta e Premi (UI51WalletPills, UI51SocialBuilder.HeaderWallet); Premi aspetta i dati del server dell'account attuale; server: niente premio giornaliero e Posta agli ospiti (test.js, 51.carica.js rigenerato); onFocusSelectAll spento; nome del giocatore scollegato tenuto per posto (GameSocialV2.PlayerName); tutorial a 17 passi (avversario e mazziere, mazzo e rimaste, matta, timer nel testo della mano); vibrazione su avvisi e carte della Collezione (UIV2MotionInstaller.AddHaptics); Classifica riattivata (tolto SetPendingActionsInteractable); Scope #40 visto nel Simulator. Scena MainMenu aggiornata via API Editor con gli helper dei builder. Test EditMode 379/379, test.js ok. **Ricaricare 51.carica.js.**
- B37 in attesa del telefono: prima il Profiler su una build Development, poi si decide cosa spegne "Grafica ridotta" (fps compresi).
- B38 tocca all'utente (giro TestFlight + APK).
- Controlli nel Simulator 07/10 (iPhone 12): pillola OLTRE 51 dentro al punteggio, chip TOCCA A TE tra tavolo e mano, righe "Ultimo accesso 5 ore fa / ieri / 3 giorni fa" (anche su SE).
- Prossimo: B37, B38 sui telefoni (Development build).

---

## 1. Tre scoperte da sapere subito

1. **La regola del 51 è sbagliata anche nel gioco, non solo nel tutorial.** `MatchScore.IsFinished` chiude la partita con `best >= 51`, quindi arrivare a 51 esatti fa vincere. Nessun punto del codice riporta un 51 esatto a 0. I testi (regole, tutorial, risultati) sono stati scritti partendo dalla stessa regola sbagliata.
2. **Dopo "Esci", "Accedi come ospite" può rientrare nell'account vero.** La sessione nascosta che parte all'avvio usa un id ospite salvato sul telefono. Se l'utente si registra da lì, quell'id diventa l'account vero. All'uscita l'id non viene cambiato, quindi l'"ospite" rientra nello stesso account. Questo spiega "visto poco fa" dopo una password sbagliata, le emoticon e la posta che passano all'ospite, il pallino della posta. Gli effetti vanno oltre: l'ospite può riscuotere la posta e i premi dell'account, e le segnalazioni finiscono sull'account vero.
3. **Gli accenti a quadratini vengono dalla codifica dei file, non dai font.** 6 file C# sono salvati in Windows-1252, non in UTF-8: `PlayFabAuthService`, `MatchmakingManager`, `AuthUIController`, `ProfileService`, `Rules51`, `SafeAreaUtil`. Compilati su Windows vanno bene. Compilati dalla Cloud Build iOS (Mac), "Questa email è già in uso" perde la è e la à. È proprio il messaggio segnalato.

---

## 2. Cause comuni (spiegano più voci insieme)

| # | Causa | Voci | File principali |
|---|---|---|---|
| C1 | **Nessun controllo "posso giocare adesso"**. I tocchi guardano solo di chi è il turno, non se il tavolo sta ancora animando (distribuzione, finestra accuso da 5 s, volo della carta, attesa del server). In più c'è un buffer voluto (v2.10) che memorizza il tocco fatto mentre il tavolo è occupato e lo gioca dopo. | T1 T2 T3 T4 | TurnController, CardView, CardViewManager |
| C2 | **Il rientro sblocca prima che arrivi lo stato**: l'avviso sparisce e compare "Sei di nuovo in partita!" prima di avere i dati. La richiesta di stato parte una volta sola, senza conferma. Lo stato nuovo sostituisce i dati ma non la grafica: restano attive le animazioni vecchie, c'è una seconda finestra accuso, il mazziere resta quello vecchio, mancano i risultati se la smazzata era finita. Ogni disconnessione fa scattare l'errore del matchmaking, per questo compare "Ricerca 1v1 online". | R1 R2 R3 | NetworkGameController, TurnController, MatchmakingManager, RoomFlowV2, UI51ConnectionOverlay |
| C3 | **La sessione nascosta può essere un account vero** (vedi scoperta 2). Il flag "HasRealLogin" è salvato per telefono ma viene usato come se descrivesse la sessione in corso; l'uscita non pulisce le credenziali dell'SDK. | S3 S4 P2 P3 M4 | PlayFabAuthService, AuthBootstrapper, AuthUIController |
| C4 | **Le cache non si azzerano al cambio account e le schermate della Home caricano una volta sola.** Profilo, portafoglio, premi, lista bloccati e scheda profilo rapido restano in memoria. Posta, Premi, Amici e Collezione leggono i dati solo all'apertura della scena. Solo la Home si ricarica dopo "Accedi". | S3 S4 ST1 ST2 M4 E1 SO1 SO2 P1 | ProfileService, WalletService, RewardsService, QuickProfileCard, UI51MailView, UI51RewardsView, UI51FriendsView |
| C5 | **Dati personali salvati in PlayerPrefs per telefono, non per account**: emoticon equipaggiate, giocatori silenziati, mail lette, XP locale, benvenuto visto. | E3 S3 E1 E2 M4 ST1 TU3 | CollectionCosmeticsV2, FriendsService, MailService, PlayerProgressLocal |
| C6 | **La presenza amici (Photon Chat) vive solo dentro la schermata Amici.** Parte quando apri Amici, "Accedi" la spegne e niente la riaccende. Se cade non riprova, e al ritorno dal background non si ricollega se la lista amici è vuota. Usa solo UDP, senza ripiego su altri protocolli. "Visto" mostra l'ultimo accesso PlayFab, non l'ultima volta online. | P1 P3 P4 SO2 L1 | FriendsChat, UI51FriendsView |
| C7 | **Risposte del server applicate senza ordine e senza riallineamento.** Se un ritiro fallisce, posta, saldo e premi non vengono ricaricati. Una risposta vecchia può sovrascrivere un ritiro appena riuscito (Premi: la lettura partita all'apertura; Posta: la lista ricaricata). Le statistiche si aggiornano solo con la risposta di fine partita. | M1 M2 M3 ST1 ST2 | RewardsService, UI51RewardsView, UI51MailView, WalletService, ProfileService |
| C8 | **I pulsanti bloccati sembrano uguali a quelli attivi**: il blocco c'è, ma non si vede, e invita al secondo tocco. In più il rinfresco della pagina riattiva il pulsante mentre il ritiro è ancora in corso. | M1 M2 | UI51Press, UI51RewardsView, UI51MailView |
| C9 | **Il nome del giocatore non si propaga.** Il nome Photon viene impostato solo alla connessione, quindi dopo la registrazione gli altri vedono ancora "Ospite". Il nome arriva da 5 fonti diverse; un posto disconnesso diventa "Bot N". | S2 N2 X2 | PhotonAuthConnector, AuthUIController, NetworkGameController |
| C10 | **L'avatar scelto non viene mai pubblicato agli altri**: al tavolo c'è un ritratto fisso, anche per te; in lobby un segnaposto; negli Amici un ritratto calcolato dall'id. | ST4 SO2 | AuthBootstrapper (look props), PlayerBannerManager, RoomFlowV2 |
| C11 | **Tutti i campi di testo nascono da un solo builder senza gestione della tastiera**: resta visibile il riquadro nativo sopra la tastiera (probabile "rettangolo strano"), nessun sollevamento del campo, Invio non passa al campo dopo. | I3 I4 I5 | UI51Input, UI51AccessBuilder, DeleteAccountModalV2 (unico con il sollevamento) |
| C12 | **Partite e Chat separate solo da `Application.version`**, ferma a 1.0.0. Un APK Android vecchio non vede le build TestFlight (presenza, inviti, codici stanza). E build TestFlight con regole diverse possono finire nella stessa stanza. | P4 TU1 | PhotonAuthConnector, FriendsChat |
| C13 | **Un solo alone tenue per quattro significati**: turno, suggerimento mossa, Matta, accuso del mazziere. Ai bordi della carta è quasi invisibile. | T1 A3 O1 | CardView, CardViewManager |
| C14 | **Le posizioni 2v2 seguono l'ordine di ingresso** (tua scelta del 16/09). Un rientro o l'host che esce spostano tutti. | L2 L3 | SeatLayout, GameSceneInitializer, RoomFlowV2 |
| C15 | **La libreria suoni punta ai vecchi file** e il builder conserva i vecchi volumi. C'è una sola sorgente musicale, senza abbassamento della musica; la dissolvenza dura 0,18 s invece di 1,2 s. | AU1 AU2 | SoundLibraryBuilder, GameAudio |

Singole (senza causa comune): I1 errori PlayFab non tradotti (1356/1199/1342) e nessuna pausa tra i tentativi; N1 nessun controllo del nome; A1 le carte reagiscono quando il dito tocca, l'interfaccia quando si alza; A2 geometria fissa delle strisce scopa; X1 6 testi con "Cirulla"; TU2/TU3 tutorial e ricompensa da costruire; O2/O3 vibrazione e grafica ridotta da rendere coerenti.

---

## 3. Ordine consigliato dei blocchi

Segue il tuo ordine F1–F11. Ho cambiato solo dove c'è una dipendenza vera, e lo dico. Ogni blocco è piccolo, verificato e consegnato da solo.

**Fase 0, preliminari (nuova, prima di tutto)**
- **B1 Ricodifica UTF-8 dei 6 file**, riga per riga perché `MatchmakingManager` è misto, più un test che rifiuta file non UTF-8. Risolve I2 e va fatta per prima: quasi tutti i blocchi successivi modificano questi file, e modificarli adesso rischia di rovinare gli accenti.
- **B2 Separare le build con regole diverse**: un suffisso "versione regole" nell'AppVersion di Photon e della Chat, e prove a due telefoni solo con la stessa build. Senza questo le prove di F2/F4 non valgono, e la regola del 51 corretta finirebbe in stanza con build vecchie.

**F1 Turno / input**
- **B3 Controllo "posso giocare adesso"**, letto al momento del tocco, ed eliminazione del buffer `pendingLocalMove` (T3, T4).
- **B4 Le animazioni di posa della carta leggono la posizione aggiornata** (T2).
- **B5 Un segnale unico "tocca a te"** quando il gioco diventa pronto (T1). Serve la tua scelta del segnale.

**F2 Rientro**
- **B6 UI di disconnessione separata dal matchmaking** e testi del rientro (R3, più la parte di X2 che vive negli stessi file).
- **B7 Lo stato ricevuto ridisegna anche la grafica**: stop alle animazioni vecchie, mazziere, risultati una volta sola (R2).
- **B8 Rientro: avviso e input bloccati finché non arriva lo stato**, nuovo tentativo se il rientro viene rifiutato, messaggio chiaro se fallisce (R1).

**F3 Sessione / account** (qui confluiscono anche E1, E2 ed ST1, che hanno la stessa causa)
- **B9 La sessione nascosta non è mai un account vero** (S3, P2, P3). È la radice di B10–B15.
- **B10 Azzeramento delle cache al cambio account** (S3, ST1, E1, SO1).
- **B11 Posta e Premi si ricaricano dopo "Accedi"** (S4, M4).
- **B12 Dati locali per account**: emoticon, silenziati, silenzio locale per l'ospite, XP locale (E3, E1, E2).
- **B13 Il nome si propaga subito dopo la registrazione** (S2, N2).
- **B14 Una sola sessione attiva per account** (S1): serve una funzione nuova nel CloudScript e la tua scelta della regola.

**F4 Presenza / inviti / lobby**
- **B15 La presenza parte al login e si riaccende da sola** (P1, P3, P4, SO2).
- **B16 Il reinvito si sblocca** (L1). Il motivo è semplice: chi hai invitato resta segnato finché non chiudi la Home.
- **B17 "Aggiungi amico": testo ed errori** (SO1). Oggi l'aggiunta è immediata e a senso unico, ma la scritta dice "Richiesta inviata".
- **B18 Scelta squadre 2v2** (L2, L3): grande, solo se la approvi, dopo i bug.

**F5 Statistiche / trofei / avatar**
- **B19 Statistiche riallineate dopo la partita, e i trofei le seguono** (ST1, ST2).
- **B20 Pubblicare l'avatar scelto** (ST4, SO2).

**F6 Premi / posta**
- **B21 Premi: basta un tocco** (M1).
- **B22 Posta: blocco visibile, riallineamento, pallino rosso** (M2, M3, M4).

**F7 Login / input mobile**
- **B23 Errori di accesso leggibili e pausa tra i tentativi** (I1).
- **B24 Controllo del nome mentre lo scrivi** (N1).
- **B25 Tastiera**: il campo si solleva, sparisce il riquadro nativo, Invio passa al campo dopo (I3, I4, I5). Si prova solo su telefono.

**F8 Tavolo / testi**
- **B26 Segno permanente "MATTA" e suggerimento mossa ben visibile** (A3, O1).
- **B27 Un gesto unico per vedere le carte accusate** (A1). Viene dopo B26: prima serve il segno permanente della Matta.
- **B28 Strisce scopa leggibili con qualsiasi numero** (A2), sistemate dal codice, senza ricostruire la scena.
- **B29 Testi nel codice** (X1, X2).
- **B31 Testi di Regole, Benvenuto e Accesso**, in un solo passaggio del builder (TU1 testi, X1).

**F9 Regola del 51 e tutorial**
- **B30 Regola: 51 esatti tornano a 0, si vince sopra 51** (TU1). **Consiglio di anticiparla subito dopo B2**: è un difetto del gioco vero, ogni partita lo subisce, ed è piccola.
- **B32 Ricompensa tutorial una volta sola, decisa dal server** (TU3).
- **B33 Tutorial guidato** (TU2): grande, dopo la regola corretta.

**F10 Audio / opzioni**
- **B34 Collegare l'audio v07** (dal builder; il vecchio resta). **B35 Abbassamento della musica e dissolvenza vera.** **B36 Vibrazione coerente.** **B37 Grafica ridotta che fa davvero risparmiare**: prima si misura sul telefono con il Profiler.

**F11 Prove su telefoni**
- **B38**: un solo giro TestFlight + APK con build identiche, seguendo la lista del backlog. Serve anche a raccogliere i log `[NET]` e `[FriendsChat]` che chiudono R1 e P4.

---

## 4. Decisioni tue (servono prima del blocco indicato)

**Per partire (B3, B5, B30, B2):**
- **T3/T4**: i tocchi fatti mentre il tavolo è occupato si scartano tutti, compresa la scelta della presa? Online, la carta toccata resta sollevata finché il server non conferma? *Consiglio: scartare tutto, e tenere la carta sollevata fino alla conferma, così su 4G non sembra un tocco perso.*
- **T1**: quale segnale per "tocca a te"? *Consiglio: scritta "TOCCA A TE" sopra il tuo banner per circa 1,5 s, una vibrazione leggera e il suono your_turn v07, tutti una volta sola quando il gioco è pronto. La scritta resta anche con la grafica ridotta.*
- **TU1, conferma della regola**: il ritorno a 0 avviene solo a fine smazzata? Per squadra in 2v2 e per giocatore in 1v3? Se due sono entrambi a 51 tornano tutti e due a 0? *Consiglio: sì a tutti e tre; nella pillola "OLTRE 51"; un momento "51 → 0" nei risultati.*
- **Separazione delle build**: alzare `bundleVersion` (1.0.0 → 1.0.1) o usare un suffisso "versione regole" in Photon? *Consiglio: il suffisso, cambiato solo quando cambiano regole o protocollo.*

**Più avanti:**
- **S3/S4/P2**: cosa deve essere la sessione nascosta? *Consiglio: un account usa e getta a ogni avvio, senza rientro automatico (è quello che la schermata mostra già oggi). Il "ricordami" eventualmente dopo, come funzione a parte.*
- **S1**: stesso account su due telefoni. *Consiglio: vince l'ultimo accesso; l'altro telefono viene fatto uscire alla Home, all'ingresso in una partita online o al ritorno dal background, mai a partita in corso; ospiti esclusi.*
- **E3/E2**: emoticon per account su questo telefono (veloce) o salvate su PlayFab (seguono l'account)? L'ospite può cambiarle? Quanto dura il silenzio messo dall'ospite? *Consiglio: per account sul telefono; l'ospite le cambia solo per la sessione; il silenzio dell'ospite dura fino alla chiusura dell'app, e per gli ospiti niente "Segnala".*
- **ST1**: cosa vede un registrato mentre il profilo carica o non carica? *Consiglio: "—" con RIPROVA, al posto del totale del telefono.*
- **S2/N2**: nome già preso dopo la registrazione? *Consiglio: chiedere un altro nome; nomi unici (come oggi, è lo Username PlayFab).*
- **P1/P3**: la presenza parte al login? In background si va offline subito? *Consiglio: sì, offline subito e ricollegamento al ritorno; la scritta "Visto" diventa "Ultimo accesso".*
- **SO1**: amicizia a senso unico (come oggi, sistemando solo la scritta) o richieste vere da accettare (grande)? *Consiglio: senso unico adesso.*
- **L2/L3**: scelta squadre 2v2? *Consiglio: rimandare dopo i bug; se la vuoi, nelle stanze private l'host sistema i posti; l'host che esce passa il ruolo (come oggi).*
- **ST4**: dove si vede l'avatar degli altri? *Consiglio: tavolo e lobby, compreso il tuo banner; Classifica e Amici dopo.*
- **M1**: quale tocco unico per il forziere? *Consiglio: il forziere si apre da solo e RACCOGLI è l'unico tocco, anche nella Posta.*
- **M2**: basta il blocco nel telefono più la protezione del server contro i ritiri doppi in sequenza (il limite accettato il 02/10)? *Consiglio: sì, più il blocco visibile.*
- **M3/M4**: mostrare il saldo dentro Posta e Premi? Quando si spegne il pallino? *Consiglio: monete e gemme in alto nella pagina; il pallino si spegne quando ogni messaggio è aperto o riscosso; stato di lettura per account; niente PostaGlobale agli ospiti.*
- **I1**: quanto dura la pausa dopo troppi tentativi? *Consiglio: quella indicata da PlayFab, altrimenti 60 s, e un messaggio unico "Email/nome utente o password non corretti".*
- **N1**: controllare dal vivo anche l'email? *Consiglio: solo il nome, per la stessa privacy del Recupero.*
- **A1/A3**: gesto e aspetto della Matta. *Consiglio: tocco che apre al rilascio; scritta "MATTA" più un bordo di colore suo; togliere il "tieni premuto per vedere il 7 vero".*
- **A2**: le strisce scopa possono scostarsi dal mockup? *Consiglio: prima la correzione dal codice, poi decidiamo dopo una prova nel Simulator.*
- **O1**: "Suggerimento mosse" = carte che possono prendere (oggi) o la mossa migliore? *Consiglio: come oggi, con un bordo netto.*
- **X1**: nuovo nome per l'accuso "Cirulla"? Il package Android `com.project51.cirulla` resta? *Consiglio: il nome lo scegli tu; nei testi il gioco si chiama "51"; il package resta (cambiarlo crea una nuova app sullo store).*
- **TU2/TU3**: copione del tutorial e ricompensa. *Consiglio: due smazzate corte con punteggi di partenza preparati; nella prima il bot arriva a 51 e torna a 0, nella seconda vinci tu sopra 51. 2 turni liberi, non conta nelle statistiche. +200 monete una volta per account, date dal server; l'ospite vede "registrati per riscattarla".*
- **AU1/AU2**: approvare la v07 dopo l'ascolto? Shuffle? *Consiglio: niente Shuffle finché non c'è un'animazione del mescolare. Musica abbassata di circa 5 dB su scopa e accuso, di circa 8 dB su vittoria e sconfitta. Stessa traccia con dissolvenza vera di 1,2 s.*
- **O2/O3**: quali eventi vibrano? Fps? *Consiglio: vibrano inizio turno, scopa, accuso, vittoria e interruttori, non dopo una sconfitta; fps decisi solo dopo il Profiler.*

---

## 5. Da fare tu, fuori dal repository

- Controllare che su PlayFab la revisione CloudScript attiva sia l'ultima (almeno 2.62, che restituisce `statistiche`).
- Nel Game Manager, aggregazione "Last" (non "Sum") per TotalGames, Wins, XP, Level e TotalScope.
- Webhook Photon (dalla nota del 03/10): PathBeforeJoin e PathLeave vuoti.
- Testi con "Cirulla" nello store e nella TitleData di PlayFab.
- Prove a due telefoni sempre con la **stessa** build (confronta l'etichetta della versione).

## 6. Cosa non è un bug, o è già a posto

- **T4**: nessuna mossa doppia arriva mai allo stato di gioco (c'è la guardia una-mossa-per-turno e la convalida su ogni telefono). Manca solo il blocco dell'interfaccia.
- **L2**: le squadre non sono casuali, seguono l'ordine di ingresso (tua scelta del 16/09). Mancano etichette e scelta.
- **L3**: il passaggio dell'host e gli inviti dei non-host sono voluti.
- **SO1**: l'aggiunta immediata è il comportamento di PlayFab. Sbagliata è la scritta "Richiesta inviata".
- **O1, O2**: gli interruttori funzionano. Il problema è quanto si vede il suggerimento e quali eventi vibrano.
- **N2**: l'identità è già il PlayFab ID e il nickname è unico.
- **M2, lato server**: un secondo ritiro dopo il primo viene già rifiutato.

## 7. Ancora aperti (servono screenshot o log)

- **ST3** trofei deformati: nel codice e nelle scene tutte le icone hanno l'aspetto bloccato. Serve uno screenshot della schermata.
- **I4** "rettangolo strano": probabilmente il riquadro nativo sopra la tastiera, ma potrebbe essere la barra di iOS per le password. Serve uno screenshot.
- **I3** "ricerca amici": il campo è in alto e la tastiera non può coprirlo. La causa è un'altra, forse quella di I4.
- **I2**: la diagnosi vale se la build iOS è stata compilata con la Cloud Build (Mac). Se l'hai compilata da questo PC, va ricontrollata.
- **R1**: le cause sono confermate, ma non sappiamo quale abbia colpito il tester. Servono i log `[NET]` e sapere quale telefono era l'host.
- **P4** Android sempre offline: tre candidati (Chat non connessa, build diversa, UDP bloccato). Servono la versione dell'APK e la lista amici di quell'account.
- **E1** ospite che non vede le emoticon: spiegato se il telefono era stato usato da un account; non spiegato su un telefono pulito contro un umano.
- **M1**: quale schermata chiedeva due tocchi (Premi, forziere o Posta)?
- **Extra, fuori dal backlog**: il pulsante **Classifica** della Home viene disattivato all'avvio (`HomeV2Integration.cs:83`) e niente lo riattiva. Probabilmente non funziona mai. Da provare con un tocco.
