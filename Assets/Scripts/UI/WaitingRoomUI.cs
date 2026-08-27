using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Project51.Core;
using Project51.Networking;
using Photon.Pun;

namespace Project51.Unity
{
    /// <summary>
    /// UI per la Waiting Room (Sala d'Attesa) del Tavolo Privato.
    /// Mostra codice stanza, slot giocatori, stato ready, bottone avvia.
    /// </summary>
    public class WaitingRoomUI : MonoBehaviour
    {
        [Header("Room Info")]
        [SerializeField] private TMP_Text roomCodeText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private TMP_Text roomCodeCopiedFeedback;

        [Header("Player Slots")]
        [SerializeField] private Transform playerSlotsContainer;
        [SerializeField] private GameObject playerSlotPrefab;
        [SerializeField] private int maxSlots = 4;

        [Header("Controls")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private TMP_Text startButtonText;
        [SerializeField] private TMP_Text statusText;

        [Header("Bot Fill (Host Only)")]
        [Tooltip("Se attivo, l'host puo' avviare la partita anche senza aver riempito tutti i posti: i posti vuoti vengono giocati da un bot (CirullaAI) gestito dal Master Client.")]
        [SerializeField] private Toggle fillWithBotsToggle;
        [SerializeField] private GameObject fillWithBotsContainer;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.3f;

        // Eventi
        public event Action OnStartRequested;
        public event Action OnLeaveRequested;

        private PlayerSlotUI[] _playerSlots;
        private bool _isHost;
        private int _requiredPlayers;

        /// <summary>
        /// Se true, l'host puo' avviare la partita anche con posti vuoti: verranno riempiti con bot
        /// (vedi GameSceneInitializer, che li assegna automaticamente confrontando i giocatori reali
        /// in stanza col formato scelto). Irrilevante per i client non host.
        /// Il toggle in scena e' opzionale: se non e' stato collegato (vedi
        /// Tools/Lobby/Add Fill-With-Bots Toggle To Waiting Room) il riempimento automatico resta
        /// comunque attivo di default; se il toggle e' presente e spento, l'host deve aspettare la
        /// stanza piena come in origine.
        /// </summary>
        public bool FillEmptySlotsWithBots => _isHost && (fillWithBotsToggle == null || fillWithBotsToggle.isOn);

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (leaveButton != null)
                leaveButton.onClick.AddListener(OnLeaveClicked);
            if (copyCodeButton != null)
                copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
            if (fillWithBotsToggle != null)
                fillWithBotsToggle.onValueChanged.AddListener(OnFillWithBotsToggled);

            // Nascondi il feedback di copia inizialmente
            if (roomCodeCopiedFeedback != null)
                roomCodeCopiedFeedback.alpha = 0f;

            CreatePlayerSlots();
        }

        private void OnEnable()
        {
            // Sottoscrivi agli eventi del matchmaking
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnPlayerJoined += OnPlayerJoined;
                MatchmakingManager.Instance.OnPlayerLeft += OnPlayerLeft;
            }
        }

        private void OnDisable()
        {
            if (MatchmakingManager.Instance != null)
            {
                MatchmakingManager.Instance.OnPlayerJoined -= OnPlayerJoined;
                MatchmakingManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            }
        }

        private void CreatePlayerSlots()
        {
            _playerSlots = new PlayerSlotUI[maxSlots];

            if (playerSlotsContainer == null || playerSlotPrefab == null)
            {
                Debug.LogWarning("[WaitingRoomUI] Player slot prefab or container not assigned");
                return;
            }

            // Pulisci container
            foreach (Transform child in playerSlotsContainer)
            {
                Destroy(child.gameObject);
            }

            // Crea slot
            for (int i = 0; i < maxSlots; i++)
            {
                var slotObj = Instantiate(playerSlotPrefab, playerSlotsContainer);
                _playerSlots[i] = slotObj.GetComponent<PlayerSlotUI>();
                if (_playerSlots[i] != null)
                {
                    _playerSlots[i].SetEmpty(i + 1);
                }
            }
        }

        /// <summary>
        /// Inizializza la waiting room con i dati della stanza.
        /// </summary>
        public void Initialize(string roomCode, bool isHost, int requiredPlayers)
        {
            _isHost = isHost;
            _requiredPlayers = requiredPlayers;
            _isLeaving = false;

            // Riattiva l'input: Hide() lo disabilita per bloccare i click durante il fade-out.
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Mostra codice stanza
            if (roomCodeText != null)
                roomCodeText.text = roomCode;

            // Configura bottone start (solo host)
            if (startButton != null)
            {
                startButton.gameObject.SetActive(isHost);
                startButton.interactable = false;
            }

            if (leaveButton != null)
                leaveButton.interactable = true;

            if (startButtonText != null)
                startButtonText.text = "AVVIA PARTITA";

            // Il toggle "riempi con bot" ha senso solo per l'host
            if (fillWithBotsToggle != null)
                fillWithBotsToggle.SetIsOnWithoutNotify(false);
            if (fillWithBotsContainer != null)
                fillWithBotsContainer.SetActive(isHost);
            else if (fillWithBotsToggle != null)
                fillWithBotsToggle.gameObject.SetActive(isHost);

            UpdateStatus();
            RefreshPlayerSlots();

            // Fade + leggero scale-in, cosi' l'ingresso nella waiting room non e' un pop istantaneo
            // dopo la chiusura animata del pannello modalita' (DOKill previene salti se Initialize
            // viene richiamato mentre l'animazione precedente e' ancora in corso).
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeInDuration);

                if (canvasGroup.transform is RectTransform rt)
                {
                    rt.DOKill();
                    rt.localScale = Vector3.one * 0.94f;
                    rt.DOScale(Vector3.one, fadeInDuration).SetEase(Ease.OutCubic);
                }
            }
        }

        /// <summary>
        /// Aggiorna la lista dei giocatori.
        /// </summary>
        public void RefreshPlayerSlots()
        {
            if (_playerSlots == null) return;

            // Reset tutti gli slot
            for (int i = 0; i < _playerSlots.Length; i++)
            {
                if (_playerSlots[i] != null)
                    _playerSlots[i].SetEmpty(i + 1);
            }

            // Se non siamo in una stanza Photon, esci
            if (!PhotonNetwork.InRoom) return;

            // Popola con i giocatori attuali
            var players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length && i < _playerSlots.Length; i++)
            {
                if (_playerSlots[i] != null)
                {
                    bool isLocal = players[i].IsLocal;
                    bool isMaster = players[i].IsMasterClient;
                    _playerSlots[i].SetPlayer(players[i].NickName, isLocal, isMaster);
                }
            }

            UpdateStatus();
            UpdateStartButton();
        }

        private void UpdateStatus()
        {
            if (statusText == null) return;

            if (!PhotonNetwork.InRoom)
            {
                statusText.text = "Connessione...";
                return;
            }

            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;

            if (current >= max)
            {
                statusText.text = _isHost ? "Pronto! Premi AVVIA" : "In attesa dell'host...";
            }
            else if (FillEmptySlotsWithBots)
            {
                int missing = Mathf.Max(0, _requiredPlayers - current);
                statusText.text = missing > 0
                    ? $"Pronto! {missing} posto/i verranno riempiti con bot ({current}/{max})"
                    : "Pronto! Premi AVVIA";
            }
            else
            {
                statusText.text = $"In attesa di giocatori... ({current}/{max})";
            }
        }

        private void UpdateStartButton()
        {
            if (startButton == null || !_isHost) return;

            if (!PhotonNetwork.InRoom)
            {
                startButton.interactable = false;
                return;
            }

            // Con "riempi con bot" attivo, l'host puo' avviare anche da solo: i posti mancanti
            // vengono assegnati a bot in GameSceneInitializer una volta caricata la GameScene.
            int minPlayers = FillEmptySlotsWithBots ? 1 : Mathf.Max(2, _requiredPlayers);
            startButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount >= minPlayers;
        }

        private void OnFillWithBotsToggled(bool _)
        {
            UpdateStatus();
            UpdateStartButton();
        }

        private void OnPlayerJoined(Photon.Realtime.Player player)
        {
            Debug.Log($"[WaitingRoomUI] Player joined: {player.NickName}");
            RefreshPlayerSlots();
        }

        private void OnPlayerLeft(Photon.Realtime.Player player)
        {
            Debug.Log($"[WaitingRoomUI] Player left: {player.NickName}");
            RefreshPlayerSlots();
        }

        private void OnStartClicked()
        {
            if (!_isHost)
            {
                Debug.LogWarning("[WaitingRoomUI] Only host can start!");
                return;
            }

            OnStartRequested?.Invoke();
        }

        private bool _isLeaving;

        private void OnLeaveClicked()
        {
            // Guardia anti doppio-invio: durante il fade-out di Hide() (0.2s) il CanvasGroup resta
            // cliccabile finche' non e' del tutto trasparente, quindi un doppio click/tap sul
            // bottone ESCI poteva rilanciare OnLeaveRequested una seconda volta (leave + rejoin che
            // sembrava un pannello che "si leva e si rimette").
            if (_isLeaving) return;
            _isLeaving = true;

            if (leaveButton != null) leaveButton.interactable = false;
            if (startButton != null) startButton.interactable = false;

            OnLeaveRequested?.Invoke();
        }

        private void OnCopyCodeClicked()
        {
            if (roomCodeText == null) return;

            GUIUtility.systemCopyBuffer = roomCodeText.text;
            Debug.Log($"[WaitingRoomUI] Room code copied: {roomCodeText.text}");

            // Mostra feedback
            ShowCopyFeedback();
        }

        private void ShowCopyFeedback()
        {
            if (roomCodeCopiedFeedback == null) return;

            roomCodeCopiedFeedback.DOKill();
            roomCodeCopiedFeedback.alpha = 1f;
            DOTween.To(() => roomCodeCopiedFeedback.alpha, x => roomCodeCopiedFeedback.alpha = x, 0f, 2f)
                .SetDelay(1f);
        }

        /// <summary>
        /// Nasconde la waiting room.
        /// </summary>
        public void Hide()
        {
            if (canvasGroup != null)
            {
                // Blocca subito i click: durante il fade-out (0.2s) il pannello resta visivamente
                // presente ma non deve piu' reagire a input (vedi guardia in OnLeaveClicked).
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                canvasGroup.DOKill();
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, 0.2f)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
