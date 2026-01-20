using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>
    /// Pannello di selezione modalità stile Clash Royale.
    /// Le card selezionano una MatchConfig e chiudono il pannello.
    /// Il bottone "Gioca" esterno legge CurrentSelection e avvia la partita.
    /// </summary>
    public class ModalitySelectorPanelUI : SlideUpPanelUI
    {
        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        [Header("Difficulty Selector (Training)")]
        [SerializeField] private Button difficultyEasyButton;
        [SerializeField] private Button difficultyMediumButton;
        [SerializeField] private Button difficultyHardButton;
        [SerializeField] private Image easyIndicator;
        [SerializeField] private Image mediumIndicator;
        [SerializeField] private Image hardIndicator;

        [Header("Difficulty Colors")]
        [SerializeField] private Color easyColor = new Color(0.2f, 0.8f, 0.2f);      // Verde
        [SerializeField] private Color mediumColor = new Color(1f, 0.8f, 0.2f);      // Giallo
        [SerializeField] private Color hardColor = new Color(0.9f, 0.2f, 0.2f);      // Rosso

        [Header("Selection Feedback")]
        [SerializeField] private TMP_Text currentSelectionText;

        // La selezione corrente
        public MatchConfig CurrentSelection { get; private set; }

        // Evento fired quando la selezione cambia (per aggiornare UI esterna, es. bottone Gioca)
        public event Action<MatchConfig> OnSelectionChanged;

        // Evento per casi speciali che richiedono flow aggiuntivi
        public event Action<MatchConfig> OnCoopModeSelected;           // 2v2 Coop -> apre panel scelta amico/random
        public event Action<MatchConfig> OnCreatePrivateRoomSelected;  // Crea stanza -> apre waiting room
        public event Action OnJoinPrivateRoomSelected;                 // Inserisci codice -> apre popup codice

        private BotDifficulty _currentDifficulty = BotDifficulty.Medium;

        protected override void Awake()
        {
            base.Awake();


            // Close button
            if (closeButton != null)
                closeButton.onClick.AddListener(() => {
                    Close();
                });

            // Difficulty buttons
            if (difficultyEasyButton != null)
            {
                difficultyEasyButton.onClick.AddListener(() => {
                    SetDifficulty(BotDifficulty.Easy);
                });
            }
            if (difficultyMediumButton != null)
            {
                difficultyMediumButton.onClick.AddListener(() => {
                    Debug.Log("[ModalitySelectorPanelUI] Medium button clicked!");
                    SetDifficulty(BotDifficulty.Medium);
                });
            }
            if (difficultyHardButton != null)
            {
                difficultyHardButton.onClick.AddListener(() => {
                    SetDifficulty(BotDifficulty.Hard);
                });
            }

            // Default selection
            CurrentSelection = new MatchConfig
            {
                Intent = MatchIntent.QuickMatch,
                Format = GameFormat.FourPlayers,
                BotDifficulty = BotDifficulty.Medium,
                TargetScore = 51
            };

            UpdateDifficultyUI();
        }

        protected override void OnOpening()
        {
            base.OnOpening();
            UpdateDifficultyUI();
            UpdateSelectionText();
        }

        #region Difficulty Selector

        public void SetDifficulty(BotDifficulty difficulty)
        {
            _currentDifficulty = difficulty;
            UpdateDifficultyUI();

            // Se la selezione corrente è Training, aggiorna anche la config
            if (CurrentSelection != null && CurrentSelection.Intent == MatchIntent.Training)
            {
                CurrentSelection.BotDifficulty = difficulty;
                OnSelectionChanged?.Invoke(CurrentSelection);
                UpdateSelectionText();
            }
        }

        // Metodi pubblici per Inspector (Unity non mostra enum nei dropdown OnClick)
        public void SetDifficulty_Easy()
        {
            SetDifficulty(BotDifficulty.Easy);
        }
        
        public void SetDifficulty_Medium()
        {
            SetDifficulty(BotDifficulty.Medium);
        }
        
        public void SetDifficulty_Hard()
        {
            SetDifficulty(BotDifficulty.Hard);
        }

        private void UpdateDifficultyUI()
        {
            
            float selectedScale = 1.3f;
            float normalScale = 1f;
            float selectedAlpha = 1f;
            float normalAlpha = 0.4f;

            if (easyIndicator != null)
            {
                bool isSelected = _currentDifficulty == BotDifficulty.Easy;
                easyIndicator.transform.DOScale(isSelected ? selectedScale : normalScale, 0.15f).SetEase(Ease.OutBack);
                easyIndicator.DOColor(new Color(easyColor.r, easyColor.g, easyColor.b, isSelected ? selectedAlpha : normalAlpha), 0.15f);
            }
            if (mediumIndicator != null)
            {
                bool isSelected = _currentDifficulty == BotDifficulty.Medium;
                mediumIndicator.transform.DOScale(isSelected ? selectedScale : normalScale, 0.15f).SetEase(Ease.OutBack);
                mediumIndicator.DOColor(new Color(mediumColor.r, mediumColor.g, mediumColor.b, isSelected ? selectedAlpha : normalAlpha), 0.15f);
            }
            if (hardIndicator != null)
            {
                bool isSelected = _currentDifficulty == BotDifficulty.Hard;
                hardIndicator.transform.DOScale(isSelected ? selectedScale : normalScale, 0.15f).SetEase(Ease.OutBack);
                hardIndicator.DOColor(new Color(hardColor.r, hardColor.g, hardColor.b, isSelected ? selectedAlpha : normalAlpha), 0.15f);
            }
        }

        #endregion

        #region Card Selection API (chiamate dai Button.OnClick in Inspector)

        // ========== PARTITE VELOCI (QuickMatch) ==========

        public void Select_QuickMatch_1v1()
        {
            SelectAndClose(MatchIntent.QuickMatch, GameFormat.OneVsOne);
        }

        public void Select_QuickMatch_2v2()
        {
            SelectAndClose(MatchIntent.QuickMatch, GameFormat.TwoVsTwo);
        }

        public void Select_QuickMatch_4P()
        {
            SelectAndClose(MatchIntent.QuickMatch, GameFormat.FourPlayers);
        }

        // ========== ALLENAMENTO (Training) ==========

        public void Select_Training_1v1()
        {
            SelectAndClose(MatchIntent.Training, GameFormat.OneVsOne);
        }

        public void Select_Training_2v2()
        {
            SelectAndClose(MatchIntent.Training, GameFormat.TwoVsTwo);
        }

        public void Select_Training_4P()
        {
            SelectAndClose(MatchIntent.Training, GameFormat.FourPlayers);
        }

        // ========== COOP / AMICI ==========

        public void Select_Coop_2v2()
        {
            var config = new MatchConfig
            {
                Intent = MatchIntent.QuickMatch,
                Format = GameFormat.TwoVsTwo,
                BotDifficulty = _currentDifficulty,
                TargetScore = 51
            };

            CurrentSelection = config;
            OnSelectionChanged?.Invoke(config);
            UpdateSelectionText();
            OnCoopModeSelected?.Invoke(config);
            Close();
        }

        public void Select_CreatePrivateRoom()
        {
            var config = new MatchConfig
            {
                Intent = MatchIntent.PrivateRoom,
                Format = GameFormat.FourPlayers,
                BotDifficulty = _currentDifficulty,
                TargetScore = 51,
                IsHost = true
            };

            CurrentSelection = config;
            OnSelectionChanged?.Invoke(config);
            OnCreatePrivateRoomSelected?.Invoke(config);
            Close();
        }

        public void Select_JoinPrivateRoom()
        {
            var config = new MatchConfig
            {
                Intent = MatchIntent.PrivateRoom,
                Format = GameFormat.FourPlayers,
                BotDifficulty = _currentDifficulty,
                TargetScore = 51,
                IsHost = false
            };

            CurrentSelection = config;
            OnSelectionChanged?.Invoke(config);
            OnJoinPrivateRoomSelected?.Invoke();
            Close();
        }

        #endregion

        #region Internal

        private void SelectAndClose(MatchIntent intent, GameFormat format)
        {

            var rules = MatchRules.Default.Clone();

            // Default balancing for 1v1: keep accusi, but prevent instant win from Cappotto.
            if (format == GameFormat.OneVsOne)
            {
                rules.CappottoEndsGameImmediately = false;
                rules.CappottoBonusPoints = 0;
            }

            CurrentSelection = new MatchConfig
            {
                Intent = intent,
                Format = format,
                BotDifficulty = _currentDifficulty,
                TargetScore = 51,
                Rules = rules
            };


            OnSelectionChanged?.Invoke(CurrentSelection);
            UpdateSelectionText();

            Close();
        }

        private void UpdateSelectionText()
        {
            if (currentSelectionText == null || CurrentSelection == null)
                return;

            string intentName = CurrentSelection.Intent switch
            {
                MatchIntent.QuickMatch => "Partita Veloce",
                MatchIntent.Training => "Allenamento",
                MatchIntent.PrivateRoom => "Stanza Privata",
                _ => "?"
            };

            string formatName = CurrentSelection.Format switch
            {
                GameFormat.OneVsOne => "1v1",
                GameFormat.TwoVsTwo => "2v2",
                GameFormat.FourPlayers => "4 Giocatori",
                _ => "?"
            };

            currentSelectionText.text = $"{intentName} - {formatName}";
        }

        #endregion

        #region Legacy API (per compatibilità con GameLaunchController esistente)

        public event Action<MatchConfig> OnConfigSelected;
        public event Action<MatchConfig> OnCreatePrivateRoomRequested;
        public event Action<MatchConfig> OnJoinPrivateRoomRequested;

        public void LaunchCurrentSelection()
        {
            if (CurrentSelection == null)
            {
                Debug.LogWarning("[ModalitySelectorPanelUI] No selection to launch!");
                return;
            }

            OnConfigSelected?.Invoke(CurrentSelection);
        }

        public void RequestJoinPrivateRoom()
        {
            var config = new MatchConfig
            {
                Intent = MatchIntent.PrivateRoom,
                Format = GameFormat.FourPlayers,
                IsHost = false
            };

            OnJoinPrivateRoomRequested?.Invoke(config);
        }

        #endregion
    }
}
