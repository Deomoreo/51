# ?? Room List UI - Guida Setup

## ? Fix Implementato

Abbiamo aggiunto la **Room List** al NetworkTestUI per permettere ai giocatori di:
- ? Vedere tutte le room disponibili
- ? Join tramite codice amico
- ? Join room specifica
- ? Refresh lista automatico

---

## ?? Componenti UI da Aggiungere

### 1. **Room List Panel**

Aggiungi al tuo Canvas questi nuovi elementi:

```
Canvas
?? Panel (Background) [ESISTENTE]
?  ?? ... [componenti esistenti]
?  ?
?  ?? Room List Panel [NUOVO]
?     ?? Title Text: "Available Rooms"
?     ?? Room List Text (TMP) - Scorrevole
?     ?? Refresh Button
?     ?
?     ?? Join By Code Section:
?        ?? Label: "Join by Code:"
?        ?? InputField (TMP): Room Code
?        ?? Join Button
```

### 2. **Layout Suggerito**

#### **Room List Text** (TMP_Text)
- **Name**: `RoomListText`
- **Content Type**: Paragraph
- **Alignment**: Top Left
- **Font Size**: 14-16
- **Auto Size**: ?
- **Rich Text**: ? (per i colori)
- **Vertical Overflow**: Truncate o Scroll
- **Height**: ~300-400 pixels

#### **Refresh Button**
- **Name**: `RefreshRoomListButton`
- **Text**: "?? Refresh Rooms"
- **OnClick**: (collegato da script)

#### **Join Code InputField** (TMP_InputField)
- **Name**: `JoinCodeInput`
- **Character Limit**: 6
- **Content Type**: Alphanumeric
- **Placeholder**: "Enter 6-digit code"
- **Text Transform**: Uppercase

#### **Join By Code Button**
- **Name**: `JoinByCodeButton`
- **Text**: "Join by Code"
- **OnClick**: (collegato da script)

---

## ?? Collegamento Script

Nel **Canvas** (o GameObject con NetworkTestUI):

1. Trascina i nuovi componenti nei campi pubblici:
   - `roomListText` ? Room List Text (TMP)
   - `refreshRoomListButton` ? Refresh Button
   - `joinCodeInput` ? Join Code InputField
   - `joinByCodeButton` ? Join By Code Button

2. Salva la scena

---

## ?? Come Funziona

### **Room List Update**
```csharp
// Photon chiama automaticamente OnRoomListUpdate()
// quando entri in lobby o quando room cambiano
public override void OnRoomListUpdate(List<RoomInfo> roomList)
{
    UpdateCachedRoomList(roomList);  // Aggiorna cache locale
    UpdateRoomListDisplay();         // Aggiorna UI
}
```

### **Join by Code**
```csharp
// Quando premi il button "Join by Code":
private void OnJoinByCodeClick()
{
    string code = joinCodeInput.text.Trim().ToUpper();
    NetworkManager.Instance.JoinRoomByCode(code);
}
```

### **Cached Room List**
```csharp
// Cache locale per performance
private Dictionary<string, RoomInfo> cachedRoomList;

// Aggiornata solo quando Photon notifica cambiamenti
```

---

## ?? Display Format

La lista mostrerà:
```
=== AVAILABLE ROOMS ===

• Room_abc123 [Code: ABC123]
  Players: 2/4
  [Click 'Join by Code' to join]

• Room_def456 [Code: DEF456]
  Players: 1/4
  [Click 'Join by Code' to join]
```

---

## ?? Flow Completo

### **Istanza 1 (Host)**
1. Connect to Photon
2. Click "Create Room"
3. Room viene creata con codice (es: ABC123)
4. **Condividi il codice** con amici

### **Istanza 2 (Client)**
1. Connect to Photon
2. Entra in Lobby (automatico)
3. **Vede la lista room** automaticamente
4. **Opzione A**: Inserisce codice ABC123 ? Join by Code
5. **Opzione B**: Vede "Room_abc123 [Code: ABC123]" nella lista
6. ? **Si unisce alla stessa room!**

---

## ?? Configurazione Photon

Per abilitare la room list, assicurati che in **NetworkManager**:

```csharp
// Quando crei una room:
var roomOptions = new RoomOptions
{
    IsVisible = true,  // ? IMPORTANTE: Room visibile in lobby
    IsOpen = true,
    // ...
};
```

### **Room Visibility**

| Modalità | IsVisible | Nella Lista |
|----------|-----------|-------------|
| **Quick Match** | ? true | SÌ |
| **Private Room** | ? false | NO (solo con code) |
| **Create Room** | ? true | SÌ |

**Nota**: Nel codice attuale, solo Quick Match è `IsVisible = true`.  
Per vedere le room create, devi cambiare in `NetworkManager.cs`:

```csharp
// In CreateRoom():
var roomOptions = new RoomOptions
{
    IsVisible = true,  // Cambia questo se vuoi room visibili
    // ...
};
```

---

## ?? Troubleshooting

### "Room list is empty"
? Assicurati di essere in Lobby (`PhotonNetwork.InLobby`)  
? Room create devono avere `IsVisible = true`  
? Premi "Refresh" per aggiornare

### "Room disappeared from list"
? Room si nasconde quando diventa piena  
? Room si nasconde quando `IsOpen = false`  
? Room viene rimossa quando tutti escono

### "Can't join by code"
? Verifica che il codice sia esatto (6 caratteri)  
? Room deve esistere ancora  
? Room non deve essere piena

---

## ?? Miglioramenti Futuri

### **Room List Scrollabile**
Aggiungi `ScrollView` per tante room:
```
ScrollView
?? Viewport
   ?? Content
      ?? RoomListText (TMP)
```

### **Room Buttons**
Invece di text, crea button per ogni room:
```csharp
// Genera dynamicamente
foreach (var room in cachedRoomList.Values)
{
    var button = Instantiate(roomButtonPrefab, roomListContainer);
    button.GetComponentInChildren<TMP_Text>().text = room.Name;
    button.onClick.AddListener(() => JoinRoomByName(room.Name));
}
```

### **Auto Refresh**
Aggiorna automaticamente ogni X secondi:
```csharp
InvokeRepeating(nameof(UpdateRoomListDisplay), 0f, 5f);  // Ogni 5 sec
```

### **Filtri**
Filtra per modalità, numero giocatori, etc:
```csharp
var filteredRooms = cachedRoomList.Values
    .Where(r => r.PlayerCount < r.MaxPlayers)
    .Where(r => r.IsOpen)
    .ToList();
```

---

## ?? Esempio UI Layout Completo

### **Quick Setup (Minimal)**
```
Canvas
?? Connection Panel
?  ?? Nickname Input
?  ?? Connect Button
?  ?? Status Text
?
?? Room Panel
?  ?? Create Room Button
?  ?? Quick Match Button
?  ?
?  ?? Join Section
?     ?? Room List Text (200x300)
?     ?? Refresh Button
?     ?? Code Input
?     ?? Join by Code Button
?
?? Game Panel
   ?? Room Info Text
```

### **Advanced Setup (Con Scroll)**
```
Canvas
?? ... (come sopra)
?
?? Room List Panel (400x500)
   ?? Title: "Rooms"
   ?? Refresh Button
   ?
   ?? ScrollView
   ?  ?? Viewport
   ?     ?? Content (Dynamic)
   ?        ?? Room Button 1
   ?        ?? Room Button 2
   ?        ?? ...
   ?
   ?? Join by Code Section
      ?? Code Input
      ?? Join Button
```

---

## ? Testing

### **Test con 2 Istanze:**

1. **Istanza A**:
   - Connect
   - Create Room
   - Vedi codice generato (es: ABC123)

2. **Istanza B**:
   - Connect
   - Guarda Room List
   - Dovrebbe vedere room di A
   - Join by Code: ABC123
   - ? **Successo!**

### **Verifica Display:**
- Room list si aggiorna quando entri in lobby
- Room scompare quando piena
- Code è visibile nella lista
- Refresh button funziona

---

## ?? Risultato Finale

Ora puoi:
- ? **Vedere tutte le room** disponibili
- ? **Join con codice** amici
- ? **Join da lista** visuale
- ? **Refresh manuale** se necessario
- ? **Testing multiplayer** facile!

**Prossimo step:** Testa con ParrelSync! ??

---

**File Modificato:** `Assets/Scripts/Networking/NetworkTestUI.cs`  
**Nuove Features:**
- `OnRoomListUpdate()` callback
- `UpdateRoomListDisplay()` display
- `JoinByCodeClick()` join logic
- `cachedRoomList` cache locale
