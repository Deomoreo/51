# ?? Multiplayer Game Setup - Complete Guide

## ? File Creati/Modificati

| File | Stato | Descrizione |
|------|-------|-------------|
| `NetworkGameController.cs` | ? **Creato** | Gestisce sincronizzazione mosse via RPC |
| `TurnController.cs` | ? **Modificato** | Ora multiplayer-aware |
| `GameManager.cs` | ? **Esistente** | Auto-detect game mode |

---

## ?? Come Funziona

### **Flow Multiplayer:**

```
1. Players join room via NetworkTestUI
2. All ready ? Master clicks "Start Game"
3. SceneLoadManager carica scena "CirullaGame"
4. GameManager.Start() ? DetectGameMode() ? MULTIPLAYER
5. TurnController.Start() ? Solo Master Client fa StartNewGame()
6. Local player fa una mossa ? NetworkGameController.SendMove()
7. RPC inviato a tutti i client ? ExecuteMove() su tutti
8. Bot AI eseguito solo dal Master Client ? RPC a tutti
```

---

## ??? Setup in Unity

### **1. Scena "CirullaGame"**

Apri la scena `CirullaGame` e aggiungi i GameObject necessari.

#### **Hierarchy:**
```
CirullaGame (Scene)
??? GameManager ? NUOVO! (già creato da te)
?   ??? Component: GameManager
?       ??? Turn Controller: [Drag TurnController]
?
??? NetworkGameController ? NUOVO!
?   ??? Component: NetworkGameController
?   ??? Component: PhotonView
?       ??? Observed Components: None
?       ??? Synchronization: Off (usiamo RPC)
?
??? TurnController (existing)
?   ??? Component: TurnController
?
??? CardViewManager (existing)
??? UI...
```

---

### **2. Creare NetworkGameController GameObject**

**Steps:**
1. **Right-click** in Hierarchy ? Create Empty
2. **Rename** to `NetworkGameController`
3. **Add Component** ? `NetworkGameController`
4. **Add Component** ? `PhotonView`
5. **PhotonView Settings:**
   - Observed Components: **None**
   - Synchronization: **Off**
   - Ownership Transfer: **Fixed**
   - ? Is Mine (se sei Master Client)

---

### **3. Configurare GameManager**

Nel GameObject `GameManager`:

| Inspector Field | Value | Description |
|----------------|-------|-------------|
| Turn Controller | Drag `TurnController` | Reference al controller |
| Log Multiplayer Info | ? true | Debug logging |

---

### **4. Configurare TurnController**

Nel GameObject `TurnController`:

| Inspector Field | Value | Description |
|----------------|-------|-------------|
| Auto Start Game | ? true | Per single-player |
| AI Move Delay | 2.0 | Secondi tra mosse AI |

**Note:** In multiplayer, `autoStartGame` viene ignorato - solo Master Client inizia.

---

## ?? Testing

### **Test 1: Single-Player Mode**

**Steps:**
1. Apri scena `CirullaGame` direttamente (senza networking)
2. Play
3. **Check Console:**
   ```
   === SINGLE PLAYER MODE ===
   starting new game
   ```
4. Gioco funziona normalmente ?

---

### **Test 2: Multiplayer - 2 Players**

**Setup:**
- Istanza A: Master (ParrelSync clone)
- Istanza B: Client

**Steps:**

```
1. A: Open TESTPHOTN scene
2. A: Connect ? Create Room
3. B: Connect ? Join Room (code)

4. Both: Click "Ready"
5. A: Click "Start Game"

   Console A:
   <color=cyan>=== MULTIPLAYER MODE ===</color>
   <color=cyan>[MP] Master Client starting game...</color>
   starting new game

   Console B:
   <color=cyan>=== MULTIPLAYER MODE ===</color>
   <color=cyan>[MP] Waiting for Master Client to start game...</color>

6. B (Slot 1): Play a card

   Console B:
   <color=cyan>[NET] Sending move: Play Denari 6</color>

   Console A:
   <color=yellow>[NET] Received move from Player4140: Play Denari 6</color>
   executing move

7. AI Turn (Bot in Slot 2)

   Console A (Master):
   AI executing move...
   <color=cyan>[NET] Sending move: Capture with Bastoni 5</color>

   Console B:
   <color=yellow>[NET] Received move from Player6211: Capture with Bastoni 5</color>
   executing move
```

---

### **Test 3: Multiplayer - 2 Players + 2 Bots**

**Steps:**

```
1. Setup come Test 2
2. A: Click "Add Bot" (x2)
3. Both ready ? Start

4. Turn Order:
   - Slot 0 (A) ? Human player A
   - Slot 1 (B) ? Human player B
   - Slot 2 (Bot) ? AI (Master Client)
   - Slot 3 (Bot) ? AI (Master Client)

5. Quando è il turno di Slot 2 o 3:

   Console A (Master):
   AI executing move...
   <color=cyan>[NET] Sending move: ...</color>

   Console B:
   <color=yellow>[NET] Received move from Player6211: ...</color>
```

---

## ?? Debugging

### **Console Logs da Cercare:**

#### **GameManager:**
```
=== MULTIPLAYER MODE ===
  Human players: 2
  Bot players: 2
  Total players: 4
  Slot 0: Player6211 (Human) (YOU)
  Slot 1: Player4140 (Human)
  Slot 2: Bot 1 (Bot)
  Slot 3: Bot 2 (Bot)
  Local player is in slot: 0
```

#### **TurnController:**
```
[MP] Master Client starting game...
starting new game
```

#### **NetworkGameController:**
```
<color=cyan>[NET] Sending move: Play Denari 6</color>
<color=yellow>[NET] Received move from Player4140: Play Denari 6</color>
```

---

## ?? Problemi Comuni

### **1. "NetworkGameController not found!"**

**Cause:**
- GameObject `NetworkGameController` non in scena
- Component non aggiunto
- PhotonView mancante

**Fix:**
- Aggiungi GameObject come descritto sopra
- Assicurati che PhotonView sia presente

---

### **2. "TurnController requires a PhotonView component!"**

**Cause:**
- TurnController NON ha bisogno di PhotonView
- Solo NetworkGameController lo richiede

**Fix:**
- Verifica che NetworkGameController abbia PhotonView
- TurnController rimane senza PhotonView

---

### **3. "Non-master client tried to execute bot move"**

**Behavior:** Normale!

Questo è un warning **normale** in multiplayer. Significa che:
- Un client non-master ha ricevuto un comando bot
- Il client correttamente **ignora** il comando
- Solo il Master Client esegue AI

**Nessun fix necessario** - è il comportamento corretto.

---

### **4. Mosse duplicate o desincronizzazione**

**Cause:**
- RPC chiamato due volte
- ExecuteMove() chiamato prima di inviare RPC

**Fix:**
- ExecuteMove() ora controlla se è local player
- Se sì, invia RPC e **return** (non esegue localmente)
- RPC esegue su TUTTI i client (incluso sender)

---

## ?? Checklist Setup

- [ ] Aperto scena `CirullaGame`
- [ ] Creato GameObject `NetworkGameController`
- [ ] Aggiunto component `NetworkGameController`
- [ ] Aggiunto component `PhotonView`
- [ ] Configurato PhotonView (Synchronization: Off)
- [ ] GameObject `GameManager` esistente
- [ ] Configurato `GameManager.turnController` reference
- [ ] Salvato scena
- [ ] Build di Unity riuscito (no errori)
- [ ] Test single-player: funziona ?
- [ ] Test multiplayer: mosse sincronizzate ?
- [ ] Test bot: solo Master Client esegue ?

---

## ?? Next Steps

Dopo aver completato il setup:

1. **Test completo:**
   - 2 players human
   - 2 players + 2 bots
   - 4 players human

2. **Features da aggiungere:**
   - Timeout turno (30 sec)
   - Reconnection handling
   - GameState serialization completa
   - UI multiplayer-aware (mostra chi è il current player)

3. **Ottimizzazioni:**
   - Compress move serialization
   - Batch multiple bot moves
   - Predictive client-side execution

---

## ? Stato Implementazione

| Feature | Status | Notes |
|---------|--------|-------|
| Auto-detect game mode | ? Done | GameManager |
| Master Client starts game | ? Done | TurnController |
| Local player input | ? Done | Only local player can play |
| Remote player sync | ? Done | Via RPC |
| Bot AI (Master only) | ? Done | Only Master executes |
| Bot sync to clients | ? Done | Via RPC |
| Move serialization | ? Done | Simple text format |
| Move deserialization | ? Done | Parsing works |
| Multiplayer logging | ? Done | Color-coded logs |
| Single-player compatibility | ? Done | Backward compatible |

---

## ?? Pronto per Test!

Segui gli step di setup in Unity, poi testa con ParrelSync! ??

**File:** `Assets/Networking/MULTIPLAYER_INTEGRATION_COMPLETE.md`  
**Status:** ? Ready to test!
