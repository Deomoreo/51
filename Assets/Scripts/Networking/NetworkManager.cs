using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace Project51.Networking
{
    /// <summary>
    /// Manager principale per la connettività di rete con Photon PUN 2.
    /// Gestisce connessione, lobby, creazione/join room.
    /// Singleton pattern per accesso globale.
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        #region Singleton

        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkManager>();
                    
                    // NON creare GameObject se non esiste - deve essere nella scena!
                    if (_instance == null)
                    {
                        Debug.LogError("NetworkManager not found in scene! Add it to a GameObject.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Public Properties

        public NetworkState CurrentState { get; private set; } = NetworkState.Disconnected;
        public bool IsConnected => PhotonNetwork.IsConnected;
        public bool IsInRoom => PhotonNetwork.InRoom;
        public bool IsMasterClient => PhotonNetwork.IsMasterClient;
        public Room CurrentRoom => PhotonNetwork.CurrentRoom;
        public Player LocalPlayer => PhotonNetwork.LocalPlayer;
        public string PlayerNickname => PhotonNetwork.NickName;

        #endregion

        #region Events

        public event Action OnConnectedToMasterEvent;
        public event Action OnJoinedLobbyEvent;
        public event Action OnLeftLobbyEvent;
        public event Action<Room> OnJoinedRoomEvent;
        public event Action OnLeftRoomEvent;
        public event Action<Player> OnPlayerJoinedRoomEvent;
        public event Action<Player> OnPlayerLeftRoomEvent;
        public event Action<string> OnConnectionFailedEvent;
        public event Action<Room> OnRoomCreatedEvent;
        public event Action<string> OnRoomJoinFailedEvent;

        #endregion

        #region Private Fields

        private GameConfiguration _currentGameConfig;
        private bool _autoJoinLobby = true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            ConfigurePhotonSettings();
        }

        private void Start()
        {
            // Auto-connessione all'avvio (opzionale)
            // ConnectToPhoton();
        }

        #endregion

        #region Photon Configuration

        private void ConfigurePhotonSettings()
        {
            // Impostazioni ottimali per un gioco turn-based mobile
            PhotonNetwork.SendRate = (int)NetworkConstants.SEND_RATE;
            PhotonNetwork.SerializationRate = (int)NetworkConstants.SERIALIZATION_RATE;
            
            // Importante per mobile: continua a girare in background
            PhotonNetwork.KeepAliveInBackground = 60f; // Ping ogni 60 secondi
            
            // Auto sincronizzazione della scena (utile per caricare game scene insieme)
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        #endregion

        #region Connection Methods

        /// <summary>
        /// Connette a Photon Cloud con nickname specificato.
        /// </summary>
        public void ConnectToPhoton(string nickname = null)
        {
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("Already connected to Photon");
                return;
            }

            CurrentState = NetworkState.Connecting;

            if (!string.IsNullOrEmpty(nickname))
            {
                PhotonNetwork.NickName = nickname;
            }
            else if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            {
                // Nickname casuale se non specificato
                PhotonNetwork.NickName = $"Player{UnityEngine.Random.Range(1000, 9999)}";
            }

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = Application.version;

            Debug.Log($"Connecting to Photon as '{PhotonNetwork.NickName}'...");
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// Disconnette da Photon.
        /// </summary>
        public void Disconnect()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            CurrentState = NetworkState.Disconnected;
        }

        #endregion

        #region Lobby Methods

        /// <summary>
        /// Entra nella lobby principale.
        /// </summary>
        public void JoinLobby()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("Cannot join lobby: not connected to Photon");
                ConnectToPhoton();
                return;
            }

            if (PhotonNetwork.InLobby)
            {
                Debug.Log("Already in lobby");
                return;
            }

            CurrentState = NetworkState.JoiningLobby;
            PhotonNetwork.JoinLobby();
        }

        /// <summary>
        /// Esce dalla lobby.
        /// </summary>
        public void LeaveLobby()
        {
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.LeaveLobby();
            }
        }

        #endregion

        #region Room Methods

        /// <summary>
        /// Crea una nuova room con la configurazione specificata.
        /// </summary>
        public void CreateRoom(GameConfiguration config, string roomName = null)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogError("Cannot create room: not connected to Photon");
                OnRoomJoinFailedEvent?.Invoke("Not connected to server");
                return;
            }

            _currentGameConfig = config;
            CurrentState = NetworkState.CreatingRoom;

            // Se c'è un room code, usalo come nome della room!
            // Questo rende il join by code molto più semplice
            if (!string.IsNullOrEmpty(config.RoomCode))
            {
                roomName = config.RoomCode;
            }
            else if (string.IsNullOrEmpty(roomName))
            {
                // Altrimenti genera nome room random
                roomName = GenerateRoomName();
            }

            // Imposta le proprietà della room
            var roomOptions = new RoomOptions
            {
                MaxPlayers = (byte)config.MaxPlayers,
                IsVisible = true,  // ? Sempre visibile in lobby (anche private room)
                IsOpen = true,
                CustomRoomProperties = CreateRoomProperties(config),
                CustomRoomPropertiesForLobby = new string[] 
                { 
                    NetworkConstants.ROOM_GAME_MODE,
                    NetworkConstants.ROOM_ALLOW_BOTS,
                    NetworkConstants.ROOM_CODE
                }
            };

            Debug.Log($"Creating room '{roomName}' with mode {config.Mode}");
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        /// <summary>
        /// Unisciti a una room esistente tramite nome.
        /// </summary>
        public void JoinRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogError("Cannot join room: not connected to Photon");
                return;
            }

            CurrentState = NetworkState.JoiningRoom;
            Debug.Log($"Joining room '{roomName}'...");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// Unisciti a una room tramite codice amico (6 caratteri).
        /// </summary>
        public void JoinRoomByCode(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                Debug.LogError("Invalid room code. Must be 6 characters.");
                OnRoomJoinFailedEvent?.Invoke("Invalid room code");
                return;
            }

            // NOTA: Il codice è una custom property, non il nome della room!
            // Quindi usiamo il codice COME NOME della room per semplicità
            // Alternativa: cerca nella room list (vedi JoinRoomByCodeFromList)
            string roomName = roomCode.ToUpper();
            JoinRoom(roomName);
        }

        /// <summary>
        /// Quick Match: unisciti o crea una room pubblica.
        /// </summary>
        public void QuickMatch()
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogError("Cannot quick match: not connected to Photon");
                return;
            }

            CurrentState = NetworkState.JoiningRoom;
            
            // Prova a unirti a una room casuale, altrimenti ne crea una
            PhotonNetwork.JoinRandomRoom();
        }

        /// <summary>
        /// Lascia la room corrente.
        /// </summary>
        public void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("Leaving room...");
                PhotonNetwork.LeaveRoom();
            }
        }

        #endregion

        #region Helper Methods

        private Hashtable CreateRoomProperties(GameConfiguration config)
        {
            var properties = new Hashtable
            {
                { NetworkConstants.ROOM_GAME_MODE, (int)config.Mode },
                { NetworkConstants.ROOM_ALLOW_BOTS, config.AllowBots },
                { NetworkConstants.ROOM_WINNING_SCORE, config.WinningScore },
                { NetworkConstants.ROOM_GAME_STARTED, false }
            };

            if (!string.IsNullOrEmpty(config.RoomCode))
            {
                properties[NetworkConstants.ROOM_CODE] = config.RoomCode;
            }

            return properties;
        }

        private string GenerateRoomName()
        {
            // Genera un nome univoco per la room
            return $"Room_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        /// <summary>
        /// Genera un codice room di 6 caratteri alfanumerici.
        /// </summary>
        public static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] code = new char[6];
            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(code);
        }

        #endregion

        #region Photon Callbacks - Connection

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Photon Master Server");
            CurrentState = NetworkState.ConnectedToMaster;
            OnConnectedToMasterEvent?.Invoke();

            // Auto join lobby
            if (_autoJoinLobby)
            {
                JoinLobby();
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"Disconnected from Photon: {cause}");
            CurrentState = NetworkState.Disconnected;
            
            string message = GetDisconnectMessage(cause);
            OnConnectionFailedEvent?.Invoke(message);
        }

        private string GetDisconnectMessage(DisconnectCause cause)
        {
            switch (cause)
            {
                case DisconnectCause.DisconnectByClientLogic:
                    return "Disconnected";
                case DisconnectCause.Exception:
                case DisconnectCause.ExceptionOnConnect:
                    return "Connection error";
                case DisconnectCause.ServerTimeout:
                case DisconnectCause.ClientTimeout:
                    return "Connection timeout";
                default:
                    return $"Disconnected: {cause}";
            }
        }

        #endregion

        #region Photon Callbacks - Lobby

        public override void OnJoinedLobby()
        {
            Debug.Log("Joined Photon Lobby");
            CurrentState = NetworkState.InLobby;
            OnJoinedLobbyEvent?.Invoke();
        }

        public override void OnLeftLobby()
        {
            Debug.Log("Left Photon Lobby");
            OnLeftLobbyEvent?.Invoke();
        }

        #endregion

        #region Photon Callbacks - Room

        public override void OnCreatedRoom()
        {
            Debug.Log($"Room created: {PhotonNetwork.CurrentRoom.Name}");
            OnRoomCreatedEvent?.Invoke(PhotonNetwork.CurrentRoom);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
            CurrentState = NetworkState.InRoom;
            OnJoinedRoomEvent?.Invoke(PhotonNetwork.CurrentRoom);
        }

        public override void OnLeftRoom()
        {
            Debug.Log("Left room");
            CurrentState = PhotonNetwork.InLobby ? NetworkState.InLobby : NetworkState.ConnectedToMaster;
            OnLeftRoomEvent?.Invoke();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"Failed to create room: {message} (code: {returnCode})");
            CurrentState = NetworkState.InLobby;
            OnRoomJoinFailedEvent?.Invoke($"Failed to create room: {message}");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"Failed to join room: {message} (code: {returnCode})");
            CurrentState = NetworkState.InLobby;
            OnRoomJoinFailedEvent?.Invoke($"Failed to join room: {message}");
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"No random room available, creating new one");
            
            // Se Quick Match non trova room, ne crea una
            var config = GameConfiguration.CreateQuickMatch();
            CreateRoom(config);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"Player joined: {newPlayer.NickName}");
            OnPlayerJoinedRoomEvent?.Invoke(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"Player left: {otherPlayer.NickName}");
            OnPlayerLeftRoomEvent?.Invoke(otherPlayer);
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Ottiene la configurazione di gioco dalla room corrente.
        /// </summary>
        public GameConfiguration GetCurrentGameConfig()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.CustomProperties == null)
                return null;

            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            var config = new GameConfiguration
            {
                Mode = (GameMode)(int)props[NetworkConstants.ROOM_GAME_MODE],
                AllowBots = (bool)props[NetworkConstants.ROOM_ALLOW_BOTS],
                WinningScore = (int)props[NetworkConstants.ROOM_WINNING_SCORE],
                MaxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers
            };

            if (props.ContainsKey(NetworkConstants.ROOM_CODE))
            {
                config.RoomCode = (string)props[NetworkConstants.ROOM_CODE];
            }

            return config;
        }

        /// <summary>
        /// Imposta il nickname del giocatore locale.
        /// </summary>
        public void SetPlayerNickname(string nickname)
        {
            PhotonNetwork.NickName = nickname;
            
            // Salva localmente per persistenza
            PlayerPrefs.SetString("PlayerNickname", nickname);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Carica il nickname salvato o ne genera uno casuale.
        /// </summary>
        public string LoadPlayerNickname()
        {
            string nickname = PlayerPrefs.GetString("PlayerNickname", "");
            
            if (string.IsNullOrEmpty(nickname))
            {
                nickname = $"Player{UnityEngine.Random.Range(1000, 9999)}";
                SetPlayerNickname(nickname);
            }

            return nickname;
        }

        #endregion
    }
}
