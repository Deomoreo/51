using System.Collections.Generic;
using UnityEngine;
using Project51.UIV2.Components;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;

namespace Project51.UIV2.Tests
{
    /// <summary>
    /// Solo per Assets/UIV2/Tests/UIV2_Sandbox.unity: dimostra che lo shell + i componenti
    /// data-driven funzionano popolandoli con dati mock. Questo script (non i prefab/
    /// componenti riusabili) e' l'UNICO posto dove questi valori demo esistono - verifica
    /// visiva della foundation, non una vera schermata.
    /// </summary>
    public class UIV2SandboxBootstrap : MonoBehaviour
    {
        [SerializeField] private UIV2TopBar topBar;
        [SerializeField] private UIV2BottomNav bottomNav;
        [SerializeField] private FriendsScreenController friendsScreen;

        [Header("Sprite reali (assegnati da UIV2SandboxSceneBuilder, non hardcoded qui)")]
        [SerializeField] private Sprite avatarPlayerDemo;
        [SerializeField] private Sprite avatarMarco;
        [SerializeField] private Sprite avatarLuca;
        [SerializeField] private Sprite avatarGiulia;
        [SerializeField] private Sprite iconGold;
        [SerializeField] private Sprite iconGems;

        private void Start()
        {
            if (topBar != null)
            {
                topBar.SetProfile(new PlayerSummaryViewData { DisplayName = "Player_Demo", Level = 12, Avatar = avatarPlayerDemo });
                topBar.SetResources(new List<ResourceViewData>
                {
                    new ResourceViewData { CurrencyId = "gold", Amount = 12500, Icon = iconGold },
                    new ResourceViewData { CurrencyId = "gems", Amount = 340, Icon = iconGems },
                });
            }

            if (bottomNav != null)
            {
                bottomNav.SetItems(new List<UIV2NavItemData>
                {
                    new UIV2NavItemData { Id = "gioca", Label = "Gioca" },
                    new UIV2NavItemData { Id = "cards", Label = "Cards" },
                    new UIV2NavItemData { Id = "shop", Label = "Shop" },
                    new UIV2NavItemData { Id = "profile", Label = "Profile" },
                });
            }

            if (friendsScreen != null)
            {
                // Ogni amico ha un avatar reale diverso (avatar_XX), mai lo stesso sprite
                // globale: dimostra che l'avatar arriva dal ViewData, non e' fisso nel prefab.
                friendsScreen.Populate(new List<FriendViewData>
                {
                    new FriendViewData
                    {
                        Player = new PlayerSummaryViewData { DisplayName = "Marco_88", Level = 20, Avatar = avatarMarco },
                        Status = FriendOnlineStatus.InMatch, StatusText = "In partita", CanInvite = false
                    },
                    new FriendViewData
                    {
                        Player = new PlayerSummaryViewData { DisplayName = "Luca_02", Level = 8, Avatar = avatarLuca },
                        Status = FriendOnlineStatus.Online, StatusText = "Disponibile", CanInvite = true
                    },
                    new FriendViewData
                    {
                        Player = new PlayerSummaryViewData { DisplayName = "Giulia_R", Level = 15, Avatar = avatarGiulia },
                        Status = FriendOnlineStatus.Offline, StatusText = "2 ore fa", CanInvite = false
                    },
                });
            }
        }
    }
}
