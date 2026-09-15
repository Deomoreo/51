using System.Collections.Generic;
using UnityEngine;
using Project51.UIV2.Components;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;

namespace Project51.UIV2.Tests
{
    /// <summary>
    /// Solo per Assets/UIV2/Tests/UIV2_HomeV2_Preview.unity: popola TopBar/BottomNav/
    /// HomeScreenV2 con HomeViewData mock per verifica visiva - stessi valori demo del
    /// mockup Home (Assets/Mockup/home_B2 (1).png) SOLO qui, mai hardcoded nei prefab (vedi
    /// HomeScreenV2.cs/UIV2FoundationBuilder.BuildHomeScreenV2Prefab).
    /// </summary>
    public class HomeV2PreviewBootstrap : MonoBehaviour
    {
        [SerializeField] private UIV2TopBar topBar;
        [SerializeField] private UIV2BottomNav bottomNav;
        [SerializeField] private HomeScreenV2 homeScreen;

        [Header("Sprite reali (assegnati da UIV2SandboxSceneBuilder, non hardcoded qui)")]
        [SerializeField] private Sprite avatarPlayerDemo;
        [SerializeField] private Sprite iconGold;
        [SerializeField] private Sprite iconGems;
        [SerializeField] private Sprite iconMode;
        [SerializeField] private Sprite iconDeck;
        [Tooltip("false = stato del mockup (silhouette cotta in avatar_frame, nessun ritratto).")]
        [SerializeField] private bool useDemoAvatar;

        private void Start()
        {
            if (topBar != null)
            {
                topBar.SetProfile(new PlayerSummaryViewData
                {
                    DisplayName = "Deomoreo",
                    Level = 7,
                    Avatar = useDemoAvatar ? avatarPlayerDemo : null,
                    EnergyCurrent = 50,
                    EnergyMax = 100,
                    XpCurrent = 0,
                    XpMax = 100,
                });
                topBar.SetResources(new List<ResourceViewData>
                {
                    // Solo monete come nel mockup; Icon null = moneta gia' cotta in bar_coin.
                    new ResourceViewData { CurrencyId = "gold", Amount = 1000, Icon = null },
                });
            }

            if (bottomNav != null)
            {
                bottomNav.SetItems(new List<UIV2NavItemData>
                {
                    new UIV2NavItemData { Id = "gioca", Label = "Gioca" },
                    new UIV2NavItemData { Id = "cards", Label = "Carte" },
                    new UIV2NavItemData { Id = "shop", Label = "Negozio" },
                    new UIV2NavItemData { Id = "profile", Label = "Profilo" },
                });
                bottomNav.SelectIndex(0);
            }

            if (homeScreen != null)
            {
                homeScreen.Bind(new HomeViewData
                {
                    SelectedMode = new SelectorOptionViewData { Id = "allenamento", DisplayName = "Allenamento", Icon = iconMode },
                    SelectedDeck = new SelectorOptionViewData { Id = "napoletano", DisplayName = "Napoletano", Icon = iconDeck },
                    RewardsBadgeCount = 1,
                    RankingBadgeCount = 0,
                    MailBadgeCount = 1,
                });

                homeScreen.OnPlayPressed += () => Debug.Log("[HomeV2PreviewBootstrap] OnPlayPressed");
                homeScreen.OnRewardsPressed += () => Debug.Log("[HomeV2PreviewBootstrap] OnRewardsPressed");
                homeScreen.OnRankingPressed += () => Debug.Log("[HomeV2PreviewBootstrap] OnRankingPressed");
                homeScreen.OnMailPressed += () => Debug.Log("[HomeV2PreviewBootstrap] OnMailPressed");
            }
        }
    }
}
