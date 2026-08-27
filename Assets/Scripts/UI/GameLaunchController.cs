using System;
using System.Collections;
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
    /// Coordina: ModalitySelector ? MatchmakingManager ? WaitingRoom ? Game Scene
    /// </summary>
    public class GameLaunchController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ModalitySelectorPanelUI modalityPanel;
        [SerializeField] private WaitingRoomUI waitingRoomUI;
        [SerializeField] private JoinRoomPopupUI joinRoomPopup;
        [SerializeField] private MatchmakingStatusUI matchmakingStatusUI;

        [Header("Scene Names")]
        // NOTE: WaitingRoom e LobbyScene NON esistono come scene separate: la waiting room e' un
        // overlay dentro MainMenu.unity. La scena di destinazione per QUALSIASI intent, una volta
        // che la partita puo' iniziare, e' sempre GameScene. Non puntare piu' a nomi di scena inesistenti
        // (causava un PhotonNetwork.LoadLevel/SceneManager.LoadScene silenzioso su una scena assente:
        // "la partita non parte mai" anche a stanza piena).
        [SerializeField] private string trainingSceneName = "GameScene";
        [SerializeField] private string quickMatchSceneName = "GameScene";
        [SerializeField] private string privateRoomSceneName = "GameScene";

        [Header("Fake Matchmaking (temporary)")]
        [Tooltip("For now, simulate matchmaking/loading even for offline training.")]
        [SerializeField] private bool useFakeMatchmakingForTraining = true;
        [SerializeField] private float fakePhase1Duration = 0.6f;
        [SerializeField] private float fakePhase2Duration = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool logEvents = true;

        // Configurazione corrente
        private MatchConfig _pendingConfig;
        private Coroutine _fakeRoutine;

        private void Awake()
        {
            // Subscribe agli eventi del ModalitySelector
            if (modalityPanel != null)
            {
                modalityPanel.OnConfigSelected += OnModalityConfigSelected;
                modalityPanel.OnCreatePrivateRoomSelected += OnCreatePrivateRoomRequested;
                modalityPanel.OnJoinPrivateRoomSelected += OnJoinPrivateRoomSelectedFromPanel;
            }

            // Subscribe agli eventi del WaitingRoom
            if (waitingRoomUI != null)
            {
                waitingRoomUI.OnStartRequested += OnWaitingRoomStartRequested;
                waitingRoomUI.OnLeaveRequested += OnWaitingRoomLeaveRequested;
            }

            // Subscribe agli eventi del JoinRoomPopup
            if (joinRoomPopup != null)
            {
                joinRoomPopup.OnJoinRequested += OnJoinRoomCodeEntered;
            }

            // Subscribe agli eventi del MatchmakingStatusUI
            if (matchmakingStatusUI != null)
            {
                matchmakingStatusUI.OnCancelRequested += OnMatchmakingCancelRequested;
            }
        }

        private bool _matchmakingEventsSubscribed;

        private void OnEnable()
        {
            EnsureMatchmakingSubscription();
        }

        private void OnDisable()
        {
            if (_matchmakingEventsSubscribed && MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnStateChanged -= OnMatchmakingStateChanged;
                MatchmakingManager.Instance.OnError -= OnMatchmakingError;
                MatchmakingManager.Instance.OnMatchFound -= OnMatchFound;
                MatchmakingManager.Instance.OnRoomCreated -= OnPrivateRoomCreated;
                MatchmakingManager.Instance.OnRoomJoined -= OnPrivateRoomJoined;
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

            MatchmakingManager.Instance.OnStateChanged += OnMatchmakingStateChanged;
            MatchmakingManager.Instance.OnError += OnMatchmakingError;
            MatchmakingManager.Instance.OnMatchFound += OnMatchFound;
            MatchmakingManager.Instance.OnRoomCreated += OnPrivateRoomCreated;
            MatchmakingManager.Instance.OnRoomJoined += OnPrivateRoomJoined;
            _matchmakingEventsSubscribed = true;
        }

        private void OnDestroy()
        {
            if (modalityPanel != null)
            {
                modalityPanel.OnConfigSelected -= OnModalityConfigSelected;
                modalityPanel.OnCreatePrivateRoomSelected -= OnCreatePrivateRoomRequested;
                modalityPanel.OnJoinPrivateRoomSelected -= OnJoinPrivateRoomSelectedFromPanel;
            }
            if (waitingRoomUI != null)
            {
                waitingRoomUI.OnStartRequested -= OnWaitingRoomStartRequested;
                waitingRoomUI.OnLeaveRequested -= OnWaitingRoomLeaveRequested;
            }
            if (joinRoomPopup != null)
                joinRoomPopup.OnJoinRequested -= OnJoinRoomCodeEntered;
            if (matchmakingStatusUI != null)
                matchmakingStatusUI.OnCancelRequested -= OnMatchmakingCancelRequested;
        }

        #region Modality Selection

        private void OnModalityConfigSelected(MatchConfig config)
        {
            if (logEvents)
                Debug.Log($"[GameLaunchController] Config selected: {config}");

            _pendingConfig = config;

            switch (config.Intent)
            {
                case MatchIntent.Training:
                    StartTrainingMatch(config);
                    break;

                case MatchIntent.QuickMatch:
                    StartQuickMatch(config);
                    break;

                case MatchIntent.PrivateRoom:
                    // Questo non dovrebbe pi� accadere - ora usiamo eventi separati
                    if (config.IsHost)
                        CreatePrivateRoom(config);
                    else
                        ShowJoinRoomPopup();
                    break;
            }
        }

        private void OnCreatePrivateRoomRequested(MatchConfig config)
        {
            if (logEvents)
                Debug.Log($"[GameLaunchController] Create private room requested: {config}");

            _pendingConfig = config;
            CreatePrivateRoom(config);
        }

        private void OnJoinPrivateRoomRequested(MatchConfig config)
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Join private room requested");

            _pendingConfig = config;
            ShowJoinRoomPopup();
        }

        private void OnJoinPrivateRoomSelectedFromPanel()
        {
            // OnJoinPrivateRoomSelected non porta la MatchConfig (a differenza di OnCreatePrivateRoomSelected):
            // Select_JoinPrivateRoom() valorizza pero' CurrentSelection subito prima di invocare l'evento.
            OnJoinPrivateRoomRequested(modalityPanel.CurrentSelection);
        }

        #endregion

        #region Training (Bot)

        private void StartTrainingMatch(MatchConfig config)
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Starting training match...");

            // Training is supported for 1v1 and 4P (and 2v2 uses 4 players). Any format maps to a PlayerCount.
            if (config == null)
            {
                Debug.LogWarning("[GameLaunchController] Training config is null.");
                ShowMatchmakingStatus("Errore", "Config mancante");
                return;
            }

            if (useFakeMatchmakingForTraining)
            {
                StartFakeMatchmakingAndLoad(config);
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

        #region Fake Matchmaking

        private void StartFakeMatchmakingAndLoad(MatchConfig config)
        {
            StopFakeMatchmaking();
            _fakeRoutine = StartCoroutine(FakeMatchmakingCoroutine(config));
        }

        private void StopFakeMatchmaking()
        {
            if (_fakeRoutine != null)
            {
                StopCoroutine(_fakeRoutine);
                _fakeRoutine = null;
            }
        }

        private IEnumerator FakeMatchmakingCoroutine(MatchConfig config)
        {
            ShowMatchmakingStatus("Preparazione partita...", "Connessione...");
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, fakePhase1Duration));

            UpdateMatchmakingStatus("Preparazione partita...", "Caricamento...");
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, fakePhase2Duration));

            HideMatchmakingStatus();
            GoToSceneForConfig(config);
            _fakeRoutine = null;
        }

        #endregion

        #region Quick Match

        private void StartQuickMatch(MatchConfig config)
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Starting quick match...");

            ShowMatchmakingStatus("Connessione in corso...");

            if (MatchmakingManager.Instance != null)
            {
                EnsureMatchmakingSubscription();
                MatchmakingManager.Instance.StartQuickMatch(config);
            }
            else
            {
                Debug.LogError("[GameLaunchController] MatchmakingManager not found!");
                HideMatchmakingStatus();
            }
        }

        #endregion

        #region Private Room

        /// <summary>
        /// Chiamato per creare una nuova stanza privata.
        /// </summary>
        public void CreatePrivateRoom(MatchConfig config = null)
        {
            var cfg = config ?? _pendingConfig ?? new MatchConfig { Intent = MatchIntent.PrivateRoom };
            _pendingConfig = cfg;

            if (logEvents)
                Debug.Log("[GameLaunchController] Creating private room...");

            ShowMatchmakingStatus("Creazione stanza...");

            if (MatchmakingManager.Instance != null)
            {
                EnsureMatchmakingSubscription();
                MatchmakingManager.Instance.CreatePrivateRoom(cfg);
            }
        }

        /// <summary>
        /// Mostra il popup per inserire il codice stanza.
        /// </summary>
        public void ShowJoinRoomPopup()
        {
            if (joinRoomPopup != null)
            {
                joinRoomPopup.Show();
            }
        }

        private void OnJoinRoomCodeEntered(string roomCode)
        {
            if (logEvents)
                Debug.Log($"[GameLaunchController] Joining room: {roomCode}");

            if (joinRoomPopup != null)
                joinRoomPopup.Hide();

            ShowMatchmakingStatus("Connessione alla stanza...");

            if (MatchmakingManager.Instance != null)
            {
                EnsureMatchmakingSubscription();
                MatchmakingManager.Instance.JoinPrivateRoom(roomCode, _pendingConfig);
            }
        }

        private void OnPrivateRoomCreated(string roomCode)
        {
            if (logEvents)
                Debug.Log($"[GameLaunchController] Private room created: {roomCode}");

            HideMatchmakingStatus();
            ShowWaitingRoom(roomCode, isHost: true);
        }

        private void OnPrivateRoomJoined()
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Joined private room");

            HideMatchmakingStatus();

            string roomCode = MatchmakingManager.Instance?.CurrentConfig?.RoomCode ?? "???";
            ShowWaitingRoom(roomCode, isHost: false);
        }

        private void ShowWaitingRoom(string roomCode, bool isHost)
        {
            if (waitingRoomUI != null)
            {
                waitingRoomUI.gameObject.SetActive(true);
                int requiredPlayers = _pendingConfig?.PlayerCount ?? 4;
                waitingRoomUI.Initialize(roomCode, isHost, requiredPlayers);
            }
        }

        private void OnWaitingRoomStartRequested()
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Host starting game...");

            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.StartGame();
            }
        }

        private void OnWaitingRoomLeaveRequested()
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Leaving waiting room...");

            if (waitingRoomUI != null)
                waitingRoomUI.Hide();

            if (MatchmakingManager.Instance != null)
                MatchmakingManager.Instance.LeaveRoom();
        }

        #endregion

        #region Matchmaking Callbacks

        private void OnMatchmakingStateChanged(MatchmakingState state)
        {
            if (logEvents)
                Debug.Log($"[GameLaunchController] Matchmaking state: {state}");

            switch (state)
            {
                case MatchmakingState.Searching:
                    UpdateMatchmakingStatus("Ricerca partita...", "Attendere...");
                    break;
                case MatchmakingState.WaitingForPlayers:
                    int currentPlayers = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
                    int maxPlayers = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.MaxPlayers : 4;
                    UpdateMatchmakingStatus("In attesa di giocatori...", $"Giocatori: {currentPlayers}/{maxPlayers}");
                    break;
                case MatchmakingState.Starting:
                    UpdateMatchmakingStatus("Partita trovata!", "Caricamento...");
                    break;
                case MatchmakingState.Idle:
                    HideMatchmakingStatus();
                    break;
            }
        }

        private void OnMatchmakingError(string error)
        {
            Debug.LogError($"[GameLaunchController] Matchmaking error: {error}");

            if (matchmakingStatusUI != null)
            {
                matchmakingStatusUI.ShowError(error);
            }

            if (joinRoomPopup != null && joinRoomPopup.gameObject.activeInHierarchy)
            {
                joinRoomPopup.ShowError(error);
            }
        }

        private void OnMatchFound()
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Match found! Loading game scene...");

            if (waitingRoomUI != null)
                waitingRoomUI.Hide();

            // Caricare la scena subito dopo waitingRoomUI.Hide() tagliava via la sua animazione
            // di uscita (0.2s) a meta': PhotonNetwork.LoadLevel/SceneManager.LoadScene non aspettano
            // nessun fade. Mostriamo un breve step "Caricamento..." (gia' usato dal fake-matchmaking
            // del training) e diamo il tempo all'animazione di finire prima di cambiare scena.
            ShowMatchmakingStatus("Partita trovata!", "Caricamento...");
            StartCoroutine(LoadGameSceneAfterTransition(MatchmakingManager.Instance?.CurrentConfig ?? _pendingConfig));
        }

        private System.Collections.IEnumerator LoadGameSceneAfterTransition(MatchConfig config)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            HideMatchmakingStatus();
            GoToGameScene(config);
        }

        private void OnMatchmakingCancelRequested()
        {
            if (logEvents)
                Debug.Log("[GameLaunchController] Matchmaking cancelled by user");

            StopFakeMatchmaking();

            if (MatchmakingManager.Instance != null)
                MatchmakingManager.Instance.Cancel();

            HideMatchmakingStatus();
        }

        #endregion

        #region UI Helpers

        private void ShowMatchmakingStatus(string status, string detail = "")
        {
            if (matchmakingStatusUI != null)
                matchmakingStatusUI.Show(status, detail);
        }

        private void UpdateMatchmakingStatus(string status, string detail = "")
        {
            if (matchmakingStatusUI != null)
                matchmakingStatusUI.UpdateStatus(status, detail);
        }

        private void HideMatchmakingStatus()
        {
            if (matchmakingStatusUI != null)
                matchmakingStatusUI.Hide();
        }

        #endregion

        #region Scene Loading

        private string GetSceneNameForConfig(MatchConfig config)
        {
            if (config == null)
                return trainingSceneName;

            switch (config.Intent)
            {
                case MatchIntent.Training:
                    return trainingSceneName;
                case MatchIntent.QuickMatch:
                    return quickMatchSceneName;
                case MatchIntent.PrivateRoom:
                    return privateRoomSceneName;
                default:
                    return trainingSceneName;
            }
        }

        private void GoToSceneForConfig(MatchConfig config)
        {
            string sceneName = GetSceneNameForConfig(config);

            // Salva la config per la scena di gioco usando il helper in Core
            MatchConfigStorage.Save(config);
            if (logEvents)
                Debug.Log($"[GameLaunchController] Config saved: {config} -> scene={sceneName}");

            // For now: training (offline) uses normal scene load.
            if (config == null || config.Intent == MatchIntent.Training)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                return;
            }

            // Online: use Photon to sync when ready (kept for future use)
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(sceneName);
            }
        }

        // Backward compatibility: keep the old name used by other code paths.
        private void GoToGameScene(MatchConfig config)
        {
            GoToSceneForConfig(config);
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
                    else
                        ShowJoinRoomPopup();
                    break;
                default:
                    Debug.LogWarning($"[GameLaunchController] Unsupported intent: {config.Intent}");
                    break;
            }
        }
    }
}
