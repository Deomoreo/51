# 51 — Collegamento della nuova UI

Ricognizione del 16 settembre 2026, ripresa dalla conversazione «Aiuto con Photopea».
Base esaminata: commit `521b34e`, Unity 2022.3.60f1, destinazione Android.

## Obiettivo

Collegare la UI V2 alla logica esistente e completare i mockup una funzione alla volta.
Per ogni funzione distinguere: grafica presente, collegamenti reali, verifica in Play Mode.
La presenza di un prefab non certifica la fedeltà grafica né il funzionamento.

## Ricognizione iniziale (prima dell'integrazione)

- Collegamento MCP all'Editor funzionante; progetto locale `51` corretto.
- Scena aperta: `Assets/UIV2/Tests/UIV2_ProfileV2_Preview.unity`, salvata, fuori dal Play Mode.
- La scena contiene EventSystem, UIV2_Root e ProfileV2PreviewBootstrap.
- La shell ha Canvas, CanvasScaler, GraphicRaycaster, UIV2Root e UIV2ModalHost.
- Nessuna compilazione in corso. La lettura della Console non ha restituito errori o warning; non equivale a un test del gioco.
- Le scene abilitate nella build sono ancora MainMenu e GameScene.
- Home, Collezione, Profilo e Negozio hanno prefab e scene di anteprima separati; Amici ha un prefab di esempio.
- I bootstrap di anteprima alimentano le schermate con dati dimostrativi. Le azioni principali producono log anziché richiamare i servizi dell'app.
- Non sono state modificate scene, prefab o script. Non sono state eseguite partite o chiamate ai servizi account.

## Inventario dei mockup e delle schermate

Percorsi dei mockup relativi ad `Assets/Mockup/`.
«Da costruire in V2» indica l'assenza di una schermata dedicata nella cartella V2 esaminata; può esistere una versione legacy da riutilizzare.

| Schermata / mockup | Stato rilevato | Lavoro successivo |
|---|---|---|
| Home — `home_B2 (1).png` | HomeScreenV2 e anteprima presenti; riferimento citato dal builder | Collegare navigazione, modalità e Gioca; verificare grafica e area video |
| Accesso — `01_schermata_iniziale.png` | AuthUIController e AuthBootstrapper presenti in MainMenu | Conservare l'accesso esistente e collegarlo alla shell V2; verificare grafica |
| Caricamento — `02_caricamento.png` | Nessuna schermata V2 dedicata | Collegare stati di caricamento, errore e riprova |
| Collezione mazzi — `20_collezione_carte.png` | CollectionScreenV2, DeckCard e pannello mazzi presenti | Catalogo reale, selezione, persistenza, fronte/retro in partita |
| Collezione emoticon — `21_collezione_emoticon (1).png` | Pannello e slot presenti; azioni demo | Collegare disponibilità, equipaggiamento e utilizzo in partita |
| Collezione accusi — `22_collezione_accusi (1).png` | Pannello e righe presenti; azioni demo | Collegare disponibilità, selezione e anteprima reale |
| Profilo — `17_profilo.png` | ProfileScreenV2 presente; bootstrap demo | Collegare identità, statistiche, progressi e azioni disponibili |
| Negozio — `16_negozio.png` | ShopScreenV2 e schede prodotti presenti; azioni demo | Definire catalogo, prezzi, proprietà e acquisti prima di abilitare transazioni |
| Amici — `07_amici.png` | FriendsScreenV2 e FriendRow, esempio della foundation | Verificare fedeltà e servizi social disponibili |
| Impostazioni — `05_impostazioni.png` | Da costruire in V2; esiste SettingsModalUI legacy | Collegare preferenze effettivamente supportate |
| Posta — `06_posta.png` | Da costruire in V2; esiste il modello dei dati | Definire fonte messaggi e azioni supportate |
| Premi — `08_premi.png` | Da costruire in V2; esiste il modello dei dati | Collegare disponibilità e riscossione reale |
| Classifica — `18_classifica.png` | Da costruire in V2; esiste il modello dei dati | Collegare classifica, caricamento, vuoto ed errori |
| Ricerca partita — `screen_1_ricerca_partita.png` | Da costruire in V2; MatchmakingStatusUI in MainMenu | Riutilizzare matchmaking esistente, annullamento e gestione errori |
| Crea stanza — `screen_2_crea_stanza.png` | Da costruire in V2; logica in GameLaunchController | Collegare opzioni, creazione e transizione alla sala |
| Inserisci codice — `screen_3_entra_codice.png` | Da costruire in V2; JoinRoomPopupUI presente in MainMenu | Riutilizzare ingresso stanza e messaggi di errore |
| Sala host — `03_crea_stanza_con_bot.png` | Da costruire in V2; WaitingRoomUI presente in MainMenu | Collegare giocatori, bot, stato pronto e avvio |
| Sala ospite — `04_lobby_non_host.png` | Da costruire in V2; stessa logica sala esistente | Separare controlli host/ospite e verificare uscita |
| Tavolo — `09_tavolo_di_gioco.png`, `09_tavolo_v4 (1).png` | GameScene esiste; cartella GameHUD V2 riservata | Confrontare HUD esistente e varianti del mockup prima di intervenire |
| Profilo rapido — `10_profilo_rapido.png` | Da costruire in V2 | Collegare dati del giocatore selezionato |
| Accuso in partita — `11_tavolo_accuso_v2.png` | Logica gameplay esistente; nessun HUD V2 dedicato | Riutilizzare regole e flusso accusi, aggiornare presentazione |
| Fine partita — `12_fine_partita.png` | Da verificare rispetto al flusso risultati esistente | Distinguere conclusione del match e della smazzata |
| Fine smazzata — `13_fine_smazzata.png` | RoundEndPanel presente in GameScene | Collegare punteggi reali e comandi continua/menu alla nuova grafica |
| Emoticon in partita — `15_pannello_emoticon.png` | Nessun HUD V2 dedicato | Collegare gli slot equipaggiati e l'invio in partita |

Anche i selettori Modalità e Mazzo richiedono collegamenti: HomeScreenV2 espone gli eventi OnModePressed e OnDeckPressed, ma non implementa da solo il relativo flusso.

## Ordine dei lavori

### 1. Ricognizione — completata per struttura e collegamenti

Questo documento costituisce la lista iniziale. La verifica grafica dettagliata va svolta schermata per schermata. Il tentativo di acquisire l'anteprima Profilo fuori dal Play Mode non ha prodotto un'immagine perché la scena non ha una Camera; non è un errore dimostrato della UI Canvas.

### 2. Primo percorso: accesso → Home V2 → allenamento → Home V2 — implementato

Intervento delimitato:

1. Integrare la shell e Home V2 nel flusso di MainMenu, mantenendo i servizi di autenticazione e il controller di avvio esistenti.
2. Collegare OnPlayPressed a GameLaunchController.Launch con una MatchConfig di allenamento valida; mantenere coerenti il testo della modalità e la configurazione effettiva.
3. Collegare il selettore modalità all'interfaccia già disponibile, verificando i suoi riferimenti reali prima di riutilizzarla.
4. Alimentare l'identità visibile con i dati disponibili; evitare valori demo presentati come saldo o progresso reale.
5. Verificare il ritorno: AppFlowManager.GoToMainMenu carica MainMenu, quindi deve ripristinare la Home V2 nel flusso integrato.

Completamento: dall'accesso si raggiunge la Home V2, si avvia un allenamento, si vedono le carte e si ritorna alla Home; un secondo avvio funziona senza listener duplicati o errori. Validare anche annullamento del selettore e configurazione della difficoltà. Nessuna partita online è necessaria per questa prima consegna.

#### Consegna del primo percorso

- `MainMenu.unity` contiene ora la shell Home V2 con i prefab originali Home, TopBar e BottomNav. Le anteprime restano separate.
- `HomeV2Integration` collega Gioca a GameLaunchController e Modalità al pannello esistente. La selezione iniziale è allenamento a quattro giocatori; le scelte successive restano in memoria nella sessione.
- Carte, Negozio e Profilo restano raggiungibili nelle rispettive pagine legacy; tornare a Gioca ripristina la Home V2. L'integrazione delle versioni V2 di queste pagine è il passo successivo.
- L'accesso ospite conserva il flusso esistente. Il ritorno dal tavolo passa direttamente alla Home, mentre un nuovo avvio mantiene la schermata di accesso.
- Il nome viene dal servizio di autenticazione; XP/livello usano PlayerProgressLocal se disponibile. Saldi ed energia demo non vengono mostrati. Mazzi, Premio, Classifica e Posta sulla Home restano disabilitati fino al loro collegamento.
- L'annotazione tecnica del video è nascosta soltanto nell'istanza della Home integrata; lo sfondo video resta da completare.
- Corretto un difetto rilevato durante la prova: MatchConfigStorage perdeva le regole della modalità nel cambio scena. Ora conserva anche le quattro opzioni MatchRules, con fallback compatibile per preferenze precedenti.

Verifiche: compilazione in Unity; accesso ospite; raycast dei pulsanti Modalità e Gioca; apertura e annullamento del pannello; allenamento 1v1 Difficile fino a fine smazzata; pulsante Menu Principale e ritorno alla Home; navigazione legacy nelle tre sezioni; nuovo avvio con regole 1v1 e difficoltà Hard confermate dentro GameScene. Le mosse del giocatore nella prova sono state inviate tramite il metodo usato dalla UI, lasciando eseguire i bot al gioco.

Test EditMode: **9 superati, 0 falliti** — quattro nuovi test di MatchConfigStorage e cinque esistenti sulla difficoltà AI. I test di persistenza ripristinano le preferenze precedenti al termine.

Controllo finale: due smazzate 1v1 completate (la seconda dopo la correzione delle regole); successivo riavvio nella stessa sessione a quattro giocatori con difficoltà Facile; un solo listener su Gioca, una sola Home e un solo EventSystem. Tornando da Carte, la barra evidenzia nuovamente Gioca. Ultima lettura Console: nessun errore. Unity lasciato in Play Mode sulla Home; modifiche salvate sul ramo locale `codex/home-v2-training`, senza commit o push.

Limiti: non verificati login con credenziali, build Android, acquisti o partite online tra due client. Selettore modalità, tavolo e risultati mantengono la grafica legacy/provvisoria. Questa consegna rende funzionante il percorso; non certifica il completamento grafico dell'app.

### 3. Navigazione e mazzi

- Collegare la barra inferiore a Home, Collezione, Negozio e Profilo.
- Collegare Classico e Corte di Giada alla selezione e al caricamento delle carte effettive.
- Verificare fronte, retro, corrispondenza seme/valore e comportamento della Matta.
- Confermare la selezione dopo riavvio e ritorno dalla partita.

### 4. Profilo e impostazioni

Riutilizzare ProfileService e PlayerProgressLocal dopo aver verificato quale sia la fonte autorevole per ogni dato. Collegare soltanto le azioni realmente supportate; completare la grafica delle impostazioni sul mockup.

### 5. Online e stanze

Integrare ricerca, creazione, codice e sala d'attesa nella nuova UI. Verificare con due client avvio, uscita, errori e disconnessione. Controllare separatamente le regole 2v2 prima di dichiarare supportato il formato.

### 6. Completamento delle schermate restanti

Un mockup alla volta: Amici, Posta, Premi, Classifica, HUD, profilo rapido, emoticon e risultati. Per Negozio definire prima il comportamento economico. Aggiungere o rifinire le animazioni durante la verifica di ciascuna funzione.

## Riferimenti tecnici essenziali

- `Assets/UIV2/Scripts/Screens/HomeScreenV2.cs`: eventi pubblici della Home.
- `Assets/UIV2/Tests/HomeV2PreviewBootstrap.cs`: il pulsante Gioca attualmente produce solo un log.
- `Assets/UIV2/Tests/*V2PreviewBootstrap.cs`: dati e azioni dimostrativi delle altre schermate.
- `Assets/Scripts/UI/GameLaunchController.cs`: Launch, configurazione e avvio allenamento.
- `Assets/Scripts/Gameplay/AppFlowManager.cs`: ritorno a MainMenu.
- `Assets/Scripts/Gameplay/CardViewManager.cs`: caricamento attuale da Resources/Cards o sprite assegnati.
- `Assets/Scripts/Auth/ProfileService.cs`: profilo e campo SelectedDeck disponibili, da collegare alla V2.
- `ProjectSettings/EditorBuildSettings.asset`: scene attive della build.

## Attenzione ai documenti storici

`PROJECT_STATUS.md` e `Assets/UIV2/README.md` descrivono fasi precedenti. Non usarli come fotografia dello stato attuale: per esempio JoinRoomPopupUI e WaitingRoomUI risultano oggi in MainMenu, mentre il vecchio audit li dichiarava assenti. Prima di correggere un problema storico, riprodurlo nello stato corrente.

#### Consegna Collezione V2 e selezione mazzi

- La voce Carte e il selettore Mazzo della Home aprono Collezione V2. Sono selezionabili Napoletano, Classico e Corte di Giada; le schede mostrano il mazzo in uso. Emoticon e Accusi restano disabilitati in attesa dei rispettivi collegamenti.
- La selezione viene salvata localmente in SelectedDeckId e copiata nella configurazione della partita al momento di Gioca. Il catalogo carica le 40 facce del mazzo richiesto tramite una definizione separata; le immagini originali non sono duplicate.
- Le carte di mano, tavolo, avversari e prese usano facce e dorsi del mazzo della partita. La Matta usa il valore temporaneo nello stesso stile, conservando identità e faccia originale del 7 di Coppe.
- Uniformate tramite Unity le dimensioni delle 80 nuove facce e del dorso Giada (altezza mondo 1,8). Preservate le proporzioni nelle anteprime della Collezione e corretta la sovrapposizione tra ventaglio e nome.
- Classico usa temporaneamente il dorso Napoletano: il pacchetto importato non contiene un dorso dedicato. L'anteprima Classico mostra l'Asso di Denari per distinguerlo.

Verifiche: **13 test EditMode superati, 0 falliti** (4 catalogo/selezione, 4 persistenza configurazione, 5 difficoltà AI); controllo delle 120 associazioni seme/valore e delle dimensioni. In Play: selezione tramite pulsante USA con controllo del puntamento; Home aggiornata; smazzata 1v1 completa con Giada e ritorno tramite Menu Principale; due pile delle prese con dorso Giada; verifica dei 10 valori temporanei della Matta e del ripristino del 7 di Coppe; riavvio Play con selezione conservata. Secondo avvio con Classico: 7 facce e 3 dorsi corrispondenti al catalogo e verifica dei 10 valori Matta.

Limiti di questa consegna: selezione locale, senza sincronizzazione del profilo online; i tre mazzi inclusi sono disponibili per la prova, senza acquisti. Non eseguita build Android. Grafica del tavolo, HUD e pulsanti provvisori ancora da rifinire nei passi successivi.

Controllo conclusivo: ritorno alla Home da Classico, navigazione Negozio e Profilo legacy, ritorno alla Collezione V2; una sola Home, Console senza errori. Unity lasciato in Play Mode sulla Collezione con Classico selezionato. Modifiche locali salvate sul ramo codex/home-v2-training, senza commit o push.

### Consegna Profilo V2 e impostazioni essenziali

- Profilo V2 integrato nel MainMenu: identità e ID dal servizio di autenticazione, livello/XP e partite/vittorie dal profilo PlayFab caricato. La TopBar usa la stessa progressione. Dopo login/registrazione il profilo viene ricaricato; dati appartenenti a un altro ID non sono presentati come statistiche del nuovo account.
- Distinti dati assenti (trattino e progressi non disponibili) da valori realmente pari a zero. Scope, Settebello e record non sono ancora alimentati; trofei e condivisione restano non disponibili. Lo spazio dei trofei vuoti è ridotto nell'istanza integrata.
- REGISTRATI apre il modulo esistente; i pulsanti Indietro riportano al Profilo. Non sono stati creati account né inviate credenziali durante le prove.
- Il pannello Impostazioni riutilizza la grafica dell'overlay HomeScreen senza modificare quella scena. L'ingranaggio del Profilo lo apre, X/sfondo/Indietro lo chiudono. Il pannello copre e blocca la navigazione sottostante.
- Audio del gioco controlla realmente AudioListener.volume e salva Settings_GameAudio; la scelta viene applicata anche all'avvio e resta valida nel cambio scena. Un solo controllo generale: non esiste ancora una separazione musicale tramite mixer. Musica separata, vibrazione, animazioni veloci, notifiche e supporto restano disabilitati e indicati come non disponibili. Lingua mostra Italiano; la riga account apre il flusso account esistente.

Verifiche: **10 test EditMode superati, 0 falliti** (2 nuovi test di visualizzazione profilo, 4 catalogo/selezione mazzi, 4 persistenza configurazione). In Play: caricamento effettivo del profilo ospite da PlayFab, coerenza nome/XP, pulsanti ingranaggio e registrazione raggiungibili, scroll fino alle azioni, apertura account e ritorno senza registrazione; audio disattivato e mantenuto dopo chiusura/riapertura, riavvio Play e ingresso in GameScene.

Limiti: le partite di allenamento non scrivono ancora risultati/XP nei servizi di profilo; i contatori mostrano quanto restituisce il servizio, non una nuova cronologia degli allenamenti. Non provati login/registrazione con credenziali, logout, condivisione, avatar personalizzati o build Android. Fedeltà grafica completa delle impostazioni da rifinire sul mockup.

Controllo finale: una sola istanza Profilo, pannello impostazioni blocca i comandi sottostanti, chiusura tramite sfondo verificata. Ripristinata la preferenza audio precedente alle prove. Console senza errori; Unity lasciato in Play Mode sul Profilo. Scena salvata e modifiche locali, senza commit/push.

### Consegna avvio, caricamenti, pannelli rapidi e swipe

- Nuova schermata iniziale con il logo 51 esistente, accesso ospite, login, registrazione e Opzioni. Sostituisce il vecchio invito a toccare lo schermo. Novità resta disabilitato.
- Caricamento a schermo intero con logo, carte animate e messaggio di stato: ingresso nella Home, richieste di autenticazione, apertura del tavolo offline e ritorno. I cambi scena sono asincroni; la percentuale segue il caricamento della scena, mentre l'attesa di autenticazione usa un indicatore senza percentuale. Errori di ingresso/caricamento offrono Riprovare e Indietro.
- Il ritorno dalla partita apre direttamente la Home. Il caricamento persistente evita duplicati quando MainMenu viene ricreato. Le schermate iniziale e di caricamento adattano il contenuto all'area sicura dello schermo.
- I pannelli rapidi Modalità e Mazzo riusano gli overlay di riferimento, con apertura/chiusura animate. Mazzo ora apre il pannello rapido, aggiornando l'anteprima senza cambiare la scelta salvata fino a USA QUESTO; chiudere annulla l'anteprima. Carte continua ad aprire la Collezione completa. Modalità collega formati, allenamento, difficoltà e azioni delle stanze esistenti.
- Le quattro pagine della barra inferiore scorrono con animazione sia al tocco sia con swipe orizzontale, anche sulla barra stessa. Lo scorrimento verticale di Profilo e Carte rimane separato. Pannelli, autenticazione e caricamento bloccano la navigazione sottostante. Il Negozio mostra un messaggio di disponibilità futura.
- Gli ospiti non mostrano livello, XP o barra esperienza nel Profilo e nella TopBar. Il lavoro su assegnazione XP e registrazione risultati resta rinviato come richiesto.

Verifiche: **16 test EditMode superati, 0 falliti**, incluso il caso di dati XP presenti per un ospite ma non visualizzati. In Play: ingresso ospite; apertura/ritorno dai moduli login e registrazione; Opzioni visibile sopra l'avvio; scelta/annullamento/conferma mazzo; selezione allenamento 1v1 Difficile e relative regole; raggiungibilità della sezione stanze tramite scroll; swipe tra pagine e sulla barra, scroll verticale, limiti prima/ultima pagina, swipe breve, tocchi rapidi e blocco con pannello aperto. Prova di scena mancante con recupero tramite Indietro. Avvio dal vero pulsante Gioca con Giada, controllo delle 10 carte visibili rispetto al catalogo, ritorno alla Home con una sola istanza di caricamento/Home/EventSystem e impostazioni ancora funzionanti. Console senza errori; ripristinato il mazzo precedente alle prove.

Limiti: verifica in Unity Editor, senza build Android o gesture su dispositivo. Non sono state inviate credenziali né creati account. Il caricamento multiplayer gestito da Photon e il flusso stanze richiedono ancora una prova con due client; questa consegna non completa il multiplayer. Grafica del tavolo e risultati resta quella precedente. Modifiche salvate localmente, senza commit o push.

Controllo visivo finale: corretta la conversione dell'area sicura del Device Simulator dalla risoluzione nativa 1170×2532 alla vista 1080×1920, riusando SafeAreaUtil. Logo, Opzioni e contenuti del caricamento verificati dentro lo schermo. Unity lasciato in Play Mode sulla nuova schermata iniziale.
