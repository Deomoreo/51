# ?? Setup Scena NetworkTest - Guida Completa

## ? Fix: GameObject Scompare al Play

### **Problema Risolto:**
I Singleton creavano **nuovi GameObject** invece di usare quelli nella scena, causando conflitti e distruzione del GameObject esistente.

### **Soluzione:**
Modificati i Singleton per **NON creare** GameObject automaticamente - devono essere già nella scena!

---

## ?? Setup Completo Scena

### **1. Crea/Verifica GameObject "NetworkManagers"**

Nella tua scena `NetworkTest.unity`:

1. **GameObject esistente?** 
   - Se hai già un GameObject (es: "Net", "NetworkManager", etc), usalo
   - Altrimenti: `GameObject ? Create Empty`
   - **Rinomina**: "NetworkManagers" (o come preferisci)

2. **Add Components** (in ordine):
   ```
   NetworkManagers (GameObject)
   ?? NetworkManager (Script)
   ?? RoomManager (Script)
   ?? PlayerDataManager (Script)
   ?? PhotonView (Component) ? IMPORTANTE!
   ?? NetworkTestUI (Script)
   ```

3. **PhotonView Settings:**
   - **Ownership**: Scene
   - **Observed Components**: Nessuno (o RoomManager se vuoi)
   - **View ID**: Auto-assegnato da Photon

---

## ?? Checklist Setup

### **GameObject Setup:**
- [ ] GameObject "NetworkManagers" creato/verificato
- [ ] NetworkManager component aggiunto
- [ ] RoomManager component aggiunto
- [ ] PlayerDataManager component aggiunto
- [ ] **PhotonView** component aggiunto ? **CRITICO!**
- [ ] NetworkTestUI component aggiunto

### **NetworkTestUI Fields:**
Collega tutti i UI components ai campi pubblici di NetworkTestUI:

**Connection UI:**
- [ ] `nicknameInput` ? InputField nickname
- [ ] `connectButton` ? Connect button
- [ ] `disconnectButton` ? Disconnect button
- [ ] `statusText` ? Status text (TMP)

**Room UI:**
- [ ] `createRoomButton` ? Create Room button
- [ ] `quickMatchButton` ? Quick Match button
- [ ] `roomInfoText` ? Room info text (TMP)

**Room List UI:**
- [ ] `roomListText` ? Room list text (TMP)
- [ ] `refreshRoomListButton` ? Refresh button
- [ ] `joinCodeInput` ? Join code input
- [ ] `joinByCodeButton` ? Join by code button

**Room Control UI:**
- [ ] `readyButton` ? Ready button
- [ ] `readyButtonText` ? Ready button text (child)
- [ ] `startGameButton` ? Start game button
- [ ] `leaveRoomButton` ? Leave room button
- [ ] `addBotButton` ? Add bot button

### **Photon Setup:**
- [ ] Photon PUN 2 installato
- [ ] App ID configurato (Window ? Photon Unity Networking ? PUN Wizard)
- [ ] PhotonServerSettings presente in Assets/

### **Scene Settings:**
- [ ] Scena salvata
- [ ] GameObject con DontDestroyOnLoad? **NO** - lascia normale
- [ ] GameObject attivo nella Hierarchy

---

## ?? Perché PhotonView è Necessario?

Il **PhotonView** è richiesto per gli **RPC** (Remote Procedure Calls) usati per sincronizzare i bot:

```csharp
// In RoomManager.AddBotToSlot():
photonView.RPC(nameof(RPC_BotAdded), RpcTarget.Others, slot, botIndex);
```

**Senza PhotonView:**
- ? Error: `photonView` è null
- ? RPC non funzionano
- ? Bot non sincronizzati tra client

**Con PhotonView:**
- ? RPC funzionano
- ? Bot visibili a tutti
- ? Sincronizzazione corretta

---

## ?? Testing Setup

### **Test 1: GameObject Persiste**
1. Play in Unity Editor
2. **Verifica**: GameObject "NetworkManagers" ancora nella Hierarchy? ?
3. **Verifica**: Tutti i component presenti? ?

### **Test 2: Connessione**
1. Play
2. Inserisci nickname
3. Click "Connect"
4. **Verifica**: Console "Connected to Photon!" ?
5. **Verifica**: Status text verde ?

### **Test 3: Room Creation**
1. Connect
2. Click "Create Room"
3. **Verifica**: Room creata, codice visibile ?
4. **Verifica**: Room info aggiornata ?

### **Test 4: Bot RPC (con ParrelSync)**
1. **Istanza A**: Create room
2. **Istanza B**: Join room
3. **Istanza A**: Add bot
4. **Verifica Istanza B**: Bot visibile? ?
5. **Verifica Console B**: "Received RPC: Bot added" ?

---

## ?? Errori Comuni

### "NetworkManager not found in scene!"
**Causa:** Singleton non trova il component  
**Fix:** Aggiungi NetworkManager component al GameObject

### "RoomManager not found in scene!"
**Causa:** Singleton non trova il component  
**Fix:** Aggiungi RoomManager component al GameObject

### "PlayerDataManager not found in scene!"
**Causa:** Singleton non trova il component  
**Fix:** Aggiungi PlayerDataManager component al GameObject

### "photonView is null"
**Causa:** Manca PhotonView component  
**Fix:** Add Component ? Photon View

### "GameObject scompare al Play"
**Causa:** (Fixato!) Era il vecchio Singleton che creava duplicati  
**Fix:** Usa la versione aggiornata dei Singleton (già fixato)

---

## ?? DontDestroyOnLoad

**Domanda:** Serve DontDestroyOnLoad sul GameObject?

**Risposta:** 
- **Durante testing nella stessa scena:** NO, non serve
- **Quando cambi scena:** SÌ, serve

**Come applicarlo:**
```csharp
// In NetworkManager.Awake():
DontDestroyOnLoad(gameObject); // ? Già presente
```

Il `DontDestroyOnLoad` è già implementato negli script, quindi il GameObject **persisterà automaticamente** quando carichi altre scene (es: dalla lobby alla game scene).

---

## ?? Struttura Scene Finale

```
NetworkTest (Scene)
?? Canvas
?  ?? Connection Panel
?  ?  ?? Nickname Input
?  ?  ?? Connect Button
?  ?  ?? Disconnect Button
?  ?  ?? Status Text
?  ?
?  ?? Room Panel
?  ?  ?? Create Room Button
?  ?  ?? Quick Match Button
?  ?  ?? Room Info Text
?  ?
?  ?? Room List Panel
?  ?  ?? Room List Text
?  ?  ?? Refresh Button
?  ?  ?? Join Code Input
?  ?  ?? Join By Code Button
?  ?
?  ?? Room Control Panel
?     ?? Ready Button
?     ?? Start Game Button
?     ?? Add Bot Button
?     ?? Leave Room Button
?
?? NetworkManagers (GameObject)
   ?? NetworkManager (Script)
   ?? RoomManager (Script)
   ?? PlayerDataManager (Script)
   ?? PhotonView (Component) ? IMPORTANTE!
   ?? NetworkTestUI (Script) ? Tutti i UI collegati qui
```

---

## ?? Quick Setup Guide

### **Veloce (5 minuti):**

1. **Create GameObject:**
   ```
   GameObject ? Create Empty ? "NetworkManagers"
   ```

2. **Add All Components:**
   ```
   Add Component ? NetworkManager
   Add Component ? RoomManager
   Add Component ? PlayerDataManager
   Add Component ? Photon View
   Add Component ? NetworkTestUI
   ```

3. **Create Basic UI:**
   ```
   UI ? Canvas
   UI ? Button (x8: connect, disconnect, create, quick, refresh, joinCode, ready, start)
   UI ? InputField TMP (x2: nickname, joinCode)
   UI ? Text TMP (x3: status, roomInfo, roomList)
   ```

4. **Link Everything:**
   - Drag buttons/texts to NetworkTestUI fields
   - Salva scena

5. **Test:**
   - Play ? Connect ? Create Room ? ?

---

## ?? Next Steps

Dopo aver completato il setup:

1. **Test connection** (solo, senza room)
2. **Test room creation** (crea e vedi info)
3. **Test con ParrelSync** (2 istanze)
4. **Test bot sync** (add bot, verifica RPC)
5. **Test ready system** (ready, start game)

Poi procedi con:
- **Scene loading** (carica game scene al start)
- **GameState sync** (sincronizza gioco vero)
- **Full multiplayer** (turni, mosse, accusi)

---

## ? Verifica Finale

Prima di procedere, verifica:

- [ ] GameObject NON scompare al Play
- [ ] Tutti i component presenti
- [ ] PhotonView configurato
- [ ] UI collegata a NetworkTestUI
- [ ] Connessione a Photon funziona
- [ ] Room creation funziona
- [ ] Console senza errori
- [ ] Ready per ParrelSync test

---

**Status:** ? Setup Completo!  
**File:** `Assets/Networking/SETUP_SCENE.md`  
**Ultima modifica:** Fix Singleton pattern
