# Build 3: stato di ogni voce del MASTER QA / FIX BACKLOG (07/10)

**08/10 notte: decisioni D11-D13 e rapporto per la pubblicazione in `RAPPORTO_STABILIZZAZIONE.md`** (D11 fatto in locale = #152; #151 diventa rischio accettato; #153 e #154 nuovi, da decidere).

Aggiornato l'08/10 sera con i risultati verificati delle partite (#141-#143, #148-#150 fatti in locale, non caricati) e la verifica delle carte nascoste (#151, piano da approvare): vedi `PIANO_RISULTATI_AUTOREVOLI.md` e `AUDIT_151_CARTE_NASCOSTE.md`. Prima: 08/10 pomeriggio con i risultati del secondo APK Android (terzo giro, #132-#137). Prima: 07/10 sera (giro "chiudi tutto senza build" + #105). **Blocco "senza device" completato**: restano aperte solo #21, #64, #101 e le voci che usciranno dai test sull'APK Android. Confronto voce per voce del backlog originale col lavoro fatto (blocchi in `DIAGNOSI.md` §0). Una voce è **RISOLTO** solo se è stata
provata (test automatico che riproduce il caso, o prova dal vivo nel Simulator) e non dipende da un telefono, da due telefoni o dal server.
Compilare non basta.

Stati: **RISOLTO** · **DA VERIFICARE** (fatto, manca la prova che conta) · **APERTO** · **RIMANDATO** (scelta voluta).

## Priorità 1: gameplay, turni e input

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 1 | Turno molto più evidente (T1) | B5 | DA VERIFICARE | Chip TOCCA A TE, suono, vibrazione, alone mano: posizione vista nel Simulator; percezione e vibrazione solo su telefono |
| 2 | Spam tap carte, carta che resta dietro la mano (T2) | B3, B4 | DA VERIFICARE | Test automatici del controllo d'ingresso; lo stress test del tap ripetuto va fatto sul telefono |
| 3 | Input prima del proprio turno, niente coda (T3) | B3 | DA VERIFICARE | Buffer eliminato, test TurnControllerInputGateTests; prova sul telefono |
| 4 | Nessun input dopo una mossa finché il gioco non è pronto (T4) | B3, giro 07/10 sera | DA VERIFICARE | Online la carta toccata resta sollevata fino all'eco del server, poi vola (provato in Play con eco finto a 1,5 s). Su 4G va visto sul telefono |

## Priorità 1: riconnessione

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 5 | Snapshot completo al rientro (R1, R2) | B7, B8 | DA VERIFICARE | Due telefoni; servono i log [NET] |
| 6 | Rientro durante proprio turno, turno avversario, distribuzione, cambio smazzata, dopo una presa | B38 | DA VERIFICARE | Prova a due telefoni |
| 7 | UI di disconnessione non da matchmaking (R3) | B6 | DA VERIFICARE | Testi e flusso separati; prova staccando la rete |

## Priorità 1: sessione, account, cache

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 8 | Stesso account su due dispositivi (S1) | B14 | DA VERIFICARE | Bloccato finché 51.carica.js non è caricato su PlayFab |
| 9 | Dopo la registrazione resta "Ospite" (S2) | B13 | DA VERIFICARE | Prova sul telefono. Niente schermata "nome già preso" (tua scelta 07/10): basta il controllo mentre scrivi (B24) |
| 10 | Dati che restano tra account A, ospite, account B (S3, E3) | B9–B12 | DA VERIFICARE | Test obbligatorio A → Esci → Ospite → B sul telefono |
| 11 | Ciclo completo della sessione | B9–B15 | DA VERIFICARE | Telefono |

## Priorità 1: presenza amici

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 12 | Online/offline amici affidabile (P1, P3) | B15 | DA VERIFICARE | Due telefoni |
| 13 | "Visto poco fa" dopo password sbagliata (P2) | B9, B15 | DA VERIFICARE | Causa (sessione nascosta = account vero) eliminata; prova sul telefono |
| 13b | Extra: orologio del telefono indietro → "Ultimo accesso poco fa" anche per accessi vecchi | giro 07/10 sera | DA VERIFICARE | "Ultimo accesso" e date della Posta usano l'ora del server ("ora" di moderazione, ora per tutti gli account, non solo ospiti). Prova: telefono con l'orologio indietro di qualche giorno |
| 14 | Login giusto/sbagliato, logout, chiusura forzata, background, rete persa, rientro | B38 | DA VERIFICARE | Telefono |
| 15 | Android sempre offline (P4) | B2, B15 | DA VERIFICARE | APK con la stessa build di TestFlight |

## Priorità 1: inviti, lobby, 2v2

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 16 | Reinvito amico (L1) | B16 | DA VERIFICARE | Due telefoni |
| 17 | 2v2 scelta squadra/compagno (L2) | B18 | RIMANDATO | Scelta accettata: dopo i bug |
| 18 | Comportamento host (L3) | — | DA VERIFICARE | Voluto così; va solo provato (host esce, non-host invita, lobby piena) |

## Priorità 1: statistiche, profilo, trofei

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 19 | Statistiche affidabili (ST1) | B19, giro 07/10 sera | DA VERIFICARE | Telefono. RIPROVA nello stato d'errore del Profilo ("Progressi non caricati"), solo a caricamento finito male: visto nel Simulator, il tocco ricarica il profilo |
| 20 | Trofei che non compaiono al primo accesso (ST2) | fuori ordine + B19 | DA VERIFICARE | Telefono |
| 21 | Icone trofei deformate (ST3) | — | APERTO | Nel codice le icone hanno l'aspetto bloccato: serve uno screenshot dal telefono |
| 22 | Avatar visto subito dagli altri (ST4) | B20 | DA VERIFICARE | Due telefoni. Avatar in Classifica e Amici: RIMANDATO (scelta accettata) |

## Priorità 1: premi e posta

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 23 | Premi che chiedono due tocchi (M1) | B21 + corsa statoPremi | DA VERIFICARE | Telefono |
| 24 | Più tocchi sulla posta (M2) | B22 | DA VERIFICARE | Serve 51.carica.js caricato |
| 25 | Saldo/stato posta aggiornato subito (M3) | B22, giro 07/10 sera | DA VERIFICARE | Monete e gemme in alto in Posta e Premi (pillole della Home), aggiornate a ogni risposta del saldo: viste nel Simulator su iPhone 12 e SE con "Raccogli tutto"/"Presto in arrivo". Il riscatto vero serve 51.carica.js caricato |
| 26 | Pallino rosso posta (M4) | B11, B22 | DA VERIFICARE | Telefono |
| 26b | Extra: dopo Accedi la pagina Premi mostra per un attimo il giorno dell'account precedente (e si può toccare) | giro 07/10 sera | DA VERIFICARE | Senza dati del server per l'account attuale la pagina aspetta ("…", giorni non toccabili, riscatto bloccato). Prova A → Accedi B sul telefono |
| 26c | Extra: il server paga premio giornaliero e posta anche a un ospite | giro 07/10 sera | DA VERIFICARE | riscattaPremio, riscattaPosta e riscattaTuttaPosta rispondono "ospite" senza pagare; test.js ok. Serve 51.carica.js caricato (rigenerato stasera) |

## Priorità 1: login, registrazione, input mobile

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 27 | Troppi tentativi, errore tecnico PlayFab (I1) | B23 | DA VERIFICARE | Test sui messaggi; prova vera con PlayFab |
| 28 | Accenti a quadratini (I2) | B1 | DA VERIFICARE | File ricodificati + test; va visto in una build della Cloud Build iOS |
| 29 | Tastiera che copre i campi (I3) | B25 + fuori ordine | DA VERIFICARE | Telefono; per "ricerca amici" la causa è un'altra (campo in alto) |
| 30 | Rettangolo strano sopra i campi (I4) | fuori ordine, giro 07/10 sera | DA VERIFICARE | Riquadro nativo tolto e "seleziona tutto al tocco" spento su tutti i campi (UI51Input + builder). Screenshot dal telefono |
| 31 | "Avanti" della tastiera (I5) | B25 | DA VERIFICARE | Invio passa al campo dopo; il tasto resta "Invio" (limite di Unity) |

## Priorità 2: nickname, amicizie, ospiti

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 32 | Disponibilità del nome mentre lo scrivi (N1) | B24 | DA VERIFICARE | Prova con PlayFab vero |
| 33 | Nickname duplicati, identità = PlayFab ID (N2) | B13, B24 | RISOLTO | Nomi unici (Username PlayFab), identità sempre il PlayFab ID |
| 34 | "Aggiungi amico" coerente (SO1) | B17 | DA VERIFICARE | Aggiunta immediata con testi giusti; scheda Richieste: RIMANDATO (scelta accettata) |
| 35 | Aggiornamenti social reattivi (SO2) | B11, B15, B16, B20 | DA VERIFICARE | Due telefoni |
| 36 | Ospite che non vede le emoticon (E1) | B10, B12 | DA VERIFICARE | Spiegato solo se il telefono era stato usato da un account; prova con telefono pulito |
| 37 | Silenzio locale per l'ospite (E2) | B12 | DA VERIFICARE | Telefono |
| 38 | Emoticon salvate per account (E3) | B12 | DA VERIFICARE | Telefono (test A → ospite → B) |

## Priorità 2: Accuso, Scopa, Matta, testi

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 39 | Anteprima Accuso: tocco o pressione lunga (A1) | B27 | DA VERIFICARE | Telefono: tocco vicino al bordo della carta |
| 40 | Indicatori Scopa sovrapposti, hitbox (A2) | B28 | DA VERIFICARE | Simulator iPhone 12, 2v2 con 6/1/2/5 e 3/4/1/6 scope: niente sovrapposizioni, "+N" lontano da ACCUSA e dal gettone M (b28_scope_prova.png). Resta l'area di tocco sul telefono |
| 41 | Matta sempre riconoscibile (A3) | B26 | RISOLTO | Bordo viola + "MATTA", provato in Play e con un test |
| 42 | Niente "Cirulla" nei testi visibili (X1) | X1, B31 | RISOLTO | Nell'app resta solo nel nome di un menu dell'Editor. Store e TitleData sono tuoi |
| 43 | Revisione testi (X2) | B6, B29, giro 07/10 sera | DA VERIFICARE | Chi si scollega o lascia il tavolo resta col suo nome su banner, risultati, sorteggio e avvisi ("Bot N" solo per i bot veri). "Capp." e "RIMASTE" restano abbreviati (tua scelta). Due telefoni |

## Priorità 2: tutorial

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 44 | Regola: 51 esatti → 0, si vince sopra 51 (TU1) | B30 | RISOLTO | Test MatchScore e test del tutorial; pillola OLTRE 51 vista nel Simulator |
| 45 | Tutorial scriptato, non casuale (TU2) | B33 | RISOLTO | Provato per intero nel Simulator |
| 46 | Deve insegnare: tavolo, mano, mazzo, avversario, dealer, dealer iniziale, turno, timer, prese, Accuso, Scope, Matta, carte rimaste, smazzate, punteggio, 51→0, >51 | B33, giro 07/10 sera | RISOLTO | 3 passi in più (17): avversario e mazziere (anche il primo sorteggiato), il mazzo con le carte rimaste (si apre il medaglione), la matta; il timer di 30 s nel passo della mano. Provato nel Simulator |
| 47 | Guidata: evidenzia, blocca le mosse sbagliate, spiega | B33 | RISOLTO | Provato nel Simulator |
| 48 | 2–3 turni senza assistenza | B33 | RISOLTO | 2 turni liberi; il test prova entrambe le scelte |
| 49 | Vivere davvero 51 → 0 | B33 | RISOLTO | Il bot chiude a 51 e torna a 0 nei risultati veri |
| 50 | Vincere superando 51 | B33 | RISOLTO | |
| 51 | Ricompensa mostrata prima di "Tutorial sì/no" | B32 | RISOLTO | Pillola +200 nel Benvenuto (Simulator) |
| 52 | Saltato: accessibile da "Aiuto" | — | RISOLTO | Impostazioni → Regole e tutorial → "Rifai il tutorial" (tua scelta 07/10: niente schermata Aiuto) |
| 53 | Ricompensa una sola volta, decisa dal server (TU3) | B32 | DA VERIFICARE | test.js ok; serve 51.carica.js caricato |
| 54 | Durata 5–8 minuti | B33 | DA VERIFICARE | Con i 3 passi nuovi circa 5 minuti: da cronometrare sul telefono |

## Priorità 2: audio

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 55 | Non cancellare il vecchio audio | B34 | RISOLTO | File v01–v03 ancora in Assets/Audio |
| 56 | Integrare v07 un po' alla volta e controllare la sincronia (Card Play, Capture, Deal, Scopa, Accuso, Your Turn, Victory, Defeat) | B34 | DA VERIFICARE | Ascolto sul telefono |
| 57 | Shuffle | — | RIMANDATO | Manca l'animazione del mescolare (scelta accettata) |
| 58 | Meno PitchJitter su CardPlay/CardCapture | B34 | RISOLTO | 0,015 e 0,01 coi 5 e 4 take veri (valori del CSV) |
| 59 | Più varianti per Scopa | B34 | RISOLTO | 3 take, prima variante non più esclusa |
| 60 | Abbassamento della musica (non del Master) | B35 | DA VERIFICARE | Test automatico; ascolto sul telefono |
| 61 | Dissolvenza Home ↔ partita | B35 | DA VERIFICARE | Ascolto sul telefono |

## Priorità 2: opzioni

| # | Voce | Blocco | Stato | Cosa manca / nota |
|---|---|---|---|---|
| 62 | "Suggerimento mosse" funziona davvero | B26 | RISOLTO | Bordo azzurro sulle carte che prendono, provato in Play |
| 63 | Vibrazione: interruttore, eventi, intensità | B36 | DA VERIFICARE | Telefono |
| 63b | Extra: avvisi (toast, connessione) e carte della Collezione non vibrano come gli altri pulsanti | giro 07/10 sera | DA VERIFICARE | Vibrazione leggera anche su avvisi, schermata di servizio e carte della Collezione (UIV2MotionInstaller.AddHaptics). Telefono |
| 64 | Grafica ridotta che migliora davvero le prestazioni | B37 | APERTO | Serve il Profiler su una Development build sul telefono |

## Stress test obbligatori (tutti B38, sul telefono)

| # | Test | Stato | Nota |
|---|---|---|---|
| 65 | Spam tap carte | DA VERIFICARE | |
| 66 | Tap carta prima del turno | DA VERIFICARE | |
| 67 | Spam premi | DA VERIFICARE | |
| 68 | Spam posta | DA VERIFICARE | Serve 51.carica.js |
| 69 | Wi-Fi → rete mobile | DA VERIFICARE | |
| 70 | Rete mobile → Wi-Fi | DA VERIFICARE | |
| 71 | Perdita di rete | DA VERIFICARE | |
| 72 | Background/ritorno | DA VERIFICARE | |
| 73 | Chiusura forzata e rientro | DA VERIFICARE | |
| 74 | Rientro nel proprio turno | DA VERIFICARE | |
| 75 | Rientro nel turno avversario | DA VERIFICARE | |
| 76 | Stesso account su due telefoni | DA VERIFICARE | Serve 51.carica.js |
| 77 | Account A → Ospite → Account B | DA VERIFICARE | |
| 78 | Password sbagliata | DA VERIFICARE | |
| 79 | Nickname duplicato | DA VERIFICARE | |
| 80 | Cambio avatar → lobby subito | DA VERIFICARE | |
| 81 | Cambio avatar → partita subito | DA VERIFICARE | |
| 82 | Statistiche prima/dopo partita | DA VERIFICARE | |
| 83 | Prima apertura del profilo dopo il login | DA VERIFICARE | |
| 84 | Host lascia la lobby | DA VERIFICARE | |
| 85 | Non-host invita | DA VERIFICARE | |
| 86 | 2v2 con ordine d'ingresso diverso | DA VERIFICARE | Le squadre seguono l'ordine d'ingresso (voluto) |
| 87 | Premio con molti tocchi | DA VERIFICARE | |
| 88 | Posta con molti tocchi | DA VERIFICARE | Serve 51.carica.js |
| 89 | Tastiera in tutti i campi principali | DA VERIFICARE | |

## Feature (il backlog chiede di non farle ora)

| # | Voce | Stato |
|---|---|---|
| 90 | Cronologia partite / giocatori recenti | RIMANDATO |
| 91 | Schede AMICI / RICHIESTE / RECENTI | RIMANDATO |
| 92 | +AMICO a fine partita | RIMANDATO |
| 93 | Settimanale con 7 premi e 8–9 giorni di tolleranza | RIMANDATO |
| 94 | Avatar da rifare | RIMANDATO |
| 95 | Modalità Scopa | RIMANDATO |
| 96 | Modalità Briscola | RIMANDATO |

## Trovati durante la diagnosi (fuori dal backlog)

| # | Voce | Stato | Nota |
|---|---|---|---|
| 97 | Pulsante Classifica della Home disattivato all'avvio e mai riattivato | RISOLTO | Tolto l'avanzo che lo spegneva; provato in Play: il tocco apre la Classifica |
| 98 | Partita finita mentre eri fuori → "abbandono" falso al riavvio | RISOLTO | Lo stato ricevuto a smazzata finita mostra i risultati e chiude la partita (B7) |
| 99 | Stato completo accettato da chiunque (client modificato) | RISOLTO | Solo dal master (B8) |
| 100 | Lista bloccati dell'account precedente scritta sul nuovo | DA VERIFICARE | Le cache si azzerano ad Accedi (B10): provare A → B con un bloccato |
| 105 | Tutorial: la riga dei pallini di avanzamento sta sopra il mazzo in alto a sinistra | RISOLTO | Pallini spostati a destra accanto a Salta (builder + GameScene); provato nel Simulator su iPhone 12 e SE: mazzo e medaglia RIMASTE liberi |

## Giro Android 08/10 (primo APK sul tablet)

Test 11 / tasto Avanti: RISOLTO su Android reale. Test 33 / orologio indietro: RIMANDATO per scelta. Le voci qui sotto sono state corrette senza nuova versione. Servono un nuovo APK e il nuovo deploy di `51.carica.js` (#101).

| # | Voce | Stato | Cosa è stato fatto / cosa manca |
|---|---|---|---|
| 106 | Turno proprio ancora più evidente | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): superato da #123 |
| 107 | Tutorial: il dito manca i controlli (Accuso) | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): il dito del tutorial va sui controlli giusti |
| 108 | Fine tutorial: niente partita lunga forzata | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): GIOCA ORA e VAI ALLA HOME funzionano (vedi #125, #135) |
| 109 | Premio tutorial +200 non arrivato (account registrato) | DA VERIFICARE | Superato da #132: la causa vera e' l'accredito rifiutato da PlayFab, non il client |
| 110 | Tastiera Android copre i campi | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): superato da #122 |
| 111 | Animazione premi troppo breve | DA VERIFICARE | Esplosione 1,1 → 1,5 s, salita 1,8 → 2,6 s, Posta 1,6 → 2,4 s, scrigno: rivelazione 0,7 → 1,4 s con il giro delle carte più lento |
| 112 | Monete e gemme della TopBar non aggiornate subito | DA VERIFICARE | Fallito sul tablet (saldo fermo): causa e correzione in #132 |
| 113 | Audio v07 che sembra "beep" sul tablet | DA VERIFICARE | Controllo fatto: la SoundLibrary punta tutti i file v07 e non c'è nessun segnaposto o ripiego. Pack vecchio NON toccato. Candidati da ascoltare: UiError sui tocchi sbagliati nel tutorial, UiClick sulla selezione carte, altoparlante del tablet |
| 114 | Avatar vecchio nella lista amici dell'altro | DA VERIFICARE | L'avatar si pubblica sul profilo PlayFab (AvatarUrl "avatar:id") a ogni cambio. La lista amici la legge dal server a ogni caricamento. Servono due telefoni |
| 115 | Amicizie: richiesta e accettazione reciproche | DA VERIFICARE | Nuovi handler server: amici, richiestaAmico, accettaAmico, rimuoviAmico. Le richieste incrociate diventano subito amicizia; i vecchi legami a senso unico vengono adottati senza doppioni; le richieste da bloccati e verso ospiti sono rifiutate (test.js). UI: scheda Richieste con Accetta/Rifiuta. Il tocco sulla riga apre la scheda amico con Invita (se è online), Rimuovi (con secondo tocco), Annulla richiesta e Blocca. Vista nel Simulator su iPhone 12. Servono due telefoni. Nessun badge in Home per le richieste nuove |
| 116 | Stesso account attivo su due client | RISOLTO | Superato da #129, verificato |
| 117 | Segnalazione di un ospite da un registrato | DA VERIFICARE | Già vera lato server: la segnalazione va sui dati interni del PlayFabId della sessione ospite, e il dispositivo la rispecchia. Ora c'è un test (test.js); manca la prova tra due telefoni |

## Secondo giro Android 08/10

Test fermati per sistemare questi punti. Nessuna build iOS, nessun cambio di versione, nessun commit. Servono un nuovo APK e il nuovo deploy di `51.carica.js` (#101, rigenerato l'08/10 alle 13:05). Le voci #108, #110 e #116 qui sopra sono superate da #125, #122 e #129.

| # | Voce | Stato | Cosa è stato fatto / cosa manca |
|---|---|---|---|
| 118 | Tutorial +200: notifica sì, monete no | DA VERIFICARE | Non riverificabile sul tablet (premio gia' consumato dal vecchio script): causa in #132, il server ora lo recupera da solo |
| 119 | Premi: riscatto ripetibile (tocchi multipli, Home, di nuovo riscattabile) | DA VERIFICARE | FALLITO sul tablet: RISCATTA -> NON DISPONIBILE -> di nuovo RISCATTA all'infinito, niente monete. Causa e correzione in #132 |
| 120 | Posta: dopo il riscatto il pulsante mostrava "10 gemme" | DA VERIFICARE | Riscatto (anche "Riscatta tutto") confermato dal server prima di cambiare la UI; "già riscattato" con gli id viene segnato come riscattato; il messaggio si ridisegna sempre dopo la risposta |
| 121 | Saldo monete/gemme non aggiornato subito | DA VERIFICARE | Fallito sul tablet: causa e correzione in #132 |
| 122 | Tastiera Android copre login e registrazione | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): Login (Test 4) e Registrazione (Test 5), la tastiera non copre piu' i campi. Rifinitura in #137 |
| 123 | TOCCA A TE non abbastanza evidente | RISOLTO | Verificato sul tablet Android (08/10, secondo APK) (Test 6): TOCCA A TE approvato. Turno degli altri: #134 |
| 124 | Dito del tutorial impreciso (Accuso) | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): dito del tutorial preciso |
| 125 | Fine tutorial: "ENTRA IN 51" → "VAI ALLA HOME" | RISOLTO | Verificato sul tablet Android (08/10, secondo APK) (Test 8): GIOCA ORA e VAI ALLA HOME funzionano. Schermata duplicata: #135 |
| 126 | Amici: la UI si aggiorna solo uscendo e rientrando | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): aggiornamento delle amicizie rapido |
| 127 | Pallino rosso su Amici per le richieste non viste | RISOLTO | Verificato sul tablet Android (08/10, secondo APK) (Test 10): pallino rosso su Amici in Home. Scheda Richieste: #136 |
| 128 | Profilo dell'amico senza il suo banner | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): banner vero dell'amico |
| 129 | Stesso account su due client: vince la sessione attiva | RISOLTO | Verificato sul tablet Android (08/10, secondo APK): politica approvata (chi e' dentro resta, il secondo login e' fermato, dopo l'uscita del primo il secondo entra). Da non toccare senza una causa precisa |
| 130 | Audio v07 non approvato (troppo "bip" sul tablet) | RIMANDATO | Nessuna modifica adesso, pack vecchio intatto |
| 131 | Lista audio futura: SFX dedicato per la ruota/selezione del mazziere | RIMANDATO | Da creare insieme alla revisione audio |

## Terzo giro Android 08/10 (secondo APK)

Superati sul tablet e da non toccare: tastiera di Login e Registrazione, dito del tutorial, aggiornamento delle amicizie, banner dell'amico, sessione singola, TOCCA A TE, pallino Amici in Home, GIOCA ORA / VAI ALLA HOME. Audio rimandato (pack v07 e vecchio intatti). Nessuna versione nuova, nessuna build iOS, nessun commit. Servono il deploy del nuovo `51.carica.js` (#101) e un nuovo APK.

| # | Voce | Stato | Cosa è stato fatto / cosa manca |
|---|---|---|---|
| 132 | Economia: premio giornaliero riscattabile all'infinito, saldo fermo, +200 del tutorial mai arrivato | RISOLTO | Verificato su Android con la revisione 19 (account Test51QA): giorno 1 +50, giorno 2 +100, Posta +100/+5, tutorial +200, forziere +270/+6, saldi subito giusti, riscattati anche dopo il riavvio. Causa confermata: valuta `C0` al posto di `CO`. Storia della correzione: | **Causa (dal codice, da confermare con `qa.js diagnosi` o col logcat):** "NON DISPONIBILE" seguito da RISCATTA è la risposta del server `ok:false, riscattato:false`, che il server deployato dà solo quando PlayFab rifiuta con certezza `AddUserVirtualCurrency` (o per un ospite): lo script rimetteva il premio e il pulsante tornava. Il vecchio script (07/10) invece segnava il tutorial e poi si fermava su quel rifiuto (`CloudScriptAPIRequestError`): +200 consumato senza monete. Lo stesso rifiuto blocca le monete di fine partita. Candidato principale: le valute `CO`/`GE` mancano nell'Economy legacy del titolo (o l'Economy legacy è spenta). Nel logcat la riga `[RewardsService] riscattaPremio: AddUserVirtualCurrency CO: ...` dice l'errore esatto. **Correzione server:** registro "Consegne": premio consumato e consegna scritti insieme, poi l'accredito; quello che PlayFab non conferma resta nel registro e si ritenta a ogni chiamata dei premi e all'ingresso in Home (mai perso, mai doppio: un accredito senza risposta si riconosce dal saldo). Il premio non torna più riscattabile. Tutorial segnato dal vecchio script con saldo sotto 200: pagato ora. **Client:** "Premio preso: arriva appena il server conferma", avviso "premio in sospeso arrivato" col saldo vero, "SOLO CON UN ACCOUNT" per gli ospiti, "Monete in arrivo" a fine partita; il recupero all'ingresso in Home (prima girava solo aprendo la Posta). test.js copre tutti i casi |
| 133 | Ambiente QA ripetibile per le ricompense | DA VERIFICARE | `Server/QA/` (LEGGIMI.md + qa.js): prove automatiche, account di prova con stati pronti (premio del giorno N, già riscattato, posta con premio e forziere, tutorial da riscattare), guasti finti (rifiuto, risposta persa, timeout, risposta lenta) e un giro di 12 prove dal telefono. Gira solo sul PC con la chiave segreta in `Server/QA/qa.json` (fuori da git); nell'app non c'è niente. I guasti funzionano solo su un titolo PlayFab di sviluppo; sul titolo dell'app lo strumento legge e prepara solo gli account elencati. Manca: creare la chiave (e, se vuoi, il titolo di sviluppo + l'app Photon di sviluppo per un APK QA) |
| 134 | Turno di un altro giocatore poco chiaro | DA VERIFICARE | Oltre all'anello blu già presente sul banner di chi gioca: la propria mano si spegne un poco (0,25 s) mentre gioca un altro e torna piena al proprio turno; un tocco sulla mano fuori turno fa due onde più ampie sull'anello di chi gioca (al massimo una ogni 0,8 s), senza suoni né avvisi. Tocchi al proprio turno col tavolo occupato restano ignorati in silenzio (niente coda). TOCCA A TE non toccato. Vale anche dopo un rientro (lo stato ridisegna la mano). Provato in Play |
| 135 | Fine tutorial: si vedeva anche la vecchia schermata dei risultati | DA VERIFICARE | Nel tutorial la schermata dei risultati di fine partita non si accende più: dopo l'ultima presa (1,2 s) compare solo GIOCA ORA / VAI ALLA HOME; GIOCA ORA riparte come prima. Provato in Play: tutorial senza pannello e GIOCA ORA funzionante, partita vera col pannello come sempre. Manca il giro completo sul telefono |
| 136 | Pallino sulla scheda RICHIESTE | DA VERIFICARE | Il numero delle richieste sulla scheda è rosso solo con richieste non ancora viste (stesso stato del pallino in Home, per account e salvato: niente pallini al riavvio); aprendo la scheda torna neutro, le richieste restano; una nuova lo riaccende. Un elenco vuoto per errore non cancella più quelle già viste. Servono due telefoni |
| 137 | Tastiera: si solleva tutta la schermata | DA VERIFICARE | Sale solo il modulo (il foglio di Accesso e Registrazione, il corpo di Recupero), non più logo e decorazioni. Movimento morbido (~0,2 s), stacco di 22 unità sopra la tastiera, Avanti fra due campi non fa scendere e risalire, alla chiusura torna giù solo senza dito sullo schermo. Su schermi piccoli il campo attivo resta visibile anche se la parte alta del modulo esce dallo schermo. Solo sul telefono |
| 138 | Economia 09/10: causa probabile `C0` (zero) al posto di `CO` nel titolo; accrediti doppi possibili con chiamate insieme | RISOLTO | Pubblicata come revisione 19 (identica a `51.carica.js` di allora, controllato) e provata su Android (vedi #132); `qa.js stato`: saldo 720 CO / 11 GE = somma dei premi, nessuna consegna rimasta. Consegna rifatta: lucchetto per giocatore (`CreateSharedGroup` per minuto) su consumo e accredito, una chiave `Cons_<id>` per consegna, al massimo una volta; esito sconosciuto = `incerto`, mai ritentato da solo (riconciliazione in `Server/QA/LEGGIMI.md` §5); il tutorial dei vecchi script non si paga più dal saldo. Prove: `test.js` + `concorrenza.js` (thread veri, guasti a caso). Piano per i premi vecchi: `RECUPERO_CONSEGNE.md`. Prima del deploy: tetto di chiamate API per esecuzione (Title settings → Limits), fine partita usa 13-15 chiamate |

## Audit economia prima del Negozio

Rivisti nel codice (51.js, RewardsService, MatchResultsV2, Posta, Premi, forzieri) e con `test.js` + `concorrenza.js`. Revisione 19 in
uso = `51.carica.js` prima dell'audit (confrontata). Garanzia di tutto il sistema: **al massimo una volta** per ogni premio; un esito
sconosciuto resta scritto (`incerto`), mai pagato da solo. "Esattamente una volta" con Economy legacy non si può garantire (nessun id di
transazione su `AddUserVirtualCurrency`): serve Economy v2.

| # | Voce | Stato | Cosa è stato fatto / cosa manca |
|---|---|---|---|
| 139 | Una consegna rifiutata per sempre bloccava tutte le altre | DA VERIFICARE | **Bug dimostrato** (prova: consegna di gemme rifiutata + consegna di monete in attesa, 20 avvii: le monete non arrivavano mai). Si ritenta una consegna vecchia per chiamata e il server riprendeva sempre la stessa. Ora prima la meno rifiutata, e dopo 10 rifiuti certi la consegna diventa `fermo` (di sicuro non arrivata, non si ritenta più da sola; si rimette in attesa con `qa.js riconcilia`). `test.js` copre il caso. Serve il nuovo upload (#101) |
| 140 | Procedura "premio non arrivato" | DA VERIFICARE | `Server/QA/LEGGIMI.md` §5: `qa.js stato <nome>` spiega ogni consegna (cosa era, quando, quanto, cosa fare) e mostra anche il segno vecchio del tutorial, che prima restava nascosto finché il telefono non richiamava il premio. `qa.js riconcilia <nome> <chiave> CO=si\|no` chiude un'incerta dopo il controllo in PlayStream o rimette in attesa una ferma; registra tutto in `Riconciliazioni` (dati interni) e rifiuta i doppi. Provato con un PlayFab finto, mai su account veri |
| 141 | Fine partita: rete persa = monete, XP e statistiche perse senza traccia | DA VERIFICARE (in locale, non caricato) | Biglietto del server, risultato in sospeso sul telefono, pagato una volta sola. **Casi limite corretti l'08/10** con #148-#150 (`PIANO_RISULTATI_AUTOREVOLI.md` §1): nessuna scadenza a tempo, mai lo stesso biglietto per due partite (anche dopo un riavvio), uno nuovo al minuto, senza biglietto per la rete = metà premio, app vecchie ancora premiate. `test.js` + `concorrenza.js` passano. Manca la prova sul telefono (piano §3) |
| 142 | Fine partita: vincitore, scope e accusi li dice il telefono | DA VERIFICARE (Fase A in locale); Fase B APERTA | **Fase A fatta (08/10, D3-D4)**: ogni persona dichiara punti, smazzate, vincitore e posto; il server li confronta nel record della stanza. Paga per intero solo `confermata` (tutti d'accordo) o `abbandono` (gli altri usciti per sempre, tetti); `inVerifica` → `incompleta` (solo partecipazione, ritentata 7 giorni, poi diventa confermata se arriva il resto); `contestata` = solo partecipazione, decisione a mano con `qa.js esito`. Registro `Incongruenze` + PlayStream con categoria bug/rete/sospetta, **nessuna sanzione automatica**. `XPConcordato` (prima `XPVerificato`) solo per partite confermate fra sole persone (D6): è un **accordo fra telefoni, non una validazione** e non vale per classifiche con premi. Limite noto: due account d'accordo. Partecipazione con partite fittizie verificata: rende meno della sconfitta concordata, dentro i tetti (`test.js`) **Fase B**: solo analisi (piano §5), prima di classifiche con premi |
| 143 | Partita rapida riempita di bot dopo 30 s: monete piene (40/20), non dimezzate come l'allenamento | DA VERIFICARE (in locale, non caricato) | **Rivista l'08/10 (D5), nessuna soglia di tempo**: conta come persona solo chi ha chiesto il biglietto di quella partita mentre sedeva (`b<posto>` nel record, scritto dal server); le uscite definitive arrivano dal nuovo webhook PathLeave `RoomLeft<segreto>` (`u<posto>`), le disconnessioni con rientro (`IsInactive`) non contano. Entrata finta prima dell'inizio = allenamento (prova in `test.js`). Serve PathLeave nel pannello Photon (#103) |
| 144 | Tetto di chiamate API per esecuzione del CloudScript | RISOLTO | Controllato da te nel Game Manager (Title settings → Limits, titolo 10A53D): **25 chiamate API e 10 s per esecuzione**. Caso peggiore misurato in `test.js`, che fallisce se cresce: Home 12, premio giornaliero 13, Posta 14, tutorial 14, inizio partita 8, fine partita 18 (anche contestata, abbandono e app vecchia). Tutti sotto 25 |
| 145 | Pagina Premi: la tabella dei 7 giorni è una copia di quella del server | RIMANDATO (limite accettato) | Se si cambia `Economia` in Title Data il telefono mostra ancora i valori vecchi (il server paga quelli nuovi): va cambiata anche `UI51RewardsView.Week` |
| 146 | Forziere VIOLA nella Posta, VERDE all'apertura | RIMANDATO (blocco grafico) | **Solo grafica, premio giusto.** +270 CO e +6 GE stanno nel verde (150-300 monete, 3-8 gemme; il viola è 400-800 e 10-25) e il messaggio di `qa.js prepara` è un forziere verde. La Posta usa sempre l'icona `chest_purple` (UI51SocialBuilder) e ignora `colore`; l'apertura (UI51ChestView) usa il colore del server, giusto. Correzione: icona per colore nella Posta |
| 147 | Asset del baule aperto | RIMANDATO (blocco grafico) | Manca; nessun asset nuovo in questo giro |
| 148 | #141 C1: premio perso dopo 24 h offline o con più di 5 risultati in sospeso | DA VERIFICARE (in locale) | **Fatto (D1)**: nessuna scadenza a tempo (prova: pagata dopo 30 giorni offline). Limite 10 biglietti aperti sul server: il più vecchio diventa `scaduta`, il suo risultato tardivo `daRiconciliare` (registro, mai pagato da solo né perso in silenzio, `qa.js esito`). Limite 10 sul telefono: esce prima la incompleta più vecchia, poi la più vecchia con biglietto, con avviso; le senza biglietto non si tolgono mai. Esiti finali: 30 + archivio di 150 |
| 149 | #141 C2-C3: biglietto mai arrivato per la rete; allenamento dopo un riavvio riusa il biglietto non pagato | DA VERIFICARE (in locale) | **Fatto**: C2 il risultato si salva con l'id del telefono e si paga metà, una volta per id, uno al minuto, al massimo 20 al giorno. C3 il telefono ricorda gli ultimi 10 biglietti finiti per account (anche dopo un riavvio) e li manda in `dopo`; il server non li restituisce mai e in allenamento dà un biglietto per partita |
| 150 | Passaggio dalla revisione 19: le app vecchie non mandano il biglietto | DA VERIFICARE (in locale) | **Fatto (D2), senza data di scadenza**: ramo acceso finché `Economia.partita.senzaBiglietto.attiva` non diventa `false` (lo spegni tu). Premio pieno solo col record Photon (un altro account seduto; abbandono con la sua uscita e i tetti), uno al minuto, 20 al giorno, evento PlayStream `partita_senza_biglietto` (`appVecchia`/`rete`) da controllare. Ordine: piano §3. Versione non aumentata |
| 151 | Lo stato inviato agli altri telefoni contiene mazzo e mani di tutti | **RISCHIO ACCETTATO** (D12, 08/10): noto e accettato per una prima versione senza competizioni con premi di valore; il multiplayer non va dichiarato protetto dai trucchi; controllo con PlayStream e `Incongruenze`, si rivaluta se emergono abusi. Architettura futura = B (servizio .NET), prototipo e migrazione NON approvati, nessun acquisto (D13) | Verifica in `AUDIT_151_CARTE_NASCOSTE.md`: stato completo a tutti (inizio, richiesta senza controlli, rientro, cambio di Master), ogni telefono distribuisce, controlla e fa da arbitro di riserva; mazzo da `System.Random` a seme d'orologio. **D8 (08/10):** niente filtro dello stato per ora, rientro e cambio di Master restano. **D10:** confronto CloudScript / servizio dedicato / plugin Photon in `PIANO_RISULTATI_AUTOREVOLI.md` §6: raccomandato un piccolo servizio di partita autorevole in .NET (regole di Core riusate senza copia: provato con 4.000 smazzate; circa 10-30 € al mese). Prototipo P0+P1 isolato proposto, **non avviato**. La prova `HiddenCards_KnownLeak151` fotografa la falla |
| 152 | D11: tetto comune per i premi non verificabili | DA VERIFICARE (in locale, non caricato) | 100 monete al giorno in tutto per allenamento, app vecchie senza biglietto, senza biglietto e partecipazione (`Economia.partita.tettoNonVerificabili`); confermate intatte; monete per tipo in `PartiteOggi.nonVerificabili`; contatori del giorno scritti con la consegna (ora Read Only). `test.js` + `concorrenza.js` con prova di mutazione. Sul telefono: 5 vittorie in allenamento = 100, poi "tetto raggiunto" |
| 153 | DLL Roslyn (`Assets/Plugins/Roslyn`, 13 MB, strumenti dell'Editor) importate per tutte le piattaforme: finiscono nell'APK/IPA | RISOLTO | Solo Editor (PluginImporter, i `.meta` restano); Unity-MCP compila ancora con Roslyn; script del player Android/iOS compilati senza Roslyn |
| 154 | Build TestFlight 2 e build nuova allo stesso tavolo | DA VERIFICARE | Protocollo multiplayer esplicito `p3` (`PhotonAuthConnector.ProtocolVersion`, TestFlight 2 = `1.0.0`), indipendente dalla versione 1.0.0; impostato prima della prima scena, visto dal vivo come `p3_2.52` sul Master; test. Build iOS 3, Android 265. Manca: TestFlight 2 e APK nuovo mai allo stesso tavolo (due telefoni) |

Verificati senza problemi: premio giornaliero (serie 1-7 e ritorno a 1, giorno del server in ora italiana, una volta al giorno sotto
lucchetto), Posta e "Raccogli tutto" (messaggio segnato e consegna scritti insieme), Posta per tutti (una volta per messaggio, niente agli
ospiti), tutorial (una volta per account, segno vecchio = incerto), forzieri (importi casuali decisi dal server prima dell'accredito e
salvati nella consegna), XP e statistiche (solo dal server, stessa formula del telefono, una chiamata per partita), pareggio (non esiste:
a parità in testa si continua), 1v1/2v2/1v3 (ognuno chiede per sé), uscita a metà (sconfitta senza XP né monete), ospiti (niente),
chiamate insieme e doppi tocchi (lucchetto, `concorrenza.js`).

Da provare su Android con il nuovo APK e il nuovo upload (insieme): Q1-Q12 di `Server/QA/LEGGIMI.md` §4 con Test51QA (soprattutto
Q2 tocchi rapidi e Q5 Posta). Partite (#141-#143, #148-#150), con `qa.js stato` prima e dopo:
- allenamento finito: +20 (vittoria) o +10, `partita chiusa ... allenamento` con XP dimezzati; RIVINCITA e riavvio: biglietto nuovo;
- partita rapida rimasta coi soli bot: come l'allenamento;
- partita con un'altra persona, entrambe a fine partita: riga "Risultato in verifica" finché l'altra non dichiara, poi +40/+20 (`confermata`);
- l'altra persona esce a metà partita (ESCI o app chiusa oltre i 60 s): `abbandono`, vittoria nei tetti degli abbandoni;
- rete tolta all'altra persona a fine partita per più di 10 minuti: `incompleta`, +10 di partecipazione; riaccesa e riaperta l'app entro 7 giorni: la differenza (+30 in vittoria, +10 in sconfitta) "risultato confermato";
- risultato contestato (si ottiene solo con un client modificato o un bug): +10, riga in `incongruenze`, poi `qa.js esito`;
- rete staccata subito prima dell'ultima presa e riaccesa in Home: avviso "+N monete: partita precedente", una volta sola;
- app chiusa a fine partita prima della risposta e riaperta: stesso risultato, mai due volte;
- rientro dopo una caduta a metà partita: un solo biglietto (`partita aperta` uguale);
- uscita volontaria: una persa, nessuna partita aperta che resta;
- app vecchia (TestFlight attuale) contro app nuova: entrambe premiate, evento `partita_senza_biglietto` in PlayStream.

## Fuori dal repository (tocca a te)

| # | Voce | Stato |
|---|---|---|
| 101 | Upload + Deploy di `Server/CloudScript/51.carica.js` su PlayFab (revisione 19 = consegna col lucchetto, in uso e provata). **Pronto in locale (08/10)** con #141-#143 e #148-#150: si carica quando decidi tu, seguendo `PIANO_RISULTATI_AUTOREVOLI.md` §3 (prima lo script, poi PathLeave nel pannello Photon, poi APK e TestFlight; le app vecchie restano premiate). Rigenerare con `carica.js` subito prima | APERTO |
| 102 | Game Manager: aggregazione "Last" per TotalGames, Wins, XP, Level, TotalScope, **XPConcordato** (nuova: partite concordate fra sole persone, non per classifiche con premi) | DA VERIFICARE |
| 103 | Webhook Photon: PathBeforeJoin vuoto; **PathLeave = `RoomLeft<segreto>`** (nome stampato da `carica.js`) insieme al deploy di #101: senza, le uscite non si vedono e le partite con un perdente sparito restano `incompleta`; controllare che l'accesso anonimo sia spento (UserId = PlayFabId) | DA VERIFICARE |
| 104 | "Cirulla" nello store e nella TitleData di PlayFab | DA VERIFICARE |

## Conteggio

RISOLTO 37 (#153; 13 verificati sul tablet l'08/10; #132 e #138 su Android con la revisione 19; #144 limiti del titolo) · DA VERIFICARE 103 (#154; #133-#137; #139-#143, #148-#150 e #152 D11 dall'audit economia e dai risultati verificati, in locale e non caricati) · APERTO 3 (#21 icone trofei; #64 grafica ridotta; #101 upload pronto, quando decidi; ordine in `CHECKLIST_RILASCIO.md`) · RISCHIO ACCETTATO 1 (#151 carte nascoste, D12) · RIMANDATO 14 (7 feature e scelte accettate; #130 e #131 audio; #145 limite accettato; #146 e #147 blocco grafico).
