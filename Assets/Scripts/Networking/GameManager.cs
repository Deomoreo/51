using UnityEngine;
using Photon.Pun;
using Project51.Core;
using Project51.Networking;
using System.Collections.Generic;

namespace Project51.Unity
{
    // Main game manager for single-player and multiplayer. Implements IGameModeProvider.
    public class GameManager : MonoBehaviour, IGameModeProvider
    {
        #region Singleton

        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // NON loggare un errore se manca: GameManager e' un sistema legacy, superato da
                    // GameSceneInitializer/GameModeService (vedi CardView/CardViewManager/CapturedPileManager,
                    // che lo interrogano via reflection solo come fallback opzionale). In GameScene.unity
                    // questo componente e' stato rimosso perche' sovrascriveva GameModeService.Current con
                    // se stesso, rompendo IsHumanPlayer/IsBotPlayer per il multiplayer (dipendevano dal
                    // RoomManager, mai presente nella scena reale -> "RoomManager not found" in loop).
                    _instance = FindObjectOfType<GameManager>();
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
                if (!_isInitialized)
                {
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
        [SerializeField] private bool logMultiplayerInfo = false;

        #endregion

        #region Properties

        private int _localPlayerIndex = -1;
        
        /// <summary>
        /// Index del player locale in modalit� multiplayer.
        /// In single-player � sempre 0.
        /// </summary>
        public int LocalPlayerIndex => _localPlayerIndex >= 0 ? _localPlayerIndex : 0;

        #endregion

        #region IGameModeProvider Implementation

        /// <summary>
        /// True if the game is running in multiplayer mode.
        /// </summary>
        bool IGameModeProvider.IsMultiplayer => CurrentGameMode == GameMode.Multiplayer;

        /// <summary>
        /// True if the local client is the Photon Master Client (or always true in single-player).
        /// </summary>
        public bool IsMasterClient
        {
            get
            {
                if (CurrentGameMode == GameMode.SinglePlayer)
                    return true;
                
                try
                {
                    return PhotonNetwork.IsMasterClient;
                }
                catch
                {
                    return true;
                }
            }
        }

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
            
            GameModeService.Current = this;
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }
        }
        
        private void OnDestroy()
        {
            if (GameModeService.Current == this as IGameModeProvider)
            {
                GameModeService.Reset();
            }
        }

        private void Start()
        {
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
            if (IsInPhotonRoomSafe())
            {
                CurrentGameMode = GameMode.Multiplayer;
            }
            else
            {
                CurrentGameMode = GameMode.SinglePlayer;
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
        }

        private void LogMultiplayerSetup(NetworkPlayerInfo[] slots, int playerCount, int localSlot)
        {
            // Removed verbose multiplayer setup logs
        }

        #endregion

        #region Single Player Initialization

        private void InitializeSinglePlayer()
        {
            _localPlayerIndex = 0;
        }

        #endregion

        #region Player Queries

        /// <summary>
        /// Verifica se il player index � il player locale.
        /// </summary>
        public bool IsLocalPlayer(int playerIndex)
        {
            if (CurrentGameMode == GameMode.SinglePlayer)
                return playerIndex == 0;
            return playerIndex == _localPlayerIndex;
        }

        /// <summary>
        /// Verifica se il player index � un player umano.
        /// </summary>
        public bool IsHumanPlayer(int playerIndex)
        {
            if (CurrentGameMode == GameMode.SinglePlayer)
                return playerIndex == 0; // In single-player, solo player 0 � umano

            var roomManager = RoomManager.Instance;
            if (roomManager == null) return false;

            if (playerIndex < 0 || playerIndex >= roomManager.PlayerSlots.Length)
                return false;

            return roomManager.PlayerSlots[playerIndex].IsHuman;
        }

        /// <summary>
        /// Verifica se il player index � un bot.
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
        /// Verifica se � il turno del player locale.
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
