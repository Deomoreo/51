using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project51.Unity
{
    /// <summary>
    /// UI per un singolo slot giocatore nella Waiting Room.
    /// </summary>
    public class PlayerSlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text slotNumberText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image hostCrown;
        [SerializeField] private Image readyCheckmark;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject filledStateRoot;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color filledColor = new Color(0.3f, 0.5f, 0.3f, 1f);
        [SerializeField] private Color localPlayerColor = new Color(0.3f, 0.6f, 0.9f, 1f);

        private bool _isEmpty = true;
        private bool _isLocalPlayer;
        private bool _isHost;

        /// <summary>
        /// Imposta lo slot come vuoto.
        /// </summary>
        public void SetEmpty(int slotNumber)
        {
            _isEmpty = true;
            _isLocalPlayer = false;
            _isHost = false;

            if (playerNameText != null)
                playerNameText.text = "";

            if (slotNumberText != null)
            {
                slotNumberText.gameObject.SetActive(true);
                slotNumberText.text = $"Slot {slotNumber}";
            }

            if (backgroundImage != null)
                backgroundImage.color = emptyColor;

            if (hostCrown != null)
                hostCrown.gameObject.SetActive(false);

            if (readyCheckmark != null)
                readyCheckmark.gameObject.SetActive(false);

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(true);

            if (filledStateRoot != null)
                filledStateRoot.SetActive(false);
        }

        /// <summary>
        /// Imposta lo slot con un giocatore.
        /// </summary>
        public void SetPlayer(string playerName, bool isLocal, bool isHost)
        {
            _isEmpty = false;
            _isLocalPlayer = isLocal;
            _isHost = isHost;

            if (playerNameText != null)
            {
                playerNameText.text = isLocal ? $"{playerName} (Tu)" : playerName;
            }

            if (slotNumberText != null)
                slotNumberText.gameObject.SetActive(false);

            if (backgroundImage != null)
                backgroundImage.color = isLocal ? localPlayerColor : filledColor;

            if (hostCrown != null)
                hostCrown.gameObject.SetActive(isHost);

            if (readyCheckmark != null)
                readyCheckmark.gameObject.SetActive(false); // TODO: implementare ready status

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(false);

            if (filledStateRoot != null)
                filledStateRoot.SetActive(true);
        }

        /// <summary>
        /// Imposta lo stato "pronto" del giocatore.
        /// </summary>
        public void SetReady(bool isReady)
        {
            if (readyCheckmark != null)
                readyCheckmark.gameObject.SetActive(isReady);
        }

        public bool IsEmpty => _isEmpty;
        public bool IsLocalPlayer => _isLocalPlayer;
        public bool IsHost => _isHost;
    }
}
