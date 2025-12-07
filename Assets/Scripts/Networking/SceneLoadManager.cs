using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System;

namespace Project51.Networking
{
    /// <summary>
    /// Gestisce il caricamento delle scene in multiplayer.
    /// Assicura che tutti i client carichino la stessa scena simultaneamente.
    /// </summary>
    public class SceneLoadManager : MonoBehaviour
    {
        #region Singleton

        private static SceneLoadManager _instance;
        public static SceneLoadManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneLoadManager>();
                    
                    if (_instance == null)
                    {
                        Debug.LogError("SceneLoadManager not found in scene!");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoaded;
        public event Action<string> OnSceneLoadFailed;

        #endregion

        #region Constants

        public const string LOBBY_SCENE = "NetworkTest";
        public const string GAME_SCENE = "CirullaGame"; // Nome della scena di gioco

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

            // Subscribe to scene loaded events
            SceneManager.sceneLoaded += OnSceneLoadedCallback;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedCallback;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Carica la scena di gioco (solo Master Client).
        /// Tutti i client verranno sincronizzati automaticamente da Photon.
        /// </summary>
        public void LoadGameScene()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("Only Master Client can load scenes!");
                return;
            }

            if (!PhotonNetwork.InRoom)
            {
                Debug.LogError("Cannot load scene: not in a room!");
                return;
            }

            Debug.Log($"<color=green>Loading game scene: {GAME_SCENE}</color>");
            OnSceneLoadStarted?.Invoke(GAME_SCENE);

            // Photon AutomaticallySyncScene deve essere true (già impostato in NetworkManager)
            try
            {
                PhotonNetwork.LoadLevel(GAME_SCENE);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load scene: {e.Message}");
                OnSceneLoadFailed?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Torna alla lobby (solo Master Client o quando si disconnette).
        /// </summary>
        public void LoadLobbyScene()
        {
            Debug.Log($"<color=yellow>Loading lobby scene: {LOBBY_SCENE}</color>");
            OnSceneLoadStarted?.Invoke(LOBBY_SCENE);

            if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                // In room e Master: usa Photon sync
                PhotonNetwork.LoadLevel(LOBBY_SCENE);
            }
            else
            {
                // Non in room o non Master: carica direttamente
                SceneManager.LoadScene(LOBBY_SCENE);
            }
        }

        #endregion

        #region Scene Callbacks

        private void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"<color=cyan>Scene loaded: {scene.name}</color>");
            OnSceneLoaded?.Invoke(scene.name);

            // Inizializza la scena in base al tipo
            if (scene.name == GAME_SCENE)
            {
                InitializeGameScene();
            }
            else if (scene.name == LOBBY_SCENE)
            {
                InitializeLobbyScene();
            }
        }

        private void InitializeGameScene()
        {
            Debug.Log("Initializing game scene for multiplayer...");

            // Verifica che siamo in room
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogError("Not in room! Cannot initialize multiplayer game.");
                return;
            }

            // Log dei player slots per debugging
            LogPlayerSlots();

            // TODO: Integrazione con GameManager
            // Il GameManager dovrà essere modificato per supportare multiplayer
            // Vedi Assets/Networking/GAMEMANAGER_INTEGRATION.md per dettagli
        }

        private void LogPlayerSlots()
        {
            var roomManager = RoomManager.Instance;
            if (roomManager == null)
            {
                Debug.LogError("RoomManager not found!");
                return;
            }

            Debug.Log("=== MULTIPLAYER GAME SETUP ===");
            Debug.Log($"  Human players: {roomManager.HumanPlayersCount}");
            Debug.Log($"  Bot players: {roomManager.BotPlayersCount}");
            Debug.Log($"  Total players: {roomManager.FilledSlotsCount}");

            for (int i = 0; i < roomManager.PlayerSlots.Length; i++)
            {
                var slot = roomManager.PlayerSlots[i];
                if (!slot.IsEmpty)
                {
                    Debug.Log($"  Slot {i}: {slot.NickName} ({slot.Type})");
                }
            }

            int localPlayerSlot = roomManager.GetLocalPlayerSlot();
            Debug.Log($"  Local player is in slot: {localPlayerSlot}");
            Debug.Log("==============================");
        }

        private void InitializeLobbyScene()
        {
            Debug.Log("Lobby scene loaded");
            // La lobby si auto-inizializza tramite NetworkTestUI
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Ottiene il nome della scena corrente.
        /// </summary>
        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Verifica se siamo nella scena di gioco.
        /// </summary>
        public bool IsInGameScene()
        {
            return GetCurrentSceneName() == GAME_SCENE;
        }

        /// <summary>
        /// Verifica se siamo nella lobby.
        /// </summary>
        public bool IsInLobby()
        {
            return GetCurrentSceneName() == LOBBY_SCENE;
        }

        #endregion
    }
}
