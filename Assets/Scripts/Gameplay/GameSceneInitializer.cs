using UnityEngine;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>
    /// Inizializza la scena di gioco basandosi sulla MatchConfig.
    /// Va messo nella scena di gioco.
    /// Non dipende direttamente da Networking per evitare dipendenze cicliche.
    /// 
    /// IMPORTANT: This script should execute BEFORE TurnController.
    /// Set Script Execution Order in Unity: GameSceneInitializer = -100, TurnController = 0
    /// </summary>
    [DefaultExecutionOrder(-100)] // Execute before other scripts
    public class GameSceneInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurnController turnController;

        [Header("Settings")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float multiplayerStartDelay = 0.5f;

        private MatchConfig _config;

        // Cache per evitare FindObjectOfType ripetuti
        private MonoBehaviour _networkController;
        private bool _networkControllerSearched;

        /// <summary>
        /// Latest loaded match config for the active game scene.
        /// Useful for gameplay systems that need rules tweaks.
        /// </summary>
        public static MatchConfig ActiveConfig { get; private set; }

        private void Awake()
        {
            // Carica la configurazione dai PlayerPrefs usando il helper in Core
            _config = MatchConfigStorage.Load();
            ActiveConfig = _config;
            Debug.Log($"[GameSceneInitializer] Loaded config: {_config}");

            EnsureResponsiveCamera();

            // Configura il GameModeService
            SetupGameModeProvider();
        }

        private void Start()
        {
            if (turnController == null)
            {
                turnController = FindObjectOfType<TurnController>();
            }

            if (autoStart && turnController != null)
            {
                if (_config.Intent == MatchIntent.Training)
                {
                    // Training: avvia subito
                    StartGame();
                }
                else
                {
                    // Multiplayer: solo il Master Client avvia
                    if (IsMasterClient())
                    {
                        Invoke(nameof(StartGame), multiplayerStartDelay);
                    }
                    // I client non-master riceveranno il GameState via NetworkGameController
                }
            }
        }

        private void SetupGameModeProvider()
        {
            IGameModeProvider provider;

            if (_config.Intent == MatchIntent.Training)
            {
                // Training: player 0 è umano, gli altri sono bot
                provider = new TrainingGameModeProvider(_config.PlayerCount, _config.BotDifficulty);
                Debug.Log($"[GameSceneInitializer] Setup Training mode with {_config.PlayerCount} players, difficulty: {_config.BotDifficulty}");
            }
            else
            {
                // Multiplayer: determina l'indice locale
                int localIndex = GetLocalPlayerIndex();
                bool isMaster = IsMasterClient();
                provider = new MultiplayerGameModeProvider(localIndex, _config.PlayerCount, isMaster);
                Debug.Log($"[GameSceneInitializer] Setup Multiplayer mode, local index: {localIndex}, isMaster: {isMaster}");
            }

            GameModeService.Current = provider;
        }

        /// <summary>
        /// Verifica se siamo il Master Client usando reflection per evitare dipendenza da Photon.
        /// </summary>
        private bool IsMasterClient()
        {
            if (_config.Intent == MatchIntent.Training)
                return true;

            // Usa reflection per accedere a PhotonNetwork.IsMasterClient
            var photonNetworkType = System.Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking");
            if (photonNetworkType != null)
            {
                var isMasterProp = photonNetworkType.GetProperty("IsMasterClient", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (isMasterProp != null)
                {
                    return (bool)isMasterProp.GetValue(null);
                }
            }

            // Fallback: assume master
            return true;
        }

        /// <summary>
        /// Verifica se siamo in una room Photon usando reflection.
        /// </summary>
        private bool IsInRoom()
        {
            var photonNetworkType = System.Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking");
            if (photonNetworkType != null)
            {
                var inRoomProp = photonNetworkType.GetProperty("InRoom", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (inRoomProp != null)
                {
                    return (bool)inRoomProp.GetValue(null);
                }
            }
            return false;
        }

        /// <summary>
        /// Ottiene l'indice del player locale nella room Photon.
        /// </summary>
        private int GetLocalPlayerIndex()
        {
            if (!IsInRoom())
                return 0;

            var photonNetworkType = System.Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking");
            if (photonNetworkType == null)
                return 0;

            // Ottieni PlayerList
            var playerListProp = photonNetworkType.GetProperty("PlayerList", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (playerListProp == null)
                return 0;

            var players = playerListProp.GetValue(null) as System.Array;
            if (players == null)
                return 0;

            // Trova il player locale
            for (int i = 0; i < players.Length; i++)
            {
                var player = players.GetValue(i);
                var isLocalProp = player.GetType().GetProperty("IsLocal");
                if (isLocalProp != null && (bool)isLocalProp.GetValue(player))
                {
                    return i;
                }
            }

            return 0;
        }

        private void StartGame()
        {
            if (turnController == null)
            {
                Debug.LogError("[GameSceneInitializer] TurnController not found!");
                return;
            }

            Debug.Log("[GameSceneInitializer] Starting new game...");
            turnController.StartNewGame();

            // In multiplayer, invia il GameState agli altri client
            if (_config.Intent != MatchIntent.Training && IsMasterClient())
            {
                SendInitialGameStateToClients();
            }
        }

        /// <summary>
        /// Invia il GameState ai client usando reflection per evitare dipendenza ciclica.
        /// </summary>
        private void SendInitialGameStateToClients()
        {
            if (turnController == null || turnController.GameState == null)
                return;

            // Cerca NetworkGameController senza import diretto
            if (!_networkControllerSearched)
            {
                _networkControllerSearched = true;
                
                // Cerca il tipo NetworkGameController
                var ngcType = System.Type.GetType("Project51.Networking.NetworkGameController, Project51.Networking");
                if (ngcType != null)
                {
                    _networkController = FindObjectOfType(ngcType) as MonoBehaviour;
                }
            }

            if (_networkController != null)
            {
                // Chiama SendInitialGameState via reflection
                var method = _networkController.GetType().GetMethod("SendInitialGameState");
                if (method != null)
                {
                    method.Invoke(_networkController, new object[] { turnController.GameState });
                    Debug.Log("[GameSceneInitializer] Sent initial GameState to clients");
                }
            }
        }

        private void EnsureResponsiveCamera()
        {
            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }

            if (targetCamera == null)
            {
                Debug.LogWarning("[GameSceneInitializer] No camera found for responsive gameplay layout.");
                return;
            }

            targetCamera.orthographic = true;

            var responsiveFit = targetCamera.GetComponent("CameraResponsiveFit") as MonoBehaviour;
            if (responsiveFit == null)
            {
                var responsiveType = System.Type.GetType("Project51.Unity.CameraResponsiveFit, Project51.Gameplay");
                if (responsiveType == null)
                {
                    Debug.LogWarning("[GameSceneInitializer] CameraResponsiveFit type not available yet. A Unity/VS refresh may be required.");
                    return;
                }

                responsiveFit = targetCamera.gameObject.AddComponent(responsiveType) as MonoBehaviour;
            }

            var applyMethod = responsiveFit.GetType().GetMethod("Apply");
            applyMethod?.Invoke(responsiveFit, null);
        }

        /// <summary>
        /// Restituisce la configurazione corrente del match.
        /// </summary>
        public MatchConfig GetConfig() => _config;

        /// <summary>
        /// Restituisce se siamo in modalità training (vs bot).
        /// </summary>
        public bool IsTrainingMode => _config?.Intent == MatchIntent.Training;

        /// <summary>
        /// Restituisce se siamo in modalità multiplayer.
        /// </summary>
        public bool IsMultiplayerMode => _config?.Intent != MatchIntent.Training;
    }
}
