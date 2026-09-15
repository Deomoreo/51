using System.Collections.Generic;
using UnityEngine;
using Project51.UIV2.Components;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;

namespace Project51.UIV2.Tests
{
    /// <summary>
    /// Solo per Assets/UIV2/Tests/UIV2_ProfileV2_Preview.unity: statistiche/trofei demo del mockup
    /// 17_profilo.png SOLO qui, mai nei prefab.
    /// </summary>
    public class ProfileV2PreviewBootstrap : MonoBehaviour
    {
        [SerializeField] private UIV2TopBar topBar;
        [SerializeField] private UIV2BottomNav bottomNav;
        [SerializeField] private ProfileScreenV2 profileScreen;
        [SerializeField] private Sprite[] trophyIcons; // trofeo, coppa blu, spada, gemma, trifoglio
        [SerializeField] private Sprite demoPortrait;
        [Tooltip("false = stato del mockup (ospite, silhouette generica).")]
        [SerializeField] private bool useDemoPortrait;

        private void Start()
        {
            if (topBar != null)
            {
                topBar.SetProfile(new PlayerSummaryViewData
                {
                    DisplayName = "Deomoreo",
                    EnergyCurrent = 50,
                    EnergyMax = 100,
                    XpCurrent = 0,
                    XpMax = 100,
                });
                topBar.SetResources(new List<ResourceViewData>
                {
                    new ResourceViewData { CurrencyId = "gold", Amount = 1000 },
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
                bottomNav.SelectIndex(3);
            }

            if (profileScreen == null) return;

            var trophies = new List<CollectionItemViewData>();
            for (int i = 0; i < 5; i++)
            {
                trophies.Add(new CollectionItemViewData
                {
                    Id = "trophy_" + i,
                    Title = "Trofeo " + (i + 1),
                    Icon = trophyIcons != null && i < trophyIcons.Length ? trophyIcons[i] : null,
                    Unlocked = i < 2,
                });
            }

            profileScreen.Bind(new ProfileViewData
            {
                PlayerName = "Deomoreo",
                PlayerId = "Guest_66B9B973",
                Level = 1,
                Avatar = useDemoPortrait ? demoPortrait : null,
                XpCurrent = 0,
                XpMax = 100,
                MatchesPlayed = 12,
                Wins = 5,
                WinRate = 0.42f,
                TotalScopas = 38,
                SettebelloCount = 7,
                PointRecord = 44,
                Trophies = trophies,
                IsGuest = true,
            });

            profileScreen.OnSettingsPressed += () => Debug.Log("[ProfileV2PreviewBootstrap] OnSettingsPressed");
            profileScreen.OnRegisterPressed += () => Debug.Log("[ProfileV2PreviewBootstrap] OnRegisterPressed");
            profileScreen.OnSharePressed += () => Debug.Log("[ProfileV2PreviewBootstrap] OnSharePressed");
            profileScreen.OnTrophyPressed += trophy => Debug.Log($"[ProfileV2PreviewBootstrap] OnTrophyPressed {trophy.Id}");
        }
    }
}
