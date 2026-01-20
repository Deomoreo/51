using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Project51.Core;

namespace Project51.Unity
{
    // Manteniamo GameMode per retrocompatibilità
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

        [Header("Popup (Legacy)")]
        [SerializeField] private ModeSelectPopupUI modeSelectPopup;

        [Header("New Modality System")]
        [Tooltip("Il nuovo pannello di selezione modalità. Se assegnato, sostituisce modeSelectPopup.")]
        [SerializeField] private ModalitySelectorPanelUI modalitySelectorPanel;
        [SerializeField] private GameLaunchController gameLaunchController;

        [Header("Events")]
        public UnityEvent<GameMode> OnStartGameRequested;
        public UnityEvent<MatchConfig> OnMatchConfigSelected;

        private GameMode _selectedMode = GameMode.VsBot;
        private MatchConfig _currentConfig;

        private void Awake()
        {
            if (!startGameButton || !modeButton || !cosmeticsButton || modeLabel == null)
            {
                Debug.LogError("[HomePanelUI] Missing references in the Inspector.", this);
                enabled = false;
                return;
            }

            // Init legacy popup se presente
            if (modeSelectPopup != null)
                modeSelectPopup.Init(this);

            // Subscribe al nuovo sistema se presente
            if (modalitySelectorPanel != null)
            {
                modalitySelectorPanel.OnConfigSelected += OnNewModeConfigSelected;
            }

            startGameButton.onClick.AddListener(OnStartGame);
            modeButton.onClick.AddListener(OnModeButtonClicked);
            cosmeticsButton.onClick.AddListener(OnCosmeticsButtonClicked);

            RefreshModeUI();
        }

        private void OnDestroy()
        {
            if (modalitySelectorPanel != null)
                modalitySelectorPanel.OnConfigSelected -= OnNewModeConfigSelected;
        }

        private void OnStartGame()
        {

            // Prefer the new modality flow: launch the currently selected config when pressing Play.
            if (modalitySelectorPanel != null && gameLaunchController != null)
            {
                var cfg = modalitySelectorPanel.CurrentSelection ?? _currentConfig;
                if (cfg != null)
                {
                    gameLaunchController.Launch(cfg);
                    return;
                }
            }

            // Se abbiamo una config dal nuovo sistema, usala
            if (_currentConfig != null && gameLaunchController != null)
            {
                // Fallback: launch with the last known config
                gameLaunchController.Launch(_currentConfig);
                return;
            }

            // Fallback al vecchio sistema
            OnStartGameRequested?.Invoke(_selectedMode);
        }

        private void OnModeButtonClicked()
        {
            // Preferisci il nuovo sistema se disponibile
            if (modalitySelectorPanel != null)
            {
                modalitySelectorPanel.Open();
            }
            else if (modeSelectPopup != null)
            {
                modeSelectPopup.Toggle();
            }
        }

        private void OnCosmeticsButtonClicked()
        {
            Debug.Log("[HomePanelUI] Cosmetics clicked.", this);
            // TODO: Aprire pannello selezione dorso carte
        }

        /// <summary>
        /// Callback dal nuovo sistema di selezione modalità.
        /// </summary>
        private void OnNewModeConfigSelected(MatchConfig config)
        {
            _currentConfig = config;

            // Aggiorna l'etichetta in base all'intent
            _selectedMode = config.Intent == MatchIntent.Training ? GameMode.VsBot : GameMode.Online;
            RefreshModeUI();

            // Notifica gli eventi
            OnMatchConfigSelected?.Invoke(config);

            Debug.Log($"[HomePanelUI] New config selected: {config}", this);
        }

        /// <summary>
        /// Metodo legacy per il vecchio ModeSelectPopupUI.
        /// </summary>
        public void SetMode(GameMode mode)
        {
            _selectedMode = mode;
            
            // Crea una config base dal GameMode legacy
            _currentConfig = new MatchConfig
            {
                Intent = mode == GameMode.VsBot ? MatchIntent.Training : MatchIntent.QuickMatch,
                Format = GameFormat.FourPlayers,
                BotDifficulty = BotDifficulty.Medium
            };

            RefreshModeUI();
        }

        private void RefreshModeUI()
        {
            if (modeLabel == null) return;

            if (_currentConfig != null)
            {
                // Mostra intent + formato
                string intentText = _currentConfig.Intent switch
                {
                    MatchIntent.QuickMatch => "ONLINE",
                    MatchIntent.PrivateRoom => "AMICI",
                    MatchIntent.Training => "BOT",
                    _ => "???"
                };

                string formatText = _currentConfig.Format switch
                {
                    GameFormat.OneVsOne => "1v1",
                    GameFormat.FourPlayers => "4P",
                    GameFormat.TwoVsTwo => "2v2",
                    _ => ""
                };

                modeLabel.text = $"{intentText} {formatText}";
            }
            else
            {
                // Fallback legacy
                modeLabel.text = _selectedMode == GameMode.VsBot ? "VS BOT" : "ONLINE";
            }
        }

        /// <summary>
        /// Restituisce la configurazione corrente del match.
        /// </summary>
        public MatchConfig GetCurrentConfig()
        {
            return _currentConfig ?? new MatchConfig
            {
                Intent = _selectedMode == GameMode.VsBot ? MatchIntent.Training : MatchIntent.QuickMatch,
                Format = GameFormat.FourPlayers
            };
        }
    }
}
