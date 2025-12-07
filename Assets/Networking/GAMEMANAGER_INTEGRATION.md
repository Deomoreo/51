# ?? GameManager Multiplayer Integration - Guida

## ?? Obiettivo

Modificare `GameManager` per supportare **modalità multiplayer**, inizializzando il gioco con i player slots da `RoomManager`.

---

## ?? Cosa Serve

### **1. Detect Multiplayer Mode**

GameManager deve sapere se è in single-player o multiplayer:

```csharp
public class GameManager : MonoBehaviour
{
    public enum GameMode
    {
        SinglePlayer,
        Multiplayer
    }

    public GameMode CurrentGameMode { get; private set; }
    
    private void Start()
    {
        // Detect se siamo in multiplayer
        if (PhotonNetwork.InRoom)
        {
            CurrentGameMode = GameMode.Multiplayer;
            InitializeMultiplayer();
        }
        else
        {
            CurrentGameMode = GameMode.SinglePlayer;
            InitializeSinglePlayer();
        }
    }
}
```

---

### **2. Initialize da Room Slots**

Invece di creare player random, usa i dati da `RoomManager`:

```csharp
private void InitializeMultiplayer()
{
    Debug.Log("Initializing multiplayer game...");
    
    var roomManager = RoomManager.Instance;
    if (roomManager == null)
    {
        Debug.LogError("RoomManager not found!");
        return;
    }

    // Ottieni player slots
    var slots = roomManager.PlayerSlots;
    int playerCount = roomManager.FilledSlotsCount;
    int localPlayerSlot = roomManager.GetLocalPlayerSlot();

    Debug.Log($"Setting up {playerCount} players");
    Debug.Log($"Local player is in slot {localPlayerSlot}");

    // Inizializza GameState con i player corretti
    InitializeGameStateMultiplayer(slots, localPlayerSlot);
}
```

---

### **3. GameState Setup**

Crea `GameState` con player names e types corretti:

```csharp
private void InitializeGameStateMultiplayer(NetworkPlayerInfo[] slots, int localSlot)
{
    // Crea array di player names
    List<string> playerNames = new List<string>();
    List<bool> isHuman = new List<bool>();

    for (int i = 0; i < slots.Length; i++)
    {
        if (!slots[i].IsEmpty)
        {
            playerNames.Add(slots[i].NickName);
            isHuman.Add(slots[i].IsHuman);
        }
    }

    // Inizializza GameState
    gameState = new GameState(playerNames.Count);
    
    // Setup player states
    for (int i = 0; i < playerNames.Count; i++)
    {
        gameState.Players[i].Name = playerNames[i];
        // Altri setup...
    }

    // Set local player index
    _localPlayerIndex = localSlot;
}
```

---

### **4. Disable AI per Human Players**

Solo i bot devono avere AI:

```csharp
private void SetupPlayerControllers(NetworkPlayerInfo[] slots)
{
    for (int i = 0; i < slots.Length; i++)
    {
        if (slots[i].IsEmpty) continue;

        if (slots[i].IsBot)
        {
            // Abilita AI per questo player
            EnableAIForPlayer(i);
        }
        else if (slots[i].IsHuman)
        {
            // Human player - disable AI
            if (slots[i].PhotonActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                // È il player locale - enable input
                EnableLocalPlayerControl(i);
            }
            else
            {
                // È un altro human player - disable input
                DisablePlayerControl(i);
            }
        }
    }
}
```

---

### **5. Turn Order**

Mantieni turn order consistente tra tutti i client:

```csharp
private void DetermineTurnOrder(NetworkPlayerInfo[] slots)
{
    // L'ordine dei turni è determinato dall'ordine degli slot
    // Questo garantisce che sia uguale per tutti i client

    List<int> turnOrder = new List<int>();
    
    for (int i = 0; i < slots.Length; i++)
    {
        if (!slots[i].IsEmpty)
        {
            turnOrder.Add(i);
        }
    }

    // Opzionale: randomizza (ma SOLO sul Master Client!)
    if (PhotonNetwork.IsMasterClient)
    {
        // Shuffle turn order
        // Poi sync via RPC agli altri client
    }
}
```

---

## ?? Esempio Completo

```csharp
using UnityEngine;
using Photon.Pun;
using Project51.Networking;

public class GameManager : MonoBehaviour
{
    public enum GameMode { SinglePlayer, Multiplayer }
    public GameMode CurrentGameMode { get; private set; }

    private int _localPlayerIndex = -1;
    private GameState gameState;

    private void Start()
    {
        // Auto-detect modalità
        if (PhotonNetwork.InRoom)
        {
            CurrentGameMode = GameMode.Multiplayer;
            InitializeMultiplayer();
        }
        else
        {
            CurrentGameMode = GameMode.SinglePlayer;
            InitializeSinglePlayer();
        }
    }

    private void InitializeMultiplayer()
    {
        Debug.Log("=== MULTIPLAYER MODE ===");
        
        var roomManager = RoomManager.Instance;
        if (roomManager == null)
        {
            Debug.LogError("RoomManager not found!");
            return;
        }

        var slots = roomManager.PlayerSlots;
        int playerCount = roomManager.FilledSlotsCount;
        int localSlot = roomManager.GetLocalPlayerSlot();

        // Crea lista player names
        List<string> names = new List<string>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty)
                names.Add(slots[i].NickName);
        }

        // Inizializza game
        gameState = new GameState(names.Count);
        for (int i = 0; i < names.Count; i++)
        {
            gameState.Players[i].Name = names[i];
        }

        _localPlayerIndex = localSlot;

        Debug.Log($"Game initialized with {names.Count} players");
        Debug.Log($"Local player index: {localSlot}");

        // Start game
        StartMultiplayerGame();
    }

    private void InitializeSinglePlayer()
    {
        Debug.Log("=== SINGLE PLAYER MODE ===");
        
        // Existing single-player initialization
        // ...
    }

    private void StartMultiplayerGame()
    {
        // Inizia il gioco multiplayer
        // Questo metodo sarà chiamato solo dopo che tutti i client
        // hanno caricato la scena

        if (PhotonNetwork.IsMasterClient)
        {
            // Master Client controlla il flow del gioco
            Debug.Log("Master Client starting game...");
            
            // TODO: Initialize deck, deal cards, etc.
            // Via RPC per sincronizzare con altri client
        }
        else
        {
            // Altri client aspettano comandi dal Master
            Debug.Log("Waiting for Master Client...");
        }
    }

    public bool IsLocalPlayer(int playerIndex)
    {
        if (CurrentGameMode == GameMode.SinglePlayer)
            return playerIndex == 0; // In single-player, solo player 0 è locale

        return playerIndex == _localPlayerIndex;
    }

    public bool IsHumanPlayer(int playerIndex)
    {
        if (CurrentGameMode == GameMode.SinglePlayer)
            return true; // In single-player, tutti human (o AI)

        var roomManager = RoomManager.Instance;
        if (roomManager == null) return false;

        var slot = roomManager.PlayerSlots[playerIndex];
        return slot.IsHuman;
    }

    public bool IsBotPlayer(int playerIndex)
    {
        if (CurrentGameMode == GameMode.SinglePlayer)
            return false; // Gestito diversamente

        var roomManager = RoomManager.Instance;
        if (roomManager == null) return false;

        var slot = roomManager.PlayerSlots[playerIndex];
        return slot.IsBot;
    }
}
```

---

## ?? TurnController Integration

`TurnController` deve sapere chi può giocare:

```csharp
public class TurnController : MonoBehaviour
{
    private void OnPlayerTurn(int playerIndex)
    {
        var gm = GameManager.Instance;

        if (gm.CurrentGameMode == GameMode.Multiplayer)
        {
            // Multiplayer: solo local player può fare input
            if (gm.IsLocalPlayer(playerIndex))
            {
                if (gm.IsBotPlayer(playerIndex))
                {
                    // Local bot - execute AI
                    ExecuteAITurn(playerIndex);
                }
                else
                {
                    // Local human - enable input
                    EnablePlayerInput(playerIndex);
                }
            }
            else
            {
                // Remote player - disable input, wait for network
                DisablePlayerInput();
                WaitForRemotePlayerMove();
            }
        }
        else
        {
            // Single-player: existing logic
            if (IsAIPlayer(playerIndex))
                ExecuteAITurn(playerIndex);
            else
                EnablePlayerInput(playerIndex);
        }
    }
}
```

---

## ?? Flow Diagram

```
Scene Load
   ?
GameManager.Start()
   ?
Check PhotonNetwork.InRoom?
   ?? Yes ? Multiplayer Mode
   ?   ?
   ?   Get RoomManager.PlayerSlots
   ?   ?
   ?   Initialize GameState with slots
   ?   ?
   ?   Set local player index
   ?   ?
   ?   Disable AI for human players
   ?   ?
   ?   Enable AI for bot players
   ?   ?
   ?   Start game (Master Client controls)
   ?
   ?? No ? Single-player Mode
       ?
       Existing initialization
```

---

## ? Checklist Integration

- [ ] Add `GameMode` enum
- [ ] Add `CurrentGameMode` property
- [ ] Detect multiplayer in `Start()`
- [ ] Create `InitializeMultiplayer()` method
- [ ] Get player slots from RoomManager
- [ ] Set local player index
- [ ] Disable AI for human players
- [ ] Keep AI for bot players
- [ ] Update TurnController logic
- [ ] Test con 2 players
- [ ] Test con bot
- [ ] Test con 4 players

---

## ?? Testing Steps

### **Test 1: Multiplayer Init**

1. Start game from lobby (2 players ready)
2. Check console:
   ```
   === MULTIPLAYER MODE ===
   Game initialized with 2 players
   Local player index: 0
   ```
3. Other client:
   ```
   === MULTIPLAYER MODE ===
   Game initialized with 2 players
   Local player index: 1
   ```

### **Test 2: Player Control**

1. Game started
2. Player 0 turn ? Client A can play
3. Client B input disabled
4. Wait for network sync

### **Test 3: Bot Support**

1. Setup: 2 humans + 1 bot
2. Bot turn ? AI executes (on Master)
3. Move synced to all clients

---

## ?? Important Notes

### **Master Client Control**

In multiplayer, **Master Client** controlla:
- Deck shuffling
- Card dealing
- Bot AI execution
- Game state authority

**Perché?**
- Evita desync
- Single source of truth
- Più semplice da debug

### **Network Sync**

Ogni azione del gioco deve essere sincronizzata:
- Mosse ? RPC
- Accusi ? RPC
- Fine turno ? RPC
- Score ? RPC

**Next step:** NetworkGameController per gestire sync!

---

**Status:** ?? Guida Completa  
**File:** `Assets/Networking/GAMEMANAGER_INTEGRATION.md`  
**Next:** Implementare integration vera
