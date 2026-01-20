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

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.3f;

        // Eventi
        public event Action OnStartRequested;
        public event Action OnLeaveRequested;

        private PlayerSlotUI[] _playerSlots;
        private bool _isHost;
        private int _requiredPlayers;

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (leaveButton != null)
                leaveButton.onClick.AddListener(OnLeaveClicked);
            if (copyCodeButton != null)
                copyCodeButton.onClick.AddListener(OnCopyCodeClicked);

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

            // Mostra codice stanza
            if (roomCodeText != null)
                roomCodeText.text = roomCode;

            // Configura bottone start (solo host)
            if (startButton != null)
            {
                startButton.gameObject.SetActive(isHost);
                startButton.interactable = false;
            }

            if (startButtonText != null)
                startButtonText.text = "AVVIA PARTITA";

            UpdateStatus();
            RefreshPlayerSlots();

            // Fade in
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeInDuration);
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

            // Abilita il bottone se abbiamo abbastanza giocatori
            int minPlayers = Mathf.Max(2, _requiredPlayers);
            startButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount >= minPlayers;
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

        private void OnLeaveClicked()
        {
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
