# ?? Project51 - Multiplayer Mobile Card Game

## Cirulla / 51 - Gioco di Carte Tradizionale Italiano

### ?? Versione Mobile Multiplayer Online

---

## ?? Quick Start

### 1. Setup Photon PUN 2
Segui la guida: [`Assets/Networking/SETUP_PHOTON_PUN2.md`](Assets/Networking/SETUP_PHOTON_PUN2.md)

### 2. Architettura
Leggi il documento completo: [`Assets/Networking/ARCHITECTURE.md`](Assets/Networking/ARCHITECTURE.md)

### 3. Stato Implementazione

#### ? Completato
- ? Core Game Logic (Rules, GameState, RoundManager)
- ? AI System (Basic bot)
- ? UI Base (Card views, Move selection, Accusi panel)
- ? Tests completi (Unit tests per regole)
- ? Network Foundation:
  - NetworkManager (Connessione Photon)
  - RoomManager (Lobby, slot, ready system)
  - PlayerDataManager (Statistiche locali)
  - NetworkTypes (Strutture dati)

#### ?? In Development (Prossimi passi)
1. **GameStateSerializer** - Serializzazione per rete
2. **NetworkGameController** - Sincronizzazione GameState
3. **NetworkTurnController** - Turn management multiplayer
4. **AIPlayer** - AI separato da gameplay

#### ?? Planned
- Lobby UI completa
- Mobile input optimization
- Matchmaking UI
- Leaderboard & Statistics UI
- Cloud save integration
- Social features (friend codes)

---

## ?? Modalità di Gioco

### 1. Solo vs 3 Bot (Offline)
- Gioco locale senza connessione
- 3 bot AI controllati localmente
- Ideale per imparare o giocare offline

### 2. Quick Match (Online)
- Matchmaking automatico
- Slot vuoti riempiti con bot
- Partenza rapida

### 3. Partita Privata (Online)
- Crea room con codice amico (6 caratteri)
- Invita amici
- Supporta bot per slot vuoti

### 4. Duo vs 2 Bot (Online)
- 2 giocatori umani + 2 bot
- Bot controllati dal Master Client

---

## ??? Architettura Tecnica

### Layer Structure

```
???????????????????????????????????????
?         UI Layer                    ?
?  (MainMenu, Lobby, Game UI)         ?
???????????????????????????????????????
              ?
???????????????????????????????????????
?      Networking Layer               ?
?  (NetworkManager, RoomManager)      ?
?  (Photon PUN 2)                     ?
???????????????????????????????????????
              ?
???????????????????????????????????????
?      Game Logic Layer               ?
?  (GameState, RoundManager, Rules)   ?
?  (Accusi, Scoring, Moves)           ?
???????????????????????????????????????
              ?
???????????????????????????????????????
?      Data Layer                     ?
?  (PlayerStats, Persistence)         ?
???????????????????????????????????????
```

### Key Components

#### Networking (`Assets/Scripts/Networking/`)
- **NetworkManager**: Gestione connessione Photon, lobby, room creation/join
- **RoomManager**: Slot giocatori, ready status, bot management, disconnection handling
- **PlayerDataManager**: Salvataggio statistiche locali (PlayerPrefs ? Cloud futuro)
- **NetworkTypes**: Enums, strutture dati, costanti

#### Core Game (`Assets/Scripts/Core/`)
- **GameState**: Stato completo della partita
- **RoundManager**: Gestione smazzate, turni, punteggi
- **Rules51**: Regole del gioco (catture, scopa, etc.)
- **AccusiChecker**: Validazione Decino/Cirulla
- **Move, Card, Deck, PlayerState**: Strutture base

#### UI (`Assets/Scripts/UI/`)
- **MoveSelectionUI**: Selezione mosse giocatore
- **AccusoPanelController**: Dichiarazione accusi
- **CardSpriteProvider**: Sprite delle carte

#### Gameplay (`Assets/Scripts/Gameplay/`)
- **TurnController**: Controller principale del gioco (da adattare per network)
- **CardView, CardViewManager**: Visualizzazione carte
- **CapturedPileManager**: Gestione pile catturate

---

## ?? Photon PUN 2 Integration

### Setup
1. Scarica Photon PUN 2 dall'Asset Store
2. Importa il package
3. Ottieni App ID da https://www.photonengine.com/
4. Configura in `PhotonServerSettings`

### Key Features Used
- **MonoBehaviourPunCallbacks**: Callbacks di rete
- **PhotonView**: Sincronizzazione oggetti
- **RPCs**: Remote Procedure Calls per sync eventi
- **Custom Properties**: Room e Player properties per stato
- **AutomaticallySyncScene**: Caricamento scene sincronizzato

### Free Tier
- 20 CCU (Concurrent Users)
- ~5 room simultanee
- Sufficiente per sviluppo e testing

---

## ?? Data Flow

### Multiplayer Game Flow

```
1. Player apre app
   ?
2. NetworkManager.ConnectToPhoton()
   ?
3. Scegli modalità (Menu UI)
   ?
4a. Quick Match               4b. Private Room
    ?                             ?
    QuickMatch()                  CreateRoom(code)
    ?                             ?
    JoinRandomRoom()              Amici: JoinRoomByCode(code)
   ?                         ?
5. RoomManager assegna slot
   ?
6. Master Client aggiunge bot se necessario
   ?
7. Tutti ready ? StartGame()
   ?
8. PhotonNetwork.LoadLevel("GameScene")
   ?
9. NetworkGameController inizializza
   ?
10. Game Loop:
    - Human turn: Move ? RPC_ExecuteMove
    - Bot turn (MasterClient): AI ? RPC_ExecuteMove
    - Sync stato a tutti
   ?
11. Fine partita ? Statistiche ? Menu
```

---

## ?? Bot System

### AI Logic
- Priorità: Scopa > Capture > PlayOnly
- Controlla Decino/Cirulla automaticamente
- Delay simulato per "thinking" (2 secondi)

### Network Bots
- Controllati dal **Master Client**
- Sostituiscono giocatori disconnessi
- Indistinguibili da client prospettiva
- `NetworkAIController` gestisce tutti i bot

### Bot Types (Future)
- Easy: Random valide
- Medium: Priorità basica (attuale)
- Hard: Logica avanzata, memoria carte

---

## ?? Data Persistence

### Local Storage (PlayerPrefs)
- Nickname giocatore
- Statistiche:
  - Total games, wins, losses
  - Win rate
  - Total score, highest score
  - Scopa/Decino/Cirulla totali
  - ELO rating (futuro)

### Cloud Storage (Future)
- Firebase/PlayFab
- Cross-device sync
- Leaderboard global
- Social features

---

## ?? Mobile Optimization

### Input
- ? Touch drag & drop per catture
- ? Tap per PlayOnly
- ?? Gesture hints
- ?? Tutorial interattivo

### UI
- ?? Responsive layout (portrait/landscape)
- ?? Safe area handling (notch)
- ?? Dynamic scaling
- ? TextMeshPro per testo

### Performance
- ? Object pooling (carte)
- ?? Sprite atlases
- ?? Compressed textures
- ?? LOD per animazioni

### Platform Specific
- Android: Internet permission
- iOS: Background modes
- Both: Low memory handling

---

## ?? Testing

### Unit Tests (`Assets/Tests/Editor/`)
- ? Rules validation
- ? Accusi checking
- ? Scoring system
- ? Matta (wild card) combinations
- ? Edge cases

### Play Mode Tests (`Assets/Tests/PlayMode/`)
- ? Matta visual hints
- ?? Network integration tests

### Manual Testing
- ?? Multiplayer scenarios
- ?? Disconnection handling
- ?? Mobile devices
- ?? Network latency

---

## ?? Development Workflow

### Branch Strategy
- `main`: Stable builds
- `develop`: Integration branch
- `feature/*`: Nuove feature
- `fix/*`: Bug fixes

### Commit Conventions
- `feat:` Nuova feature
- `fix:` Bug fix
- `refactor:` Code refactoring
- `test:` Test additions/changes
- `docs:` Documentation
- `ui:` UI changes

### Build Process
1. Unity ? Build Settings ? Android/iOS
2. Configure:
   - Bundle ID
   - Version
   - Icons
   - Permissions
3. Build & Run

---

## ?? TODOs

### High Priority
- [ ] Implementare GameStateSerializer
- [ ] Creare NetworkGameController
- [ ] Adattare TurnController per multiplayer
- [ ] Separare AI in AIPlayer class
- [ ] Main Menu UI
- [ ] Lobby UI
- [ ] Room UI

### Medium Priority
- [ ] Touch input optimization
- [ ] Responsive UI layouts
- [ ] Connection indicator
- [ ] Timeout turno UI
- [ ] Statistiche UI
- [ ] Settings menu

### Low Priority
- [ ] Leaderboard
- [ ] ELO ranking
- [ ] Friend system
- [ ] Chat (lobby)
- [ ] Achievements
- [ ] Tutorial
- [ ] Sound effects & music
- [ ] Cosmetic customization

### Future
- [ ] Cloud save
- [ ] Cross-platform sync
- [ ] Spectator mode
- [ ] Replay system
- [ ] Advanced AI difficulty
- [ ] Social login (Google, Facebook)
- [ ] Monetization (ads, cosmetics)

---

## ?? Resources

### Documentation
- [Photon PUN 2 Docs](https://doc.photonengine.com/pun/current/getting-started/pun-intro)
- [Unity Multiplayer Guide](https://docs.unity3d.com/Manual/UNetOverview.html)
- [Mobile Optimization](https://docs.unity3d.com/Manual/MobileOptimizationPracticalGuide.html)

### Assets Used
- TextMeshPro (UI text)
- 2D Sprites (card graphics)
- Photon PUN 2 (networking)

### External Links
- [Cirulla Rules (Italian)](https://www.pagat.com/fishing/cirulla.html)
- [Unity Forum](https://forum.unity.com/)
- [Photon Forum](https://forum.photonengine.com/)

---

## ?? Team / Contributors

### Development
- Core game logic ?
- Multiplayer system ??
- UI/UX design ??
- Testing ?

---

## ?? License

[Da definire - Privato per ora]

---

## ?? Vision

Creare il miglior gioco mobile di Cirulla/51:
- ? Esperienza multiplayer fluida
- ?? AI intelligente per partite offline
- ?? Statistiche dettagliate e ranking
- ?? Community globale di giocatori
- ?? Tornei e eventi speciali (futuro)

---

**Last Updated**: [Data corrente]
**Version**: 0.2.0-alpha (Multiplayer Foundation)
**Unity Version**: 2022.3 LTS
**Photon PUN**: 2.x
