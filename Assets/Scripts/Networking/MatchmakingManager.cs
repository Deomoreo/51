using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Project51.Core;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Project51.Networking
{
    /// <summary>
    /// Gestisce la connessione a Photon e il matchmaking.
    /// Supporta: Quick Match, Private Room (crea/unisciti), Training (offline).
    /// </summary>
    public class MatchmakingManager : MonoBehaviourPunCallbacks
    {
        public static MatchmakingManager Instance { get; private set; }

        /// <summary>
        /// Configurazione corrente del match.
        /// </summary>
        public MatchConfig CurrentConfig { get; private set; }

        /// <summary>
        /// Stato corrente del matchmaking.
        /// </summary>
        public MatchmakingState State { get; private set; } = MatchmakingState.Idle;

        // Eventi
        public event Action<MatchmakingState> OnStateChanged;
        public event Action<string> OnError;
        public event Action OnMatchFound;
        public event Action<string> OnRoomCreated; // passa il codice stanza
        public event Action OnRoomJoined;
        public event Action<Photon.Realtime.Player> OnPlayerJoined;
        public event Action<Photon.Realtime.Player> OnPlayerLeft;

        [Header("Settings")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private int roomCodeLength = 5;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void SetState(MatchmakingState newState)
        {
            if (State == newState) return;
            State = newState;
            Debug.Log($"[Matchmaking] State changed to: {newState}");
            OnStateChanged?.Invoke(newState);
        }

        #region Public API

        /// <summary>
        /// Avvia una partita di allenamento (offline vs bot).
        /// </summary>
        public void StartTraining(MatchConfig config)
        {
            if (config == null)
            {
                OnError?.Invoke("Config is null");
                return;
            }

            CurrentConfig = config.Clone();
            CurrentConfig.Intent = MatchIntent.Training;
            
            Debug.Log($"[Matchmaking] Starting Training: {CurrentConfig}");
            SetState(MatchmakingState.Starting);

            // Per training non serve Photon, vai diretto al gioco
            OnMatchFound?.Invoke();
        }

        /// <summary>
        /// Avvia Quick Match (coda pubblica).
        /// </summary>
        public void StartQuickMatch(MatchConfig config)
        {
            if (config == null)
            {
                OnError?.Invoke("Config is null");
                return;
            }

            CurrentConfig = config.Clone();
            CurrentConfig.Intent = MatchIntent.QuickMatch;

            Debug.Log($"[Matchmaking] Starting Quick Match: {CurrentConfig}");
            SetState(MatchmakingState.Connecting);

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
            else if (PhotonNetwork.IsConnectedAndReady)
            {
                JoinOrCreateRandomRoom();
            }
        }

        /// <summary>
        /// Crea una stanza privata.
        /// </summary>
        public void CreatePrivateRoom(MatchConfig config)
        {
            if (config == null)
            {
                OnError?.Invoke("Config is null");
                return;
            }

            CurrentConfig = config.Clone();
            CurrentConfig.Intent = MatchIntent.PrivateRoom;
            CurrentConfig.IsHost = true;
            CurrentConfig.RoomCode = GenerateRoomCode();

            Debug.Log($"[Matchmaking] Creating Private Room: {CurrentConfig.RoomCode}");
            SetState(MatchmakingState.Connecting);

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
            else if (PhotonNetwork.IsConnectedAndReady)
            {
                CreatePrivateRoomInternal();
            }
        }

        /// <summary>
        /// Unisciti a una stanza privata con codice.
        /// </summary>
        public void JoinPrivateRoom(string roomCode, MatchConfig config)
        {
            if (string.IsNullOrEmpty(roomCode))
            {
                OnError?.Invoke("Room code is empty");
                return;
            }

            CurrentConfig = config?.Clone() ?? new MatchConfig();
            CurrentConfig.Intent = MatchIntent.PrivateRoom;
            CurrentConfig.IsHost = false;
            CurrentConfig.RoomCode = roomCode.ToUpper();

            Debug.Log($"[Matchmaking] Joining Private Room: {CurrentConfig.RoomCode}");
            SetState(MatchmakingState.Connecting);

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
            else if (PhotonNetwork.IsConnectedAndReady)
            {
                JoinPrivateRoomInternal();
            }
        }

        /// <summary>
        /// Annulla il matchmaking corrente.
        /// </summary>
        public void Cancel()
        {
            Debug.Log("[Matchmaking] Cancelling...");
            
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            else if (PhotonNetwork.IsConnected && State == MatchmakingState.Searching)
            {
                // Se stiamo cercando, disconnetti
                PhotonNetwork.Disconnect();
            }

            SetState(MatchmakingState.Idle);
            CurrentConfig = null;
        }

        /// <summary>
        /// Avvia la partita (solo host in stanza privata).
        /// </summary>
        public void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[Matchmaking] Only host can start the game!");
                return;
            }

            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[Matchmaking] Not in a room!");
                return;
            }

            Debug.Log("[Matchmaking] Host starting game...");
            
            // Chiudi la stanza
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            SetState(MatchmakingState.Starting);
            OnMatchFound?.Invoke();
        }

        /// <summary>
        /// Esci dalla stanza corrente.
        /// </summary>
        public void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            SetState(MatchmakingState.Idle);

            // Fondamentale: dopo aver lasciato una stanza, Photon riconnette automaticamente il
            // client al Master Server (comportamento normale, serve per poter creare/unirsi ad
            // un'altra stanza) - questo rifà scattare OnConnectedToMaster(). Se CurrentConfig non
            // viene azzerato qui, quel callback lo trova ancora popolato con Intent=PrivateRoom/
            // IsHost=true e ricrea da solo la STESSA stanza appena lasciata (stesso RoomCode, perche'
            // CreatePrivateRoomInternal non lo rigenera): e' quello che faceva riapparire la waiting
            // room subito dopo aver premuto ESCI, senza alcun altro click.
            CurrentConfig = null;
        }

        #endregion

        #region Internal Methods

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Evita caratteri ambigui
            var code = new char[roomCodeLength];
            for (int i = 0; i < roomCodeLength; i++)
            {
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(code);
        }

        private RoomOptions GetRoomOptions(bool isPrivate)
        {
            var options = new RoomOptions
            {
                MaxPlayers = (byte)CurrentConfig.PlayerCount,
                IsVisible = !isPrivate,
                IsOpen = true,
                CleanupCacheOnLeave = true,
                CustomRoomProperties = new Hashtable
                {
                    { "format", (int)CurrentConfig.Format },
                    { "target", CurrentConfig.TargetScore }
                },
                CustomRoomPropertiesForLobby = new[] { "format", "target" }
            };
            return options;
        }

        private void JoinOrCreateRandomRoom()
        {
            SetState(MatchmakingState.Searching);

            var expectedProps = new Hashtable
            {
                { "format", (int)CurrentConfig.Format }
            };

            PhotonNetwork.JoinRandomRoom(expectedProps, (byte)CurrentConfig.PlayerCount);
        }

        private void CreatePrivateRoomInternal()
        {
            SetState(MatchmakingState.CreatingRoom);

            var options = GetRoomOptions(isPrivate: true);
            PhotonNetwork.CreateRoom(CurrentConfig.RoomCode, options, TypedLobby.Default);
        }

        private void JoinPrivateRoomInternal()
        {
            SetState(MatchmakingState.JoiningRoom);
            PhotonNetwork.JoinRoom(CurrentConfig.RoomCode);
        }

        #endregion

        #region Photon Callbacks

        public override void OnConnectedToMaster()
        {
            Debug.Log("[Matchmaking] Connected to Master");

            if (CurrentConfig == null)
            {
                SetState(MatchmakingState.Idle);
                return;
            }

            switch (CurrentConfig.Intent)
            {
                case MatchIntent.QuickMatch:
                    JoinOrCreateRandomRoom();
                    break;
                case MatchIntent.PrivateRoom:
                    if (CurrentConfig.IsHost)
                        CreatePrivateRoomInternal();
                    else
                        JoinPrivateRoomInternal();
                    break;
            }
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"[Matchmaking] Join random failed: {message}. Creating new room...");
            
            // Nessuna stanza disponibile, creane una
            var options = GetRoomOptions(isPrivate: false);
            PhotonNetwork.CreateRoom(null, options, TypedLobby.Default);
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[Matchmaking] Room created: {PhotonNetwork.CurrentRoom.Name}");
            
            if (CurrentConfig?.Intent == MatchIntent.PrivateRoom)
            {
                OnRoomCreated?.Invoke(CurrentConfig.RoomCode);
            }
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[Matchmaking] Joined room: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");

            if (CurrentConfig?.Intent == MatchIntent.PrivateRoom)
            {
                SetState(MatchmakingState.InWaitingRoom);

                // Photon chiama SEMPRE OnJoinedRoom() subito dopo OnCreatedRoom() quando si crea
                // una stanza (creare una stanza implica anche entrarci). Per l'host questo evento
                // e' quindi ridondante: OnCreatedRoom() ha gia' mostrato la waiting room con
                // isHost=true. Se qui rilanciassimo OnRoomJoined (pensato per chi entra con un
                // codice), GameLaunchController richiamerebbe ShowWaitingRoom(isHost:false) subito
                // dopo, sovrascrivendo l'host con la UI da ospite: il bottone AVVIA spariva e
                // l'animazione di ingresso ripartiva da capo a meta'.
                if (!CurrentConfig.IsHost)
                {
                    OnRoomJoined?.Invoke();
                }
            }
            else // Quick Match
            {
                // Controlla se la stanza � piena
                if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
                {
                    SetState(MatchmakingState.Starting);
                    OnMatchFound?.Invoke();
                }
                else
                {
                    SetState(MatchmakingState.WaitingForPlayers);
                }
            }
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[Matchmaking] Join room failed: {message}");
            SetState(MatchmakingState.Idle);
            OnError?.Invoke($"Impossibile entrare nella stanza: {message}");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[Matchmaking] Create room failed: {message}");
            SetState(MatchmakingState.Idle);
            OnError?.Invoke($"Impossibile creare la stanza: {message}");
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Debug.Log($"[Matchmaking] Player joined: {newPlayer.NickName}");
            OnPlayerJoined?.Invoke(newPlayer);

            // Per Quick Match, controlla se siamo pronti
            if (CurrentConfig?.Intent == MatchIntent.QuickMatch)
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
                {
                    SetState(MatchmakingState.Starting);
                    OnMatchFound?.Invoke();
                }
            }
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            Debug.Log($"[Matchmaking] Player left: {otherPlayer.NickName}");
            OnPlayerLeft?.Invoke(otherPlayer);
        }

        public override void OnLeftRoom()
        {
            Debug.Log("[Matchmaking] Left room");
            SetState(MatchmakingState.Idle);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"[Matchmaking] Disconnected: {cause}");
            
            if (cause != DisconnectCause.DisconnectByClientLogic)
            {
                OnError?.Invoke($"Disconnesso: {cause}");
            }
            
            SetState(MatchmakingState.Idle);
        }

        #endregion
    }

    /// <summary>
    /// Stati del matchmaking.
    /// </summary>
    public enum MatchmakingState
    {
        Idle,
        Connecting,
        Searching,
        CreatingRoom,
        JoiningRoom,
        WaitingForPlayers,
        InWaitingRoom,
        Starting
    }
}
