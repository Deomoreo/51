# 51 — Backlog dello sprint

Lista viva di tutto quello che resta da fare. Ogni nuova idea si aggiunge qui con un codice.
`UI_INTEGRATION_ROADMAP.md` resta il resoconto dettagliato delle consegne.

Stato: ☐ da fare · ◐ in corso · ☑ fatto · ⏸ rimandato · ❓ da decidere

Regola: bug e rifiniture di cose esistenti si fanno in ordine. Schermate e sistemi nuovi partono solo dopo il tuo via, uno alla volta.

---

## ▶ PUNTO DI RIPRESA — 01/10, versione 2.33

**2.33:** le correzioni chieste il 01/10. Build pulito (0 errori); test EditMode 345 totali: 338 ok, 0 falliti, 7 saltati (Explicit). Non committata, come la 2.32: serve il tuo via.
- Pannelli: toccando dentro un pannello (scelta dell'icona, modalità, mazzo, stanza privata...) non si chiude più; si chiude toccando fuori o sulla X. Il foglio non prendeva il tocco, che passava al velo dietro. Provati dal vivo tutti i pannelli del menu che si possono aprire (Modalità, Mazzo, Editor del profilo, Impostazioni, Termini, Elimina account, Crea/Entra stanza) con 25 tocchi dentro ciascuno: nessuno arriva al velo. Al tavolo opzioni e abbandono erano già a posto. Restano nella scena 3 pannelli vecchi che nessun pulsante apre più (`SettingsModal`, `ModePanelContainer`, `DeckPanelRoot`).
- Barra di caricamento: sale solo in avanti e a velocità costante, non si deforma più (estremità sempre tonde, il riflesso resta dentro) e la schermata si chiude solo a barra piena.
- Carta che pulsava "grande piccola": succedeva quando l'ultimo tocco (per esempio "Continua" o "AL TAVOLO") restava sul bordo basso di una carta della mano. L'hover la solleva, lei sfuggiva al punto toccato, riscendeva e ripartiva, senza fine. Ora resta sollevata finché il puntatore è sul suo posto; sui telefoni l'hover c'è solo con il dito sullo schermo, quindi a inizio smazzata nessuna carta resta su. Provato dal vivo: prima oscillava di continuo, ora sale una volta e sta ferma, e scende quando il puntatore va via. Test: `HoverKeepsTheCardWhileThePointerIsOnItsRestPlace`.
- Mazzo: toccandolo più volte il medaglione resta aperto (sparisce 2,4 s dopo l'ultimo tocco) senza ripartire con sollevamento e conteggio.
- Carta giocata: le carte in tavola ora si spostano mentre la carta vola, così atterra nel suo posto libero invece che a metà sopra quella centrale; dopo una presa le carte rimaste chiudono i buchi scivolando. Prima il tavolo si sistemava di scatto dopo l'atterraggio e, con l'ultima carta della mano, solo alla distribuzione seguente. Provato con 34 giocate automatiche: nessuna carta atterrata sopra un'altra, nessuno scatto del tavolo.
- Emblema del sole: il tuo `Sun_fix.png` al posto di `sun_emblem`, al centro della ruota del Sorteggio e sul tavolo. Il pezzo di nastro in basso non c'è più.
- ASSET MANCANTI DA CREARE: nessuno.
- Prossimo passo: i mockup che mancano, a partire dalla Fase 6 (tavolo a 4 giocatori). Il tuo via è arrivato il 01/10 ("non ci fermiamo").

**2.32:** i quattro punti a cui hai detto sì il 30/09. Build pulito (0 errori); test EditMode 344 totali: 337 ok, 0 falliti, 7 saltati (Explicit). Non committata: serve il tuo via.
- Soglia di trascinamento: in tutte le scene ora vale circa 1,6 mm di dito invece di 10 pixel fissi (29 pixel su iPhone 12, 20 su iPhone SE, 10 nell'Editor). Un tocco che rotola appena non diventa più un trascinamento, quindi i pulsanti dentro le liste che scorrono partono. Si vede solo su un telefono vero. Test: `DragThresholdIsAboutOnePointSixMillimetres`.
- Photon: le stanze ora sono separate per versione dell'app (2.32 gioca solo con 2.32). Prima la versione restava vuota e telefoni con build diverse finivano insieme, perdendo per esempio gli accusi. Tolte le vecchie righe che provavano a impostarla senza effetto.
- Fase 4 rilanciata: i 4 interruttori delle Impostazioni del menu (Effetti, Musica, Vibrazione, Grafica ridotta) ora prendono il tocco su tutta la loro area. Provato dal vivo; nella scena cambiano solo quelli e il numero di versione.
- Worktree vecchio `.claude/worktrees/focused-heisenberg-90035f` cancellato (le correzioni degli accusi erano già tutte nel commit della Fase 5). Resta una cartella vuota che Windows tiene occupata: si può cancellare a mano.
- Resta a te: prova online con due telefoni con la STESSA versione (accusi e ruota del Sorteggio). Con la 2.32 due versioni diverse non si trovano più: installa la stessa build su entrambi.

**2.31:** Fase 5 (Tavolo 1v1) finita: fatti anche S9 (opzioni e abbandono) e S10 (Sorteggio). Build pulito (0 errori); test EditMode 343 totali: 336 ok, 0 falliti, 7 saltati (Explicit). Committata in locale il 30/09 (commit 53bc0ac, nessun push).
- S9 e S10: dettagli nel blocco della Fase 5 qui sotto.
- ASSET MANCANTI DA CREARE: emblema del sole (`sun_emblem`) senza il pezzo di nastro che si vede in basso, al centro della ruota del Sorteggio. Arrivato il 01/10 (`Sun_fix.png`), messo nella 2.33.

**2.30:** chiusi i 13 punti rimasti aperti dalla Fase 4. Build pulito (0 errori); test EditMode 316 totali: 309 ok, 0 falliti, 7 saltati (Explicit). Committata in locale il 29/09 insieme alla 2.29 (commit 1457601, nessun push).
- Fatto e provato nel Simulator su iPhone 12 e SE:
  - Emoticon: il tocco in griglia toglie un'emoticon già in uso e aggiunge in coda una libera. A slot pieni il tocco su una libera non cambia niente. Testo d'aiuto: "Tocca un'emoticon qui sotto per aggiungerla" oppure "Slot pieni: tocca un'emoticon per toglierla". Non è un aggancio opzionale: vale anche per la schermata classica (`CollectionCosmeticsV2.Toggle`). Test: `EmoticonToggleRemovesOneInUseAndAppendsAFreeOne`.
  - Profilo ospite: il testo ora dice "Mazzi, emoticon e accusi da sbloccare". Righe dei vantaggi alte 47 (erano 55): su iPhone SE CREA UN ACCOUNT si vede intero senza scorrere.
  - Salvataggio dell'aspetto: una sola scrittura per avatar, cornice e banner (`ProfileService.SetCosmetics`), con l'esito a schermo. Durante l'invio SALVA è attenuato e il foglio non si chiude. Se va bene: "Aspetto salvato" e chiusura. Se fallisce: "Non salvato: le tue scelte sono ancora qui. Riprova." e il foglio resta aperto. Dopo 15 secondi senza risposta si può riprovare. Test: `TheLookIsSavedInOneWriteAndTheCacheOnlyAdoptsWhatTheCloudAccepted`.
  - Dialogo Elimina account: se la tastiera copre i pulsanti, la finestra sale quanto basta (`DeleteAccountModalV2.KeyboardLift`, 4 test). Nell'Editor la tastiera non esiste, quindi lì non cambia niente.
- Revisione del codice (3 aree): un solo difetto, corretto. Toccando Elimina o Annulla la tastiera si chiude e la finestra sarebbe scesa sotto il dito, facendo perdere il tocco: ora non scende finché c'è un dito sullo schermo. Si vede solo su un telefono vero.
- Decisioni prese (restano così finché non le cambi):
  - Notifiche: riga nascosta. L'app non manda notifiche.
  - "Cambia password": nascosta finché su PlayFab non c'è il modello di e-mail per il recupero (H5). Intanto si usa "Password dimenticata?" dall'Accesso.
  - Lingua: riga informativa "Italiano", non si tocca. C'è una sola lingua.
  - Riga "prossimo sblocco" nel Profilo: non costruita. L'unico sblocco a livello che esiste (banner Porpora al livello 10) si vede già nell'editor con il lucchetto.
  - Collezione: "Prossimi sblocchi", "Nuova emoticon", monete e gemme restano nascosti finché non esistono negozio, missioni e valute.
  - Sfocatura dietro i pannelli del menu: resta solo il velo scuro. Al tavolo la sfocatura c'è già e la usano i pannelli della Fase 5.
  - Cornice e banner scelti visibili al tavolo (D2): diventa il primo passo della Fase 5, con i banner nuovi.
- Restano a te (non si possono fare dall'Editor):
  - Prova con un accesso vero: Profilo con account, editor, SALVA (salvataggio reale su PlayFab), registrazione dall'app.
  - Prova su un telefono vero: tastiera nel dialogo Elimina account.
- Limite noto del salvataggio: se la risposta arriva dopo i 15 secondi e nel frattempo si è premuto di nuovo SALVA, il messaggio può non corrispondere all'ultima richiesta. L'aspetto salvato resta comunque quello accettato da PlayFab.
- ASSET MANCANTI DA CREARE: nessuno.
- Fase 5 (Tavolo 1v1): via ricevuto il 29/09. Scelte tue: pulsante ACCUSA sempre visibile (come oggi, grafica nuova); barra in alto col solo punteggio (carte rimaste toccando il mazzo); Sorteggio rifatto come ultimo passo; scheda profilo rapido rimandata.
- Fase 5, ordine dei passi: S0 preparazione, S1 barra in alto, S2 banner (con cornice e banner veri), S2b aspetto dell'avversario online, S3 carte, S4 sfondo e mazzo, S5 emoticon, S6 accuso, S7 scope, S8 scelta della presa, S9 opzioni e abbandono, S10 Sorteggio.
- Fase 5, stato al 30/09 sera: tutti i passi fatti, committata in locale con la 2.31. Menu **Tools/UI51/Build Fase 5 (Tavolo 1v1)** (`Assets/UI51/Editor/UI51TableBuilder.cs`) in `GameScene.unity`. Test EditMode 343 totali: 336 ok, 0 falliti, 7 saltati (Explicit).
  - ☑ S0 preparazione: emblema del sole importato, builder del tavolo creato.
  - ☑ S1 barra in alto: Abbandona, pillola del punteggio ("TU · A 51 · avversario"), Opzioni. Provata su iPhone 12 e SE. Revisione fatta: corretto il nome dell'avversario online, che poteva contenere tag di formattazione.
  - ☑ S2 banner dei giocatori: banner nuovi per me e per l'avversario in alto, scope dietro al banner (4 carte e poi "+N"), gettone "M" del mazziere, cornice, banner e livello veri per chi ha un account. Provato su iPhone 12 e SE. Revisione fatta: un nome lungo ora finisce in "..." invece di schiacciare l'avatar; un nome online vuoto diventa "Giocatore N"; nelle partite a 4 tutti contro tutti l'avversario in alto mostra i suoi punti ("34 pt") al posto del livello, finché la pillola a 4 punteggi della Fase 6 non li mostra tutti.
  - ☑ S2b aspetto dell'avversario online: chi ha un account manda cornice, banner e livello agli altri al tavolo (di nuovo a ogni ingresso in stanza, anche al rientro); ospiti e bot restano col banner avversario di sempre. "Liv. 100" con due cifre di carte prese ora entra intero. Scritto e provato in Unity, revisione chiusa (niente errore Photon uscendo dalla stanza; il tuo livello al tavolo e' come in Home anche se il profilo online non arriva). Da provare con due telefoni veri (serve un account vero, tocca a te).
  - ☑ S3 carte e posizioni definitive dei banner: le mie 3 carte in fila sopra al mio banner, i dorsi dell'avversario in fila sotto al suo; le carte in tavola in una griglia nella fascia libera tra le due mani (piu' colonne quando lo spazio in altezza non basta, fino a 12 carte anche su iPhone SE); la carta scelta sul tavolo sale di poco; le carte prese volano verso la pastiglia delle prese del banner. Provato su iPhone 12 e SE (1, 4, 5, 8, 9 e 12 carte), controllo veloce anche a 4 giocatori e 2 contro 2. Revisione chiusa con 2 correzioni: piu' spazio tra le righe del tavolo, cosi' due carte scelte una sopra l'altra non si toccano piu'; una carta che passa dal tavolo a una mano (rientro online, rivincita) non resta sollevata.
  - ☑ S4 sfondo, tavolo e mazzo: sfondo della Home sfocato; tavolo nuovo disegnato dal gioco (legno a tre toni, filo d'oro, feltro verde piu' chiaro al centro, sole all'8%) che segue i banner, quindi i dorsi dell'avversario e la mia mano restano dentro al bordo anche su iPhone SE; cuscino rosso col mazzo in alto a sinistra, che sparisce quando le carte sono finite. Toccando il mazzo si solleva e compare per 2,4 secondi il medaglione con le carte rimaste. Le carte ora partono dal mazzo quando si distribuisce, in tutte le modalita' (scelta mia: se preferisci che partano dal posto del mazziere come prima e' una riga). Provato su iPhone 12 e SE, controllo veloce a 4 giocatori. Revisione chiusa con 1 correzione: con l'accuso del mazziere (15/30) le carte da scoprire non restano piu' ferme e scoperte sopra al mazzo prima di volare.
  - ☑ S5 emoticon: pulsante Emoji tondo come nel mockup; toccandolo, le emoticon scelte in Collezione (fino a 3) compaiono in fila dentro al mio banner al posto di nome e carte prese, quindi non coprono piu' la mano ne' il gettone "M". Si chiude inviando, ritoccando Emoji, toccando fuori o dopo 3,5 secondi; senza emoticon scelte dice "Nessuna emoticon - Scegline in Collezione". La mia emoticon sale dal banner in una nuvoletta crema e sparisce dopo 2,6 secondi (una nuova riparte da capo); quella dell'avversario prende per 3,2 secondi il posto del suo avatar. Nei 4 giocatori lo stesso per chi sta in alto, i due laterali tengono le nuvolette di prima fino alla Fase 6. Online niente di nuovo da mandare. Provato su iPhone 12 e SE e a 4 giocatori. Revisione del piano e del codice chiusa: area di tocco di Emoji sicura (con lo stesso effetto di pressione degli altri tondi), fila centrata anche con 1-2 emoticon, emoticon dell'avversario un po' piu' piccola cosi' il vapore di "arrabbiato" non esce dall'anello.
  - ☑ S6 accuso: il pulsante ACCUSA e' il medaglione d'oro col pugno del mockup, sempre visibile. Quando si apre la finestra dell'accuso pulsa un alone d'oro, passa un riflesso, l'anello del tempo si svuota, i secondi stanno in un pallino sull'anello e fra la mano e il banner compare "Hai un accuso? Tocca il pugno". Toccato con un accuso smette di chiamare; senza accuso trema e fa il suono d'errore, come prima. L'accuso di chiunque (io, bot, avversario online) ora e' quello del mockup: un pugno solo cade al centro del tavolo con due onde d'urto e il bagliore, il tavolo trema, sotto c'e' CIRULLA o DECINO con "chi · +punti" (i punti veri: +10 per il Decino). Le carte non saltano piu'. Nel 1v1 le carte dell'avversario si girano nella sua mano (ora anche nelle mani successive alla prima); nei 4 giocatori, per l'accuso di un altro, le sue 3 carte compaiono sotto la scritta con il bordo d'oro e la matta si trasforma. Sui telefoni bassi (SE) tutto un po' piu' piccolo per non coprire la mano. Provato su iPhone 12 e SE e a 4 giocatori. Revisione chiusa con 1 correzione: se due giocatori accusano insieme (fine finestra, o online) si vedono uno dopo l'altro, ognuno col suo nome, punti e carte, e il gioco riparte dopo l'ultimo (prima il secondo cancellava il primo).
  - ☑ Accusi online (bug gia' esistente trovato dalla revisione di S6), corretto il 30/09 in un worktree a parte e poi portato qui e provato in Unity: lo 15/30 del mazziere conta solo nel totale di smazzata, quindi non scopre piu' la sua mano ne' toglie punti al suo Cirulla/Decino sugli altri telefoni; chi riceve un accuso si allinea al totale del dichiarante e conta anche l'accuso per il bonus XP (prima si perdeva se lo dichiarava il Master in automatico); un solo accuso per giocatore per mano, anche se un rientro riapre la finestra (e un secondo tocco su un accuso gia' contato non da' piu' la scossa d'errore); un accuso arrivato a un telefono ancora alla mano prima aspetta la sua ultima mossa (prima poteva essere pagato due volte a tutti). Un Cirulla da 2 punti (moltiplicatore 0,5) ora scopre le carte. Due revisioni avversarie chiuse, test nuovi in `NetworkAccusoSyncTests`. Da provare con due telefoni con la STESSA versione (il messaggio dell'accuso e' cambiato: una versione vecchia nella stessa stanza perde gli accusi).
  - ☑ S7 scope: toccando le scope sotto al mio banner o a quello dell'avversario in alto si apre il visore del mockup: velo scuro, "Le tue scope" (o "Le scope di ..."), le carte a ventaglio che entrano una dopo l'altra, due chip ("12 carte prese", "6 scope") e "Tocca ovunque per chiudere". Si chiude toccando ovunque, a fine smazzata e quando si apre la finestra dell'accuso; il gioco non si ferma. Oltre 6 scope le carte si stringono nella stessa larghezza di 6. Senza scope il tocco non fa nulla (niente suono ne' vibrazione). Nei 4 giocatori vale per il posto in alto; i banner laterali aspettano la Fase 6. Provato su iPhone 12 e SE (misure uguali al mockup), 9 scope, fine smazzata e 4 giocatori. Corretto anche un difetto dei telefoni veri: un tocco su un pannello sopra alle carte poteva giocare la carta sotto (il controllo usava i dati del dito del fotogramma prima); ora il controllo guarda il punto toccato in quel momento. Da provare su un telefono vero. Revisione chiusa con 1 correzione: toccando le scope subito dopo l'apertura della finestra dell'accuso (primo quinto di secondo) il visore non si apriva; ora si apre e resta aperto, mentre un visore gia' aperto si chiude quando la finestra si apre. Provato dal vivo.
  - ☑ S8 scelta della presa: quando la carta puo' prendere in piu' modi sale dal basso il vassoio del mockup (carta giocata, freccia, "SCEGLI LA PRESA", X; sotto le prese in fila, ognuna col suo colore oro, verde acqua, rosa, viola, con numero, mini carte e chip "+N", denari, SCOPA). Sul tavolo le carte si alzano con un anello del colore della prima presa e un numero per ogni presa che le contiene; toccare una carta del tavolo gioca la sua prima presa. Con tante prese la fila scende fino al 75% e poi scorre col dito (dalla quinta i colori ripartono, i numeri no). Un secondo tocco sulla stessa carta non fa rimbalzare il vassoio, il doppio tocco non sceglie nulla, la X ha un'area di tocco piu' grande del disegno, aprendo il visore delle scope il vassoio si chiude. Provato su iPhone 12 e SE (misure uguali al mockup), con 5 e 8 prese e a 4 giocatori. Revisione chiusa con 2 correzioni: con le prese che entrano tutte lo scorrimento resta spento (sui telefoni veri un tocco che si muove di mezzo millimetro veniva preso per un trascinamento e la presa non partiva); a 4 giocatori la pila dei numeri si stringe per restare dentro la carta. Limite noto: a 4 giocatori su iPhone SE con 11 carte in tavola la fila piu' bassa del tavolo finisce 6,6 unita' sotto il bordo del vassoio (anelli e numeri restano visibili).
  - ☑ S9 opzioni e abbandono:
    - Opzioni (rotella in alto a destra) è un foglio che sale dal basso: "Opzioni" e "La partita continua mentre sei qui", poi AUDIO (Musica, Effetti sonori, Vibrazione), GRAFICA (Grafica ridotta) e PARTITA (Suggerimenti mosse), tutti interruttori, e TORNA AL TAVOLO.
    - Abbandona (in alto a sinistra) apre il dialogo rosso "Abbandonare la partita?" con i chip Sconfitta e Nessuna esperienza, RESTA AL TAVOLO e Abbandona.
    - Il testo del dialogo cambia col tipo di partita:
      - in allenamento a 2: "La vittoria andrà a [avversario]";
      - online con altre persone: il tuo posto lo prende un bot e "[nome] continuerà la partita" (l'avversario nel 1v1, il compagno nel 2 contro 2), altrimenti "la partita continuerà".
    - Indietro chiude il dialogo.
    - Provato su iPhone 12 e SE e a 4 giocatori.
    - Revisione chiusa con 1 correzione: chi è uscito da poco ed è ancora nella finestra di rientro viene chiamato col suo nome e non "Bot N".
    - Restano minori: con altri pannelli aperti Indietro chiude prima il dialogo; toccando Abbandona i suoni di apertura sono due.
  - ☑ S10 Sorteggio (1 contro 1):
    - Al posto del vecchio pannello c'è la ruota del mockup: "PARTITA 1 VS 1", "Chi fa il mazziere?", ruota verde e blu con i due avatar, lancetta d'oro in alto e sole al centro.
    - Tempi:
      - la ruota parte dopo 1,2 secondi e gira per 3,4;
      - si ferma con il mazziere sotto la lancetta (lo decide sempre il master);
      - a 4,7 secondi sale la scheda MAZZIERE ("Sei tu!" oppure il nome dell'avversario, con chi gioca per primo);
      - poi "La partita inizia tra 3, 2, 1" e al tavolo dopo 7,7 secondi.
    - Online i tempi sono fissi, uguali su tutti i telefoni.
    - Offline:
      - con animazioni veloci o grafica ridotta tutto dura 4,8 secondi;
      - AL TAVOLO compare col risultato e chiude subito.
    - Scelta tua (30/09): la ruota gira solo a inizio partita e alla rivincita. Nelle smazzate dopo non c'è sorteggio e si sposta solo il gettone "M". Questo sostituisce, per il 1v1, la "roulette sotto i 2 secondi a ogni smazzata" di K7. A 4 giocatori resta la roulette di prima (Fase 6).
    - Avatar e nomi restano sempre dritti mentre la ruota gira (scelta tua, 30/09; nel mockup girano con la ruota). In cima stanno come nel mockup, in fondo l'avatar resta staccato dal sole.
    - Provato su iPhone 12 e SE (misure come da progetto), animazioni veloci, grafica ridotta, seconda smazzata senza ruota, 4 giocatori con la roulette vecchia.
    - Revisione chiusa: nessun difetto. Una rifinitura fatta: online con le animazioni veloci la dissolvenza finiva un decimo di secondo prima della consegna.
    - Limiti noti:
      - dopo un blocco del telefono di oltre 3 secondi il suono del risultato può saltare;
      - se l'app va in background durante la ruota, grafica e tempo di consegna potrebbero non coincidere (non verificato).
  - Da decidere (S5): la nuvoletta della mia emoticon copre per 2,6 secondi meta' della prima carta della mano, come nel mockup. Se preferisci che resti sopra al banner basta spostarla.
  - Partite a 4: banner mio e in alto nuovi, sinistra e destra vecchi fino alla Fase 6.
  - Da non rilanciare: `TablePlayerBannersBuilder`, `UIV2FoundationBuilder.Settings` e, da S5, "Build Emoticon Quick Bar", "Build Animated Emoticons" e `FrontendExpansionBuilder` Game, da S6 `TableActionButtonsBuilder` e "Tools/UIV2/Build Accuso Window" (cancellano i collegamenti nuovi).

**2.29:** UI51 Fase 4 (Collezione, Profilo, Impostazioni) costruita e provata in Unity. Menu **Tools/UI51/Build Fase 4 (Collezione, Profilo, Impostazioni)** (`Assets/UI51/Editor/UI51MetaBuilder.cs`) in `MainMenu.unity`. Build pulito (0 errori); test EditMode 310 totali: 303 ok, 0 falliti, 7 saltati (Explicit). Committata con la 2.30.
- Costruito:
  - Impostazioni e dialogo "Elimina account".
  - Profilo: stato ospite, stato account, editor di Avatar, Cornice e Banner.
  - Collezione: intestazione, schede a segmenti col conteggio ("Mazzi 4", "Emoticon 3/3", "Accuso 1"), griglia dei mazzi, emoticon (3 slot "In partita" più griglia), scheda Accuso.
- Decisioni dell'utente: editor del profilo costruito e collegato; "Grafica ridotta" tenuta nelle Impostazioni (sezione GRAFICA).
- Script toccati, solo agganci opzionali (vuoti = UIV2 classica): `CollectionScreenV2` (schede a segmenti, `SetTabCount`), `CollectionEmoticonsPanel` (`equippedCountLabel`), `CollectionCosmeticsV2` (`PreviewFist`, ordine degli slot), `DeckCardView` (`preserveArtAspect`), `UIV2CollectionCard` (`orderLabel`), `CollectionItemViewData` (`Order`), `ProfileScreenV2`, `ProfileEditorV2` e `ProfileCosmetics` (nuovi), `SettingsV2Integration`, `DeleteAccountModalV2`, `HomeV2Integration`.
- Verificato nel Simulator su iPhone 12 e SE: Impostazioni, dialogo Elimina account, Profilo ospite, le tre schede della Collezione. Provati: scelta del mazzo, rimozione e aggiunta di un'emoticon, messaggi d'aiuto, ANTEPRIMA dell'accuso, tocchi, dissolvenza delle intestazioni. Preferenze del giocatore rimesse com'erano dopo le prove.
- Verificato solo con dati di prova: Profilo con account ed editor (nessun accesso reale, SALVA mai premuto). "Elimina" non è mai stato premuto.
- Correzioni:
  - Intestazioni di Collezione e Profilo: in gioco uscivano in Poppins invece che in Cinzel e Nunito. `UIV2DesignSystem` ora salta ogni nodo il cui nome comincia per "UI51" (prima solo il nome esatto). Nella prima verifica del Profilo l'errore era sfuggito. Test: `StylingSkipsEverythingUnderUI51Nodes`.
  - Scheda Accuso su iPhone SE: sforava di 1 unità. Area del pugno alta 186 invece di 190.
- Correzioni nate dalla revisione del codice (4 aree, ogni difetto controverificato):
  - "Esci" dalle Impostazioni: non ricaricava la scena, e Indietro riportava nella Home come ospite non scelto. Ora fa la stessa uscita del pannello account (`SettingsV2Integration.Logout`). Provato in gioco da ospite su iPhone 12: si torna all'Accesso con "Continua come ospite".
  - Registrazione dall'app: l'account restava in veste ospite fino al riavvio (niente editor, niente "Elimina account", niente XP). Difetto precedente alla Fase 4. Ora `AuthUIController` chiama `PlayFabAuthService.MarkRegistered(email)`. Test: `MarkRegistered_TurnsTheSessionIntoARealLoginWithEmail`. Non provato con una registrazione vera.
  - Editor del profilo: con rete lenta, subito dopo un accesso, poteva aprirsi sui dati del profilo precedente. Ora usa la stessa condizione della carta (`HomeV2Integration.CloudReady`).
  - CREA UN ACCOUNT nel Profilo ospite: dopo il primo tocco restava rimpicciolito al 97% (due effetti al tocco sullo stesso pulsante). Il builder ora toglie `UI51Press` da quel pulsante. Provato in gioco: torna al 100%.
  - Segnalazione smentita dal controllo in Unity: lo scorrimento di Profilo e Collezione usa la colonna nuova, anche dopo aver riaperto la scena da disco e in gioco.
  - Salvataggio dell'aspetto (fino a 3 scritture separate, senza messaggio se una falliva): corretto in 2.30.
- Differenze dal mockup rimaste (ognuna col via):
  - Impostazioni: riga Notifiche e "Cambia password" nascoste; Lingua "Italiano" non si tocca; velo dietro i pannelli senza sfocatura; il dialogo tiene il cerchio rosso anche nel messaggio finale.
  - Profilo: l'intestazione sta nella barra in alto e il contenuto parte circa 17 unità più in basso; si vedono solo dati veri; 8 avatar; banner "Stellato"; cerchio dell'ospite a bordo continuo; la riga "prossimo sblocco" non c'è. Chiusi in 2.30: CREA UN ACCOUNT su iPhone SE, esito del salvataggio dell'aspetto.
  - Collezione: scheda "Mazzi" al posto di "Dorsi" (dalla 2.30 anche nel testo del Profilo ospite); 4 mazzi veri; "Prossimi sblocchi", "Nuova emoticon", monete e gemme nascosti; conteggio "6 / 6" invece di "6 disponibili"; bordi tratteggiati resi continui; arte dei mazzi adattata alla carta (fino al 6% di deformazione); carte dei mazzi senza animazione al tocco.
  - Emoticon: dalla 2.30 il tocco in griglia toglie un'emoticon già in uso, come nel mockup.
  - Accuso: ANTEPRIMA fa battere il pugno sul posto, senza suono; "In uso" è fisso perché l'accuso è uno solo; "Nuovi accusi in arrivo" è un testo fisso.
- Da provare su un telefono vero: la tastiera nel dialogo Elimina account (dalla 2.30 la finestra sale da sola).
- ASSET MANCANTI DA CREARE: nessuno.
- Prossimo: Fase 5 (Tavolo 1v1), col via.

**2.28:** UI51 Fase 3 (Home e pannelli) costruita e provata in Unity. Menu **Tools/UI51/Build Fase 3 (Home)** (`Assets/UI51/Editor/UI51HomeBuilder.cs`) in `MainMenu.unity`. Build pulito (0 errori, 0 warning); test EditMode 288 ok, 0 falliti, 7 saltati (Explicit). Committata in `42bef09`, non inviata al server.
- Costruito: testata account (avatar, "Livello N", barra XP) e testata ospite (badge OSPITE, Registrati), colonna dei pulsanti laterali, tile Modalità e Mazzo, GIOCA, barra in basso, pannello Modalità a 3 schede (Online, Allenamento, Stanza privata) e pannello Mazzo.
- Decisioni dell'utente: Amici, Missioni e Notizie nascoste per ora; ambiente animato della Home mantenuto.
- Script toccati, solo agganci opzionali (vuoti = UIV2 classica): `UIV2TopBar` (SetGuest, Registrati), `UIV2SelectorChip` (SetBadge), `SelectorOptionViewData` (Caption, ShortName), `QuickSelectionPanels` (schede, CONFERMA, testo difficoltà), `HomeScreenV2` (SetGuest), `HomeV2Integration`.
- Verificato nel Simulator su iPhone 12 e SE: Home ospite, Home con account, le tre schede di Modalità, pannello Mazzo. Su iPhone 12 le misure coincidono col mockup (avatar 58, margini 20/14, tile 169×64, GIOCA 350×60, 24 sopra la nav). Su SE tutto entra senza sovrapposizioni.
- Correzioni:
  - Testi spariti: i rect erano più bassi della riga TMP. Altezze alzate di 2-4 unità.
  - Celle e schede alte 0: nelle righe serviva `childForceExpandHeight`.
  - Ospite: Opzioni sale in cima, la colonna destra ora è impilata.
  - Spunta di selezione fatta con due tratti `UI51Shape` (nel mockup è un tratto SVG, non l'icona).
  - "ALLENAMENTO" nella tile era tagliato: il testo si riduce da 9 a 7 pt.
  - GIOCA si intravedeva dietro i pannelli: fondo dei pannelli pieno (il .97 del mockup conta su una sfocatura che non abbiamo).
- Differenze dal mockup rimaste (ognuna col via):
  - Mancano: titolo di rango; monete e gemme (nascoste); azioni di Posta, Premi e Classifica; campo del codice nella scheda Stanza privata; sfocatura dietro i pannelli; mazzi bloccati.
  - Diverse: barra in basso alta 61 invece di 72; 3 livelli di difficoltà invece di 4; angoli delle anteprime dei mazzi quasi squadrati; il pulsante del mazzo dice "USA QUESTO MAZZO" con nome e numero di carte sulla riga sopra.
  - Barra in basso: l'icona selezionata si ingrandisce un po'; le icone inattive non sono attenuate; etichette tutte in grassetto.
  - Pulsanti laterali: l'avviso di novità è un pallino, non un numero.
  - I mockup dei pannelli non si aprono nel browser (manca `support.js`): confrontati con le misure del loro sorgente.
- ASSET MANCANTI DA CREARE: nessuno. Monete e gemme non sono tra le icone controllate, ma per ora sono nascoste.
- Seguita dalla Fase 4 (vedi 2.29).

**2.27:** Fase 2 UI51 provata in Unity. Build Fase 2 pulito (0 errori, 0 warning), Play senza errori in console. Login, Registrazione, Termini/Privacy e Caricamento verificati nel Simulator su iPhone 12 e SE contro i mockup (`Design/51_handoff/.../mockups/*.dc.html`).
- Campi di `AppLoadingView` collegati. Aggiunto `Percent`: la percentuale sotto la barra, vuota quando il caricamento è indeterminato.
- Correzioni:
  - `UI51Input`: i campi erano invisibili, perché `color` è una tinta. Ora si imposta `fill`.
  - Lo sheet ora arriva fino al bordo, con un'estensione di 80 px sotto la safe area.
  - I titoli di Termini/Privacy erano vuoti per via dell'Ellipsis su un rect basso. Ora usano NoWrap.
  - Nella schermata legale la dissolvenza in basso si estende oltre la safe area.
  - Caricamento: le carte dell'onda erano schiacciate a sinistra. Il LayoutGroup è stato tolto e ogni carta ha una posizione assoluta, perché UIKeyframes legge la posizione quando parte.
  - Caricamento: il suggerimento veniva tagliato. Ora è un figlio diretto di Safe.
- Scostamenti residui:
  - Sui telefoni col notch `DesignCanvasFit` scala a circa 0.9x, quindi logo e titoli sono un po' più bassi del mockup. Si può cambiare, serve il via.
  - L'etichetta versione mostra "v2.27" invece del segnaposto del mockup.
  - I testi legali sono quelli veri (10-11 voci d'indice).
  - Il colore dell'etichetta Step è leggermente diverso.

**2.26:** UI51 Fase 2 (Accesso), scritta nel cloud. Menu **Tools/UI51/Build Fase 2 (Accesso)** (`Assets/UI51/Editor/UI51AccessBuilder.cs`) in `MainMenu.unity`.
- Main (login), Registrazione, Termini/Privacy (un solo LegalModalV2 con indice e sezioni), Caricamento. Solo grafica: gli script esistenti restano, il builder ricollega i campi serializzati.
- Ogni schermata ha la struttura UI51 → Bg (envelope) → Overlay → Safe (`DesignCanvasFit` 390×844, ora con `Reference` e `Fill` pubblici). Le grafiche legacy (`Design`, `Dim`, `Background`, `DesignArea`) vengono spente, non cancellate.

**Rimandati Fase 2:**
- Grafica non ancora animata o collegata: barre e bagliore della robustezza password; globo (lingua) senza funzione. (Le animazioni del caricamento funzionano dalla 2.27.)
- AuthUIController non modificato (file ISO-8859/CRLF): i suoi bottoni indietro restano sui vecchi oggetti, quelli nuovi li governa AuthScreensV2. `LoginBack` aggiunto anche se non è nel mockup.
- Approssimazioni: sfondo "center 30%"; interlinea TMP; riempimento della barra senza estremità arrotondata; "mt -4" di Password dimenticata; bordo laterale e inferiore dello sheet nascosti dall'offset; ombra del titolo della registrazione; stile della scrollbar legale.
- Dipende dallo script: formato di "Lo sapevi?". Omessa la nota segnaposto in fondo ai documenti legali.

**2.25:** Fase 1 UI51 provata in Unity. Build All pulito (0 errori, 0 warning); test EditMode 288 ok, 0 falliti, 7 saltati (Explicit). Gallery verificata nel Simulator su iPhone 12 e SE.
- Correzioni: i fogli emoticon ora hanno 8 fotogrammi (serviva SetDirty sull'importer); `PlayerBanner.m_Name` rinominato `nameText` (era duplicato).
- Sorgenti degli atlas non compresse, quindi niente più warning di compressione. Il font non segnala più finti "caratteri mancanti".
- Aggiunto il riempimento sotto la BottomNav.
- Scrim di sheet e dialog estesi oltre la safe area (coprono notch e home indicator); lo sheet arriva al bordo inferiore.
- Il Dialog senza icona non lascia più lo spazio vuoto in alto: `m_IconRow` viene nascosto.
- Da rifinire: le etichette di sezione della gallery (oro 12 px) si leggono male sul pavimento chiaro dello sfondo. È solo la scena di prova.

**2.24:** UI51 Fase 1, Fondamenta del nuovo design (`Design/51_handoff`, SPEC autorevole). Solo file nuovi sotto `Assets/UI51/`: nessuna scena o asset esistente toccato.
- Arte importata per area in `Assets/UI51/Art/<Area>/` con un Sprite Atlas per area (sfondi esclusi). Font Cinzel/Nunito come TMP dinamici con accentate italiane.
- Token (colori, raggi, font, letter-spacing), shader `UI51/Shape` (rettangolo arrotondato con gradiente) e `UI51/Banner` (parametrico, 6 stili), preset `UIAnim` sez. 6.
- Componenti e prefab sez. 3: UI51_Root (Canvas 390×844, match 0.5, SafeArea), bottoni, toggle, pannello, tab, AvatarFrame, badge, PlayerBanner ×3, BottomNav, BottomSheet, Dialog, emoticon da fogli 4×2.
- Scena di prova `Assets/UI51/Scenes/UI51_Gallery.unity` (fuori dai Build Settings) con i bottoni PROVE.
- Menu unico: **Tools/UI51/Build All (Fase 1)**. Si ferma se una scena aperta ha modifiche non salvate.
- Test EditMode `UI51FoundationTests` (fotogrammi emoticon, letter-spacing). Compilazione, test e refresh del grafo vanno fatti in locale.

**Aperti UI51 (ognuno col via):**
- Fase 2 committata (`aeb3bfc`). Fase 3 committata (`42bef09`). Fase 4 (Collezione, Profilo, Impostazioni) provata (vedi 2.29), da committare col via. Fasi 5–10: Tavolo 1v1, Tavolo 2v2/1v3, Fine smazzata/partita, Amici/Posta/Notizie/Premi, Overlay connessione, Pulizia (lista file per file da confermare).
- Rimandati: coriandoli → F7; ConnectionOverlay → F9; ventaglio carte prese, picker emoticon, "+N" ed emo-fly → F5; input nel Dialog → F4.
- Scostamenti noti: niente blur di sfondo; gradienti conici resi lineari; bordi superiori di sheet e nav approssimati.

**2.23:** correzioni chieste dall'utente dopo la 2.22.
- La tab Negozio è tornata nella bottom bar (BottomNavPolishBuilder, RestoreShop). La 2.22 l'aveva tolta seguendo la revisione approvata. L'icona è ancora `ic_cart` arancio: manca `ic_cart_cream`.
- Bottom bar più bassa, senza la striscia sopra.
  - `BottomNavSafeAreaBleed`: sink 0.5; l'host si accorcia di quanto scende il contenuto.
  - Effetto collaterale: nella Home GIOCA e le pillole scendono di ~51 px; lo spazio fino alla nav passa da ~190 a ~108 px.
- Pile laterali degli avversari più verso il centro: `CardViewManager.sideHandInsetFromBannerEdge` = 78.
  - Se cambia un valore predefinito del prefab, serve un reimport ForceUpdate.
- Il velo scuro delle finestre copriva già la bottom bar, misurato in pixel. La barra è già blu notte, per questo sembrava scoperta. Non cambiato.
- Icone rapide della Home: una sola striscia d'ombra morbida (`ShadowStrip`) dietro tutta la colonna al posto degli aloni per icona (Build Home Quick Actions).
- Verificato nel Simulator su iPhone 12, SE e iPad Mini. G4 fatto: commit della 2.23 su `codex/home-v2-training`, non pushato.

**Aperti (ognuno col via):**
- Riportare GIOCA/pillole più in alto nella Home, se lo spazio in basso ora sembra poco.
- Su iPad la scritta Opzioni finisce sotto la pillola MAZZO (già noto dalla 2.19).
- Icona crema del carrello per la tab Negozio (asset dell'utente).

**2.22:** collegati gli asset consegnati e fatte le correzioni rapide approvate della revisione 2.20.
- Icone crema (16) collegate: rilanciato Tools/UIV2/Apply Icon Set v2. Al tavolo Emoji usa `ic_chat` e ACCUSO `ic_accuso`.
- Tavolo: nuovo builder Tools/UIV2/Apply Room And Table Kit (2.22).
  - Imposta lo sfondo stanza 2x (1882x3344, max 4096) su GameBackground con scala Cover salvata.
  - Imposta il kit feltro/vignetta/cornice 9-slice (`Assets/Art/Table/`) su TableFeltRenderer, in modalità kit.
  - Mette il contorno navy alle didascalie Emoji/ACCUSO; il materiale deve essere dello stesso font.
  - Va rilanciato dopo Apply Table Layout V4, che non imposta più lo sfondo e rifà le didascalie senza contorno.
  - La cornice sostituisce l'ammorbidimento del bordo del tavolo.
- Accesso: ACCEDI e REGISTRATI funzionano (Canvas_Login riattivato in MainMenu; deve restare attivo).
- Tolte le voci "prossimamente": Tools/UIV2/Hide Coming Soon (2.22), da rilanciare dopo Build Delete Account, Profile o Collection. Tolta anche la tab Negozio.
- Lobby: stato di caricamento invece del codice KKKKK e di "Stanza di" vuota.
- Verificato nel Simulator su iPhone 12, SE e iPad Mini.

**Aperti:** chiusi nella 2.23.

**2.21:** nuovo mazzo **Barocco** (nome provvisorio) dell'utente integrato: `Assets/Art/Decks/51_BAROCCO_PNG_Unity/`
(facce 287x452, dorso 874x1376, PPU = altezza/1.8), `Resources/CardDecks/barocco.asset`, 4ª voce del catalogo.
Polish Quick Deck Panel ora clona la cella per i mazzi nuovi (griglia 3 colonne, passo 310x286). Test mazzi estesi
a barocco. Verificato nel Simulator (iPhone 12): pannello Mazzo, anteprima, tavolo (mano, carte in tavola, dorsi).
Napoletano resta il predefinito così com'è (scelta dell'utente, 25/09).
Consegnati dall'utente e **non ancora collegati:** 2 fogli icone crema (16 icone), kit tavolo (cornice, feltro,
vignetta), sfondo sala (941x1672 RGB: serve 1882x3344).
**Prossimo:** ritagliare e collegare le icone crema, kit tavolo, poi le correzioni rapide della revisione (ognuna con il via).

**2.20:** fiamme Home provvisorie meno in risalto (nucleo spento verso l'arancio, un po' di trasparenza,
ondeggiamento più calmo: devono leggersi come sfondo) in attesa dei frame flipbook dell'utente. Tolto lo spazio
residuo sotto la bottom bar. Bottom bar rifinita (BottomNavPolishBuilder): icone centrate, linguetta del selettore
119 -> 132 verso il basso, scritte ExtraBold con contorno e ombra attaccata. Icone rapide Home (Tools/UIV2/Build Home
Quick Actions): niente quadrato blu (resta area di tocco a alfa 0), alone scuro morbido, scritte con contorno — le
scritte restano. Schermata iniziale: ACCEDI/REGISTRATI ExtraBold crema con contorno scuro, REGISTRATI ciano.
Pannello Mazzo (Tools/UIV2/Polish Quick Deck Panel): dorsi veri ovunque, anteprima arrotondata con anello,
didascalia "Nome · 40 carte" a 32 su una riga (misurata sul mockup), scritte con contorno. Il chip MODALITÀ della
Home ripete titolo e icona della riga scelta nel pannello Modalità. Verificato nel Simulator (iPhone 12).
**Da rilanciare dopo un rebuild completo di Home/FrontendFlow:** Build Home Quick Actions e Polish Quick Deck Panel.
**Aperti:** frame flipbook fiamme (utente), proposta swipe pannelli (sfondo condiviso dietro UIV2Pager + contenuto
pagine vuote), proposta animazioni cambio tab (asset utente), cella mazzo selezionata con bagliore oro morbido invece
del pulsante pieno, icone modalità per numero di giocatori (1v1/2v2/1v3). Commit G4 ancora da fare.

**2.19:** set icone crema collegato in un colpo solo (mappa `CreamIcons` in `NewIcon`: chest/trophy/exit/nav_back/
close/settings crema, ic_mail resta com'è; Tools/UIV2/Apply Icon Set v2 ripassa anche i riferimenti v2 oro).
Fiamme: niente più accendi/spegni A/B, una fiamma per torcia con shader `UIV2/FlameWobble` (lingue che salgono,
allungamento e tremolio, ferma con Grafica ridotta tramite `_UIV2Still`). Bagliore torce più piccolo (0.75) e tenue
(alpha 0.1). Luci del castello riallineate alle finestre. Sfondo a tutta pagina: tolta la sfumatura in alto, la
Region ignora il bordo alto della safe area e l'artwork arriva fino alla nav (AboveNav 124). Aggiunti home_bushes
(in primo piano, costruiti per ultimi); esclusi home_vines (ingombranti, doppiano l'edera della base).
Verificato nel Simulator: iPhone 12, iPhone SE, iPad Mini 4. Aperti: fiamma destra in parte dietro il tasto Opzioni,
MODALITÀ/MAZZO sulla balaustra su iPad (centro Home, compito dell'utente). G4 commit ancora da fare.

**2.18:** sfondo Home animato rifatto sui nuovi asset (BackgroundHome/, vecchi nastri/semi/gemma cancellati).
Base + overlay 1:1 a tutto schermo: stelle e luci del castello che respirano, stella cadente ogni ~10 s
(`UIV2AmbientFloat.pause`), bagliore torce leggero; due fiamme per torcia (home_flame_a/b) in dissolvenza incrociata,
perno sul fondo visibile della fiamma. Valori in `HomeOverlayLayers`/`HomeFlameLayers` del builder, misurati in pixel
sul mockup (coppe, fiamme, castello). Esclusi home_vines e home_bushes: nel mockup ci sono già quelli della base.
Icone crema ic_chest/ic_trophy/ic_exit importate, NON ancora collegate: manca ic_mail crema (cambio set in un colpo solo).
Verificato nel Simulator: iPhone 12, iPhone SE, iPad Mini 4 (anche Grafica ridotta = posa di riposo).

**2.17:** sfondo Home rifatto sul mockup (Assets/Mockup, 941x1672). Artwork tra bordo alto della safe area e nav
(Region/AboveNav, 170), cornice ancorata in alto: colonne intere sui telefoni alti, arco e gemma visibili su 9:16
e iPad (l'eccedenza scende dietro la nav). Verde dell'arco (Filler) nella barra di stato, bordo alto sfumato
(`Assets/Art/Generated/Home_TopFade.png`, 24 px d'artwork). Decorazioni ricollocate sul mockup; entrambi i nastri
da home_ribbon_right (sinistro specchiato). Verificato nel Simulator: iPhone 12, iPhone SE, iPad Mini 4.
Limiti: nastri non si avvolgono davanti/dietro le colonne, bastone con foglie (mockup liscio).

**2.16:** Home top bar: per gli ospiti esagono livello + barra XP nascosti (XpMax=0 in HomeV2Integration), verificato live.

**2.15:** sfondo Home a 1882x3344 (import 4096, ASTC). Stile icone scelto: CREMA. Rinominati ic_nav_back_cream,
ic_close_cream, ic_settings_cream. Mancano in crema: ic_chest, ic_mail, ic_trophy, ic_exit (poi cambio set in un colpo solo).

**2.13-2.14 (decisioni del report 24/09 + sfondo Home animato):**
- Ospite: niente più ingresso automatico (il login vero può ancora entrare da solo). Gli ospiti non prendono
  XP né ricompense: nel Profilo niente barra, al suo posto "Registrati per guadagnare XP". ID PlayFab nel
  Profilo: nascosto per gli ospiti, "#" + 8 caratteri per gli account.
- Musica: interruttore vero anche nelle Impostazioni della Home, stessa preferenza del tavolo.
- Etichetta "v1.83" → versione reale (`VersionLabelV2`, legge `Application.version`).
- Sfondo Home animato (`Tools/UIV2/Build Home Ambient`, `UIV2FoundationBuilder.HomeAmbient.cs`): base a
  riempimento senza bande, bagliore che respira, gemma sull'arco, nastri/denari/coppe/bastoni/spade ai bordi con
  micro-movimenti sfasati (`UIV2AmbientFloat`, DOTween, niente Update). Solo sulla pagina Gioca, fermo con Grafica
  ridotta (pose di riposo esatte). Centro libero per la Home. Posizioni/escursioni nella tabella
  `HomeAmbientLayers` del builder; decorazioni importate a 256/512 px con mipmap.
- Non collegati per ora: ic_volume, ic_questionmark, bar_pill_cream, badge_red (aspetta Posta/Premio), divider_gold.
- EditMode 281 passati, 0 falliti, 7 saltati. Verificato nel Simulator su iPhone 12 e iPhone SE.
  **Prossimo passo:** centro della Home (lo fa l'utente), asset mancanti del report 24/09.

**2.12 (E1+D1, 5 effetti, correzioni grafiche della sezione 1, asset nuovi):**
- XP: una sola curva `PlayerXp` 100+20×(L−1); fine partita 40 vittoria / 20 sconfitta, +2 a scopa e +5 ad accuso
  (max +20), metà in allenamento; l'abbandono conta come sconfitta. Riga "+XP" nei risultati con riempimento e lampo,
  scoppio al level up. Ospite: nome "Ospite XXXX", XP locale (`PlayerProgressLocal` ora si crea da solo).
- Home: numero del livello nell'esagono di bars.png; icone della colonna uniformi (riquadro 60×54), Premio non più tagliato.
- Tavolo: panno 60 px troppo in basso (scala camera vecchia nel builder) → banner bot 2/4 e bagliore Emoji/ACCUSO
  non stanno più sul bordo; intestazione più grande su sfumatura navy; ritratti avatar_0N nei banner.
- Emoticon nuove animate (Animator, 10 fps) al tavolo, frame 0 nei pannelli di scelta; set icone v2 al posto delle vecchie.
- Effetti: contorno che pulsa sulle carte giocabili, scia di 0,4 s sulla scopa, passaggi in dissolvenza di 0,25 s,
  vecchi canvas del MainMenu spenti davvero. Tutti spenti con Grafica ridotta.
- `SafeAreaFitter`/`SafeAreaTopOnly` ricalcolano anche al cambio di risoluzione: prima la Home restava schiacciata
  nel 64% sinistro su 16:9 dopo un cambio schermo (barre Android, foldable, Simulator). Verificato iPhone 12 → SE.
- EditMode 282 passati, 0 falliti, 7 saltati. **Prossimo passo: decisioni aperte nel report del 24/09**
  (identità ospite stabile, uso di ic_volume/ic_questionmark e dei pezzi inutilizzati di bars.png, centro Home vuoto).

**Fix 2.10 (robustezza flusso multiplayer, TurnController):** se il Master rifiuta una mossa del
giocatore di turno, rimanda a tutti il suo GameState (resync autoritativo; i duplicati da doppio tocco
fuori turno vengono ignorati). Il tocco del giocatore durante un'animazione non va più perso: viene
tenuto e rigiocato appena finisce, e prima dell'invio in rete si scarta se non è più valido.
**Da riprovare su due telefoni.**

**Fix 2.09 (presa doppia bloccata in multiplayer):** il tocco sul pannello "scegli la presa"
passava anche alla carta in mano sotto (`OnMouseDown` ignora la UI) e inviava uno scarto forzato
illegale: chi giocava lo applicava, l'altro client lo rifiutava → partite divergenti. Ora:
guard UI in `CardView.OnMouseDown`, niente più scarti forzati (CardViewManager/TurnController),
validazione mosse uguale su tutti i client, `RoundEndPanel` non intercetta più i tocchi da nascosto.
(I due punti aperti del 2.09 sono chiusi nel 2.10.)

**Fix build 2.08:** rimosso `SettingsModalUI.cs` (inutilizzato), che causava i 4 avvisi
"same field name is serialized multiple times". Multiplayer su dispositivo non funzionante:
nell'Editor partita rapida e privata funzionano. Causa probabile: la build release sceglieva
la regione Photon migliore per ogni telefono (l'Editor usa DevRegion eu), quindi i dispositivi
finivano in regioni diverse. Ora `FixedRegion = eu` in PhotonServerSettings. **Da riprovare
su due telefoni con la stessa build 2.08.**

**K7 completato (2.07):** l'ospite già entrato salta la schermata iniziale e arriva
alla Home; GIOCA avvia la partita con un tocco. Accesso e registrazione cancellano la
scorciatoia. La roulette del mazziere fa un solo giro breve, con lo stesso mazziere e
la stessa autorità. I risultati di smazzata proseguono da soli dopo un conto di 8 s,
una sola volta, solo offline o sull'host. La rivincita finale resta manuale. Verificato
con test EditMode e runtime su GameScene e MainMenu. Da provare con due client Photon
reali. Dettagli: `docs/ui/k7-flow-progress.md`.

**I5 completato (2.06):** Grafica ridotta è una scelta unica e persistente nelle
Impostazioni di Home e tavolo, con migrazione di Animazioni veloci. Spegne particelle,
coriandoli, shader decorativi, sfondo animato, sfocature e pulsazioni; accorcia i tempi
senza interrompere il volo K5. Indicatori utili fermi e vibrazione indipendente.
Ripristino anche a pannello aperto; effetti nuovi e riaperti rispettano la scelta.
**271 EditMode passati, zero fallimenti; runtime I5, 3 K5 e controlli K6 passati.**
Misure di batteria/prestazioni su telefono ancora da fare. Dettagli:
`docs/ui/i5-reduced-graphics.md`.

**K6 implementato (2.05):** feedback aptico sui tocchi accettati e sui momenti locali,
interruttore Vibrazione condiviso tra Home e tavolo; particelle finite su scopa,
accuso, vittoria locale e EXP realmente assegnata. Pool limitato a quattro emettitori,
disattivabile per I5. Volo K5 preservato. **261 EditMode passati, zero fallimenti;
3 runtime K5 e controlli runtime K6 passati.** Vibrazione fisica e build native ancora
da verificare su telefono. Dettagli: `docs/ui/k6-feedback-plan.md`. I5 completato nella 2.06.

**Correzione K5 (2.04):** segnalati scatti nel volo delle carte. La suddivisione della 2.03
fermava quasi la carta a metà volo e interrompeva lo spostamento all'80% della durata.
Ripristinata la traiettoria continua precedente, mantenendo ombre e cleanup; audio all'arrivo.
Nuovo test di continuità fallito sulla 2.03 e passato dopo la correzione. **255 EditMode e
3 runtime K5 passati**; campionamento del volo conferma assenza della pausa intermedia.

**Ripresa del 23/09:** G4 verificato nei commit esistenti; K3 completato con UIEffect 5.9.0
e UIParticle 4.11.4 già presente. Compilazione senza errori e **228/228 test EditMode passati**.
**K1 completato (2.00):** tempi comuni, pressione/rilascio dei pulsanti, pannelli e pagine,
contatori dei risultati, contatori valuta pronti e volo premi riutilizzabile. Builder
`Tools/UIV2/Apply Motion Kit` sui prefab e completamento automatico una volta per scena.
**Verifiche:** 229 test EditMode passati; test runtime esplicito passato separatamente in Play Mode.
**K2 completato (2.01):** tema condiviso, pulsanti oro/blu/piatto/icona, due pannelli,
scala Poppins 40/32/24/20; oro C9 anche su CONTINUA e RIVINCITA. Builder
`Tools/UIV2/Apply Design System`, completamento a runtime senza risalvare scene.
**Verifiche K2:** 237 test EditMode passati e test runtime K1 passato separatamente;
controllo visivo in Play Mode di avvio, accesso, impostazioni, Home, Collezione, Profilo e risultati.
Regole e compatibilità in `docs/ui/design-system.md`.
**K4 consegnato (2.02, 23/09):** riflessi sui pulsanti oro e sulle carte selezionate/toccate,
bagliori dalla silhouette reale, dissolvenza/bruciatura della matta, shader olografico opt-in,
fondo Home con luce lenta e sfocatura GPU senza lettura dei pixel sulla CPU.
Builder `Tools/UIV2/Apply Shader Kit` eseguito; completamento automatico a runtime.
**Verifiche:** 249 test EditMode passati, zero fallimenti; 3 test runtime K4 passati separatamente.
Il runtime K1 è rimasto esplicito e non rieseguito in questo giro. Avvio, Home, tavolo e sfocatura
Impostazioni controllati in Play Mode; corretto e ricontrollato il capovolgimento Direct3D.
**Da collegare più avanti:** olografico ai mazzi/oggetti realmente rari con F2/F5/J5 (oggi manca
la rarità nei dati). **Da provare in G2/I7:** Android, costo GPU e orientamento sugli altri backend,
giro visivo completo matta/accusi/risultati e bordi delle liste mascherate.
Dettagli K4 in `docs/ui/k4-shaders-plan.md`.
**K5 consegnato (2.03, 23/09):** panno con trama deterministica e luce centrale, ombre morbide
condivise sulle carte e sulle copie animate, risposta della mano con piccolo assestamento,
discesa più decisa e posa finale della giocata nei medesimi 0,35 secondi (audio all'impatto).
Corretti conflitti hover/selezione/matta, ripristino su interruzione/riuso e rilascio del feltro
rigenerato. Integrazione nei componenti esistenti: nessuna scena/prefab modificata a mano.
**Verifiche K5:** 254 EditMode passati, zero fallimenti; 3 runtime K5 e 2 runtime carte K4
passati separatamente. Tavolo e presa completa controllati in Play Mode; zero ombre isolate,
zero copie residue e console senza errori. Android e multiplayer reale restano in G2/I7.
Dettagli e immagini in `docs/ui/k5-table-plan.md`. **Prossimo task: K6 — vibrazione e particelle.**
Il volo di premi reali attende E2/F1; nessuna
valuta o ricompensa finta aggiunta. Nessuna build Android eseguita in questo giro.

**Ultimo lavoro fatto (19/09):**
- G1 chiuso (1.93): tavolo provato su tablet 3:4, 20:9 e iPhone; mano più vicina al fondo sui telefoni lunghi.
- I1 chiuso (1.94): dorso delle carte al tavolo. Vedi Sprint 6.
- I3 chiuso (1.95): emoticon rapide al tavolo al posto del pannello.
- I2 chiuso (1.96): icone centrate sulla faccia dei riquadri blu e oro.
- C9 chiuso (1.97): scritte GIOCA e GIOCA COME OSPITE con il contorno bruno del mockup.
- G6 chiuso (1.98): tavolo sfocato dietro il fine smazzata.

**G4 verificato il 23/09:** il lavoro dopo il checkpoint `b21dc11` è già salvato nei commit
`b8d7b49` e `8743964` (415 file modificati rispetto al checkpoint), sul branch
`codex/home-v2-training`. Il precedente avviso di lavoro non committato era rimasto obsoleto.

**Come riprendere (per chiunque, anche senza Claude):**
- Questo file è la lista viva. `UI_INTEGRATION_ROADMAP.md` è il resoconto dettagliato delle consegne.
- La UI si costruisce con gli script del menu Unity `Tools/UIV2/...` (cartella `Assets/Editor`):
  non modificare a mano le scene o i prefab che quegli script generano, rilancia lo script.
- Versione in `ProjectSettings > bundleVersion`: +0,01 a ogni giro di modifiche.
- Test: `Window > General > Test Runner > EditMode`, 255 passati; 3 runtime K5 passati nella 2.04. Ultima verifica dei 2 runtime carte K4 nella 2.03; runtime K1 e blur K4 non rieseguiti.
- Mockup in `Assets/Mockup/`. Da ora sono un punto di partenza, non un vincolo (vedi I8).

**Ordine deciso il 19/09 (prima la qualità, poi i sistemi):**
1. **G4** ☑ commit del lavoro precedente verificati il 23/09 (`b8d7b49`, `8743964`)
2. **Qualità** sull'app che c'è già: K3 librerie → K1 kit di movimento → K2 sistema di design →
   K4 shader → K5 tavolo → K6 vibrazione e particelle → I5 grafica ridotta → K7 flusso veloce
3. **I7** giro di prova vero dell'app (anche il bug della Cirulla a tavolo vuoto)
4. **Sistemi**, sopra una base già curata, seguendo "Economia e regole" nello Sprint 6:
   E1+D1 (XP, livelli, profilo) → F1 + J4 (monete, gemme, barra in alto) → E5 (tavoli con puntata) →
   E2 (giornaliere) → E3 (missioni) → F2 (negozio) → F5/J5 (sblocchi) → J2 (pass mensile) → F4 + K11
   (video e pubblicità) → F3 (acquisti) → D3 (posta) → D2 (avatar e cornice) → I4 (profilo rapido) →
   D4 (amici) → J3 (frasi rapide e chat amici) → K8 (tutorial)
5. **G2** build Android e prova su telefono vero (anche prima, quando si vuole provare in mano)

---

## Sprint 1 — Tavolo: bug

| # | Cosa | Stato |
|---|------|-------|
| A1 | Ultima carta del giro: l'animazione si ferma, le carte del tavolo spariscono e ricompaiono solo a fine timer accuso | ☑ 1.79 |
| A2 | Scelta tra più prese: resta a schermo un quadratino giallo; le opzioni sono rettangoli di testo con simboli illeggibili → nuova grafica | ☑ 1.79 |
| A3 | Ventaglio della mano: la carta centrale è coperta da quelle ai lati | ☑ 1.79 |
| A4 | Mani degli avversari (posti 1, 2, 3): il banner copre le carte, soprattutto al posto 2 | ☑ 1.79 |
| A5 | Prese dei giocatori sparse sul tavolo → posizione ordinata vicino al banner (le scope dietro al banner restano) | ☑ 1.79 |
| A6 | Emoticon in partita: solo le 3 equipaggiate, non tutte e 6 | ☑ 1.79 |
| A7 | Roulette mazziere: con 2 giocatori mostra 4 posti | ☑ 1.79 |
| A8 | Barra in alto: la scritta del turno si sovrappone a "Mano X di Y" | ☑ 1.80 |
| A9 | Chip MAZZIERE sul banner copre avatar e nome | ☑ 1.80 |
| A10 | Accuso del mazziere: il mazzetto mostra già le carte prese prima dell'animazione; testo "Tu fa Scopa da 30!" da correggere | ☑ 1.80 |
| A11 | Carte scoperte di un avversario dopo un accuso: la matta appare come carta gialla (va con B4) | ☑ 1.80 |
| A12 | Pannello emoticon: pulsante chiudi con "X" di testo invece dell'icona del mockup | ☑ 1.80 |

## Sprint 2 — Tavolo: sequenze ed effetti

| # | Cosa | Stato |
|---|------|-------|
| B1 | Roulette mazziere dai mockup 27–28: nastro, riquadri ai posti, trofeo, chip MAZZIERE, CONTINUA che si chiude da solo dopo 4 secondi | ☑ 1.79 (bagliore del vincitore e sfocatura vera: ☑ 1.81) |
| B2 | Accuso manuale più evidente, finestra di almeno 5 secondi | ☑ 1.80 |
| B3 | Animazione pugno: tutte le carte (mani e tavolo) saltano a caso restando visibili al proprio posto, poi si riprende a giocare | ☑ 1.80 |
| B4 | Effetto del 7 di coppe (matta) che si trasforma in un'altra carta per l'accuso | ☑ 1.80 |
| B5 | Impostazioni in partita (mockup 26): animazioni veloci, suggerimenti mosse, musica, effetti, abbandona partita. Toglie anche l'ESCI in più dai risultati | ☑ 1.81 (musica ed effetti salvano solo la scelta: nel gioco non ci sono ancora suoni; l'abbandono conta come sconfitta dal 2.12) |
| B6 | Reveal accuso del mazziere: tempi e grafica definitivi | ☑ 1.82 |
| B7 | Multiplayer: i client che non sono host non vedono la sequenza del mazziere | ☑ 1.82 (provato simulando un client nell'Editor, non ancora con due dispositivi veri) |

## Sprint 3 — Accesso, Home, stile

| # | Cosa | Stato |
|---|------|-------|
| C1 | Login V2 (mockup 23) | ☑ 1.84 (ritarato sui pixel del mockup nella 1.84) |
| C2 | Registrazione V2 (mockup 24) | ☑ 1.84 |
| C3 | Novità (mockup 25): contenuto e punto da cui si apre | ☑ 1.87 pagina dal pulsante "Novità" della schermata iniziale; contenuti da **PlayFab Title News** (Game Manager → Content → Title News), NUOVO = non ancora viste. Oggi PlayFab non ha notizie: si vede "Nessuna novità per ora" |
| C4 | Poppins su tutta la UI. Tutti i pesi presenti (Regular→ExtraBold); Poppins Medium è il font predefinito dal 17/09. Resta da applicarlo alle schermate esistenti | ☑ 1.90 `Tools/UIV2/Apply Poppins Everywhere`: 519 testi da LiberationSans a Poppins (grassetto → Poppins Bold, resto → Medium), contorni navy/marrone ricreati sul nuovo atlas. Controllati nel Simulator: iniziale, Home, Modalità, Impostazioni, Collezione, Profilo, tavolo |
| C5 | Bagliore morbido dietro Accuso, Gioca, trofeo e pulsanti principali | ☑ 1.91 `Tools/UIV2/Build Soft Glows`: GIOCA (Home), GIOCA COME OSPITE, riga selezionata di Modalità (teal, segue la selezione), trofeo e RIVINCITA dei risultati. Accuso aveva già il suo (pulsa durante la finestra). Il bagliore a pillola usa il "Bagliore morbido cerchio" in 9-slice al centro |
| C6 | Effetti particellari: schermata iniziale, Home, pagine, ricompense, vittoria | ◐ 1.91 `Tools/UIV2/Build Mote Fields` + componente `UIV2MoteField`: pulviscolo oro in schermata iniziale e Home/pagine, scoppio di luce dal trofeo quando vinci. **Ricompense**: manca la schermata (E2/F1), il componente ha già `Burst()` per quando ci sarà. Scintille a stella: asset mancante |
| C7 | Pannello Modalità allineato al mockup panel_modalita_v2 | ☑ 1.91 `Tools/UIV2/Calibrate Quick Mode Panel`: righe 28/19 ExtraBold con contorno navy spesso, sottotitoli del mockup, titoli di sezione con la linea, "Difficoltà" grigio chiaro, pillole con contorno, barra di scorrimento oro, bagliore teal sulla riga scelta |
| C8 | Impostazioni: "51Cirulla · v…" usciva sotto la cornice (dalla 1.86, riga Elimina account) | ☑ 1.90 cornice 1650, scritta dentro; corretto nel builder Build Delete Account |
| C9 | Scritte GIOCA (Home) e GIOCA COME OSPITE senza il contorno marrone spesso del mockup | ☑ 1.97 `Tools/UIV2/Style Gold Button Labels`: Poppins ExtraBold, faccia #FFFCF2 piatta (il prefab aveva un gradiente che la tingeva di crema), contorno bruno #945408 di 5-6 px, corpo dall'altezza delle maiuscole del mockup (38 e 22 px). Misurato al pixel contro i mockup. Gli altri pulsanti oro (CONTINUA, RIVINCITA, REGISTRATI PER SALVARE...) hanno ancora lo stile vecchio: da uniformare in I7 se ti piace questo |
| C10 | Bagliori dietro i pulsanti: sembravano una lastra rettangolare | ☑ 1.92 gradiente smoothstep generato (`glow_soft_pill`), parte sotto il pulsante e sfuma in 20-35 px |
| C11 | Titoli sui nastri dei pannelli (Modalità, Mazzo, Impostazioni, stanze, risultati, roulette...) | ☑ 1.92 `Tools/UIV2/Calibrate Panel Titles`: 14 titoli in ExtraBold bianco con contorno verde scuro, centrati sul nastro come nei mockup |
| C12 | Chiudere i pannelli toccando fuori | ☑ 1.92 `DismissOnBackdrop` (`Tools/UIV2/Build Backdrop Dismiss`): Modalità, Mazzo, Crea/Entra stanza, Emoticon, Personalizza. Esclusi di proposito ricerca partita, sale d'attesa, risultati, roulette |
| C13 | Barra in basso sollevata dal fondo, icone piccole | ☑ 1.92 `BottomNavSafeAreaBleed`: sfondo fino al bordo e contenuto più in basso sui telefoni con barra di sistema; icone 62→72, linguetta oro 238x117→262x128 (9-slice); 2.12: 254x119, riempimento oro 226x82 come il mockup |

## Sprint 4 — Profilo, social, progressione

| # | Cosa | Stato |
|---|------|-------|
| D1 | Profilo: dati reali (livello, XP, statistiche) | ☑ 2.12 livello nell'esagono in Home, nome ospite "Ospite XXXX". 2.13: ospiti senza XP né ricompense (invito a registrarsi al posto della barra), ID accorciato a "#XXXXXXXX" e nascosto agli ospiti. Aperto: l'ospite è un account nuovo a ogni avvio (nome e statistiche cloud non restano) |
| D2 | Scelta di icona e banner nel profilo, visibili al tavolo | ◐ 2.29: editor di avatar, cornice e banner nel Profilo; l'avatar scelto si vede anche nella Home. Provato solo con dati di prova. Aperto: al tavolo non si vedono ancora |
| D3 | Posta (mockup 06): messaggi e ricompense dal server | ☐ |
| D4 | Amici (mockup 07): lista, richieste, invito in stanza (attiva "Amici" in sala d'attesa) | ☐ |
| D5 | Classifica, tornei, eventi | ⏸ futuro |
| E1 | XP a fine partita e livelli (compreso l'allenamento, oggi non assegna XP) | ☑ 2.12 curva unica `PlayerXp` 100+20×(L−1) per locale, cloud e Home; 40 vittoria / 20 sconfitta, +2 scopa, +5 accuso (max +20), metà in allenamento; riga "+XP" nei risultati con riempimento, lampo e scoppio al level up |
| E2 | Ricompense giornaliere (mockup 08) | ☐ |
| E3 | Missioni (servono anche per sbloccare i mazzi) | ☐ |
| E4 | Penalità per abbandoni ripetuti | ☐ |
| E5 | ~~Sistema di energia~~ → **tavoli con puntata in monete** (deciso il 17/09): allenamento sempre gratis, ricarica con bonus giornaliero, video e negozio. Va con F1 e F4 | ☐ |

## Sprint 5 — Negozio e sblocchi

| # | Cosa | Stato |
|---|------|-------|
| F1 | Valute (monete e gemme) con saldo salvato sul server | ☐ |
| F2 | Negozio (mockup 16) con acquisti in gemme | ☐ |
| F3 | Acquisti con soldi veri (Google Play / App Store) | ☐ |
| F4 | Video pubblicitari con premio | ☐ |
| F5 | Mazzi bloccati con anteprima, sblocco con acquisto, missioni o livello | ☐ |
| F6 | Tavoli da gioco sbloccabili | ⏸ futuro |

## Sprint 6 — Richieste del 19/09 (da provare e rifinire, non più "copiare il mockup")

### Bug e rifiniture: si fanno in ordine, senza bisogno di via

| # | Cosa | Stato |
|---|------|-------|
| I1 | Dorso delle carte al tavolo: si vedeva sempre il napoletano | ☑ 1.94. Due cause: (1) il tavolo leggeva il mazzo dal `MatchConfig` salvato in PlayerPrefs, che valeva "default" (= napoletano) o un mazzo vecchio in ogni percorso di avvio che non lo riscriveva; ora usa sempre il mazzo scelto dal giocatore (`CardDecks.LoadForMatch()`). (2) Il mazzo Classico non aveva un dorso suo: collegato `51_CARD_BACK_MASTER.png` (sorgente 4x, PPU tarato perché abbia la stessa misura delle facce). Test: ogni mazzo ha un dorso diverso |
| I2 | Icone nei riquadri piccoli non centrate | ☑ 1.96 causa: i riquadri `sq_blue`/`sq_gold` hanno il bordo 3D più spesso sotto, quindi la faccia chiara sta 4-7 px più in alto del centro del rettangolo e le icone sembravano basse (più i margini trasparenti delle icone). `Tools/UIV2/Center Icons On Button Faces` mette il centro visibile di ogni icona sul centro della faccia: 14 icone (Home, Profilo, X di chiusura, schermata iniziale, tavolo). Controllato a schermo prima/dopo. Coperti solo i riquadri blu e oro in MainMenu e GameScene: se ne vedi altri storti, dimmi quali |
| I3 | Emoticon al tavolo: si apriva un pannello che copriva il tavolo e fermava il gioco. Ora c'è una **scelta rapida** | ☑ 1.95 striscia sopra Emoji con le 3 equipaggiate (stile dei banner), un tocco invia e chiude, si chiude da sola dopo 3,5 s, un tocco fuori la chiude e passa comunque sotto, niente velo. `Tools/UIV2/Build Emoticon Quick Bar`. Segue il pulsante anche quando scende sui telefoni lunghi. Il vecchio pannello `GamePresentationV2/Emoticons` resta in scena ma non si apre più (è il ripiego se la striscia manca). Il tocco fuori non è stato provato con un dito vero (solo in Editor) |
| I4 | Profilo rapido al tavolo: tocco sul banner di un giocatore → scheda piccola (mockup `10_profilo_rapido`) | ☐ non esiste ancora (dipende da D1/D2 per i dati veri) |
| I5 | Impostazioni → **Grafica ridotta**: spegne particelle, bagliori pulsanti, sfocature, sfondo animato, shader e accorcia le animazioni (per telefoni lenti e batteria). Va fatta insieme a K1-K6, così ogni effetto nuovo nasce già spegnibile | ☑ 2.06 — unica scelta Home/tavolo con migrazione di Animazioni veloci; spegnimento e ripristino anche durante l'uso, inclusi effetti precedenti a K4/K6. Indicatori utili statici, volo K5 continuo, vibrazione indipendente. 271 EditMode passati, runtime I5/K5/K6 verificati. Misure su telefono ancora da fare. `docs/ui/i5-reduced-graphics.md` |
| I6 | Sfondo animato nella Home | ☑ 2.02 — shader con gradiente/luci lenti sul BackgroundLayer, insieme al pulviscolo C6; nessun video o asset aggiuntivo |
| I7 | **Giro di prova completo dell'app** (avvio → accesso → Home → partita → risultati → rivincita/Home, e online): trovare tutto ciò che è lento, macchinoso, poco chiaro o grezzo, e correggerlo. Obiettivo: velocità, fluidità e chiarezza al massimo | ◐ 2.12: chiusi numero livello in Home, targa oro barra in basso, vecchi canvas MainMenu (spenti; restano Canvas_Login e ModePanelRoot perché servono), avatar al tavolo, banner bot 2/4 e bagliore Emoji/ACCUSO sul bordo (panno 60 px troppo in basso), intestazione tavolo, riga XP nei risultati, etichetta GameFormat (ok). 2.11: Cirulla a tavolo vuoto e doppio accuso MP ☑. 2.13: "Musica" nelle Impostazioni della Home ☑, etichetta v1.83 → versione reale ☑; 2.14: sfondo Home animato ☑. Ancora aperti: monete e gemme (J4), centro Home vuoto (lo fa l'utente), resync a metà distribuzione che non ferma le coroutine, ritardo del pugno sul client in ritardo, riprova Cirulla a tavolo vuoto |
| I8 | Direzione: i mockup servivano a dare vita all'app, da ora le migliorie le decidiamo noi. UI pulita, niente dettagli inutili | regola |
| I9 | Icone nuove senza "quadrato + icona dentro"; navigazione e chiusure coerenti | Brief aggiornato il 23/09: `docs/ui/asset-refresh-brief.md`. Confermati «Indietro» nelle schermate e «Chiudi» nei popup al posto delle X di chiusura. Proposta: icone illustrate libere, meno cornici e materiali coerenti. Asset e integrazione ancora da fare. 2.12: set icone v2 integrato (`Tools/UIV2/Apply Icon Set v2`), emoticon animate; resta la rimozione dei riquadri (non approvata) |

### Sistemi: ognuno parte solo con il tuo via

| # | Cosa | Stato |
|---|------|-------|
| J1 | **Decidere l'economia**, prima di costruire i sistemi sotto | ☑ decisa il 19/09: vedi "Economia e regole". I numeri sono valori di partenza da tarare giocando |
| J2 | Pass **mensile** (deciso il 19/09, non settimanale): circa 30 livelli, fila gratis + premium, tema grafico per stagione; missioni nuove ogni settimana | ☐ dopo E1, F1, E3 |
| J3 | Chat (deciso il 19/09): al tavolo **solo frasi rapide** pronte ("Bella giocata!", "Ancora una?") più emoticon; **chat libera solo tra amici**, con filtro parolacce, segnala e blocca | ☐ dopo D4. Servizio chat da scegliere (es. Photon Chat) |
| J4 | Barra in alto della Home funzionante: livello e XP si leggono già; **monete e gemme non ci sono** (`SetResources(null)`) | ☐ con F1 |
| J5 | Sblocco di animazioni accuso e tavoli (oltre ai mazzi di F5) | ☐ dopo J1 |

### Qualità e stile (deciso il 19/09: si fa PRIMA dei sistemi)

Obiettivo: l'app deve sembrare disegnata, non assemblata. Pubblico misto (adulti e giovani): base
pulita e leggibile, con animazioni e premi che danno soddisfazione. Grafica: per ora asset attuali
rifiniti da noi; l'artista UI arriva dopo il lancio (K10).

| # | Cosa | Stato |
|---|------|-------|
| K1 | **Kit di movimento** unico, applicato ovunque da un builder: pulsanti che si schiacciano e rimbalzano al tocco, pannelli che entrano ed escono con un piccolo rimbalzo, numeri che contano, ricompense che volano verso il contatore, passaggi morbidi tra pagine e scene | ☑ 2.00 — `Tools/UIV2/Apply Motion Kit` + installazione una volta per scena. Contatori nei risultati e componenti pronti per valute/premi (dati reali con E2/F1). 229 test EditMode + test runtime esplicito passati; dettagli nella roadmap |
| K2 | **Sistema di design fissato**: 3-4 tipi di pulsante (primario oro, secondario blu, piatto, icona), 2 tipi di pannello, scala dei testi (titolo, sottotitolo, testo, didascalia), palette. Poi ogni schermata si riallinea a quello. Comprende lo stile oro di C9 su tutti i pulsanti oro (CONTINUA, RIVINCITA...) | ☑ 2.01 — `Tools/UIV2/Apply Design System`, tema condiviso e completamento scene a runtime. Poppins 40/32/24/20 con eccezioni calibrate; materiali oro condivisi. 237 test EditMode + runtime K1 passati; regole in `docs/ui/design-system.md` |
| K3 | Librerie gratuite MIT: **UIEffect** (riflessi, dissolvenze, gradienti, ombre sulla UI) e **UIParticle** (particelle vere dentro i pannelli) | ☑ 1.99 — UIEffect 5.9.0 installato tramite UPM, tag fissato; UIParticle 4.11.4 già presente, hash nel lockfile. Compilazione senza errori, 228/228 test EditMode |
| K4 | **Shader nostri**: riflesso di luce che passa su pulsanti, carte e oggetti rari; bagliore calcolato sulla forma esatta del pulsante (sostituisce i bagliori allungati, mai più storti); dissolvenza o bruciatura per carte speciali (accuso, matta); olografico per i mazzi rari; sfondo animato della Home (I6); sfocatura su scheda video, più veloce | ☑ 2.02 — Shader Kit, silhouette alpha, matta, Home, blur GPU. 249 EditMode + 3 runtime K4 passati; avvio/Home/tavolo/blur controllati. Olografico e riflessi per oggetti rari pronti opt-in, collegamento a dati reali con F2/F5/J5. Prestazioni Android da misurare in G2 |
| K5 | **Tavolo più ricco**: panno con texture e luce al centro, ombre sotto le carte, mano che reagisce al tocco, carte giocate con più peso | ☑ 2.04 — Panno e ombre K5; ripristinato volo continuo dopo gli scatti della 2.03. 255 EditMode + 3 runtime K5; dettagli in `docs/ui/k5-table-plan.md` |
| K6 | **Risposta a ogni tocco**: vibrazione (con interruttore nelle Impostazioni) e particelle sui momenti forti (scopa, accuso, vittoria, premi) | ☑ 2.05 — toggle Home/tavolo, aptica locale, pool finito; 261 EditMode + runtime K5/K6 passati. Prova aptica/build su telefono pendente; dettagli in `docs/ui/k6-feedback-plan.md` |
| K7 | **Flusso veloce**: dall'apertura alla partita in 2 tocchi (l'ospite già entrato salta la schermata iniziale), roulette del mazziere più breve o saltabile, risultati che proseguono da soli dopo qualche secondo | ☑ 2.07 (23/09): ospite diretto in Home, roulette a un giro, conto di 8 s solo host/offline, rivincita manuale. Dal 30/09 (2.31) nel 1v1 c'è la ruota del Sorteggio solo a inizio partita e alla rivincita (7,7 s). Manca la prova con due client reali. `docs/ui/k7-flow-progress.md` |
| K8 | **Tutorial**: la prima volta una partita guidata contro un bot (prese, scope, accuso) + pagina Regole sempre consultabile | ☐ deciso |
| K9 | Segnalazione crash e statistiche d'uso (dove la gente abbandona). Da dichiarare nella Privacy (H3) | ☐ |
| K10 | Artista UI per guida di stile e pezzi chiave (pulsanti, pannelli, icone, cornici; vedi I9) | ⏸ dopo il lancio, quando i giocatori crescono |
| K11 | Pubblicità tra le partite, regola leggera: al massimo una ogni 3 partite finite e non prima di 3 minuti dall'ultima; mai durante la partita, mai nelle prime 2 partite del giorno, mai a chi ha comprato qualcosa | ☐ con F4 |

### Economia e regole (deciso il 19/09; numeri di partenza da tarare)

Principi: **mai pagare per vincere**, si vende solo estetica. Guadagno da estetica, pass premium,
video facoltativi e poca pubblicità (K11), senza dare fastidio. Saldi e premi decisi dal server
(PlayFab), mai dal telefono, altrimenti si imbroglia.

- **XP** (livelli, sblocchi): online vittoria 40, sconfitta 20, +2 per scopa, +5 per accuso (bonus massimo +20).
  Allenamento: metà XP. XP per passare di livello: 100 + 20 × (livello - 1).
- **Monete** (valuta di gioco): servono per entrare nei **tavoli con puntata** (E5). Fasce 100 / 500 /
  2.000 / 10.000, sbloccate ai livelli 1 / 5 / 10 / 20. Chi vince prende il piatto meno il 10% del banco
  (a coppie si divide). Allenamento: 10 monete a partita, massimo 100 al giorno.
- **Ricarica gratis**: sotto il minimo del tavolo più basso, una volta al giorno 500 monete, più un video
  facoltativo per averne altre 500. Nessuno resta mai bloccato.
- **Gemme** (valuta premium): si comprano; poche gratis (ogni 5 livelli, pass, 7° giorno delle giornaliere).
  Servono per estetica e pass premium.
- **Giornaliere (E2)**: calendario di 7 giorni, il 7° con gemme; se salti un giorno la serie riparte.
- **Missioni (E3)**: 3 giornaliere + 5 settimanali; danno monete e punti del pass.
- **Pass (J2)**: mensile, circa 30 livelli, fila gratis + premium.
- **Sblocchi, misti**:
  - mazzi: alcuni col livello, alcuni nel pass, i più belli nel negozio, qualcuno con missioni o eventi;
  - tavoli: livello + monete;
  - animazioni accuso: pass e negozio;
  - emoticon: 6 di base, altre da pass e negozio;
  - avatar e cornici: traguardi, pass, negozio.
- **Video facoltativi (F4)**: raddoppiare le monete di fine partita, ricarica extra, un premio in più al giorno.

Precisazioni sulle voci già esistenti:
- **F2 Negozio**: esiste solo la grafica (`ShopScreenV2`, con dati finti di anteprima), non è collegato a niente.
- **D3 Posta**: oggi il badge è fisso a 0 e non arrivano messaggi. Deve funzionare anche per gli ospiti,
  non solo per chi ha fatto l'accesso (anche l'ospite ha un account PlayFab).
- **D2 Profilo**: nella pagina Profilo un pannello per scegliere avatar e cornice, visibili poi nel banner
  al tavolo e nel profilo rapido (I4).
- **E2 Ricompense giornaliere**: confermate, dopo J1.
- **D4 Amici**: tutta la logica (lista, richieste, invito in stanza) è da fare; la schermata c'è.

## Già aperti dalle sessioni precedenti

| # | Cosa | Stato |
|---|------|-------|
| G1 | Adattamento a schermi con proporzioni diverse | ☑ 1.93 — 1.92 tavolo su area di design 1080x1920 centrata: `CameraResponsiveFit` sulla camera (larghezza bloccata sui telefoni stretti, altezza sui tablet), `PortraitCanvasMatch` sui Canvas del tavolo, banner/pulsanti/roulette/bolle ancorati al centro (`Tools/UIV2/Apply Table Design Area`), carte in pixel del mockup (mano 272, tavolo 157: erano giganti). Verificato nel Simulator iPhone 1170x2532 e a 1080x1920. ☑ 1.93: provati in partita tablet 1536x2048 (3:4) e 1080x2400 (20:9), tutto allineato. Il limite 2,1 non è attivo (modalità "Native Aspect Ratio": il valore conta solo in Custom), quindi niente bande nere. Sui telefoni allungati il posto locale (banner, Emoji/Accuso, bolla, e con loro mano e prese) scende di metà dello spazio libero in basso, safe area esclusa, al massimo 120 px di design (`LocalSeatBottomShift`, applicato dal builder): 20:9 ≈ 120, iPhone ≈ 57, 9:16 e tablet 0 |
| G2 | Build Android e prova su dispositivo | ☐ |
| G3 | Invito tramite link in sala d'attesa | ☐ |
| G4 | Commit del lavoro fatto dopo il checkpoint b21dc11 | ☑ verificato il 23/09: `b8d7b49` e `8743964` |
| G5 | Musica ed effetti sonori | ☑ 1.82 (31 file consegnati il 17/09 e collegati; `Resources/Audio/SoundLibrary` per regolare i volumi) |
| G7 | Audio non ancora usato: premi in monete e gemme (servono con F1/E2), ui_confirm_01/03, match_start_TEMP e il doppione home_theme_loop.wav | ☐ |
| G8 | Annulla/esci da "crea stanza" e "entra in stanza privata" tornava alla Home invece che al pannello Modalità | ☑ 1.83 |
| G9 | Suoni della partita che continuavano dopo essere usciti dal tavolo | ☑ 1.83 |
| G10 | Roulette del mazziere: partiva (con i suoi suoni) mentre il caricamento copriva ancora lo schermo, e la fanfara d'inizio arrivava per prima | ☑ 1.83 |
| G6 | Sfocatura vera e velo anche dietro fine smazzata (mockup 13), fine partita ed emoticon | ☑ 1.98 fine smazzata con il tavolo sfocato + velo "Sfocatura sfondo" al posto del nero al 72% (`Tools/UIV2/Build Round Results Blur`, foto scattata prima che il pannello compaia). Fine partita lasciato così: è a schermo intero con fondo pieno (mockup 12), la sfocatura non si vedrebbe. Emoticon: il pannello non si apre più (I3) |
| H1 | Termini e Privacy leggibili in app (finestra scrollabile dai link della registrazione) | ☑ 1.85 |
| H2 | Recupero password: template configurabile, messaggio neutro, nessun errore PlayFab grezzo a schermo | ☑ 1.85 (manca il template su PlayFab: vedi sotto) |
| H3 | Privacy: dichiarati gli SDK realmente presenti (PlayFab, Photon, Google Play Games) | ☑ 1.85 |
| H4 | URL pubblici (Termini, Privacy, Elimina account, Reset password) + deploy della cartella `Web/` | ☐ **serve te** |
| H5 | Template email `51_PasswordRecovery` su PlayFab Game Manager + SMTP del titolo | ☐ **serve te** |
| H6 | Backend/serverless per reset password ed eliminazione account (Secret Key solo lato server) | ☐ **serve te** |
| H8 | La X di Termini/Privacy aperti da Accesso/Registrazione non funzionava (solo Esc) | ☑ 1.87 |
| H9 | Logo 51 della schermata iniziale in Accesso e Registrazione al posto del riquadro "51" | ☑ 1.87 |
| H10 | Da ospite già entrato: Accesso non offre più "Accedi come ospite"; Opzioni → riga Account apre la Registrazione V2 (non il vecchio pannello Ospite) | ☑ 1.88 |
| H11 | Opzioni spostate dal Profilo alla colonna della Home, sotto Posta | ☑ 1.88 |
| H12 | Pannello account per chi ha un login vero (prima il vecchio "Logout / Back" senza grafica V2) | ☑ 1.89 "Il tuo account": nome, nome utente, email (da PlayFab), ID giocatore, ESCI DALL'ACCOUNT → schermata iniziale |
| H7 | `Impostazioni → Account → Elimina account` in app, con doppia conferma | ☑ 1.86 lato app (`AccountDeletionService` → `POST {BackendBaseUrl}/api/delete-account`, `Authorization: Bearer <SessionTicket>`). Finché H6 non c'è e `BackendBaseUrl` è vuoto mostra "non ancora disponibile", mai un falso successo. 1.87: la voce compare solo dopo l'ingresso e solo con login vero (email), non per gli ospiti; prima dell'ingresso nelle Opzioni non c'è nessuna riga Account |

---

## G1 — perché il tavolo è storto nel Simulator (diagnosi del 17/09)

Il tavolo (carte, pile, feltro) è **mondo 3D**, disegnato da una camera ortografica; i banner, la
barra e i pulsanti sono **UI**. I due mondi oggi seguono regole diverse, e questo è tutto il difetto.

1. `Main Camera` di GameScene è ferma a `orthographicSize = 5`: mezza altezza fissa, larghezza
   visibile = `5 x aspect`. Cambia il telefono, cambia la larghezza del tavolo.
   - 1080x1920 (9:16) → area visibile 5,63 x 10
   - 1170x2532 (iPhone, quello del Simulator) → **4,62 x 10: 18% più stretta**
   - tablet 3:4 → 7,50 x 10: molto più larga, tutto sperduto al centro
2. I Canvas usano invece `ScaleWithScreenSize 1080x1920` con `match = 0,5`, cioè una media fra
   larghezza e altezza. Fuori dal 9:16 **UI e tavolo scivolano uno rispetto all'altro**: i banner
   non stanno più dove stanno le carte.
3. Esiste già uno script `CameraResponsiveFit` che farebbe il lavoro giusto, ma **non è attaccato
   alla camera**, ed è tarato su un'area di riferimento 12 x 8,5 (contro il 5 di adesso): non basta
   accenderlo, le posizioni del tavolo vanno riconciliate con l'area scelta.
4. ~~`androidMaxAspectRatio = 2,1` manda in bande nere i 20:9~~ Corretto il 19/09: con
   `androidSupportedAspectRatio: 1` (Native Aspect Ratio) quel valore non viene usato.

Lavoro necessario: scegliere un'area di gioco di riferimento, agganciarci la camera, allineare la
regola dei Canvas e riverificare le posizioni del tavolo a tre proporzioni (9:16, 20:9, tablet 3:4).
Non è una spunta da mettere: tocca il posizionamento di tutto il tavolo.

## Legale e account — stato al 18/09

Fatto in app (1.85), senza dipendere da niente di esterno:
- I testi stanno in `Assets/Legal/` come file, non nel codice: si aggiornano senza ricompilare.
- Dalla registrazione, "Termini di servizio" e "Privacy Policy" aprono una finestra scrollabile
  (`LegalModalV2`, costruita sul `UIV2ModalHost` e su `AnimatedModalV2` esistenti). Toccare un link
  NON spunta la casella: leggere non è accettare.
- `Assets/Resources/AppConfig.asset` raccoglie gli indirizzi pubblici e l'ID del template email.
  **Tutti i campi sono vuoti**: nessun URL è stato inventato. Qui dentro va solo roba pubblica.
- Recupero password: usa il template configurato (vuoto = quello predefinito di PlayFab) e risponde
  sempre la stessa frase, esista o no l'indirizzo, così nessuno può scoprire chi ha un account.
  Gli errori del servizio finiscono nel log, mai a schermo.
- Un segnaposto non ancora configurato non arriva mai sotto gli occhi dell'utente: sparisce insieme
  alla frase che lo conteneva.

### Due modifiche ai testi consegnati
1. Tolta dalla copia in app la nota **"Prima della pubblicazione: aggiornare questa sezione..."**:
   era un promemoria per lo sviluppatore, sarebbe finito sotto gli occhi degli utenti. L'originale
   nel pacchetto è intatto.
2. Riscritta la sezione 3 della Privacy con gli SDK **realmente presenti** nel progetto: PlayFab,
   Photon (Exit Games) e Google Play Games Services. Unity Analytics risulta disattivato
   (`UnityConnectSettings` tutto a 0) e non è dichiarato. Da rivedere se si aggiungono pubblicità,
   acquisti o crash reporting.

### Attenzione
La Privacy in app dice già che si può cancellare l'account da **Impostazioni → Account → Elimina
account**. Quel percorso **non esiste ancora** (H7) e senza backend (H6) non può funzionare.
Finché non c'è, il documento promette una cosa che l'app non fa.

## Parere sul sistema di energia

Consiglio di **non** mettere l'energia che blocca le partite, almeno all'inizio.

- **Pochi giocatori online.** Un gioco multiplayer appena uscito vive di persone in coda. Se l'energia ferma chi vuole giocare, le code si allungano e aumentano le partite con i bot.
- **Il 51 si gioca a sessioni lunghe.** Chi gioca a carte fa molte partite di fila. Fermarlo dopo 5 partite è il modo più veloce per fargli disinstallare l'app.
- **Nelle app di carte italiane funziona un altro modello:** tavoli con puntata in monete. Vinci e guadagni, perdi e paghi l'ingresso. Le monete si ricaricano con il bonus giornaliero, i video pubblicitari o il negozio. È un freno "morbido": l'allenamento contro i bot resta sempre gratis e senza limiti.
- **Guadagni migliori:** oggetti estetici (mazzi, tavoli, emoticon, animazioni accuso, banner), pass stagionale, acquisto per togliere la pubblicità e video facoltativi per raddoppiare i premi di fine partita.

**Deciso (17/09):** niente energia, E5 diventa "tavoli con puntata in monete".

## Asset mancanti da creare

**Aggiornamento 23/09 — revisione stile UI:** lista di produzione e specifiche in
[`docs/ui/asset-refresh-brief.md`](docs/ui/asset-refresh-brief.md).
Confermato: «Indietro» nelle schermate e «Chiudi» nei popup, nello stile del gioco.
Le altre scelte del brief sono proposte di brainstorming; nessun nuovo asset ancora prodotto.

| Priorità | Da preparare | Direzione |
|---|---|---|
| P0 | Campione coordinato: navigazione + Premio + Classifica + Posta | Validare insieme scala, materiali e leggibilità prima di produrre il set completo |
| P1 | Chevron opzionale per «Indietro» | Panna, morbido, senza disco di legno, cornice o riquadro; testo separato |
| P1 | Scrigno, coppa, busta | Silhouette autonome senza quadrato blu; oro e blu coerenti, etichetta sotto |
| P1 | Ingranaggio, audio, aiuto, uscita, rimozione | Un'unica famiglia semplice; uscita/rimozione distinte dalla chiusura |
| P2 | Carte, negozio, profilo/amici | Riallineare peso e materiali alle icone approvate, riusando ciò che funziona |
| P2 | Indicatore di selezione, badge notifica, divisore | Piccoli supporti discreti; evitare una nuova cornice intorno a ogni elemento |

**Non serve un PNG «Chiudi»:** testo localizzabile e componente esistente da adattare.
Rimozione dei riquadri, area cliccabile, stati premuto/disabilitato e disposizione dei
comandi sono lavoro UI, non illustrazioni da commissionare. Dettagli e criteri nel brief.

### Consegne precedenti

Consegnati il 25/09 (dopo la revisione 2.20): 2 fogli icone crema 4x2, kit tavolo, sfondo sala (poi riconsegnato 2x),
mazzo Barocco (collegato nella 2.21); il resto è collegato nella 2.22. File in `DragonsHoard/sprites_unity/sprites_unity/Immagine ChatGPT 25 set 2026*`.

Consegnati il 17/09: Poppins Regular/Medium/SemiBold, bagliore morbido cerchio e rettangolo, velo "Sfocatura sfondo" (la sfocatura vera si fa via codice sotto al velo, fatta nella 1.81).

Consegnati il 17/09 anche i 31 file audio (musica + effetti), collegati nella 1.82.

Consegnato il 17/09: `ic_arrow_left` (pulsante indietro tondo, usato in 23 e 24).

Consegnato il 18/09: pacchetto legale/auth (`51_Legal_Auth_Package`) con Termini, Privacy, template
email PlayFab, pagine web e riferimenti backend. Termini e Privacy sono in app dalla 1.85.

Ancora da fare, ma NON sono asset da disegnare: vedi "Legale e account" qui sotto.
