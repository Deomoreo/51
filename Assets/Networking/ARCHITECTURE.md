# Architettura Multiplayer - Project51 (Cirulla)

## ?? Obiettivo
Trasformare Project51 in un gioco mobile multiplayer online con supporto bot e modalità diverse.

---

## ?? Modalità di Gioco

### 1. Solo vs 3 Bot
- 1 giocatore umano locale
- 3 bot AI controllati localmente
- **No networking** (offline)

### 2. Duo vs 2 Bot
- 2 giocatori umani online
- 2 bot AI controllati dal Master Client
- **Networking**: Sincronizzazione mosse e GameState

### 3. Online 4 Players
- 4 giocatori umani online
- Sistema di disconnessione ? sostituzione con bot
- **Networking**: Full sync

### 4. Quick Match
- Matchmaking automatico
- Riempie slot vuoti con bot
- **Networking**: Room pubbliche

---

## ??? Architettura di Sistema

### Layer 1: Core Game (Esistente)
**Componenti già implementati:**
- `GameState.cs` - Stato completo del gioco
- `RoundManager.cs` - Gestione smazzate e turni
- `Rules51.cs` - Regole del gioco
- `AccusiChecker.cs` - Verifica Decino/Cirulla
- `Move.cs` - Rappresentazione mosse
- `Card.cs`, `Deck.cs`, `PlayerState.cs`

**Modifiche necessarie:**
- ? Già serializable (struct/class)
- ?? Aggiungere serializzazione custom per `GameState`
- ?? Separare logica AI in `AIPlayer.cs`

### Layer 2: Networking (Nuovo)
**Componenti creati:**
- ? `NetworkManager.cs` - Gestione connessione Photon
- ? `RoomManager.cs` - Gestione lobby e slot giocatori
- ? `PlayerDataManager.cs` - Persistenza statistiche
- ? `NetworkTypes.cs` - Tipi e costanti network

**Componenti da creare:**
- ?? `NetworkGameController.cs` - Sincronizzazione GameState via RPC
- ?? `NetworkAIController.cs` - Controllo bot dal Master Client
- ?? `GameStateSerializer.cs` - Serializzazione GameState per rete

### Layer 3: UI (Adattamento)
**Componenti esistenti da adattare:**
- `TurnController.cs` ? `NetworkTurnController.cs`
- `MoveSelectionUI.cs` ? Aggiungere timeout turno
- `AccusoPanelController.cs` ? Sincronizzare dichiarazioni

**Nuovi componenti UI:**
- ?? `LobbyUI.cs` - Interfaccia lobby
- ?? `RoomUI.cs` - Lista giocatori, ready status, slot
- ?? `MainMenuUI.cs` - Menu principale mobile
- ?? `QuickMatchUI.cs` - Searching screen
- ?? `StatsUI.cs` - Visualizzazione statistiche
- ?? `ConnectionIndicator.cs` - Indicatore connessione

### Layer 4: Mobile Adaptation
**Adattamenti necessari:**
- ?? Input touch ottimizzato
- ?? Layout responsive (portrait/landscape)
- ?? Notifiche push (futuro)
- ?? Gestione interruzioni (chiamate, background)

---

## ?? Flusso di Gioco Multiplayer

### A. Menu Principale
```
[Gioca] ? Scegli modalità:
  ?? Partita Rapida (Quick Match)
  ?? Crea Partita Privata (con codice)
  ?? Unisciti con Codice
  ?? Vs Bot (Offline)

[Statistiche]
[Impostazioni]
[Esci]
```

### B. Quick Match Flow
```
1. Player preme "Partita Rapida"
2. NetworkManager.QuickMatch()
3. Photon cerca room disponibile
   ?? Trovata ? Join
   ?? Non trovata ? Crea nuova room pubblica
4. RoomManager assegna player a slot
5. MasterClient riempie slot vuoti con bot
6. Countdown automatico (30s) o tutti ready
7. Start game
```

### C. Private Room Flow
```
1. Player preme "Crea Partita"
2. Genera codice 6 caratteri (es: ABC123)
3. NetworkManager.CreateRoom(config, code)
4. Player invita amici con codice
5. Amici: "Unisciti con Codice" ? ABC123
6. Host preme "Inizia" quando ready
7. Start game
```

### D. In-Game Flow
```
1. PhotonNetwork.LoadLevel("GameScene")
2. NetworkGameController.Initialize()
3. Sincronizza GameState iniziale (RPC)
4. Loop turni:
   ?? Human turn: Input ? ValidateMove ? RPC_ExecuteMove
   ?? Bot turn (MasterClient): AI ? RPC_ExecuteMove
   ?? Sync stato a tutti
5. Fine smazzata ? RPC_EndRound
6. Fine partita ? RPC_GameOver ? Statistiche
7. Torna a lobby/menu
```

---

## ?? Photon RPC Design

### RPC Calls Principali

#### 1. Sincronizzazione Stato
```csharp
[PunRPC]
void RPC_SyncGameState(string gameStateJson)
{
    // Deserializza e applica stato
    // Chiamato all'inizio e dopo eventi importanti
}
```

#### 2. Esecuzione Mossa
```csharp
[PunRPC]
void RPC_ExecuteMove(int playerIndex, string moveJson)
{
    // Deserializza Move
    // Applica a GameState locale
    // Aggiorna UI
}
```

#### 3. Dichiarazione Accuso
```csharp
[PunRPC]
void RPC_DeclareAccuso(int playerIndex, int accusoType)
{
    // Applica Decino/Cirulla
    // Aggiorna punti
}
```

#### 4. Fine Round
```csharp
[PunRPC]
void RPC_EndRound(int[] finalScores)
{
    // Mostra scores
    // Prepara prossimo round o fine partita
}
```

#### 5. Gestione Disconnessione
```csharp
[PunRPC]
void RPC_PlayerDisconnected(int slot, int botIndex)
{
    // Notifica disconnessione
    // Sostituisce con bot
}
```

---

## ?? Sistema Bot (AI)

### Architettura Bot
- **Master Client** controlla tutti i bot
- Bot simulano umani con delay
- AI esistente in `TurnController.ExecuteAITurn()`

### Separazione AI
```csharp
public class AIPlayer
{
    private GameState gameState;
    private int playerIndex;
    
    public Move SelectMove(List<Move> validMoves)
    {
        // Logica AI esistente
        // Priorità: Scopa > Capture > PlayOnly
    }
    
    public AccusoType? CheckAccuso(List<Card> hand)
    {
        // Controlla Decino/Cirulla
    }
}
```

### Bot in Network
```csharp
public class NetworkAIController : MonoBehaviour
{
    private List<AIPlayer> botPlayers;
    
    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Per ogni bot nel turno corrente
        foreach (var bot in botPlayers)
        {
            if (IsBoTturn(bot))
            {
                Move move = bot.SelectMove(...);
                photonView.RPC("RPC_ExecuteMove", RpcTarget.All, ...);
            }
        }
    }
}
```

---

## ?? Sincronizzazione GameState

### Problema
`GameState` è complesso con molti oggetti nested.

### Soluzione: Serializzazione Custom
```csharp
[Serializable]
public class GameStateData
{
    public int currentPlayerIndex;
    public int dealerIndex;
    public int smazzataNumber;
    public List<CardData> tableCards;
    public List<PlayerStateData> players;
    public List<CardData> remainingDeck;
    // ... tutti i campi necessari
}

public static class GameStateSerializer
{
    public static string Serialize(GameState state)
    {
        var data = new GameStateData();
        // Popola data da state
        return JsonUtility.ToJson(data);
    }
    
    public static GameState Deserialize(string json)
    {
        var data = JsonUtility.FromJson<GameStateData>(json);
        // Ricostruisci GameState da data
        return state;
    }
}
```

### Quando Sincronizzare
- ? Inizio partita (full state)
- ? Dopo ogni mossa (delta o full?)
- ? Fine smazzata (scores update)
- ? Dopo dichiarazione accuso
- ? Non continuamente (troppo traffico)

### Ottimizzazione
- **Full Sync**: Solo all'inizio e dopo eventi importanti
- **Delta Sync**: Solo dati cambiati (Move application)
- **Validation**: Master Client valida mosse

---

## ?? Gestione Disconnessioni

### Scenari

#### 1. Player Disconnette Prima dell'Inizio
- Rimuovi da slot
- Altro player può unirsi

#### 2. Player Disconnette Durante Partita
- Grace period: 60 secondi per riconnettersi
- Dopo timeout ? Sostituisci con bot (MasterClient)
- RPC_PlayerDisconnected notify a tutti

#### 3. Master Client Disconnette
- Photon assegna nuovo Master Client automaticamente
- Nuovo Master assume controllo bot
- OnMasterClientSwitched callback

#### 4. Tutti Disconnettono
- Photon chiude room
- Nessun salvataggio partita (futuro: save state)

---

## ?? Validazione e Anti-Cheat

### Architettura Autoritative
- **Master Client** è l'autorità
- Valida tutte le mosse ricevute
- Può rifiutare mosse invalide

```csharp
[PunRPC]
void RPC_RequestMove(int playerIndex, string moveJson, PhotonMessageInfo info)
{
    if (!PhotonNetwork.IsMasterClient) return;
    
    // Valida move
    Move move = JsonUtility.FromJson<Move>(moveJson);
    if (!IsValidMove(move, playerIndex))
    {
        // Rifiuta move
        photonView.RPC("RPC_InvalidMove", info.Sender);
        return;
    }
    
    // Accetta e propaga
    photonView.RPC("RPC_ExecuteMove", RpcTarget.All, playerIndex, moveJson);
}
```

### Timeout Turno
- 30 secondi per mossa
- Se scaduto ? Mossa automatica (prima valida o PlayOnly)
- NetworkTurnController gestisce timer

---

## ?? Mobile Specific

### Input Touch
- Drag & Drop per catture
- Tap per PlayOnly
- Double-tap per auto-play (se solo 1 mossa)
- Gesture hints

### UI Responsive
- Portrait mode preferito (mobile)
- Landscape supportato (tablet)
- Dynamic scaling based on screen size
- Safe area handling (notch)

### Performance
- Object pooling per carte
- Sprite atlases
- Minimal RPC frequency
- Compress GameState JSON

### Background Handling
```csharp
void OnApplicationPause(bool pause)
{
    if (pause)
    {
        // Salva stato
        // Mantieni connessione (KeepAliveInBackground)
    }
    else
    {
        // Riprendi
        // Sync stato se necessario
    }
}
```

---

## ??? Struttura File

```
Assets/
??? Scripts/
?   ??? Core/                    (Esistente - logica gioco)
?   ?   ??? GameState.cs
?   ?   ??? RoundManager.cs
?   ?   ??? Rules51.cs
?   ?   ??? ...
?   ?
?   ??? Networking/              (Nuovo - multiplayer)
?   ?   ??? NetworkManager.cs ?
?   ?   ??? RoomManager.cs ?
?   ?   ??? PlayerDataManager.cs ?
?   ?   ??? NetworkTypes.cs ?
?   ?   ??? NetworkGameController.cs ??
?   ?   ??? NetworkAIController.cs ??
?   ?   ??? GameStateSerializer.cs ??
?   ?   ??? NetworkTurnController.cs ??
?   ?
?   ??? AI/                      (Separato da Gameplay)
?   ?   ??? AIPlayer.cs ??
?   ?
?   ??? UI/
?   ?   ??? Menus/
?   ?   ?   ??? MainMenuUI.cs ??
?   ?   ?   ??? LobbyUI.cs ??
?   ?   ?   ??? RoomUI.cs ??
?   ?   ?   ??? StatsUI.cs ??
?   ?   ?
?   ?   ??? Game/                (Esistente - da adattare)
?   ?       ??? MoveSelectionUI.cs
?   ?       ??? AccusoPanelController.cs
?   ?       ??? ...
?   ?
?   ??? Gameplay/                (Esistente)
?       ??? TurnController.cs
?       ??? CardView.cs
?       ??? ...
?
??? Scenes/
?   ??? MainMenu.unity ??
?   ??? Lobby.unity ??
?   ??? GameScene.unity (esistente, da adattare)
?   ??? ...
?
??? Networking/
?   ??? SETUP_PHOTON_PUN2.md ?
?
??? Prefabs/
    ??? NetworkManagers.prefab ??
    ??? ...
```

---

## ?? Roadmap Implementazione

### ? Fase 1: Foundation (Completata)
- ? Setup Photon PUN 2 guide
- ? NetworkManager (connessione, lobby, room)
- ? RoomManager (slot, ready, bot management)
- ? PlayerDataManager (statistiche locali)
- ? NetworkTypes (strutture dati)

### ?? Fase 2: Core Networking (Prossimi passi)
1. **GameStateSerializer** - Serializzazione stato
2. **NetworkGameController** - Sync GameState via RPC
3. **NetworkTurnController** - Adattare TurnController per network
4. **AIPlayer** - Separare AI da TurnController

### ?? Fase 3: UI & Lobby
1. **MainMenuUI** - Menu principale
2. **LobbyUI** - Lista room/quick match
3. **RoomUI** - Slot giocatori, ready, codice amico
4. **ConnectionIndicator** - Stato connessione

### ?? Fase 4: In-Game Networking
1. Integrare NetworkGameController in scena
2. Test mosse sincronizzate
3. Test accusi sincronizzati
4. Test end round/game

### ?? Fase 5: Bot System
1. Implementare NetworkAIController
2. Bot controllati da Master Client
3. Sostituzione player disconnessi
4. Test mixed human/bot games

### ?? Fase 6: Mobile Optimization
1. Touch input system
2. Responsive UI (portrait/landscape)
3. Performance optimization
4. Background handling

### ?? Fase 7: Polish & Features
1. Matchmaking improvements
2. Leaderboard (local ? cloud)
3. ELO ranking system
4. Social features (friend codes)
5. Achievements

### ?? Fase 8: Publishing
1. Android build & test
2. iOS build & test
3. Store assets (icon, screenshots)
4. Privacy policy, GDPR
5. Release!

---

## ?? Testing Strategy

### Unit Tests (Esistenti)
- ? Game rules
- ? Accusi logic
- ? Scoring
- ?? Serialization

### Integration Tests
- ?? NetworkManager connection flow
- ?? RoomManager slot assignment
- ?? GameState sync accuracy

### Multiplayer Tests
- ?? 2 players on same network
- ?? 4 players online
- ?? Player disconnect scenarios
- ?? Master Client switch
- ?? Bot behavior

### Performance Tests
- ?? Mobile devices (various specs)
- ?? Network latency simulation
- ?? Memory profiling
- ?? Battery consumption

---

## ?? Note Tecniche

### Photon Limitations (Free Tier)
- **20 CCU** (Concurrent Users)
- Sufficiente per ~5 room simultanee (4 players each)
- Upgrade disponibile se necessario

### Latency Handling
- Turn-based ? latency meno critica
- RPC con BufferedViaServer per reliability
- Timeout turno generoso (30s)

### Data Usage
- ~10-50 KB per partita (dipende da lunghezza)
- GameState sync: ~2-5 KB per evento
- Ottimo per mobile data plans

---

## ?? Funzionalità Future

### Cloud Save
- Firebase o PlayFab
- Cross-device persistence
- Backup statistics

### Social Features
- Friend list
- Chat in lobby
- Spectator mode
- Replay system

### Monetization (Opzionale)
- Ads tra partite
- Cosmetic avatars/carte
- Premium pass (senza ads)
- NO pay-to-win

### Advanced AI
- Difficulty levels (Easy/Medium/Hard)
- Personality traits
- Learning AI (futuro lontano)

---

**Prossimo Step**: Implementare GameStateSerializer e NetworkGameController!
