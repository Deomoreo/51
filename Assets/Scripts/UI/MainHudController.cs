using System;
using UnityEngine;
using DG.Tweening;

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

        [Header("Data Providers")]
        [SerializeField] private PlayerDataProvider playerDataProvider;

        private void Start()
        {
            if (playerDataProvider == null)
                return;

            if (topBarUI != null)
                topBarUI.Initialize(playerDataProvider);

            bottomNavBar.SelectTab(BottomNavBarUI.MainTab.Home, fireEvent: false);
            swipeController.SwitchPage(0);
            if (topBarUI != null) topBarUI.SetTitle("Home");

            if (bottomNavController != null)
                bottomNavController.SelectTabUIOnly(0, animate: false);
        }

        private void OnEnable()
        {
            if (bottomNavBar != null)
                bottomNavBar.OnTabSelected += HandleTabSelected;

            if (swipeController != null)
            {
                swipeController.OnPageChanged += HandlePageChanged;
                swipeController.OnPageProgress += HandlePageProgress;
            }
        }

        private void OnDisable()
        {
            if (bottomNavBar != null)
                bottomNavBar.OnTabSelected -= HandleTabSelected;

            if (swipeController != null)
            {
                swipeController.OnPageChanged -= HandlePageChanged;
                swipeController.OnPageProgress -= HandlePageProgress;
            }
        }

        private void HandleTabSelected(BottomNavBarUI.MainTab tab)
        {
            int targetIndex = TabToIndex(tab);
            SetTopBarTitleForIndex(targetIndex);

            swipeController.SwitchPage(targetIndex);

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

        private int TabToIndex(BottomNavBarUI.MainTab tab)
        {
            switch (tab)
            {
                case BottomNavBarUI.MainTab.Home: return 0;
                case BottomNavBarUI.MainTab.Deck: return 1;
                case BottomNavBarUI.MainTab.Shop: return 2;
                case BottomNavBarUI.MainTab.Profile: return 3;
                default: return 0;
            }
        }

        private BottomNavBarUI.MainTab IndexToTab(int index)
        {
            switch (index)
            {
                case 0: return BottomNavBarUI.MainTab.Home;
                case 1: return BottomNavBarUI.MainTab.Deck;
                case 2: return BottomNavBarUI.MainTab.Shop;
                case 3: return BottomNavBarUI.MainTab.Profile;
                default: return BottomNavBarUI.MainTab.Home;
            }
        }

        private void SetTopBarTitleForIndex(int index)
        {
            if (topBarUI == null) return;

            switch (index)
            {
                case 0: topBarUI.SetTitle("Home"); break;
                case 1: topBarUI.SetTitle("Collezione"); break;
                case 2: topBarUI.SetTitle("Negozio"); break;
                case 3: topBarUI.SetTitle("Profilo"); break;
            }
        }
    }
}