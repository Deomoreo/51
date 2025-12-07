# ?? NetworkManager Events Reference

## ?? IMPORTANTE: Nomi Eventi Aggiornati

A causa di conflitti con i callback di Photon PUN 2, tutti gli eventi di `NetworkManager` hanno il suffisso **"Event"**.

---

## ?? Eventi NetworkManager

### Connection Events

#### `OnConnectedToMasterEvent`
```csharp
public event Action OnConnectedToMasterEvent;
```
**Quando:** Connessione al Master Server Photon riuscita  
**Uso:**
```csharp
NetworkManager.Instance.OnConnectedToMasterEvent += () => {
    Debug.Log("Connected!");
};
```

#### `OnConnectionFailedEvent`
```csharp
public event Action<string> OnConnectionFailedEvent;
```
**Quando:** Connessione fallita o disconnessione  
**Parametro:** `string message` - Messaggio di errore  
**Uso:**
```csharp
NetworkManager.Instance.OnConnectionFailedEvent += (message) => {
    Debug.LogError($"Connection failed: {message}");
};
```

---

### Lobby Events

#### `OnJoinedLobbyEvent`
```csharp
public event Action OnJoinedLobbyEvent;
```
**Quando:** Entrato nella lobby Photon  
**Uso:**
```csharp
NetworkManager.Instance.OnJoinedLobbyEvent += () => {
    Debug.Log("In lobby");
};
```

#### `OnLeftLobbyEvent`
```csharp
public event Action OnLeftLobbyEvent;
```
**Quando:** Uscito dalla lobby  

---

### Room Events

#### `OnRoomCreatedEvent`
```csharp
public event Action<Room> OnRoomCreatedEvent;
```
**Quando:** Room creata con successo  
**Parametro:** `Room` - La room appena creata  
**Uso:**
```csharp
NetworkManager.Instance.OnRoomCreatedEvent += (room) => {
    Debug.Log($"Room created: {room.Name}");
};
```

#### `OnJoinedRoomEvent`
```csharp
public event Action<Room> OnJoinedRoomEvent;
```
**Quando:** Entrato in una room (creata o joinata)  
**Parametro:** `Room` - La room corrente  
**Uso:**
```csharp
NetworkManager.Instance.OnJoinedRoomEvent += (room) => {
    Debug.Log($"Joined room: {room.Name}");
    // Inizializza RoomManager
    RoomManager.Instance.OnRoomJoined();
};
```

#### `OnLeftRoomEvent`
```csharp
public event Action OnLeftRoomEvent;
```
**Quando:** Uscito dalla room  

#### `OnRoomJoinFailedEvent`
```csharp
public event Action<string> OnRoomJoinFailedEvent;
```
**Quando:** Fallito join/creazione room  
**Parametro:** `string message` - Messaggio di errore  
**Uso:**
```csharp
NetworkManager.Instance.OnRoomJoinFailedEvent += (message) => {
    Debug.LogError($"Failed to join: {message}");
};
```

---

### Player Events

#### `OnPlayerJoinedRoomEvent`
```csharp
public event Action<Player> OnPlayerJoinedRoomEvent;
```
**Quando:** Un altro giocatore entra nella room  
**Parametro:** `Player` - Il giocatore che è entrato  
**Uso:**
```csharp
NetworkManager.Instance.OnPlayerJoinedRoomEvent += (player) => {
    Debug.Log($"{player.NickName} joined");
};
```

#### `OnPlayerLeftRoomEvent`
```csharp
public event Action<Player> OnPlayerLeftRoomEvent;
```
**Quando:** Un altro giocatore esce dalla room  
**Parametro:** `Player` - Il giocatore che è uscito  
**Uso:**
```csharp
NetworkManager.Instance.OnPlayerLeftRoomEvent += (player) => {
    Debug.Log($"{player.NickName} left");
};
```

---

## ?? Esempio Completo di Utilizzo

```csharp
using UnityEngine;
using Project51.Networking;
using Photon.Realtime;

public class NetworkUIController : MonoBehaviour
{
    private void Start()
    {
        // Subscribe a tutti gli eventi
        SubscribeToNetworkEvents();
    }

    private void OnDestroy()
    {
        // Unsubscribe (IMPORTANTE!)
        UnsubscribeFromNetworkEvents();
    }

    private void SubscribeToNetworkEvents()
    {
        var nm = NetworkManager.Instance;

        // Connection
        nm.OnConnectedToMasterEvent += HandleConnected;
        nm.OnConnectionFailedEvent += HandleConnectionFailed;

        // Lobby
        nm.OnJoinedLobbyEvent += HandleJoinedLobby;
        nm.OnLeftLobbyEvent += HandleLeftLobby;

        // Room
        nm.OnRoomCreatedEvent += HandleRoomCreated;
        nm.OnJoinedRoomEvent += HandleJoinedRoom;
        nm.OnLeftRoomEvent += HandleLeftRoom;
        nm.OnRoomJoinFailedEvent += HandleRoomJoinFailed;

        // Players
        nm.OnPlayerJoinedRoomEvent += HandlePlayerJoined;
        nm.OnPlayerLeftRoomEvent += HandlePlayerLeft;
    }

    private void UnsubscribeFromNetworkEvents()
    {
        if (NetworkManager.Instance == null) return;
        var nm = NetworkManager.Instance;

        // Connection
        nm.OnConnectedToMasterEvent -= HandleConnected;
        nm.OnConnectionFailedEvent -= HandleConnectionFailed;

        // Lobby
        nm.OnJoinedLobbyEvent -= HandleJoinedLobby;
        nm.OnLeftLobbyEvent -= HandleLeftLobby;

        // Room
        nm.OnRoomCreatedEvent -= HandleRoomCreated;
        nm.OnJoinedRoomEvent -= HandleJoinedRoom;
        nm.OnLeftRoomEvent -= HandleLeftRoom;
        nm.OnRoomJoinFailedEvent -= HandleRoomJoinFailed;

        // Players
        nm.OnPlayerJoinedRoomEvent -= HandlePlayerJoined;
        nm.OnPlayerLeftRoomEvent -= HandlePlayerLeft;
    }

    // Event Handlers
    private void HandleConnected()
    {
        Debug.Log("? Connected to Photon!");
    }

    private void HandleConnectionFailed(string message)
    {
        Debug.LogError($"? Connection failed: {message}");
    }

    private void HandleJoinedLobby()
    {
        Debug.Log("?? Joined lobby");
    }

    private void HandleLeftLobby()
    {
        Debug.Log("?? Left lobby");
    }

    private void HandleRoomCreated(Room room)
    {
        Debug.Log($"?? Room created: {room.Name}");
    }

    private void HandleJoinedRoom(Room room)
    {
        Debug.Log($"?? Joined room: {room.Name}");
        
        // IMPORTANTE: Inizializza RoomManager
        RoomManager.Instance.OnRoomJoined();
    }

    private void HandleLeftRoom()
    {
        Debug.Log("?? Left room");
    }

    private void HandleRoomJoinFailed(string message)
    {
        Debug.LogError($"? Room join failed: {message}");
    }

    private void HandlePlayerJoined(Player player)
    {
        Debug.Log($"?? {player.NickName} joined the room");
    }

    private void HandlePlayerLeft(Player player)
    {
        Debug.Log($"?? {player.NickName} left the room");
    }
}
```

---

## ?? Perché il Suffisso "Event"?

### Problema Originale
```csharp
// ? CONFLITTO - Non funziona!
public event Action OnJoinedRoom;  // Evento C#

public override void OnJoinedRoom()  // Callback Photon
{
    // Errore: nome duplicato!
}
```

### Soluzione
```csharp
// ? CORRETTO - Funziona!
public event Action<Room> OnJoinedRoomEvent;  // Evento C# (rinominato)

public override void OnJoinedRoom()  // Callback Photon (override)
{
    OnJoinedRoomEvent?.Invoke(PhotonNetwork.CurrentRoom);  // Trigger evento
}
```

---

## ?? Best Practices

### 1. Sempre Unsubscribe
```csharp
private void OnDestroy()
{
    if (NetworkManager.Instance != null)
    {
        NetworkManager.Instance.OnJoinedRoomEvent -= MyHandler;
    }
}
```

### 2. Null Check
```csharp
NetworkManager.Instance.OnJoinedRoomEvent += (room) => {
    if (room != null)
    {
        // Usa room
    }
};
```

### 3. Inizializza RoomManager
```csharp
NetworkManager.Instance.OnJoinedRoomEvent += (room) => {
    // IMPORTANTE: Chiama sempre dopo join room
    RoomManager.Instance.OnRoomJoined();
};
```

---

## ?? Confronto con Callback Photon Diretti

### Opzione 1: Eventi NetworkManager (Consigliato)
```csharp
// ? Disaccoppiato, facile da gestire
NetworkManager.Instance.OnJoinedRoomEvent += HandleRoom;
```

**Pro:**
- Disaccoppiato dal networking
- Facile subscribe/unsubscribe
- Gestione centralizzata

**Contro:**
- Un layer in più

### Opzione 2: Callback Photon Diretti
```csharp
// ?? Richiede ereditare da MonoBehaviourPunCallbacks
public class MyClass : MonoBehaviourPunCallbacks
{
    public override void OnJoinedRoom()
    {
        // Handle room
    }
}
```

**Pro:**
- Diretto, no layer extra

**Contro:**
- Accoppiato a Photon
- Devi ereditare da MonoBehaviourPunCallbacks
- Più difficile da testare

---

## ?? Quick Reference Table

| Evento | Parametro | Quando |
|--------|-----------|--------|
| `OnConnectedToMasterEvent` | - | Connesso a Photon |
| `OnConnectionFailedEvent` | `string` | Connessione fallita |
| `OnJoinedLobbyEvent` | - | Entrato in lobby |
| `OnLeftLobbyEvent` | - | Uscito da lobby |
| `OnRoomCreatedEvent` | `Room` | Room creata |
| `OnJoinedRoomEvent` | `Room` | Entrato in room |
| `OnLeftRoomEvent` | - | Uscito da room |
| `OnRoomJoinFailedEvent` | `string` | Join room fallito |
| `OnPlayerJoinedRoomEvent` | `Player` | Player entrato |
| `OnPlayerLeftRoomEvent` | `Player` | Player uscito |

---

## ?? Troubleshooting

### "OnConnectedToMaster is a 'method group'"
**Problema:** Stai usando il vecchio nome senza "Event"  
**Soluzione:** Usa `OnConnectedToMasterEvent` invece

### "Event never triggers"
**Problema:** Non sei connesso a Photon  
**Soluzione:** Chiama `NetworkManager.Instance.ConnectToPhoton()` prima

### "NullReferenceException on unsubscribe"
**Problema:** NetworkManager è stato distrutto  
**Soluzione:** Controlla sempre `if (NetworkManager.Instance != null)`

---

**Ultimo aggiornamento:** Dopo fix conflitti eventi  
**File correlati:**
- `Assets/Scripts/Networking/NetworkManager.cs`
- `Assets/Scripts/Networking/NetworkTestUI.cs`
- `Assets/Networking/NEXT_STEPS.md`
