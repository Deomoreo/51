# Stato del progetto "51" (Cirulla) — Audit onesto

Data audit: 2026-08-25. Metodo: analisi statica del codice/scene + verifica compilazione dal vivo. **Nessun test automatico è stato eseguito** (vedi Fase 1) e **nessuna sessione multiplayer reale è stata giocata** — tutte le classificazioni sotto sono dedotte leggendo codice YAML/C#, non osservando comportamento a runtime, salvo dove indicato diversamente.

---

## Fase 1 — Test automatici: cosa fanno DAVVERO

**Non sono stati eseguiti.** Al momento dell'audit il tuo editor Unity aveva il progetto già aperto (sessione interattiva dalle 18:17); lanciare un secondo processo Unity in batch mode avrebbe richiesto chiuderlo, e su tua indicazione esplicita ho evitato di forzarlo. Quindi: **nessun conteggio reale di pass/fail/skip.**

Quello che ho potuto verificare:
- **Compilazione**: letto l'`Editor.log` della tua sessione live. Nessun `error CS`, `AssetDatabase: script compilation time: 1.2s` completato, `LogAssemblyErrors (0ms)` (zero errori da loggare). La build degli assembly (`Assembly-CSharp.dll`, `Assembly-CSharp-Editor.dll`) risulta aggiornata a dopo l'ultima modifica dei file .cs modificati. **Il codice attuale compila senza errori** — verificato, non dedotto.
- **Conteggio metodi di test**: 16 file in `Assets/Tests/Editor/`, **143 metodi `[Test]`/`[UnityTest]` totali** (contati via grep, non eseguiti).

### Copertura per area (dedotta dai nomi dei metodi di test, non dall'esecuzione)

| Area | Copertura | File principali |
|---|---|---|
| Cattura base (uguale/somma/15/PlayOnly/forzata) | Ampia | Rules51CoreTests, Rules51ExtraTests, Rules51RestoredTests, Rules51ValidMovesTests, AceAndCaptureRulesTests, CirullaRulesTests |
| Asso (cattura singola/tutte/PlayOnly) | Ampia | AceAndCaptureRulesTests, Rules51CoreTests, Rules51ExtraTests, Rules51RestoredTests |
| Matta (jolly) | Molto ampia | MattaAccusiCombinationsTests (33 test da solo), MattaAndAccusiRulesTests, MattaVisualHintsTests, Rules51CoreTests, Rules51RestoredTests |
| Accusi (Cirulla, Decino) | Ampia | AccusiCheckerTests, AccusiAndPunteggioComprehensiveTests, CirullaRulesTests, MattaAccusiCombinationsTests |
| Punteggio fine smazzata (Scopa, Sette Bello, Denari, Carte, Primiera, Grande, Piccola, **Cappotto**) | Buona | AccusiAndPunteggioComprehensiveTests, PunteggioManagerTests, RoundManagerEdgeTests |
| CirullaAI (bot) | Buona | CirullaAITests (10 test: scelta mossa, preferenze Medium/Hard, check generico su tutte le difficoltà) |
| Flusso end-to-end di una smazzata | Presente ma minimo | Rules51IntegrationTests (3 test) |

### Aree SENZA nessun test (verificato via grep, zero occorrenze)
- **Networking/multiplayer**: `NetworkGameController`, `MatchmakingManager`, `RoomManager` — mai referenziati in `Assets/Tests/Editor/`.
- **UI**: nessun test su `HomePanelUI`, `GameLaunchController`, `ModalitySelectorPanelUI`, `PrivateRoomOptionsUI`, `JoinRoomPopupUI`, `WaitingRoomUI`.
- **`TurnController.EndRound()`**: ha un TODO ("Implement full scoring...") ma è codice morto, mai chiamato — non testato perché non è mai eseguito nemmeno dal gioco stesso.
- **Logica di squadra (2v2)**: non testata perché non esiste nel motore (vedi Fase 3).
- **Partita completa multi-smazzata** (un match a punteggio target, non una singola mano): non coperta, solo `FullSmazzata_Simulated_Play_Sequence` su una singola smazzata.
- `MattaVisualHintsTests` usa reflection per invocare metodi privati di `TurnController` — non potendo eseguire i test, non è verificato se questi 3 test passino davvero o si rompano silenziosamente.

---

## Fase 2 — Le 3 scene: cosa c'è davvero dentro

### MainMenu.unity (26.532 righe) — in Build Settings, enabled
Canvas/Panel principali: `HeaderModePanel`, `AccountPanel`, `GuestPanel`, `RegisterPanel`, `TapToEnterPanel`, `LoginPanel`, `MatchmakingStatusUI`, `UIBackgroundModePanel`.
Script presenti: `GameLaunchController`, `MatchmakingStatusUI`, `ModalitySelectorPanelUI`, `MatchmakingManager`.

**Scoperta più importante di tutto l'audit**: `PrivateRoomOptionsUI`, `JoinRoomPopupUI` e `WaitingRoomUI` **non sono componenti di nessun GameObject in nessuna scena né in nessun prefab del progetto**. I campi corrispondenti in `GameLaunchController.cs` (righe 20-21 e simili) sono quindi `null` a runtime in `MainMenu.unity`. Il codice è protetto da `if (x != null)`, quindi non crasha — semplicemente non mostra mai nulla. Confermato indipendentemente da `Assets/Scripts/LOBBY_SETUP_README.md`, presente nel repo: è una checklist di setup manuale (creare i prefab, assegnarli agli slot) **mai completata**.

Placeholder testuali minori: `StatusText`, `VersionTxt`, `GuestInfoText`, `AccountNameTxt`, `AccountStatus` con testo di default "New Text" (probabilmente popolati a runtime da script, non verificato), più 15 oggetti `Text (TMP)` mai rinominati.

### GameScene.unity (3.550 righe) — in Build Settings, enabled
Canvas/Panel: `GameCanvas`, `AccusoPanel`, `MoveSelectionUI`, `RoundEndPanel`.
Script presenti: `GameSceneInitializer`, `TurnController`, `NetworkGameController`, `AccusoPanelController`.

`AccusoUIBridge` (modificato oggi secondo git status) **non è presente in nessuna scena né prefab** — stesso pattern di orfanità della UI stanze private, ma qui su un file di gameplay core. Placeholder minori: solo 2 "New Text" + 2 "Text (TMP)".

### TESTPHOTN.unity (4.698 righe) — in Build Settings ma **enabled: 0** (esclusa dalla build)
Contiene solo UI di debug Photon grezza (`NetworkTestUI`, bottoni "Create Room"/"Connect"/"Add Bot"/"Ready"), zero riferimenti al flusso reale (`HomePanelUI`, `GameLaunchController`, `MatchConfig`).

**Valutazione**: materiale di test superato, sicuro da buttare come scena. **Unica cautela**: esercita `RoomManager.cs`, che contiene l'unica logica di bot-replacement su disconnessione esistente nel progetto (oggi orfana rispetto al gameplay reale, vedi Fase 3). Prima di eliminare la scena, valuta se salvare/migrare quella logica altrove.

---

## Fase 3 — Percorsi reali: tabella riassuntiva

| Percorso | Stato | Nota sintetica |
|---|---|---|
| Training 1vs1 (BOT) | ⚠️ PARZIALE | Percorso completo, ma la difficoltà bot scelta dall'utente è ignorata |
| Training 2vs2 (BOT) | ⚠️ PARZIALE | Come sopra + nessuna logica di squadra: è un 4-player rietichettato |
| Training FourPlayers (BOT) | ✅ FUNZIONANTE | Formato nativo del motore; bug difficoltà bot si applica ma non blocca |
| QuickMatch OneVsOne | ❌ MANCANTE | Matchmaking Photon funziona, ma la scena di destinazione `"LobbyScene"` non esiste nel repo |
| QuickMatch TwoVsTwo | ❌ MANCANTE | Stesso blocco + nessuna logica di squadra |
| QuickMatch FourPlayers | ❌ MANCANTE | Stesso blocco scena mancante |
| Matchmaking insufficiente | ❌ MANCANTE | Nessun timeout, nessun fallback bot: attesa potenzialmente infinita, uscita solo manuale |
| Creazione stanza privata | ❌ MANCANTE (nella pratica) | Codice genera davvero il codice stanza su Photon, ma `WaitingRoomUI` che dovrebbe mostrarlo non esiste in scena |
| Ingresso in stanza con codice | ❌ MANCANTE (nella pratica) | `JoinRoomPopupUI` non esiste in scena: l'utente non può nemmeno digitare un codice |
| WaitingRoomUI (lista giocatori) | ❌ MANCANTE (nella pratica) | Codice solido (eventi Photon reali) ma componente assente da ogni scena |
| Disconnessione a metà partita | ❌ MANCANTE | Logica di bot-replacement esiste in `RoomManager.cs` ma è orfana, mai referenziata da `TurnController`/`NetworkGameController` |

### Dettaglio single player

**Training 1vs1**: percorso verificato per intero — `HomePanelUI.OnStartGame()` → `GameLaunchController.Launch()`/`StartTrainingMatch()` → `MatchConfigStorage.Save()` + `SceneManager.LoadScene("GameScene")` → `GameSceneInitializer.Awake()`/`Start()` → `TurnController.StartNewGame()` → `Rules51.CreateNewGame()` → bot via `CirullaAI.ChooseMove()`. **Bug trasversale a tutti e 3 i formati training**: `BotDifficulty` (scelto in UI, salvato in `MatchConfig`) non arriva mai a `TurnController`, che usa invece un campo serializzato separato `aiDifficulty` (enum `AIDifficulty`, diverso da `BotDifficulty`, mai sincronizzato — nessuna conversione trovata in tutto `Assets/Scripts`). **La selezione difficoltà nella UI è cosmetica.**

**Training 2vs2**: stesso percorso, `GameFormat.TwoVsTwo` mappa a `PlayerCount=4` in `MatchConfig.cs:60`, ma zero logica di squadra in `Rules51`, `PunteggioManager`, `RoundManager`, `GameState`, `CirullaAI` (grep confermato, zero risultati fuori dai file UI di selezione). Nessun pairing compagni, nessun punteggio di squadra, nessuna condizione di vittoria di squadra.

**Training FourPlayers (1vs3)**: è il formato per cui il motore è stato scritto nativamente — percorso completo e funzionante, a parte il bug difficoltà bot condiviso.

**Scoperta accessoria**: `TurnController.cs` righe 695-721, `EndRound()`, contiene un TODO ("Implement full scoring: Scopa, Sette Bello, Primiera, Denari, Cards, Grande, Piccola, Cappotto") ma il metodo **non è mai chiamato da nessuna parte** — è dead code fuorviante. Il punteggio reale funziona tramite `RoundEndPanel.Show()` → `PunteggioManager.CalculateSmazzataScores()`.

### Dettaglio multiplayer online

`GameLaunchController.cs` righe 26-27 definisce `quickMatchSceneName = "LobbyScene"` e `privateRoomSceneName = "WaitingRoom"` (confermati identici anche nei valori serializzati in `MainMenu.unity`). **Nessuno dei due file `.unity` esiste nel repository** — solo `MainMenu.unity`, `GameScene.unity`, `TESTPHOTN.unity` esistono, e le uniche scene in Build Settings sono `MainMenu`/`GameScene`. `GoToSceneForConfig()` usa questi nomi per qualunque `MatchIntent` diverso da `Training`: anche se il matchmaking Photon riesce perfettamente (ed è implementato correttamente: `StartQuickMatch` → `JoinOrCreateRandomRoom` → `OnMatchFound`), il caricamento scena successivo punta al vuoto. **La partita online non può concretamente iniziare tramite questo percorso**, indipendentemente dal formato.

**Matchmaking insufficiente**: `MatchmakingManager.cs` non contiene alcuna `Coroutine`, `WaitForSeconds`, timeout o fallback a bot (grep mirato su `FillWithBots|AddBot|Timeout`: l'unico riscontro è in `RoomManager.cs`/`NetworkTestUI.cs`, entrambi orfani). Se la stanza resta sotto la soglia minima, lo stato resta `MatchmakingState.WaitingForPlayers` indefinitamente; l'unica uscita è l'utente che preme "Annulla".

### Dettaglio stanze private

Qui si sommano due problemi distinti e indipendenti:

1. **Wiring eventi duplicato/ambiguo nel codice**: esistono 3 meccanismi paralleli per "crea stanza privata" (`PrivateRoomOptionsUI.OnCreateRoomRequested`, `ModalitySelectorPanelUI.OnCreatePrivateRoomSelected`, `ModalitySelectorPanelUI.OnCreatePrivateRoomRequested`) — solo il terzo ha un sottoscrittore reale in `GameLaunchController.cs:48`. Stessa cosa per l'ingresso in stanza (`OnJoinPrivateRoomSelected` orfano vs `OnJoinPrivateRoomRequested` sottoscritto).
2. **I componenti UI non esistono fisicamente in `MainMenu.unity`** (Fase 2): `PrivateRoomOptionsUI`, `JoinRoomPopupUI`, `WaitingRoomUI` non sono su nessun GameObject di nessuna scena.

Il secondo problema rende il primo in gran parte accademico: anche seguendo il percorso "giusto" (quello sottoscritto), il codice arriva a chiamare `ShowWaitingRoom()`/`ShowJoinRoomPopup()` su riferimenti `null`, che sono no-op silenziosi per via dei null-check. Concretamente: `MatchmakingManager.CreatePrivateRoom()` genera davvero un codice stanza a 5 caratteri e lo registra su Photon (backend funzionante), **ma l'utente non lo vede mai** perché non c'è alcun pannello in scena che lo mostri. Allo stesso modo l'utente non può digitare un codice per entrare in una stanza, perché il popup relativo non esiste in scena.

Per la disconnessione a metà partita: `RoomManager.cs` (righe 538-589, 623-705) contiene una logica reale e non banale di sostituzione con bot su `OnPlayerLeftRoom`, propagata via RPC. Ma né `NetworkGameController.cs` né `TurnController.cs` — i controller effettivamente usati in `GameScene.unity` — referenziano mai `RoomManager`, i suoi `PlayerSlots`, o eventi Photon di disconnessione (zero occorrenze, grep mirato). `RECONNECT_GRACE_PERIOD = 60f` è dichiarata in `NetworkTypes.cs:165` ma mai usata altrove. Ipotesi più probabile basata sul codice: **il gioco resta bloccato in attesa del turno del giocatore disconnesso** — richiede conferma pratica con almeno 2 dispositivi reali, non verificabile staticamente con certezza assoluta.

---

## Blocchi critici

Ordinati: prima quello che blocca il single player, poi il multiplayer online, poi le stanze private.

### Single player
1. **Difficoltà bot ignorata** (tutti e 3 i formati training) — `TurnController.cs:50` usa `aiDifficulty` (enum `AIDifficulty`) invece di leggere `MatchConfig.BotDifficulty`; nessuna conversione tra i due enum esiste. La UI di selezione difficoltà è cosmetica.
2. **2v2 senza logica di squadra** — `GameFormat.TwoVsTwo` è un 4-player free-for-all rietichettato: nessun pairing, nessun punteggio di squadra, nessuna vittoria di squadra in nessuna parte del motore (`Rules51`, `PunteggioManager`, `RoundManager`, `CirullaAI`).

### Multiplayer online
3. **Scene di destinazione inesistenti** — `"LobbyScene"` e `"WaitingRoom"` (referenziate in `GameLaunchController.cs:26-27`) non esistono come file `.unity` nel repo né in Build Settings. Blocca **ogni** partita QuickMatch (1v1, 2v2, 4p) dopo un matchmaking Photon altrimenti funzionante.
4. **Nessun timeout/fallback bot nel matchmaking** — se non si trovano abbastanza giocatori, `MatchmakingManager` resta in attesa indefinita; unica uscita è l'annullamento manuale.
5. Stessa mancanza di logica di squadra del punto 2, applicabile a QuickMatch TwoVsTwo.

### Stanze private
6. **UI di stanze private assente da ogni scena** — `PrivateRoomOptionsUI`, `JoinRoomPopupUI`, `WaitingRoomUI` non sono componenti di nessun GameObject in `MainMenu.unity` (né altrove). Il backend (generazione codice, creazione/ingresso stanza Photon) è implementato e funzionante, ma l'utente non può concretamente completare né la creazione né l'ingresso in una stanza, perché gli schermi necessari non esistono in scena. Confermato da `LOBBY_SETUP_README.md`: setup manuale documentato ma mai completato.
7. **Wiring eventi duplicato/ambiguo** nel codice di creazione/ingresso stanza (3 meccanismi paralleli, solo 1 collegato) — secondario rispetto al punto 6, ma da sistemare comunque quando si aggiunge la UI mancante, per evitare di ricollegare l'evento sbagliato.
8. **Disconnessione a metà partita non gestita dal gameplay reale** — `RoomManager.cs` ha una logica di bot-replacement completa ma orfana; `TurnController`/`NetworkGameController` non la richiamano mai. Nessuna riconnessione implementata.

---

## Debito tecnico noto (richiamato, non rianalizzato)

- Problemi di reflection nel codice: **già risolti** in precedenza.
- Codice morto: **già rimosso** in precedenza.
- `GetLocalPlayerIndex()` in `CardViewManager.cs:1396`: **ancora orfano** (nessun call site nel codice), confermato nuovamente durante questo audit.

---

## Non toccare

Sistemi che i 143 test in `Assets/Tests/Editor/` coprono in modo ampio (anche se non li ho potuti eseguire — vedi limiti sotto) e che quindi **non richiedono di essere rimessi in discussione senza una ragione specifica**:
- Motore di cattura di `Rules51` (uguale/somma/15/PlayOnly/cattura forzata, Asso, Matta).
- Rilevamento accusi (`AccusiChecker`: Cirulla, Decino) in tutte le combinazioni con la Matta.
- Calcolo punteggio fine smazzata in `PunteggioManager` (Scopa, Sette Bello, Denari, Carte, Primiera, Grande, Piccola, Cappotto).
- Logica di scelta mossa di `CirullaAI` per le difficoltà Medium/Hard.

Da trattare con più cautela nonostante i test, perché la copertura è debole o indiretta: `TurnController` (i 3 test che lo toccano usano reflection, mai eseguiti in questo audit), il flusso end-to-end multi-smazzata (solo 3 test di integrazione).

---

## Quanto fidarsi di queste conclusioni

**Verificato con certezza** (compilatore/log dal vivo, o grep esaustivo su tutto il repository):
- Il codice compila senza errori nello stato attuale del working tree.
- `PrivateRoomOptionsUI`, `JoinRoomPopupUI`, `WaitingRoomUI`, `AccusoUIBridge` non sono componenti in nessuna scena/prefab (ricerca per GUID script su tutti i file `.unity`/`.prefab`).
- `"LobbyScene"`/`"WaitingRoom"` non esistono come file `.unity` nel repo.
- Zero test automatici toccano networking, matchmaking, room management.
- `GetLocalPlayerIndex()` orfano.

**Dedotto dalla sola lettura del codice, non eseguito**: tutti i percorsi di Fase 3 — cioè ogni ✅/⚠️/❌ in questo documento descrive cosa dovrebbe succedere leggendo il codice C#/YAML, non cosa succede davvero premendo i bottoni. In particolare:
- Il cablaggio Inspector-only (quale metodo è davvero collegato a quale bottone nella UI) non è verificabile via grep quando ci sono più eventi candidati (caso stanze private) — ho riportato l'ambiguità esplicitamente dove esiste.
- Il comportamento runtime di Photon (race condition, timeout di rete reali, errori silenziosi di `PhotonNetwork.LoadLevel`) non è verificabile senza almeno due client reali.
- Non ho eseguito **nessuno** dei 143 test — il "Non toccare" si basa sulla mia lettura di cosa i test *dovrebbero* verificare in base al nome del metodo e al contenuto, non su un'esecuzione reale che confermi che passano.
- I fork di analisi usati per accelerare questo audit hanno avuto due tentativi falliti (uno sull'analisi scene, uno sul multiplayer online) in cui hanno deviato dal compito assegnato invece di produrre l'analisi richiesta; sono stati rilanciati con istruzioni più stringenti e i risultati finali usati in questo report provengono dai tentativi riusciti — non un problema di sostanza, ma lo segnalo per trasparenza sul processo.

In sintesi: fidati delle affermazioni su compilazione, presenza/assenza di componenti in scena, ed esistenza di file — sono verificate. Tratta ogni classificazione ✅/⚠️/❌ di Fase 3 come "questo è quello che dice il codice", non come "questo è quello che ho visto succedere".
