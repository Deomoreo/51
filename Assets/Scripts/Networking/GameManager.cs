using UnityEngine;
using Photon.Pun;
using Project51.Core;
using Project51.Networking;
using System.Collections.Generic;

namespace Project51.Unity
{
    /// <summary>
    /// Main game manager che gestisce sia single-player che multiplayer.
    /// Wrappa TurnController e gestisce l'inizializzazione in base alla modalità.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    
                    // Se ancora null dopo FindObjectOfType, significa che non c'è nella scena
                    if (_instance == null)
                    {
                        Debug.LogError("GameManager not found in scene! Make sure there's a GameManager component in the scene.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Game Mode

        public enum GameMode
        {
            SinglePlayer,
            Multiplayer
        }

        private GameMode _currentGameMode = GameMode.SinglePlayer; // Default a single-player
        
        public GameMode CurrentGameMode 
        { 
            get 
            {
                // Se non siamo ancora inizializzati, rileva al volo
                if (!_isInitialized)
                {
                    // Rilevamento rapido senza logging
                    // Assume single-player se non possiamo verificare Photon
                    return IsInPhotonRoomSafe() ? GameMode.Multiplayer : GameMode.SinglePlayer;
                }
                return _currentGameMode;
            } 
            private set { _currentGameMode = value; } 
        }

        /// <summary>
        /// Static, instance-free check to know if we are in multiplayer safely.
        /// Use this from other systems during early initialization to avoid touching instance state.
        /// </summary>
        public static bool IsMultiplayerSafe => IsInPhotonRoomSafe();
        
        private bool _isInitialized = false;
        
        /// <summary>
        /// Verifica in modo sicuro se siamo in una room Photon.
        /// Gestisce il caso in cui PhotonNetwork non sia inizializzato.
        /// This version catches ALL exceptions including TypeInitializationException.
        /// </summary>
        private static bool IsInPhotonRoomSafe()
        {
            try
            {
                return CheckPhotonInRoom();
            }
            catch
            {
                // Catch absolutely everything (including non-Exception throwables)
                return false;
            }
        }
        
        /// <summary>
        /// Separate method to isolate Photon access - if PhotonNetwork static initializer fails,
        /// the exception is thrown when this method is JIT compiled or entered.
        /// </summary>
        private static bool CheckPhotonInRoom()
        {
            return PhotonNetwork.InRoom;
        }

        #endregion

        #region Components

        [Header("Controllers")]
        [SerializeField] private TurnController turnController;

        [Header("Debug")]
        [SerializeField] private bool logMultiplayerInfo = true;

        #endregion

        #region Properties

        private int _localPlayerIndex = -1;
        
        /// <summary>
        /// Index del player locale in modalità multiplayer.
        /// In single-player è sempre 0.
        /// </summary>
        public int LocalPlayerIndex => _localPlayerIndex;

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

            // Find TurnController if not assigned
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
        }

        private void Start()
        {
            // Auto-detect modalità di gioco
            DetectGameMode();

            if (CurrentGameMode == GameMode.Multiplayer)
            {
                InitializeMultiplayer();
            }
            else
            {
                InitializeSinglePlayer();
            }
        }

        #endregion

        #region Game Mode Detection

        private void DetectGameMode()
        {
            // Se siamo in una room Photon, è multiplayer
            if (IsInPhotonRoomSafe())
            {
                CurrentGameMode = GameMode.Multiplayer;
                if (logMultiplayerInfo)
                {
                    Debug.Log("<color=cyan>=== MULTIPLAYER MODE ===</color>");
                }
            }
            else
            {
                CurrentGameMode = GameMode.SinglePlayer;
                if (logMultiplayerInfo)
                {
                    Debug.Log("=== SINGLE PLAYER MODE ===");
                }
            }
            
            _isInitialized = true;
        }

        #endregion

        #region Multiplayer Initialization

        private void InitializeMultiplayer()
        {
            var roomManager = RoomManager.Instance;
            if (roomManager == null)
            {
                Debug.LogError("RoomManager not found! Cannot initialize multiplayer game.");
                return;
            }

            var slots = roomManager.PlayerSlots;
            int playerCount = roomManager.FilledSlotsCount;
            int localSlot = roomManager.GetLocalPlayerSlot();

            if (playerCount < 2)
            {
                Debug.LogError("Need at least 2 players to start multiplayer game!");
                return;
            }

            if (localSlot < 0)
            {
                Debug.LogError("Local player not assigned to a slot!");
                return;
            }

            _localPlayerIndex = localSlot;

            if (logMultiplayerInfo)
            {
                LogMultiplayerSetup(slots, playerCount, localSlot);
            }

            // TODO: Modificare TurnController per supportare multiplayer
            // Per ora il TurnController parte automaticamente in autoStartGame
            // In futuro, disabilitare autoStartGame e controllare il flow da qui
            
            Debug.LogWarning("Multiplayer game initialization detected, but TurnController is not yet multiplayer-aware!");
            Debug.LogWarning("See Assets/Networking/GAMEMANAGER_INTEGRATION.md for next steps.");
        }

        private void LogMultiplayerSetup(NetworkPlayerInfo[] slots, int playerCount, int localSlot)
        {
            Debug.Log("<color=yellow>=== MULTIPLAYER GAME SETUP ===</color>");
            Debug.Log($"  Human players: {RoomManager.Instance.HumanPlayersCount}");
            Debug.Log($"  Bot players: {RoomManager.Instance.BotPlayersCount}");
            Debug.Log($"  Total players: {playerCount}");

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    string playerType = slots[i].IsHuman ? "Human" : "Bot";
                    string isLocal = (i == localSlot) ? " (YOU)" : "";
                    Debug.Log($"  Slot {i}: {slots[i].NickName} ({playerType}){isLocal}");
                }
            }

            Debug.Log($"  <color=cyan>Local player is in slot: {localSlot}</color>");
            Debug.Log("<color=yellow>==============================</color>");
        }

        #endregion

        #region Single Player Initialization

        private void InitializeSinglePlayer()
        {
            _localPlayerIndex = 0; // In single-player, player 0 è sempre locale

            // TurnController già gestisce single-player con autoStartGame
            Debug.Log("Single-player mode: TurnController will auto-start game.");
        }

        #endregion

        #region Player Queries

        /// <summary>
        /// Verifica se il player index è il player locale.
        /// </summary>
        public bool IsLocalPlayer(int playerIndex)
        {
            if (CurrentGameMode == GameMode.SinglePlayer)
                return playerIndex == 0;

            return playerIndex == _localPlayerIndex;
        }

        /// <summary>
        /// Verifica se il player index è un player umano.
        /// </summary>
        public bool IsHumanPlayer(int playerIndex)
        {
            if (CurrentGameMode == GameMode.SinglePlayer)
                return playerIndex == 0; // In single-player, solo player 0 è umano

            var roomManager = RoomManager.Instance;
            if (roomManager == null) return false;

            if (playerIndex < 0 || playerIndex >= roomManager.PlayerSlots.Length)
                return false;

            return roomManager.PlayerSlots[playerIndex].IsHuman;
        }

        /// <summary>
        /// Verifica se il player index è un bot.
        /// </summary>
        public bool IsBotPlayer(int playerIndex)
        {
            if (CurrentGameMode == GameMode.SinglePlayer)
                return playerIndex != 0; // In single-player, player 1-3 sono bot

            var roomManager = RoomManager.Instance;
            if (roomManager == null) return false;

            if (playerIndex < 0 || playerIndex >= roomManager.PlayerSlots.Length)
                return false;

            return roomManager.PlayerSlots[playerIndex].IsBot;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Ottiene il GameState corrente dal TurnController.
        /// </summary>
        public GameState GetGameState()
        {
            return turnController?.GameState;
        }

        /// <summary>
        /// Verifica se è il turno del player locale.
        /// </summary>
        public bool IsLocalPlayerTurn()
        {
            var gameState = GetGameState();
            if (gameState == null) return false;

            return IsLocalPlayer(gameState.CurrentPlayerIndex);
        }

        #endregion
    }
}
