using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Networking;
using Photon.Realtime;
using Photon.Pun;
using System.Collections.Generic;

public class NetworkTestUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_InputField nicknameInput;
    public Button connectButton;
    public Button disconnectButton;
    public Button createRoomButton;
    public Button quickMatchButton;
    public TMP_Text statusText;
    public TMP_Text roomInfoText;

    [Header("Room List UI")]
    public TMP_Text roomListText;
    public Button refreshRoomListButton;
    public TMP_InputField joinCodeInput;
    public Button joinByCodeButton;

    [Header("Room Control UI")]
    public Button readyButton;
    public TMP_Text readyButtonText;
    public Button startGameButton;
    public Button leaveRoomButton;
    public Button addBotButton;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private bool isLocalPlayerReady = false;

    private void Start()
    {
        // Setup buttons
        connectButton.onClick.AddListener(OnConnectClick);
        disconnectButton.onClick.AddListener(OnDisconnectClick);
        createRoomButton.onClick.AddListener(OnCreateRoomClick);
        quickMatchButton.onClick.AddListener(OnQuickMatchClick);

        if (refreshRoomListButton != null)
            refreshRoomListButton.onClick.AddListener(OnRefreshRoomListClick);

        if (joinByCodeButton != null)
            joinByCodeButton.onClick.AddListener(OnJoinByCodeClick);

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClick);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClick);

        if (leaveRoomButton != null)
            leaveRoomButton.onClick.AddListener(OnLeaveRoomClick);

        if (addBotButton != null)
            addBotButton.onClick.AddListener(OnAddBotClick);

        // Subscribe to events (nomi aggiornati con suffisso "Event")
        NetworkManager.Instance.OnConnectedToMasterEvent += OnConnected;
        NetworkManager.Instance.OnJoinedRoomEvent += OnRoomJoined;
        NetworkManager.Instance.OnLeftRoomEvent += OnLeftRoom;
        NetworkManager.Instance.OnConnectionFailedEvent += OnConnectionFailed;
        NetworkManager.Instance.OnJoinedLobbyEvent += OnJoinedLobby;
        NetworkManager.Instance.OnPlayerJoinedRoomEvent += OnPlayerJoinedRoom;
        NetworkManager.Instance.OnPlayerLeftRoomEvent += OnPlayerLeftRoom;

        // Subscribe to RoomManager events
        RoomManager.Instance.OnRoomStateChanged += OnRoomStateChanged;
        RoomManager.Instance.OnPlayerReadyChanged += OnPlayerReadyChanged;
        RoomManager.Instance.OnAllPlayersReady += OnAllPlayersReady;

        UpdateUI();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectedToMasterEvent -= OnConnected;
            NetworkManager.Instance.OnJoinedRoomEvent -= OnRoomJoined;
            NetworkManager.Instance.OnLeftRoomEvent -= OnLeftRoom;
            NetworkManager.Instance.OnConnectionFailedEvent -= OnConnectionFailed;
            NetworkManager.Instance.OnJoinedLobbyEvent -= OnJoinedLobby;
            NetworkManager.Instance.OnPlayerJoinedRoomEvent -= OnPlayerJoinedRoom;
            NetworkManager.Instance.OnPlayerLeftRoomEvent -= OnPlayerLeftRoom;
        }

        // Unsubscribe from RoomManager events
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomStateChanged -= OnRoomStateChanged;
            RoomManager.Instance.OnPlayerReadyChanged -= OnPlayerReadyChanged;
            RoomManager.Instance.OnAllPlayersReady -= OnAllPlayersReady;
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

    private void OnRefreshRoomListClick()
    {
        UpdateRoomListDisplay();
    }

    private void OnJoinByCodeClick()
    {
        if (joinCodeInput == null) return;

        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Room code is empty!");
            return;
        }

        NetworkManager.Instance.JoinRoomByCode(code);
    }

    private void OnReadyClick()
    {
        isLocalPlayerReady = !isLocalPlayerReady;
        RoomManager.Instance.SetLocalPlayerReady(isLocalPlayerReady);
        UpdateReadyButton();
        
        Debug.Log($"<color=cyan>Local player ready: {isLocalPlayerReady}</color>");
    }

    private void OnStartGameClick()
    {
        if (!NetworkManager.Instance.IsMasterClient)
        {
            Debug.LogWarning("Only Master Client can start the game!");
            return;
        }

        if (!RoomManager.Instance.AllPlayersReady)
        {
            Debug.LogWarning("Not all players are ready!");
            return;
        }

        if (RoomManager.Instance.FilledSlotsCount < 2)
        {
            Debug.LogWarning("Need at least 2 players to start!");
            return;
        }

        Debug.Log("<color=green>=== STARTING GAME ===</color>");
        RoomManager.Instance.StartGame();
    }

    private void OnLeaveRoomClick()
    {
        Debug.Log("Leaving room...");
        
        // RESET ready state PRIMA di uscire
        isLocalPlayerReady = false;
        UpdateReadyButton();
        
        NetworkManager.Instance.LeaveRoom();
    }

    private void OnAddBotClick()
    {
        if (!NetworkManager.Instance.IsMasterClient)
        {
            Debug.LogWarning("Only Master Client can add bots!");
            return;
        }

        if (RoomManager.Instance.FilledSlotsCount >= 4)
        {
            Debug.LogWarning("Room is full!");
            return;
        }

        Debug.Log("Adding bot...");
        RoomManager.Instance.AddBotToSlot();
    }

    public override void OnConnected()
    {
        statusText.text = "? Connected to Photon!";
        statusText.color = Color.green;
    }

    private void OnRoomJoined(Photon.Realtime.Room room)
    {
        // RESET ready state quando entri in room (fix bug #3)
        isLocalPlayerReady = false;
        UpdateReadyButton();

        // Estrai il room code se esiste
        string roomCode = "";
        if (room.CustomProperties != null && 
            room.CustomProperties.ContainsKey(NetworkConstants.ROOM_CODE))
        {
            roomCode = $" | Code: {room.CustomProperties[NetworkConstants.ROOM_CODE]}";
        }

        statusText.text = $"In Room: {room.Name}{roomCode}";
        UpdateRoomInfo();

        // IMPORTANTE: Inizializza RoomManager
        RoomManager.Instance.OnRoomJoined();

        // Log del codice per condividerlo facilmente
        if (!string.IsNullOrEmpty(roomCode))
        {
            Debug.Log($"<color=yellow>ROOM CODE: {room.CustomProperties[NetworkConstants.ROOM_CODE]}</color>");
            Debug.Log($"Share this code with friends to join!");
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined lobby - updating room list");
        UpdateRoomListDisplay();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room - resetting ready state");
        
        // RESET ready state quando esci dalla room
        isLocalPlayerReady = false;
        UpdateReadyButton();
    }

    private void OnPlayerJoinedRoom(Player player)
    {
        Debug.Log($"<color=cyan>Player joined room: {player.NickName}</color>");
        
        // IMPORTANTE: Notifica al RoomManager che un player è entrato
        // (questo aggiorna gli slot per tutti i client)
        UpdateRoomInfo();
    }

    public override void OnPlayerLeftRoom(Player player)
    {
        Debug.Log($"<color=orange>Player left room: {player.NickName}</color>");
        
        // Aggiorna la UI quando qualcuno esce
        UpdateRoomInfo();
    }

    private void OnRoomStateChanged()
    {
        // Aggiorna UI quando lo stato della room cambia
        UpdateRoomInfo();
        UpdateRoomControlButtons();
    }

    private void OnPlayerReadyChanged(int slot, bool ready)
    {
        Debug.Log($"Player in slot {slot} ready: {ready}");
        UpdateRoomInfo();
        UpdateRoomControlButtons();
    }

    private void OnAllPlayersReady()
    {
        Debug.Log("<color=yellow>? All players are ready! Master can start the game.</color>");
        UpdateRoomControlButtons();
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
            UpdateRoomControlButtons();
        }
        else
        {
            roomInfoText.text = "Not in room";
            HideRoomControlButtons();
        }
    }

    private void UpdateRoomControlButtons()
    {
        var nm = NetworkManager.Instance;
        var rm = RoomManager.Instance;

        if (!nm.IsInRoom)
        {
            HideRoomControlButtons();
            return;
        }

        // Ready Button
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(true);
            readyButton.interactable = true;
        }
        UpdateReadyButton();

        // Start Game Button (solo Master Client)
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(nm.IsMasterClient);
            startGameButton.interactable = rm.AllPlayersReady && rm.FilledSlotsCount >= 2;
        }

        // Leave Room Button
        if (leaveRoomButton != null)
        {
            leaveRoomButton.gameObject.SetActive(true);
            leaveRoomButton.interactable = true;
        }

        // Add Bot Button (solo Master Client)
        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(nm.IsMasterClient);
            addBotButton.interactable = rm.FilledSlotsCount < 4;
        }
    }

    private void UpdateReadyButton()
    {
        if (readyButton == null) return;

        // Cambia colore in base allo stato ready
        var colors = readyButton.colors;
        if (isLocalPlayerReady)
        {
            colors.normalColor = new Color(0.2f, 0.8f, 0.2f); // Verde
            colors.highlightedColor = new Color(0.3f, 0.9f, 0.3f);
            if (readyButtonText != null)
                readyButtonText.text = "? READY";
        }
        else
        {
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f); // Grigio
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            if (readyButtonText != null)
                readyButtonText.text = "Ready?";
        }
        readyButton.colors = colors;
    }

    private void HideRoomControlButtons()
    {
        if (readyButton != null)
            readyButton.gameObject.SetActive(false);
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(false);
        if (leaveRoomButton != null)
            leaveRoomButton.gameObject.SetActive(false);
        if (addBotButton != null)
            addBotButton.gameObject.SetActive(false);
    }

    private void UpdateRoomInfo()
    {
        if (!NetworkManager.Instance.IsInRoom) return;

        var rm = RoomManager.Instance;
        roomInfoText.text = rm.GetRoomDebugInfo();
    }

    // ==================== ROOM LIST FUNCTIONS ====================

    /// <summary>
    /// Callback Photon quando la lista room viene aggiornata.
    /// </summary>
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        UpdateRoomListDisplay();
    }

    /// <summary>
    /// Aggiorna la cache locale della lista room.
    /// </summary>
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Rimuovi room che sono state chiuse/rimosse
            if (info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }
                continue;
            }

            // Aggiorna o aggiungi room
            cachedRoomList[info.Name] = info;
        }
    }

    /// <summary>
    /// Aggiorna il display della lista room.
    /// </summary>
    private void UpdateRoomListDisplay()
    {
        if (roomListText == null)
        {
            Debug.LogWarning("Room List Text not assigned!");
            return;
        }

        if (!Photon.Pun.PhotonNetwork.InLobby)
        {
            roomListText.text = "Not in lobby\n(Connect first)";
            return;
        }

        if (cachedRoomList.Count == 0)
        {
            roomListText.text = "No rooms available\n(Create one or wait)";
            return;
        }

        // Costruisci la lista
        string list = "=== AVAILABLE ROOMS ===\n\n";

        foreach (var roomInfo in cachedRoomList.Values)
        {
            // Mostra solo room aperte e visibili
            if (!roomInfo.IsOpen || !roomInfo.IsVisible)
                continue;

            string roomCode = "";
            if (roomInfo.CustomProperties != null && 
                roomInfo.CustomProperties.ContainsKey(NetworkConstants.ROOM_CODE))
            {
                roomCode = $" [Code: {roomInfo.CustomProperties[NetworkConstants.ROOM_CODE]}]";
            }

            list += $"• {roomInfo.Name}{roomCode}\n";
            list += $"  Players: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}\n";
            list += $"  <color=yellow>[Click 'Join by Code' to join]</color>\n\n";
        }

        roomListText.text = list;
    }

    /// <summary>
    /// Join una room dalla lista (se hai il nome).
    /// </summary>
    public void JoinRoomByName(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Room name is empty!");
            return;
        }

        NetworkManager.Instance.JoinRoom(roomName);
    }
}