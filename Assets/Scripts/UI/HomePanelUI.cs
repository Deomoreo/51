using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Project51.Unity
{
    public enum GameMode
    {
        VsBot,
        Online
    }

    public class HomePanelUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button modeButton;
        [SerializeField] private Button cosmeticsButton;
        [SerializeField] private TMP_Text modeLabel;

        [Header("Popup")]
        [SerializeField] private ModeSelectPopupUI modeSelectPopup;

        [Header("Events")]
        public UnityEvent<GameMode> OnStartGameRequested;

        private GameMode _selectedMode = GameMode.VsBot;

        private void Awake()
        {
            if (!startGameButton || !modeButton || !cosmeticsButton || modeLabel == null || modeSelectPopup == null)
            {
                Debug.LogError("[HomePanelUI] Missing references in the Inspector.", this);
                enabled = false;
                return;
            }

            modeSelectPopup.Init(this);

            startGameButton.onClick.AddListener(OnStartGame);
            modeButton.onClick.AddListener(OnModeButtonClicked);
            cosmeticsButton.onClick.AddListener(OnCosmeticsButtonClicked);

            RefreshModeUI();
        }

        private void OnStartGame()
        {
            Debug.Log($"[HomePanelUI] Start Game clicked. Mode={_selectedMode}", this);
            OnStartGameRequested?.Invoke(_selectedMode);
        }

        private void OnModeButtonClicked()
        {
            modeSelectPopup.Toggle();
        }

        private void OnCosmeticsButtonClicked()
        {
            Debug.Log("[HomePanelUI] Cosmetics clicked.", this);
        }

        public void SetMode(GameMode mode)
        {
            _selectedMode = mode;
            RefreshModeUI();
        }

        private void RefreshModeUI()
        {
            modeLabel.text = _selectedMode == GameMode.VsBot ? "VS BOT" : "ONLINE";
        }
    }
}
