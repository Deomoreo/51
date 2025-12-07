# ?? PROSSIMI PASSI - Guida Pratica

## ? Completato Oggi (Foundation)

Abbiamo creato la base completa per il sistema multiplayer:

### 1. Networking Core
- ? `NetworkManager.cs` - Gestione connessioni Photon
- ? `RoomManager.cs` - Lobby, slot, bot management  
- ? `PlayerDataManager.cs` - Statistiche locali
- ? `NetworkTypes.cs` - Tipi e costanti
- ? `Project51.Networking.asmdef` - Assembly definition

### 2. Documentazione
- ? `SETUP_PHOTON_PUN2.md` - Guida installazione Photon
- ? `ARCHITECTURE.md` - Architettura completa sistema
- ? `MULTIPLAYER_README.md` - Overview progetto

---

## ?? STEP 1: Installare Photon PUN 2

### A. Download & Import
1. Apri Unity Asset Store in Unity (Window ? Asset Store)
2. Cerca "**Photon PUN 2 FREE**"
3. Download & Import
4. Accetta tutto (lascia tutte le checkbox selezionate)

### B. Configurazione Account
1. Vai su https://www.photonengine.com/
2. Crea account / Login
3. Dashboard ? Create New App:
   - **Photon Type**: PUN
   - **Name**: "Project51_Cirulla"
   - Click **Create**
4. Copia l'**App ID** mostrato

### C. Setup in Unity
1. In Unity: Window ? Photon Unity Networking ? PUN Wizard
2. Incolla l'App ID copiato
3. Click "**Setup Project**"
4. Verifica che sia apparso `Assets/Photon/` folder

**? Checkpoint**: Dovresti vedere `PhotonServerSettings` in Assets/

---

## ?? STEP 2: Testare la Connessione Base

### A. Crea Test Scene
1. Crea nuova scena: `Assets/Scenes/NetworkTest.unity`
2. Salva la scena

### B. Aggiungi NetworkManager
1. Crea Empty GameObject: "**NetworkManagers**"
2. Add Component ? **NetworkManager** (il nostro script)
3. Add Component ? **RoomManager**
4. Add Component ? **PlayerDataManager**

### C. Crea Script di Test
Crea `Assets/Scripts/Networking/NetworkTestUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Networking;

public class NetworkTestUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nicknameInput;
    public Button connectButton;
    public Button disconnectButton;
    public Button createRoomButton;
    public Button quickMatchButton;
    public TMP_Text statusText;
    public TMP_Text roomInfoText;

    private void Start()
    {
        // Setup buttons
        connectButton.onClick.AddListener(OnConnectClick);
        disconnectButton.onClick.AddListener(OnDisconnectClick);
        createRoomButton.onClick.AddListener(OnCreateRoomClick);
        quickMatchButton.onClick.AddListener(OnQuickMatchClick);

        // Subscribe to events (NOTE: nomi con suffisso "Event")
        NetworkManager.Instance.OnConnectedToMasterEvent += OnConnected;
        NetworkManager.Instance.OnJoinedRoomEvent += OnRoomJoined;
        NetworkManager.Instance.OnConnectionFailedEvent += OnConnectionFailed;

        UpdateUI();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectedToMasterEvent -= OnConnected;
            NetworkManager.Instance.OnJoinedRoomEvent -= OnRoomJoined;
            NetworkManager.Instance.OnConnectionFailedEvent -= OnConnectionFailed;
        }
    }

    private void Update()
    {
        UpdateUI();
    }

    private void OnConnectClick()
    {
        string nickname = nicknameInput.text;
        if (string.IsNullOrEmpty(nickname))
            nickname = $"Player{Random.Range(1000, 9999)}";

        NetworkManager.Instance.ConnectToPhoton(nickname);
    }

    private void OnDisconnectClick()
    {
        NetworkManager.Instance.Disconnect();
    }

    private void OnCreateRoomClick()
    {
        var config = GameConfiguration.CreatePrivateRoom(
            NetworkManager.GenerateRoomCode()
        );
        NetworkManager.Instance.CreateRoom(config);
    }

    private void OnQuickMatchClick()
    {
        NetworkManager.Instance.QuickMatch();
    }

    private void OnConnected()
    {
        statusText.text = "? Connected to Photon!";
        statusText.color = Color.green;
    }

    private void OnRoomJoined(Photon.Realtime.Room room)
    {
        statusText.text = $"? In Room: {room.Name}";
        UpdateRoomInfo();
    }

    private void OnConnectionFailed(string message)
    {
        statusText.text = $"? Error: {message}";
        statusText.color = Color.red;
    }

    private void UpdateUI()
    {
        var nm = NetworkManager.Instance;
        
        connectButton.interactable = !nm.IsConnected;
        disconnectButton.interactable = nm.IsConnected;
        createRoomButton.interactable = nm.IsConnected && !nm.IsInRoom;
        quickMatchButton.interactable = nm.IsConnected && !nm.IsInRoom;

        if (nm.IsInRoom)
        {
            UpdateRoomInfo();
        }
        else
        {
            roomInfoText.text = "Not in room";
        }
    }

    private void UpdateRoomInfo()
    {
        if (!NetworkManager.Instance.IsInRoom) return;

        var rm = RoomManager.Instance;
        roomInfoText.text = rm.GetRoomDebugInfo();
    }
}
```

### D. Crea UI Canvas
1. Create UI ? Canvas (se non esiste)
2. Aggiungi componenti UI:

**Layout**:
```
Canvas
?? Panel (Background)
?  ?? Nickname InputField
?  ?? Connect Button
?  ?? Disconnect Button
?  ?? Create Room Button
?  ?? Quick Match Button
?  ?? Status Text (TMP)
?  ?? Room Info Text (TMP)
```

3. Collega tutto allo script `NetworkTestUI`
4. Add Component allo stesso GameObject del Canvas

### E. Test!
1. Play in Unity Editor
2. Inserisci nickname
3. Click **Connect**
4. Dovresti vedere "? Connected to Photon!"
5. Click **Quick Match** o **Create Room**
6. Dovresti vedere info della room

**? Checkpoint**: Se vedi "Connected to Photon", funziona! ??

---

## ?? STEP 3: Test Multiplayer Vero

### Test con 2 Istanze

#### Opzione A: Editor + Build
1. **Build** il progetto (File ? Build Settings ? Build)
2. Avvia la build
3. Avvia anche Unity Editor Play mode
4. Connetti entrambi
5. Create room in uno ? Join room nell'altro

#### Opzione B: ParrelSync (Plugin)
1. Installa ParrelSync dall'Asset Store (gratuito)
2. Crea clone del progetto
3. Apri entrambi
4. Play su entrambi, testa multiplayer

### Cosa Testare
- ? Connessione simultanea
- ? Creazione room
- ? Join room
- ? Visualizzazione slot giocatori
- ? Ready system
- ? Aggiunta/rimozione bot (Master Client)

---

## ?? STEP 4: Implementare GameState Sync (Prossimo Grande Step)

Una volta verificato che il networking base funziona, procediamo con:

### A. GameStateSerializer
Serializziamo il `GameState` per inviarlo via rete.

### B. NetworkGameController  
Gestisce la sincronizzazione dello stato di gioco.

### C. NetworkTurnController
Adatta `TurnController` per funzionare in multiplayer.

### D. Separare AI
Estrai la logica AI in `AIPlayer.cs` separato.

---

## ?? Checklist Prima di Continuare

Prima di implementare i prossimi step, assicurati di aver completato:

- [ ] Photon PUN 2 installato e configurato
- [ ] App ID inserito in PhotonServerSettings
- [ ] NetworkManager, RoomManager, PlayerDataManager funzionanti
- [ ] Test scene creata e testata
- [ ] Connessione a Photon riuscita
- [ ] Creazione/join room funzionante
- [ ] RoomManager mostra slot correttamente

---

## ?? Troubleshooting Comuni

### "Could not connect to Photon"
- ? Verifica App ID in PhotonServerSettings
- ? Controlla connessione internet
- ? Firewall non blocca Unity

### "Room not found"
- ? Room code corretto (6 caratteri)
- ? Room non è piena (max 4 players)
- ? Room non è già iniziata

### "Script missing"
- ? Compila senza errori
- ? Assembly definitions corretti
- ? Namespace `Project51.Networking` presente

### "Photon namespace not found"
- ? Photon PUN 2 importato correttamente
- ? Riavvia Unity se necessario
- ? Verifica `Assets/Photon/` esiste

---

## ?? Suggerimenti

### Durante lo Sviluppo
- Usa **Full logging** in PhotonServerSettings (durante debug)
- Testa sempre con almeno 2 istanze
- Log abbondante: `Debug.Log` è tuo amico
- Salva frequentemente!

### Performance
- Non chiamare RPC ogni frame
- Sincronizza solo quando necessario
- Comprimi GameState JSON se grande

### Testing
- Simula disconnessioni (chiudi client)
- Testa con latency alta (Photon regions lontane)
- Testa Master Client switch

---

## ?? Quando Sei Pronto

Una volta completati gli step 1-3, fammi sapere e procediamo con:

1. **GameStateSerializer** - Serializzazione
2. **NetworkGameController** - Sync controller
3. **NetworkTurnController** - Multiplayer turn logic
4. **AIPlayer** - Separazione AI

Oppure se hai domande/problemi durante il setup, chiedi pure! ??

---

**Buon lavoro!** ??
