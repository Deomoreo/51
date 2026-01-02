using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Project51.Unity
{
    public class MainHudController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private PanelSwipeController swipeController;

        [Header("Navigation UI")]
        [SerializeField] private BottomNavBarUI bottomNavBar;
        [SerializeField] private TopBarUI topBarUI;

        [Header("Optional UI Animator (expanding tabs)")]
        [SerializeField] private BottomNavController bottomNavController;

        [Header("Slide-Up Panels")]
        [Tooltip("Pannello Deck Selector (slide-up).")]
        [SerializeField] private SlideUpPanelUI deckSelectorPanel;
        [Tooltip("Pannello Modality Selector (slide-up). Può essere lo stesso del deckSelector o uno diverso.")]
        [SerializeField] private SlideUpPanelUI modalitySelectorPanel;

        [Header("Home - Buttons")]
        [Tooltip("Bottoni che aprono il Deck Selector.")]
        [SerializeField] private Button[] deckSelectorButtons;
        [Tooltip("Bottoni che aprono il Modality Selector.")]
        [SerializeField] private Button[] modalitySelectorButtons;

        [Header("Data Providers")]
        [SerializeField] private PlayerDataProvider playerDataProvider;

        private void Awake()
        {
            // Navigation wiring
            if (bottomNavBar != null)
                bottomNavBar.OnTabSelected += HandleTabSelected;

            if (swipeController != null)
            {
                swipeController.OnPageChanged += HandlePageChanged;
                swipeController.OnPageProgress += HandlePageProgress;
            }

            // Deck Selector buttons
            RegisterButtonsToPanel(deckSelectorButtons, deckSelectorPanel);

            // Modality Selector buttons (può essere lo stesso pannello o uno diverso)
            RegisterButtonsToPanel(modalitySelectorButtons, modalitySelectorPanel);

            // Subscribe to panel visibility
            if (deckSelectorPanel != null)
                deckSelectorPanel.OnVisibilityChanged += HandlePanelVisibilityChanged;

            if (modalitySelectorPanel != null && modalitySelectorPanel != deckSelectorPanel)
                modalitySelectorPanel.OnVisibilityChanged += HandlePanelVisibilityChanged;
        }

        private void RegisterButtonsToPanel(Button[] buttons, SlideUpPanelUI panel)
        {
            if (buttons == null || panel == null) return;

            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OpenPanel(panel));
            }
        }

        private void OpenPanel(SlideUpPanelUI panel)
        {
            if (panel == null) return;

            // Se il pannello supporta bottom bar, passagliela
            if (panel is DeckSelectorPanelUI deckPanel && bottomNavBar != null)
                deckPanel.SetBottomBarRoot(bottomNavBar.gameObject);

            panel.Open();
        }

        private void Start()
        {
            if (playerDataProvider != null && topBarUI != null)
                topBarUI.Initialize(playerDataProvider);

            if (bottomNavBar != null)
                bottomNavBar.SelectTab(BottomNavBarUI.MainTab.Home, fireEvent: false);

            if (swipeController != null)
                swipeController.SwitchPage(0);

            if (topBarUI != null)
                topBarUI.SetTitle("Home");

            if (bottomNavController != null)
                bottomNavController.SelectTabUIOnly(0, animate: false);

            // Re-subscribe per sicurezza
            if (bottomNavBar != null)
            {
                bottomNavBar.OnTabSelected -= HandleTabSelected;
                bottomNavBar.OnTabSelected += HandleTabSelected;
            }
        }

        private void OnDestroy()
        {
            if (bottomNavBar != null)
                bottomNavBar.OnTabSelected -= HandleTabSelected;

            if (swipeController != null)
            {
                swipeController.OnPageChanged -= HandlePageChanged;
                swipeController.OnPageProgress -= HandlePageProgress;
            }

            if (deckSelectorPanel != null)
                deckSelectorPanel.OnVisibilityChanged -= HandlePanelVisibilityChanged;

            if (modalitySelectorPanel != null && modalitySelectorPanel != deckSelectorPanel)
                modalitySelectorPanel.OnVisibilityChanged -= HandlePanelVisibilityChanged;
        }

        private void OnEnable()
        {
            if (bottomNavBar != null)
            {
                bottomNavBar.OnTabSelected -= HandleTabSelected;
                bottomNavBar.OnTabSelected += HandleTabSelected;
            }

            if (swipeController != null)
            {
                swipeController.OnPageChanged -= HandlePageChanged;
                swipeController.OnPageChanged += HandlePageChanged;
                swipeController.OnPageProgress -= HandlePageProgress;
                swipeController.OnPageProgress += HandlePageProgress;
            }
        }

        private void OnDisable() { }

        private void HandleTabSelected(BottomNavBarUI.MainTab tab)
        {
            int targetIndex = TabToIndex(tab);
            SetTopBarTitleForIndex(targetIndex);

            if (swipeController != null)
            {
                if (swipeController.IsDragging)
                    swipeController.EndDragAndSnapTo(swipeController.CurrentIndex);

                swipeController.SwitchPage(targetIndex);
                HandlePageChanged(targetIndex);
            }

            if (bottomNavController != null)
                bottomNavController.SelectTabUIOnly(targetIndex, animate: true);
        }

        private void HandlePageChanged(int newIndex)
        {
            var tab = IndexToTab(newIndex);
            bottomNavBar.SelectTab(tab, fireEvent: false);
            SetTopBarTitleForIndex(newIndex);

            if (bottomNavController != null)
                bottomNavController.SelectTabUIOnly(newIndex, animate: true);
        }

        private void HandlePageProgress(float pageFloat)
        {
            if (bottomNavBar == null) return;
            bottomNavBar.SetSelectedByProgress(pageFloat);
        }

        private void HandlePanelVisibilityChanged(bool isOpen)
        {
            // Blocca swipe e bottom bar quando un pannello è aperto
            if (swipeController != null)
                swipeController.enabled = !isOpen;

            if (bottomNavBar != null)
                bottomNavBar.enabled = !isOpen;

            if (bottomNavController != null)
                bottomNavController.enabled = !isOpen;
        }

        private int TabToIndex(BottomNavBarUI.MainTab tab) => tab switch
        {
            BottomNavBarUI.MainTab.Home => 0,
            BottomNavBarUI.MainTab.Deck => 1,
            BottomNavBarUI.MainTab.Shop => 2,
            BottomNavBarUI.MainTab.Profile => 3,
            _ => 0
        };

        private BottomNavBarUI.MainTab IndexToTab(int index) => index switch
        {
            0 => BottomNavBarUI.MainTab.Home,
            1 => BottomNavBarUI.MainTab.Deck,
            2 => BottomNavBarUI.MainTab.Shop,
            3 => BottomNavBarUI.MainTab.Profile,
            _ => BottomNavBarUI.MainTab.Home
        };

        private void SetTopBarTitleForIndex(int index)
        {
            if (topBarUI == null) return;

            topBarUI.SetTitle(index switch
            {
                0 => "Home",
                1 => "Collezione",
                2 => "Negozio",
                3 => "Profilo",
                _ => "Home"
            });
        }
    }
}