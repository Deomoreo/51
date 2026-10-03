using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project51.Core;
using Project51.Networking;
using Photon.Pun;

namespace Project51.Unity
{
    /// <summary>
    /// Controller principale per il flusso dalla Home al Game.
    /// Coordina: Home (QuickSelectionPanels/RoomFlowV2) -> MatchmakingManager -> GameScene
    /// </summary>
    public class GameLaunchController : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logEvents = true;

        // Configurazione corrente
        private MatchConfig _pendingConfig;

        private bool _matchmakingEventsSubscribed;

        private void OnEnable()
        {
            EnsureMatchmakingSubscription();
        }

        private void OnDisable()
        {
            if (_matchmakingEventsSubscribed && MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnError -= OnMatchmakingError;
                MatchmakingManager.Instance.OnMatchFound -= OnMatchFound;
            }
            _matchmakingEventsSubscribed = false;
        }

        /// <summary>
        /// Iscrizione "lazy" agli eventi di MatchmakingManager: OnEnable() da solo non basta,
        /// perche' MatchmakingManager.Instance potrebbe non essere ancora pronto a quel punto
        /// (il flusso di autenticazione PlayFab/Photon e' asincrono e gira prima della Home).
        /// Richiamato anche subito prima di ogni operazione che dipende da questi eventi, cosi'
        /// l'iscrizione avviene comunque appena l'istanza e' disponibile, indipendentemente
        /// dall'ordine di Awake/OnEnable tra i due componenti.
        /// </summary>
        private void EnsureMatchmakingSubscription()
        {
            if (_matchmakingEventsSubscribed || MatchmakingManager.Instance == null)
                return;

            MatchmakingManager.Instance.OnError += OnMatchmakingError;
            MatchmakingManager.Instance.OnMatchFound += OnMatchFound;
            _matchmakingEventsSubscribed = true;
        }

        #region Training (Bot)

        private void StartTrainingMatch(MatchConfig config)
        {
            CancelPendingLaunch(); // allenamento scelto mentre una partita online aspettava il controllo della sospensione
            if (logEvents)
                Debug.Log("[GameLaunchController] Starting training match...");

            // Training is supported for 1v1 and 4P (and 2v2 uses 4 players). Any format maps to a PlayerCount.
            if (config == null)
            {
                Debug.LogWarning("[GameLaunchController] Training config is null.");
                return;
            }

            if (AppLoading.IsAvailable)
            {
                GoToSceneForConfig(config);
                return;
            }

            // Per training non serve matchmaking, vai diretto al gioco
            if (MatchmakingManager.Instance != null)
            {
                EnsureMatchmakingSubscription();
                MatchmakingManager.Instance.StartTraining(config);
            }
            else
            {
                // Fallback: vai direttamente alla scena
                GoToSceneForConfig(config);
            }
        }

        #endregion

        #region Quick Match

        private void StartQuickMatch(MatchConfig config)
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Starting quick match...");

            // Moderazione: gioco online sospeso -> la schermata Sospensione (contro i bot si gioca sempre).
            WhenOnlineAllowed(() => StartQuickMatchNow(config));
        }

        // Partenze online in attesa del controllo della sospensione: ogni partenza nuova, l'allenamento o Annulla (CancelPendingLaunch)
        // la scartano, cosi' una risposta in ritardo non crea o non apre una stanza che non si vuole piu'.
        private int _launchSeq;

        private void WhenOnlineAllowed(System.Action go, System.Action blocked = null)
        {
            int id = ++_launchSeq;
            Project51.Unity.UI.UI51SuspensionView.WhenOnlineAllowed(() => { if (this != null && id == _launchSeq) go(); }, blocked);
        }

        /// <summary>Annulla una partenza online ancora in attesa del controllo della sospensione.</summary>
        public void CancelPendingLaunch() => _launchSeq++;

        private void StartQuickMatchNow(MatchConfig config)
        {
            if (this == null) return;

            if (MatchmakingManager.Instance != null)
            {
                EnsureMatchmakingSubscription();
                MatchmakingManager.Instance.StartQuickMatch(config);
            }
            else
            {
                Debug.LogError("[GameLaunchController] MatchmakingManager not found!");
            }
        }

        #endregion

        #region Private Room

        /// <summary>
        /// Chiamato per creare una nuova stanza privata.
        /// </summary>
        /// <param name="blocked">Sospensione letta dal server: la schermata Sospensione e' aperta, chi chiama torna libero.</param>
        public void CreatePrivateRoom(MatchConfig config = null, System.Action blocked = null)
        {
            WhenOnlineAllowed(() => CreatePrivateRoomNow(config), blocked);
        }

        private void CreatePrivateRoomNow(MatchConfig config)
        {
            if (this == null) return;
            var cfg = config ?? _pendingConfig ?? new MatchConfig { Intent = MatchIntent.PrivateRoom };
            _pendingConfig = cfg;

            if (logEvents)
                Debug.Log("[GameLaunchController] Creating private room...");

            if (MatchmakingManager.Instance != null)
            {
                EnsureMatchmakingSubscription();
                MatchmakingManager.Instance.CreatePrivateRoom(cfg);
            }
        }

        public void JoinPrivateRoom(string roomCode, MatchConfig config, System.Action blocked = null)
        {
            _pendingConfig = config;
            WhenOnlineAllowed(() => JoinRoomNow(roomCode), blocked);
        }

        private void JoinRoomNow(string roomCode)
        {
            if (this == null) return;
            if (logEvents)
                Debug.Log($"[GameLaunchController] Joining room: {roomCode}");

            if (MatchmakingManager.Instance != null)
            {
                EnsureMatchmakingSubscription();
                MatchmakingManager.Instance.JoinPrivateRoom(roomCode, _pendingConfig);
            }
        }

        #endregion

        #region Matchmaking Callbacks

        private void OnMatchmakingError(string error)
        {
            Debug.LogWarning($"[GameLaunchController] Matchmaking error: {error}");
        }

        private void OnMatchFound()
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Match found! Loading game scene...");

            // Breve attesa prima di cambiare scena: PhotonNetwork.LoadLevel/SceneManager.LoadScene
            // non aspettano nessun fade, cosi' l'uscita della lobby finisce prima del cambio scena.
            StartCoroutine(LoadGameSceneAfterTransition(MatchmakingManager.Instance?.CurrentConfig ?? _pendingConfig));
        }

        private System.Collections.IEnumerator LoadGameSceneAfterTransition(MatchConfig config)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            GoToSceneForConfig(config);
        }

        #endregion

        #region Scene Loading

        private void GoToSceneForConfig(MatchConfig config)
        {
            string sceneName = AppFlowManager.SCENE_GAME;

            // Salva la config per la scena di gioco usando il helper in Core
            MatchConfigStorage.Save(config);
            if (logEvents)
                Debug.Log($"[GameLaunchController] Config saved: {config} -> scene={sceneName}");

            // For now: training (offline) uses normal scene load.
            if (config == null || config.Intent == MatchIntent.Training)
            {
                if (!AppLoading.LoadScene(sceneName)) UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                return;
            }

            // Online: use Photon to sync when ready (kept for future use)
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(sceneName);
            }
        }

        #endregion

        /// <summary>
        /// Entry point called by the Home "Gioca" button.
        /// Executes the correct flow depending on the selected config.
        /// </summary>
        public void Launch(MatchConfig config)
        {
            if (logEvents)
                Debug.Log($"[GameLaunchController] Launch requested: {config}");

            _pendingConfig = config;

            if (config == null)
            {
                Debug.LogWarning("[GameLaunchController] Launch called with null config.");
                return;
            }

            switch (config.Intent)
            {
                case MatchIntent.Training:
                    StartTrainingMatch(config);
                    break;
                case MatchIntent.QuickMatch:
                    StartQuickMatch(config);
                    break;
                case MatchIntent.PrivateRoom:
                    if (config.IsHost)
                        CreatePrivateRoom(config);
                    break;
                default:
                    Debug.LogWarning($"[GameLaunchController] Unsupported intent: {config.Intent}");
                    break;
            }
        }
    }
}
