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
    /// top bar: sfondo/video placeholder, quick action a destra, selettori Modalita'/Mazzo,
    /// CTA GIOCA.
    /// </summary>
    public class HomeScreenV2 : MonoBehaviour
    {
        [SerializeField] private RawImage backgroundVideoSlot;
        [SerializeField] private UIV2QuickActionButton rewardsButton;
        [SerializeField] private UIV2QuickActionButton rankingButton;
        [SerializeField] private UIV2QuickActionButton mailButton;
        [SerializeField] private UIV2SelectorChip modeSelector;
        [SerializeField] private UIV2SelectorChip deckSelector;
        [SerializeField] private UIV2Button playButton;

        private SelectorOptionViewData _currentMode;
        private SelectorOptionViewData _currentDeck;

        public event Action OnRewardsPressed;
        public event Action OnRankingPressed;
        public event Action OnMailPressed;
        public event Action OnPlayPressed;
        public event Action<SelectorOptionViewData> OnModePressed;
        public event Action<SelectorOptionViewData> OnDeckPressed;

        private void Awake()
        {
            if (rewardsButton != null) rewardsButton.OnClicked += () => OnRewardsPressed?.Invoke();
            if (rankingButton != null) rankingButton.OnClicked += () => OnRankingPressed?.Invoke();
            if (mailButton != null) mailButton.OnClicked += () => OnMailPressed?.Invoke();
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

        /// <summary>
        /// Player/Resources dentro HomeViewData sono per UIV2_TopBar - il chiamante li
        /// smista li' separatamente, qui vengono ignorati di proposito.
        /// </summary>
        public void Bind(HomeViewData data)
        {
            if (data == null) return;
            SetMode(data.SelectedMode);
            SetDeck(data.SelectedDeck);
            SetRewardsBadge(data.RewardsBadgeCount);
            SetRankingBadge(data.RankingBadgeCount);
            SetMailBadge(data.MailBadgeCount);
        }

        public void SetMode(SelectorOptionViewData mode)
        {
            _currentMode = mode;
            if (modeSelector != null) modeSelector.SetValue(mode != null ? mode.DisplayName : "-", mode?.Icon);
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

        /// <summary>
        /// Hook per il futuro VideoPlayer/RenderTexture sull'area centrale - finche' non
        /// viene chiamato resta il placeholder neutro (vedi builder: VideoPlaceholderMarker).
        /// </summary>
        public void SetBackgroundVideoTexture(Texture texture)
        {
            if (backgroundVideoSlot == null) return;
            backgroundVideoSlot.texture = texture;
            backgroundVideoSlot.enabled = texture != null;
        }
    }
}
