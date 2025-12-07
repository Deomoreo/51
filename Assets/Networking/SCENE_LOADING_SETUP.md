# ?? Scene Loading System - Setup Completo

## ? Cosa Abbiamo Implementato

Un sistema completo per caricare la scena di gioco quando tutti i player sono ready!

### **File Creati:**
- ? `SceneLoadManager.cs` - Gestisce loading scene in multiplayer
- ? Integrazione con `RoomManager.StartGame()`
- ? Sincronizzazione automatica tra tutti i client

---

## ?? Setup Requirements

### **1. Refresh Unity Editor** ?? IMPORTANTE!

Dopo aver creato `SceneLoadManager.cs`, devi:

1. **Torna a Unity Editor**
2. **Aspetta** che Unity ricompili automaticamente
3. **Oppure** forza recompile: `Assets ? Reimport All`

**Perché?**
Unity deve rigenerare i file di progetto `.csproj` per includere il nuovo script nell'assembly `Project51.Networking`.

---

## ?? Setup in Scene

### **1. Aggiungi SceneLoadManager al GameObject**

Nel tuo **NetworkManagers** GameObject:

```
NetworkManagers (GameObject)
?? NetworkManager
?? RoomManager  
?? PlayerDataManager
?? SceneLoadManager ? AGGIUNGI QUESTO!
?? PhotonView
?? NetworkTestUI
```

**Steps:**
1. Select "NetworkManagers" GameObject
2. **Add Component** ? SceneLoadManager
3. Salva scena

---

### **2. Verifica Nome Scene**

In `SceneLoadManager.cs`:

```csharp
public const string LOBBY_SCENE = "NetworkTest";  // ? Tua lobby scene
public const string GAME_SCENE = "CirullaGame";   // ? Tua game scene
```

**Verifica** che i nomi corrispondano alle tue scene!

**Come verificare:**
1. `File ? Build Settings`
2. Check i nomi esatti delle scene
3. Se diversi, modifica le costanti in `SceneLoadManager.cs`

---

### **3. Aggiungi Scene al Build**

**IMPORTANTE!** Le scene devono essere nel Build Settings:

1. `File ? Build Settings`
2. **Add Open Scenes** o drag & drop:
   - `NetworkTest` (lobby)
   - `CirullaGame` (game scene)
3. **Ordine consigliato:**
   - Index 0: NetworkTest
   - Index 1: CirullaGame

---

## ?? Come Funziona

### **Flow Completo:**

```
1. Players in lobby, tutti ready
   ?
2. Master Client clicks "Start Game"
   ?
3. RoomManager.StartGame() chiamato
   ?
4. SceneLoadManager.LoadGameScene()
   ?
5. PhotonNetwork.LoadLevel("CirullaGame")
   ? (Photon sincronizza automaticamente)
6. TUTTI i client caricano la stessa scena
   ?
7. SceneLoadManager.OnSceneLoadedCallback()
   ?
8. InitializeGameScene() chiamato
   ?
9. LogPlayerSlots() - mostra setup multiplayer
   ?
10. Game scene pronta! ?
```

---

## ?? Console Output Atteso

### **Quando Start Game:**

**Master Client:**
```
=== STARTING GAME ===
Loading game scene: CirullaGame
Scene loaded: CirullaGame
Initializing game scene for multiplayer...
=== MULTIPLAYER GAME SETUP ===
  Human players: 2
  Bot players: 0
  Total players: 2
  Slot 0: deo (Human)
  Slot 1: moreo (Human)
  Local player is in slot: 0
==============================
```

**Other Clients:**
```
Scene loaded: CirullaGame
Initializing game scene for multiplayer...
=== MULTIPLAYER GAME SETUP ===
  Human players: 2
  Bot players: 0
  Total players: 2
  Slot 0: deo (Human)
  Slot 1: moreo (Human)
  Local player is in slot: 1
==============================
```

**Note:** Local player slot diverso per ogni client!

---

## ?? Testing

### **Test 1: Scene Loading Base**

**Setup:** 2 istanze ParrelSync

**Steps:**
1. **Istanza A**: Create room, ready
2. **Istanza B**: Join room, ready
3. **Istanza A**: Click "Start Game"
4. **Verifica Entrambe**: Scene "CirullaGame" si carica? ?
5. **Check Console**: Log "MULTIPLAYER GAME SETUP" visibile? ?

---

### **Test 2: Player Slots Preserved**

**Steps:**
1. Start game (from test 1)
2. **Check Console logs** in entrambe le istanze
3. **Verifica**:
   - Slot 0: deo ?
   - Slot 1: moreo ?
   - Local player slot diverso ?

---

### **Test 3: Bot Support**

**Steps:**
1. **Istanza A**: Create room
2. **Istanza A**: Add bot
3. **Istanza B**: Join, ready
4. **Istanza A**: Ready, start game
5. **Check Console**:
   ```
   Human players: 2
   Bot players: 1
   Total players: 3
   Slot 0: deo (Human)
   Slot 1: moreo (Human)
   Slot 2: Bot 1 (Bot)
   ```

---

## ?? Troubleshooting

### "SceneLoadManager not found!"

**Causa:** GameObject non ha component  
**Fix:** Add Component ? SceneLoadManager

### "Cannot load scene: Scene not in build"

**Causa:** Scene non in Build Settings  
**Fix:**
1. `File ? Build Settings`
2. Add "CirullaGame" scene
3. Save

### "Only Master Client can load scenes!"

**Causa:** Client (non Master) sta provando a caricare  
**Fix:** Solo Master può caricare scene (è normale!)

### "PhotonNetwork not found" errors

**Causa:** Unity non ha rigenerato i progetti  
**Fix:**
1. Close Visual Studio
2. In Unity: `Assets ? Reimport All`
3. Riapri Visual Studio
4. Wait for IntelliSense refresh

---

## ?? NetworkManager AutoSyncScene

**Verifica** che `AutomaticallySyncScene` sia `true`:

In `NetworkManager.ConfigurePhotonSettings()`:
```csharp
PhotonNetwork.AutomaticallySyncScene = true; // ? Deve essere true!
```

**Perché importante?**
- Se `false`: Solo Master carica scene
- Se `true`: Tutti i client sincronizzati automaticamente ?

---

## ?? Next Steps

### **Dopo Scene Loading Funziona:**

1. **GameManager Integration** (prossimo step!)
   - Modifica GameManager per multiplayer mode
   - Initialize con player slots
   - Setup turn order
   - Disable AI per human players

2. **GameState Sync**
   - NetworkGameController
   - RPC per mosse
   - Turn sync

3. **Full Multiplayer Gameplay**
   - Move sync
   - Accusi sync
   - Score sync

---

## ?? File Modificati

| File | Modifiche | Status |
|------|-----------|--------|
| `SceneLoadManager.cs` | Nuovo file | ? Creato |
| `RoomManager.cs` | +LoadGameScene() | ? Updated |
| `SETUP_SCENE.md` | +SceneLoadManager | ? Updated |

---

## ? Checklist Setup

Prima di testare:

- [ ] Unity Editor refreshed/ricompilato
- [ ] SceneLoadManager aggiunto a NetworkManagers GameObject
- [ ] Scene names corretti in SceneLoadManager.cs
- [ ] Scene aggiunte a Build Settings
- [ ] PhotonNetwork.AutomaticallySyncScene = true
- [ ] Scena salvata
- [ ] Ready per test con ParrelSync

---

## ?? Pro Tips

### **1. Scene Loading Screen**

Aggiungi un loading screen per smooth transition:

```csharp
// In SceneLoadManager.LoadGameScene():
ShowLoadingScreen(); // ? Custom UI
PhotonNetwork.LoadLevel(GAME_SCENE);
```

### **2. Scene Unload**

Quando torni alla lobby, pulisci:

```csharp
// In LoadLobbyScene():
SceneManager.LoadScene(LOBBY_SCENE, LoadSceneMode.Single);
```

### **3. Error Handling**

Gestisci errori di loading:

```csharp
try {
    PhotonNetwork.LoadLevel(scene);
} catch (Exception e) {
    Debug.LogError($"Scene load failed: {e}");
    OnSceneLoadFailed?.Invoke(e.Message);
}
```

---

**Status:** ? Scene Loading System Implementato!  
**File:** `Assets/Networking/SCENE_LOADING_SETUP.md`  
**Next:** GameManager Integration
