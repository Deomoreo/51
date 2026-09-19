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
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Tornati al menu fuori da una stanza, la config della partita finita non deve far ripartire
        // da sola una ricerca/stanza alla prossima connessione (OnConnectedToMaster la riusa).
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "MainMenu" && !PhotonNetwork.InRoom)
            {
                CurrentConfig = null;
                SetState(MatchmakingState.Idle);
            }
        }

        /// <summary>Partita veloce: dopo questa attesa i posti vuoti vanno ai bot e si parte.</summary>
        public const float QuickMatchBotFillSeconds = 30f;

        /// <summary>Finestra di rientro dopo una disconnessione a partita iniziata (Photon PlayerTtl).</summary>
        public const int RejoinWindowMilliseconds = 60000;

        private float waitingSince = -1f;

        /// <summary>Secondi mancanti al riempimento con bot, -1 se non si sta aspettando.</summary>
        public float QuickMatchSecondsLeft => waitingSince < 0f ? -1f : Mathf.Max(0f, QuickMatchBotFillSeconds - (Time.unscaledTime - waitingSince));

        private void Update()
        {
            if (State != MatchmakingState.WaitingForPlayers || CurrentConfig?.Intent != MatchIntent.QuickMatch || !PhotonNetwork.InRoom)
                return;
            if (waitingSince < 0f) waitingSince = Time.unscaledTime;
            // Decide solo l'host; gli altri seguono il cambio scena sincronizzato.
            if (PhotonNetwork.IsMasterClient && QuickMatchSecondsLeft <= 0f)
            {
                Debug.Log($"[Matchmaking] Quick match: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers} players after {QuickMatchBotFillSeconds}s, filling with bots.");
                LockRoomForMatch();
                SetState(MatchmakingState.Starting);
                OnMatchFound?.Invoke();
            }
        }

        /// <summary>
        /// Chiude la stanza a partita iniziata e abilita il rientro: un giocatore che cade resta
        /// "inattivo" per RejoinWindowMilliseconds e puo' riprendere il suo posto.
        /// </summary>
        private void LockRoomForMatch()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) return;
            var room = PhotonNetwork.CurrentRoom;
            room.IsOpen = false;
            room.IsVisible = false;
            room.PlayerTtl = RejoinWindowMilliseconds;
            room.EmptyRoomTtl = RejoinWindowMilliseconds;
        }

        private void SetState(MatchmakingState newState)
        {
            if (State == newState) return;
            State = newState;
            if (newState != MatchmakingState.WaitingForPlayers) waitingSince = -1f;
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
                PhotonNetwork.LeaveRoom(false);
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
            
            // Chiudi la stanza e abilita il rientro dopo una disconnessione
            LockRoomForMatch();

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
                PhotonNetwork.LeaveRoom(false);
            }
            SetState(MatchmakingState.Idle);

            // Fondamentale: dopo aver lasciato una stanza, Photon riconnette automaticamente il
            // client al Master Server (comportamento normale, serve per poter creare/unirsi ad
            // un'altra stanza) - questo rif√† scattare OnConnectedToMaster(). Se CurrentConfig non
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

            // Solo una richiesta appena fatta dall'utente (Connecting) prosegue da sola: dopo l'uscita
            // da una stanza/partita Photon torna comunque al Master e non deve ripartire nulla.
            if (CurrentConfig == null || State != MatchmakingState.Connecting)
            {
                if (!PhotonNetwork.InRoom) SetState(MatchmakingState.Idle);
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
            if (CurrentConfig == null) return;
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
            // Rientro in una partita gia' iniziata (stanza chiusa): lo gestisce NetworkGameController.
            if (!PhotonNetwork.CurrentRoom.IsOpen)
            {
                Debug.Log($"[Matchmaking] Rejoined match in progress: {PhotonNetwork.CurrentRoom.Name}");
                return;
            }
            if (CurrentConfig == null) { PhotonNetwork.LeaveRoom(false); return; }
            Debug.Log($"[Matchmaking] Joined room: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");

            // Chi si unisce con un codice arriva con il Format scelto nella PROPRIA UI locale
            // (di default), che puo' non combaciare col formato reale scelto dall'host. La stanza
            // e' gia' autorevole su questo tramite le CustomRoomProperties impostate da
            // CreatePrivateRoomInternal/GetRoomOptions: le rileggiamo per allineare CurrentConfig,
            // cosi' che PlayerCount (derivato da Format) sia coerente su tutti i client.
            if (CurrentConfig != null && PhotonNetwork.CurrentRoom?.CustomProperties != null)
            {
                var props = PhotonNetwork.CurrentRoom.CustomProperties;
                if (props.ContainsKey("format"))
                    CurrentConfig.Format = (GameFormat)(int)props["format"];
                if (props.ContainsKey("target"))
                    CurrentConfig.TargetScore = (int)props["target"];
            }

            // CRITICO: salviamo SUBITO la config (corretta) in PlayerPrefs, qui - non solo quando
            // arriva "OnMatchFound". In una stanza privata OnMatchFound scatta SOLO sull'host quando
            // preme "Avvia" (e' un evento C# locale, MAI un RPC di rete): un client che si e' unito
            // con un codice non lo riceve mai. Per lui il passaggio a GameScene avviene invece in
            // automatico via Photon (PhotonNetwork.AutomaticallySyncScene, attivato quando l'host
            // chiama PhotonNetwork.LoadLevel), completamente al di fuori di
            // GameLaunchController/OnMatchFound. Senza questo salvataggio, GameSceneInitializer.Awake()
            // nella nuova scena carica da PlayerPrefs qualunque MatchConfig fosse rimasto li' da PRIMA
            // (es. Training, il default) - e GameSceneInitializer.IsMasterClient() tratta
            // Intent==Training come "sono sempre il master": il client si mette a giocare una
            // partita completamente locale/offline contro bot, scollegata dalla vera partita in
            // corso sugli altri client. E' la causa di "sul telefono la partita e' un'altra
            // sessione e gioca da solo".
            MatchConfigStorage.Save(CurrentConfig);

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
                // Controlla se la stanza ÔøΩ piena
                if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
                {
                    LockRoomForMatch();
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
            Debug.LogWarning($"[Matchmaking] Join room failed: {message}");
            SetState(MatchmakingState.Idle);
            OnError?.Invoke(returnCode == 32765 ? "La stanza Ë piena. Chiedi un altro codice." : returnCode == 32764 ? "La partita Ë gi‡ iniziata o la stanza Ë chiusa." : "Codice non valido o stanza non pi˘ disponibile.");
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
                    LockRoomForMatch();
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
