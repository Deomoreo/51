using System;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Components;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Prima schermata reale della UI V2. Non possiede un proprio TopBar/BottomNav (restano
    /// globali su UIV2_Root, riusati cosi' come sono) - gestisce solo il contenuto sotto la
    /// top bar: quick action a destra, selettori Modalita'/Mazzo,
    /// CTA GIOCA.
    /// </summary>
    public class HomeScreenV2 : MonoBehaviour
    {
        [SerializeField] private UIV2QuickActionButton rewardsButton;
        [SerializeField] private UIV2QuickActionButton rankingButton;
        [SerializeField] private UIV2QuickActionButton mailButton;
        [Tooltip("UI51 Amici, sopra a Posta (vuoto = nessun pulsante).")]
        [SerializeField] private UIV2QuickActionButton friendsButton;
        [Tooltip("UI51 Notizie, sotto a Posta (vuoto = nessun pulsante).")]
        [SerializeField] private UIV2QuickActionButton newsButton;
        [Tooltip("Opzioni, sotto a Posta. Sempre attivo: non fa parte delle azioni ancora da fare.")]
        [SerializeField] private UIV2QuickActionButton settingsButton;
        [SerializeField] private UIV2SelectorChip modeSelector;
        [SerializeField] private UIV2SelectorChip deckSelector;
        [SerializeField] private UIV2Button playButton;

        private SelectorOptionViewData _currentMode;
        private SelectorOptionViewData _currentDeck;

        public event Action OnRewardsPressed;
        public event Action OnRankingPressed;
        public event Action OnMailPressed;
        public event Action OnNewsPressed;
        public event Action OnFriendsPressed;
        public event Action OnSettingsPressed;
        public event Action OnPlayPressed;
        public event Action<SelectorOptionViewData> OnModePressed;
        public event Action<SelectorOptionViewData> OnDeckPressed;

        private void Awake()
        {
            if (rewardsButton != null) rewardsButton.OnClicked += () => OnRewardsPressed?.Invoke();
            if (rankingButton != null) rankingButton.OnClicked += () => OnRankingPressed?.Invoke();
            if (mailButton != null) mailButton.OnClicked += () => OnMailPressed?.Invoke();
            if (newsButton != null) newsButton.OnClicked += () => OnNewsPressed?.Invoke();
            if (friendsButton != null) friendsButton.OnClicked += () => OnFriendsPressed?.Invoke();
            if (settingsButton != null) settingsButton.OnClicked += () => OnSettingsPressed?.Invoke();
            if (playButton != null && playButton.Button != null)
            {
                playButton.Button.onClick.AddListener(() => OnPlayPressed?.Invoke());
            }
            if (modeSelector != null && modeSelector.Button != null)
            {
                modeSelector.Button.onClick.AddListener(() => OnModePressed?.Invoke(_currentMode));
            }
            if (deckSelector != null && deckSelector.Button != null)
            {
                deckSelector.Button.onClick.AddListener(() => OnDeckPressed?.Invoke(_currentDeck));
            }
        }

        public void SetMode(SelectorOptionViewData mode)
        {
            _currentMode = mode;
            if (modeSelector != null) modeSelector.SetValue(mode != null ? mode.DisplayName : "-", mode?.Icon);
            if (modeSelector != null && mode != null && mode.Caption != null) modeSelector.SetSmallLabel(mode.Caption);
            if (modeSelector != null && mode != null) modeSelector.SetBadge(mode.ShortName);
        }

        public void SetDeck(SelectorOptionViewData deck)
        {
            _currentDeck = deck;
            if (deckSelector != null) deckSelector.SetValue(deck != null ? deck.DisplayName : "-", deck?.Icon);
        }

        public void SetRewardsBadge(int count)
        {
            if (rewardsButton != null) rewardsButton.SetBadgeCount(count);
        }

        public void SetRankingBadge(int count)
        {
            if (rankingButton != null) rankingButton.SetBadgeCount(count);
        }

        public void SetMailBadge(int count)
        {
            if (mailButton != null) mailButton.SetBadgeCount(count);
        }

        public void SetNewsBadge(int count)
        {
            if (newsButton != null) newsButton.SetBadgeCount(count);
        }

        public void SetDeckInteractable(bool interactable)
        {
            if (deckSelector != null && deckSelector.Button != null) deckSelector.Button.interactable = interactable;
        }

        /// <summary>UI51: l'ospite vede solo Opzioni tra i pulsanti laterali.</summary>
        public void SetGuest(bool guest)
        {
            foreach (var action in new[] { rewardsButton, rankingButton, mailButton, newsButton, friendsButton })
                if (action != null) action.gameObject.SetActive(!guest);
        }

        public void SetPendingActionsInteractable(bool interactable)
        {
            if (deckSelector != null && deckSelector.Button != null) deckSelector.Button.interactable = interactable;
            foreach (var action in new[] { rankingButton }) // Posta e Premi: UI51MailView, UI51RewardsView (Fase 8)
            {
                if (action == null) continue;
                var button = action.Button;
                if (button != null) button.interactable = interactable;
            }
        }
    }
}
