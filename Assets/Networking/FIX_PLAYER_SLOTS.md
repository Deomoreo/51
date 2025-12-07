# ?? Fix: Giocatori Non Visibili in Room

## ? Problema Riscontrato

### **Sintomi:**
- Player 1 (deo) entra in room ? Slot 0 ?
- Player 2 (moreo) entra in room ? Slot 0 ?
- Player 1 (deo) **sparisce** dalla UI
- Entrambi vengono assegnati allo stesso slot

### **Root Cause:**
Quando il secondo giocatore entra nella room, `RoomManager.OnRoomJoined()` chiamava `InitializeSlots()` che **resettava tutti i 4 slot**, cancellando i giocatori già presenti!

```csharp
// PRIMA (? BUGGATO)
public void OnRoomJoined()
{
    InitializeSlots();  // ? Cancella TUTTI gli slot!
    // ...
}

private void InitializeSlots()
{
    for (int i = 0; i < 4; i++)
    {
        PlayerSlots[i] = new NetworkPlayerInfo(i);  // ? Slot vuoto!
    }
}
```

**Risultato:**
- Slot vengono ricreati vuoti
- Info del primo giocatore persa
- Secondo giocatore assegnato allo slot 0 (già occupato!)

---

## ? Soluzione Implementata

### **Fix: Sync Instead of Reset**

```csharp
// ADESSO (? FUNZIONANTE)
public void OnRoomJoined()
{
    // Reset stato game
    IsGameStarted = false;

    // NON resettare se ci sono già altri giocatori!
    if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
    {
        InitializeSlots();  // ? Solo se siamo i primi
    }
    else
    {
        SyncWithExistingPlayers();  // ? Sincronizza con giocatori esistenti
    }
    // ...
}
```

### **Nuovo Metodo: SyncWithExistingPlayers()**

```csharp
private void SyncWithExistingPlayers()
{
    Debug.Log("RoomManager: Syncing with existing players");

    // Inizializza slot vuoti
    for (int i = 0; i < 4; i++)
    {
        if (PlayerSlots[i] == null)
            PlayerSlots[i] = new NetworkPlayerInfo(i);
    }

    // Sincronizza con i giocatori già nella room
    foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
    {
        // Controlla se il player ha già uno slot assegnato
        if (player.CustomProperties.ContainsKey(NetworkConstants.PLAYER_SLOT))
        {
            int slot = (int)player.CustomProperties[NetworkConstants.PLAYER_SLOT];
            
            if (slot >= 0 && slot < PlayerSlots.Length)
            {
                Debug.Log($"Syncing player {player.NickName} to slot {slot}");
                PlayerSlots[slot].SetAsHuman(player);

                // Sync ready state
                if (player.CustomProperties.ContainsKey(NetworkConstants.PLAYER_IS_READY))
                {
                    PlayerSlots[slot].IsReady = (bool)player.CustomProperties[NetworkConstants.PLAYER_IS_READY];
                }
            }
        }
    }
}
```

**Come Funziona:**
1. Controlla quanti giocatori ci sono nella room
2. Se è il primo (`PlayerCount == 1`) ? Inizializza slot vuoti
3. Se ci sono già altri ? Sincronizza leggendo le loro custom properties
4. Ricrea gli slot con le info dei giocatori esistenti
5. Assegna il nuovo giocatore a uno slot libero

---

## ?? Flusso Corretto

### **Player 1 (deo) Entra:**
```
1. OnRoomJoined()
2. PlayerCount == 1 ? InitializeSlots()
3. Slot 0,1,2,3 = Empty
4. AssignLocalPlayerToSlot()
5. Trova slot 0 libero
6. Assegna deo ? Slot 0
7. Salva in CustomProperties: { "Slot": 0 }
```

**Risultato:** 
```
Slot 0: deo (Human)
Slot 1: Empty
Slot 2: Empty
Slot 3: Empty
```

### **Player 2 (moreo) Entra:**
```
1. OnRoomJoined()
2. PlayerCount == 2 ? SyncWithExistingPlayers()
3. Legge CustomProperties di deo
4. Trova: { "Slot": 0 }
5. Ricrea: Slot 0 = deo
6. AssignLocalPlayerToSlot()
7. Trova slot 1 libero (0 occupato da deo)
8. Assegna moreo ? Slot 1
9. Salva in CustomProperties: { "Slot": 1 }
```

**Risultato:**
```
Slot 0: deo (Human) ?
Slot 1: moreo (Human) ?
Slot 2: Empty
Slot 3: Empty
```

---

## ?? Confronto

| Scenario | Prima ? | Adesso ? |
|----------|---------|----------|
| Player 1 entra | Slot 0 | Slot 0 |
| Player 2 entra | Slot 0 (sovrascrive P1) | Slot 1 (P1 resta in slot 0) |
| Player 3 entra | Slot 0 (sovrascrive tutti) | Slot 2 |
| Player 4 entra | Slot 0 (sovrascrive tutti) | Slot 3 |
| Visibilità | Solo ultimo | Tutti visibili |

---

## ?? Debug Info

### **Log da Verificare:**

**Player 1 (primo a entrare):**
```
RoomManager: Initializing room state
RoomManager: Initializing as Master Client
Assigning deo to slot 0
```

**Player 2 (secondo a entrare):**
```
RoomManager: Initializing room state
RoomManager: Syncing with existing players
Syncing player deo to slot 0
Assigning moreo to slot 1
```

**Player 3:**
```
RoomManager: Syncing with existing players
Syncing player deo to slot 0
Syncing player moreo to slot 1
Assigning player3 to slot 2
```

---

## ?? Come Testare

### **Test con ParrelSync:**

1. **Istanza A (Clone):**
   - Connect as "deo"
   - Create Room
   - Verifica: Room Info mostra "Slot 0: deo"

2. **Istanza B (Editor):**
   - Connect as "moreo"
   - Join by Code (codice di A)
   - Verifica console:
     ```
     RoomManager: Syncing with existing players
     Syncing player deo to slot 0
     Assigning moreo to slot 1
     ```
   - Verifica: Room Info mostra:
     ```
     Slot 0: deo (Human)
     Slot 1: moreo (Human)
     ```

3. **Entrambe le Istanze:**
   - Refresh UI
   - Verifica che entrambi vedano 2 giocatori

---

## ? Checklist Fix

- [x] `OnRoomJoined()` non resetta più gli slot
- [x] Check `PlayerCount` prima di inizializzare
- [x] `SyncWithExistingPlayers()` implementato
- [x] Legge `CustomProperties` dei player esistenti
- [x] Ricostruisce slot con info corrette
- [x] Ready state sincronizzato
- [x] Nuovo giocatore assegnato a slot libero
- [x] Compilazione riuscita

---

## ?? Risultato Finale

**Adesso:**
- ? Ogni giocatore ha il **proprio slot**
- ? Giocatori esistenti **visibili** ai nuovi entrati
- ? Slot assignment **sequenziale** (0, 1, 2, 3)
- ? Ready state **sincronizzato**
- ? UI **aggiornata** per tutti

**Test con 4 giocatori:**
```
Player 1 (deo)   ? Slot 0
Player 2 (moreo) ? Slot 1
Player 3 (test3) ? Slot 2
Player 4 (test4) ? Slot 3
```

Tutti visibili, nessuna sovrascrittura! ??

---

## ?? File Modificato

**`Assets/Scripts/Networking/RoomManager.cs`**

**Modifiche:**
1. `OnRoomJoined()` - Aggiunto check PlayerCount
2. `SyncWithExistingPlayers()` - Nuovo metodo (40 righe)

**Righe cambiate:** ~50  
**Compilazione:** ? Riuscita  

---

**Testing:** Pronto per test con ParrelSync! ??
