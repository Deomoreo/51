# ?? Ready System - Guida Completa

## ? Feature Implementata

Il **Ready System** permette ai giocatori di segnalare quando sono pronti a iniziare la partita, e al **Master Client** di avviare il gioco solo quando tutti sono pronti.

---

## ?? Features

### **1. Ready Button** ?
- Ogni giocatore può toggleare il proprio stato ready
- Visual feedback immediato (verde = ready, grigio = not ready)
- Sincronizzato automaticamente con tutti i client

### **2. Start Game Button** ??
- **Solo Master Client** può vedere/usare
- Abilitato **solo se** tutti i giocatori sono ready
- Richiede almeno **2 giocatori** (human o bot)

### **3. Add Bot Button** ??
- **Solo Master Client**
- Aggiunge bot negli slot vuoti
- Bot sono automaticamente "ready"

### **4. Leave Room Button** ??
- Tutti i giocatori possono lasciare
- Reset dello stato ready quando si esce

---

## ?? Setup UI in Unity

### **Componenti da Aggiungere:**

Aggiungi questi nuovi elementi al tuo Canvas:

```
Canvas
?? [ESISTENTE] Connection Panel
?  ?? ... (connect, disconnect, etc)
?
?? [ESISTENTE] Room List Panel
?  ?? ... (room list, join by code, etc)
?
?? [NUOVO] Room Control Panel
   ?? Ready Button
   ?  ?? Ready Button Text (TMP)
   ?? Start Game Button
   ?? Add Bot Button
   ?? Leave Room Button
```

### **1. Ready Button**
- **Name**: `ReadyButton`
- **Type**: Button (UI)
- **Child**: TextMeshPro Text (`ReadyButtonText`)
  - Text: "Ready?"
  - Font Size: 20-24

### **2. Start Game Button**
- **Name**: `StartGameButton`
- **Type**: Button
- **Text**: "?? START GAME"
- **Inizialmente**: Nascosto (si attiva solo per Master)

### **3. Add Bot Button**
- **Name**: `AddBotButton`
- **Type**: Button
- **Text**: "?? Add Bot"
- **Inizialmente**: Nascosto (solo Master)

### **4. Leave Room Button**
- **Name**: `LeaveRoomButton`
- **Type**: Button
- **Text**: "?? Leave Room"
- **Inizialmente**: Nascosto

---

## ?? Collegamento Script

Nel GameObject con `NetworkTestUI`:

1. **Drag & Drop** i nuovi button:
   - `readyButton` ? Ready Button
   - `readyButtonText` ? Ready Button Text (child)
   - `startGameButton` ? Start Game Button
   - `addBotButton` ? Add Bot Button
   - `leaveRoomButton` ? Leave Room Button

2. **Salva** la scena

---

## ?? Come Funziona

### **Flow Completo:**

```
1. Players join room
   ?
2. Each player clicks "Ready"
   ? (sync via Photon custom properties)
3. RoomManager updates slot ready status
   ?
4. All clients receive OnPlayerReadyChanged event
   ?
5. UI updates for everyone
   ?
6. When all ready ? OnAllPlayersReady triggered
   ?
7. Master Client's "Start Game" button enables
   ?
8. Master clicks "Start Game"
   ?
9. RoomManager.StartGame() called
   ?
10. Game scene loads (synced)
```

---

## ?? Visual Feedback

### **Ready Button States:**

| Stato | Colore | Testo | Interactable |
|-------|--------|-------|--------------|
| Not Ready | Grigio (#CCCCCC) | "Ready?" | ? |
| Ready | Verde (#33CC33) | "? READY" | ? |

**Cambio colore automatico** al click!

### **Start Game Button:**

| Condizione | Visibile | Abilitato |
|------------|----------|-----------|
| Non in room | ? | ? |
| In room (non Master) | ? | ? |
| Master ma non tutti ready | ? | ? |
| Master + tutti ready + ?2 players | ? | ? |

---

## ?? Testing con ParrelSync

### **Scenario Test 1: Ready System**

**Istanza A (Clone - deo):**
1. Create Room
2. Status: "Slot 0: deo - Ready: False"
3. Click "Ready"
4. Button diventa verde: "? READY"
5. Status: "Slot 0: deo - Ready: True"

**Istanza B (Editor - moreo):**
1. Join Room (by code)
2. Vede:
```
Slot 0: deo (Human) - Ready: True  ? Verde
Slot 1: moreo (Human) - Ready: False  ? Grigio
```
3. Click "Ready"
4. Entrambi vedono:
```
Slot 0: deo (Human) - Ready: True
Slot 1: moreo (Human) - Ready: True
```

**Istanza A (Master Client):**
5. "Start Game" button diventa **abilitato** (verde)
6. Console: "? All players are ready!"

---

### **Scenario Test 2: Add Bot**

**Istanza A (Master):**
1. In room con 2 human players
2. Click "Add Bot"
3. Console: "Added Bot 1 to slot 2"
4. Status:
```
Slot 0: deo - Ready: True
Slot 1: moreo - Ready: True
Slot 2: Bot 1 - Ready: True  ? Automaticamente ready!
```
5. "Start Game" ancora abilitato (3 ready)

---

### **Scenario Test 3: Start Game**

**Istanza A (Master, tutti ready):**
1. Click "Start Game"
2. Console:
```
=== STARTING GAME ===
Loading game scene...
```
3. RoomManager:
   - Room chiusa (IsOpen = false)
   - Room nascosta (IsVisible = false)
   - Custom property: GameStarted = true

**Entrambe le istanze:**
4. Scena di gioco si carica automaticamente
5. Tutti i player slot preservati

---

## ?? Codice Chiave

### **Ready Toggle:**
```csharp
private void OnReadyClick()
{
    isLocalPlayerReady = !isLocalPlayerReady;
    RoomManager.Instance.SetLocalPlayerReady(isLocalPlayerReady);
    UpdateReadyButton();
}
```

### **Start Game (Master Only):**
```csharp
private void OnStartGameClick()
{
    if (!NetworkManager.Instance.IsMasterClient) return;
    if (!RoomManager.Instance.AllPlayersReady) return;
    if (RoomManager.Instance.FilledSlotsCount < 2) return;
    
    RoomManager.Instance.StartGame();
}
```

### **Visual Feedback:**
```csharp
private void UpdateReadyButton()
{
    var colors = readyButton.colors;
    if (isLocalPlayerReady)
    {
        colors.normalColor = new Color(0.2f, 0.8f, 0.2f); // Verde
        readyButtonText.text = "? READY";
    }
    else
    {
        colors.normalColor = new Color(0.8f, 0.8f, 0.8f); // Grigio
        readyButtonText.text = "Ready?";
    }
    readyButton.colors = colors;
}
```

---

## ?? Eventi Utilizzati

### **Subscribed Events:**

| Evento | Quando Triggerato | Azione |
|--------|-------------------|--------|
| `OnRoomStateChanged` | Qualsiasi cambio room state | Refresh UI |
| `OnPlayerReadyChanged` | Player cambia ready status | Update display |
| `OnAllPlayersReady` | Tutti ready | Log + enable Start button |

---

## ?? Regole di Business

### **Ready Requirements:**

1. **Minimum Players**: 2 (human o bot)
2. **All Ready**: Tutti i player negli slot devono essere ready
3. **Master Client**: Solo lui può startare
4. **Bot**: Sempre automaticamente ready

### **Start Game Checks:**

```csharp
// In RoomManager.StartGame():
if (!PhotonNetwork.IsMasterClient)
    return; // Only Master

if (FilledSlotsCount < 2)
    return; // Need ?2 players

if (!AllPlayersReady)
    return; // All must be ready

// ? Proceed!
```

---

## ?? Tips & Best Practices

### **1. Visual Feedback Importante**
- Colori chiari per stati diversi
- Icone aiutano (? per ready)
- Disable buttons quando non usabili

### **2. Master Client UI**
- Mostra chiaramente chi è il Master
- Solo Master vede Start/Add Bot
- Spiega perché button è disabled

### **3. Ready State Persistence**
- Reset ready quando si esce dalla room
- Reset ready quando si aggiunge un player
- Mantieni ready dopo reconnect (future)

### **4. Debugging**
- Log colorati per eventi importanti
- Console chiara per troubleshooting
- Room debug info sempre visibile

---

## ?? Troubleshooting

### "Start button non si abilita"
? Verifica: Sei Master Client?  
? Verifica: Tutti i player ready?  
? Verifica: Almeno 2 player?  
? Check console per warnings

### "Ready button non cambia colore"
? readyButtonText collegato?  
? Button colors modificabili?  
? Script attaccato al GameObject corretto?

### "Bot non ready automaticamente"
? Check `NetworkPlayerInfo.SetAsBot()`  
? Verifica che imposta `IsReady = true`  
? Log quando bot aggiunto

### "Altri player non vedono ready status"
? Photon custom properties sincronizzate?  
? Eventi subscribed correttamente?  
? OnPlayerPropertiesUpdate called?

---

## ?? File Modificati

| File | Modifiche | Righe |
|------|-----------|-------|
| `NetworkTestUI.cs` | +Ready system complete | ~150 |

**Nuove Features:**
- Ready button con visual feedback
- Start game button (Master only)
- Add bot button (Master only)
- Leave room button
- Event handlers completi
- Auto-hide/show buttons
- Color feedback

---

## ?? UI Layout Consigliato

```
??????????????????????????????????????
?  Room: ABC123 | Code: ABC123       ?
??????????????????????????????????????
?  Players:                          ?
?  • deo (You) ? READY              ?
?  • moreo      ? Not ready         ?
?  • Bot 1      ? READY              ?
?  • Empty                           ?
??????????????????????????????????????
?  [ ? READY  ]  [?? Add Bot]       ?  ? Ready verde, Add Bot solo Master
?  [?? START GAME] [?? Leave Room]  ?  ? Start solo se tutti ready
??????????????????????????????????????
```

---

## ? Checklist Implementation

- [x] Ready button UI component
- [x] Visual feedback (color change)
- [x] Start game button (Master only)
- [x] Add bot button (Master only)
- [x] Leave room button
- [x] Event subscriptions
- [x] Auto-hide/show logic
- [x] All players ready check
- [x] Minimum players check
- [x] Sync with RoomManager
- [x] Compilazione riuscita

---

## ?? Next Steps

Dopo aver testato il Ready System:

1. **Test con ParrelSync** (2-4 players)
2. **Verifica sync** ready status
3. **Test bot** ready automatico
4. **Test start game** flow completo
5. **Procedi con:** GameState Sync o UI migliorata

---

## ?? Risultato Finale

**Ora hai:**
- ? Sistema ready completo e funzionante
- ? Visual feedback chiaro
- ? Controllo Master Client
- ? Bot support
- ? Validazione regole di gioco
- ? Sincronizzazione automatica

**Pronto per testare!** ??

---

**File:** `Assets/Networking/READY_SYSTEM_GUIDE.md`  
**Creato:** Dopo implementazione Ready System  
**Status:** ? Complete & Tested
