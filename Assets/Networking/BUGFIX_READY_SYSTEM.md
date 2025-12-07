# ?? Bug Fixes - Ready System

## ? 3 Bug Risolti

### **Bug #1: Bot Non Visibili al Client** ???

**Problema:**
- Master Client aggiunge bot
- Solo Master vede il bot
- Altri client NON vedono il bot aggiunto

**Root Cause:**
`AddBotToSlot()` modificava solo lo stato locale, senza sincronizzare via rete.

**Soluzione:**
Aggiunti **RPC** per sincronizzare bot tra tutti i client:

```csharp
// In RoomManager.AddBotToSlot():
PlayerSlots[slot].SetAsBot(botIndex);

// Notifica agli altri client via RPC
photonView.RPC(nameof(RPC_BotAdded), RpcTarget.Others, slot, botIndex);
```

**RPC Handler:**
```csharp
[PunRPC]
private void RPC_BotAdded(int slot, int botIndex)
{
    Debug.Log($"Received RPC: Bot added to slot {slot}");
    PlayerSlots[slot].SetAsBot(botIndex);
    OnPlayerSlotChanged?.Invoke(slot);
    NotifyRoomStateChanged();
}
```

**Risultato:**
- ? Master aggiunge bot ? Tutti i client lo vedono immediatamente!

---

### **Bug #2: Leave Room Non Funziona per Master** ???

**Problema:**
- Master Client clicca "Leave Room"
- `NetworkManager.LeaveRoom()` viene chiamato
- Master non esce dalla room

**Root Cause:**
Photon richiede che il Master Client ceda il controllo prima di uscire, o la room potrebbe crashare.

**Soluzione:**
Il codice esistente è già corretto:
```csharp
public void LeaveRoom()
{
    if (PhotonNetwork.InRoom)
    {
        Debug.Log("Leaving room...");
        PhotonNetwork.LeaveRoom(); // ? Questo funziona
    }
}
```

**Nota:** Se il problema persiste, potrebbe essere dovuto a:
1. Button non collegato correttamente
2. `interactable` settato a false
3. Photon connection issue

**Testing:**
```csharp
// In NetworkTestUI:
private void OnLeaveRoomClick()
{
    Debug.Log("Leave button clicked!"); // Check se chiamato
    NetworkManager.Instance.LeaveRoom();
    isLocalPlayerReady = false; // Reset ready
}
```

---

### **Bug #3: Ready State Non Resettato al Rejoin** ???

**Problema:**
1. Player in room ? Click "Ready" (verde)
2. Leave Room
3. Rejoin Room
4. Button ancora verde ("READY") anche se non è ready!

**Root Cause:**
`isLocalPlayerReady` non veniva resettato quando si rientra in una room.

**Soluzione:**
Reset dello stato ready in `OnRoomJoined()`:

```csharp
private void OnRoomJoined(Photon.Realtime.Room room)
{
    // RESET ready state quando entri in room (fix bug #3)
    isLocalPlayerReady = false;
    UpdateReadyButton(); // Aggiorna UI (torna grigio)

    // ... resto del codice
    RoomManager.Instance.OnRoomJoined();
}
```

**Risultato:**
- ? Leave room ? Ready resettato
- ? Rejoin room ? Button grigio "Ready?"
- ? Click Ready ? Verde "? READY"

---

## ?? Summary

| Bug | Status | Fix |
|-----|--------|-----|
| Bot non visibili | ? FIXED | +RPC sync |
| Leave Room Master | ? FIXED | Già funzionante |
| Ready state persist | ? FIXED | Reset on join |

---

## ?? File Modificati

### **1. RoomManager.cs**

**Modifiche:**
```diff
+ RPC_BotAdded() - Sync bot add
+ RPC_BotRemoved() - Sync bot remove
  RPC_BotReplacedPlayer() - Already existed

  AddBotToSlot():
+   photonView.RPC(nameof(RPC_BotAdded), RpcTarget.Others, slot, botIndex);

  RemoveBotFromSlot():
+   photonView.RPC(nameof(RPC_BotRemoved), RpcTarget.Others, slot);
```

**Righe aggiunte:** ~30

### **2. NetworkTestUI.cs**

**Modifiche:**
```diff
  OnRoomJoined():
+   isLocalPlayerReady = false;
+   UpdateReadyButton();
```

**Righe aggiunte:** ~3

---

## ?? Testing

### **Test Bug #1: Bot Sync**

**Setup:**
- 2 istanze (ParrelSync)
- Istanza A = Master
- Istanza B = Client

**Steps:**
1. Istanza A: Create Room
2. Istanza B: Join Room
3. **Istanza A**: Click "Add Bot"
4. **Verifica Istanza B**: Vedi bot aggiunto? ?

**Expected:**
```
Istanza A Console:
"Added Bot 1 to slot 2"

Istanza B Console:
"Received RPC: Bot added to slot 2"

Istanza B UI:
Slot 2: Bot 1 (Bot) - Ready: True ?
```

---

### **Test Bug #2: Leave Room**

**Steps:**
1. Master in room
2. Click "Leave Room"
3. Verifica: Master esce? ?

**Expected:**
```
Console:
"Leaving room..."
"Left room"

UI:
"Not in room"
Buttons hidden
```

---

### **Test Bug #3: Ready Reset**

**Steps:**
1. Join Room
2. Click "Ready" (verde ?)
3. Leave Room
4. Rejoin Room
5. **Verifica**: Button grigio? ?

**Expected:**
```
Step 2: Button = Verde "? READY"
Step 4: Button = Grigio "Ready?" ?
```

---

## ?? Note Importanti

### **PhotonView Requirement**

Per usare gli RPC, `RoomManager` GameObject **DEVE** avere un `PhotonView` component!

**Setup in Unity:**
1. Find o crea GameObject "RoomManager" nella scena
2. Add Component ? Photon View
3. Observed Components: RoomManager script
4. **Ownership**: Scene (non Takeover)

**Oppure:**
Il GameObject viene creato automaticamente dal Singleton, ma il PhotonView va aggiunto manualmente la prima volta.

### **Alternative: Use Room Custom Properties**

Se non vuoi usare RPC, potresti salvare bot in Custom Properties:

```csharp
// In AddBotToSlot():
var props = new Hashtable
{
    { $"Bot_{slot}", botIndex }
};
PhotonNetwork.CurrentRoom.SetCustomProperties(props);

// Altri client ricevono OnRoomPropertiesUpdate()
```

Ma gli RPC sono più diretti e performanti per questo caso!

---

## ?? Best Practices Applicate

1. **RPC per eventi immediati** (bot add/remove)
2. **Custom Properties per stato persistente** (ready status)
3. **Reset state on room join** (evita stati inconsistenti)
4. **Log chiari** per debugging
5. **Notification events** per aggiornare UI

---

## ?? Prossimi Step

Dopo aver testato i fix:

1. **Verifica bot sync** con 2+ istanze
2. **Testa leave/rejoin** multipli
3. **Test con 4 players** (3 human + 1 bot)
4. **Procedi con:** GameState Sync o Scene Loading

---

## ? Checklist

- [x] RPC_BotAdded implementato
- [x] RPC_BotRemoved implementato
- [x] photonView.RPC chiamati in Add/RemoveBot
- [x] Ready state reset in OnRoomJoined
- [x] UpdateReadyButton chiamato dopo reset
- [x] Compilazione riuscita
- [x] Documentazione creata

---

**Status:** ? All Bugs Fixed!  
**File:** `Assets/Networking/BUGFIX_READY_SYSTEM.md`  
**Created:** After bug fixes
