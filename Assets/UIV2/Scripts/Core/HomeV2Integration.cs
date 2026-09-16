using System.Collections.Generic;
using Project51.Auth;
using Project51.Core;
using Project51.UIV2.Components;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;
using Project51.Unity;
using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>Connects the V2 Home to the existing MainMenu services and pages.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class HomeV2Integration : MonoBehaviour
    {
        [SerializeField] private Canvas homeCanvas;
        [SerializeField] private CanvasGroup homeContent;
        [SerializeField] private HomeScreenV2 home;
        [SerializeField] private CollectionScreenV2 collection;
        [SerializeField] private ProfileScreenV2 profile;
        [SerializeField] private SettingsV2Integration settings;
        [SerializeField] private AuthUIController authUI;
        [SerializeField] private UIV2TopBar topBar;
        [SerializeField] private UIV2BottomNav navigation;
        [SerializeField] private GameLaunchController launcher;
        [SerializeField] private ModalitySelectorPanelUI modes;
        [SerializeField] private PanelSwipeController legacyPages;
        [SerializeField] private BottomNavBarUI legacyNavigation;
        [SerializeField] private CanvasGroup[] legacyVisuals;
        [SerializeField] private UIV2Pager pager;
        [SerializeField] private QuickSelectionPanels quickPanels;
        [SerializeField] private StartScreenV2 startScreen;

        private static MatchConfig sessionSelection;
        private AuthBootstrapper auth;
        private PlayerProgressLocal progress;
        private bool showingHome;
        private string profileOwnerId;
        private bool loadingProfile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => sessionSelection = null;

        private void Start()
        {
            if (home == null || launcher == null || modes == null || homeCanvas == null
                || navigation == null || legacyPages == null || legacyNavigation == null)
            {
                Debug.LogError("[HomeV2Integration] MainMenu references are incomplete.", this);
                enabled = false;
                return;
            }
            home.OnPlayPressed += Play;
            home.OnModePressed += OpenModes;
            home.OnDeckPressed += OpenDecks;
            CardDecks.SelectionChanged += RefreshDecks;
            if (profile != null)
            {
                profile.OnSettingsPressed += OpenSettings;
                profile.OnRegisterPressed += OpenRegistration;
                profile.SetActionsAvailable(settings != null, authUI != null, false);
            }
            if (collection != null)
            {
                collection.DecksPanel.OnDeckActionPressed += SelectDeck;
                collection.SetTabInteractable(CollectionTab.Emoticons, false);
                collection.SetTabInteractable(CollectionTab.Accusi, false);
                collection.DecksPanel.SetFooter(string.Empty);
            }
            modes.OnSelectionChanged += SelectionChanged;
            modes.OnVisibilityChanged += ModeVisibilityChanged;
            navigation.OnItemSelected += Navigate;
            pager.CanNavigate = CanNavigate;
            pager.OnPageChanged += PageChanged;
            navigation.SetItems(new List<UIV2NavItemData>
            {
                new UIV2NavItemData { Id = "gioca", Label = "Gioca" },
                new UIV2NavItemData { Id = "cards", Label = "Carte" },
                new UIV2NavItemData { Id = "shop", Label = "Negozio" },
                new UIV2NavItemData { Id = "profile", Label = "Profilo" }
            });
            modes.SetSelection(sessionSelection ?? new MatchConfig
            {
                Intent = MatchIntent.Training,
                Format = GameFormat.FourPlayers,
                BotDifficulty = BotDifficulty.Medium
            });
            home.SetPendingActionsInteractable(false);
            home.SetDeckInteractable(collection != null);
            RefreshDecks();
            home.SetRewardsBadge(0);
            home.SetRankingBadge(0);
            home.SetMailBadge(0);
            auth = AuthBootstrapper.Instance;
            if (authUI != null)
            {
                authUI.OnLoginSuccess += ReloadAccountProfile;
                authUI.OnRegistrationSuccess += ReloadAccountProfile;
            }
            if (auth != null)
            {
                auth.OnAuthReady += RefreshProfile;
                if (auth.Profile != null)
                {
                    if (auth.Profile.IsLoaded) profileOwnerId = auth.PlayFabAuth?.PlayFabId;
                    auth.Profile.OnProfileLoaded += CloudProfileLoaded;
                    auth.Profile.OnProfileUpdated += RefreshProfile;
                }
                if (auth.PlayFabAuth != null) auth.PlayFabAuth.OnDisplayNameChanged += DisplayNameChanged;
            }
            progress = PlayerProgressLocal.Instance;
            if (progress != null) progress.OnExpChanged += ExpChanged;
            RefreshProfile();
            foreach (var group in legacyVisuals)
            {
                if (group == null) continue;
                group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
            }
            PageChanged(0);
        }

        private void Play()
        {
            if (!showingHome || !CanNavigate() || pager.IsMoving) return;
            var config = modes.CurrentSelection.Clone();
            config.DeckBackId = CardDecks.SelectedId;
            launcher.Launch(config);
        }

        private void OpenModes(SelectorOptionViewData unused) { if (CanNavigate() && !pager.IsMoving) quickPanels.OpenModes(); }

        private void OpenSettings() { if (settings != null) settings.Open(); }

        private void OpenRegistration()
        {
            if (authUI == null) return;
            authUI.ShowAuthUI();
            authUI.ShowRegisterPanel();
        }

        private void OpenDecks(SelectorOptionViewData unused)
        {
            if (CanNavigate() && !pager.IsMoving) quickPanels.OpenDecks();
        }

        private void SelectDeck(DeckViewData deck)
        {
            if (deck != null && deck.Unlocked) CardDecks.Select(deck.Id);
        }

        private void RefreshDecks()
        {
            var catalog = CardDecks.Catalog;
            if (catalog == null) return;
            string selected = CardDecks.SelectedId;
            var entry = catalog.Find(selected);
            if (entry != null) home.SetDeck(new SelectorOptionViewData { Id = entry.Id, DisplayName = entry.DisplayName });
            if (collection == null) return;
            var decks = new List<DeckViewData>();
            foreach (var item in catalog.Entries)
                decks.Add(new DeckViewData { Id = item.Id, Name = item.DisplayName, Subtitle = item.Subtitle,
                    Artwork = item.Artwork, Unlocked = true, Equipped = item.Id == selected });
            collection.DecksPanel.Bind(decks, decks.Count);
        }

        private void SelectionChanged(MatchConfig config)
        {
            sessionSelection = config.Clone();
            string intent = config.Intent == MatchIntent.Training ? "Allenamento"
                : config.Intent == MatchIntent.QuickMatch ? "Partita veloce" : "Stanza privata";
            string format = config.Format == GameFormat.OneVsOne ? "1v1"
                : config.Format == GameFormat.TwoVsTwo ? "2v2" : "4 giocatori";
            home.SetMode(new SelectorOptionViewData { Id = config.Intent.ToString(), DisplayName = intent + " · " + format });
        }

        private void Navigate(int index)
        {
            if (!CanNavigate()) { navigation.SelectIndex(pager.CurrentIndex, false); return; }
            pager.Select(index);
        }

        private bool CanNavigate()
        {
            if (modes.IsOpen || quickPanels.IsOpen || (settings != null && settings.IsOpen)) return false;
            if (AppLoadingView.Instance != null && AppLoadingView.Instance.IsVisible) return false;
            if (startScreen != null && startScreen.View.blocksRaycasts) return false;
            var gate = homeCanvas.GetComponent<CanvasGroup>();
            return gate == null || gate.interactable;
        }

        private void PageChanged(int index)
        {
            showingHome = index == 0;
            navigation.SelectIndex(index, notify: false);
            RefreshProfile();
        }

        private void ModeVisibilityChanged(bool visible)
        {
            // Separate from the root CanvasGroup, which belongs to the authentication gate.
            if (homeContent == null) return;
            homeContent.interactable = !visible;
            homeContent.blocksRaycasts = !visible;
        }

        private void DisplayNameChanged(string unused) => RefreshProfile();
        private void ExpChanged(int total, int gained) => RefreshProfile();

        private void CloudProfileLoaded()
        {
            profileOwnerId = auth?.PlayFabAuth?.PlayFabId;
            RefreshProfile();
        }

        private void ReloadAccountProfile()
        {
            if (auth?.Profile == null) return;
            profileOwnerId = null;
            loadingProfile = true;
            RefreshProfile();
            auth.Profile.LoadProfile(() =>
            {
                if (this == null) return;
                loadingProfile = false;
                RefreshProfile();
            });
        }

        private void RefreshProfile()
        {
            if (topBar == null) return;
            string displayName = auth?.PlayFabAuth?.GetBestDisplayName();
            int maxXp = progress != null ? progress.ExpToNextLevel : 0;
            int level = progress != null ? progress.Level : 1;
            int xp = progress != null ? Mathf.RoundToInt(progress.LevelProgress * maxXp) : 0;
            var cloud = auth?.Profile;
            bool isGuest = auth?.PlayFabAuth == null || !auth.PlayFabAuth.HasRealLogin;
            bool cloudLoaded = !loadingProfile && cloud != null && cloud.IsLoaded
                && profileOwnerId == auth?.PlayFabAuth?.PlayFabId;
            if (cloudLoaded)
            {
                level = Mathf.Max(1, cloud.Level);
                maxXp = 100 * level;
                xp = Mathf.Clamp(cloud.XP - 50 * level * (level - 1), 0, maxXp);
            }
            if (isGuest) { xp = 0; maxXp = 0; }
            if (profile != null)
            {
                profile.Bind(new ProfileViewData
                {
                    PlayerName = string.IsNullOrEmpty(displayName) ? "Ospite" : displayName,
                    PlayerId = auth?.PlayFabAuth?.PlayFabId,
                    IsGuest = isGuest,
                    Level = level, XpCurrent = xp, XpMax = maxXp,
                    HasProgress = !isGuest && (cloudLoaded || progress != null),
                    HasMatchStats = !isGuest && cloudLoaded,
                    HasAdvancedStats = false,
                    MatchesPlayed = cloudLoaded ? cloud.TotalGames : 0,
                    Wins = cloudLoaded ? cloud.Wins : 0
                });
            }
            topBar.SetProfile(new PlayerSummaryViewData
            {
                DisplayName = string.IsNullOrEmpty(displayName) ? "Ospite" : displayName,
                Level = level,
                XpCurrent = xp,
                XpMax = maxXp,
                EnergyCurrent = 0,
                EnergyMax = 0
            });
            // No authoritative currency/energy service exists yet; do not display preview balances.
            topBar.SetResources(null);
        }

        private void OnDestroy()
        {
            if (home != null) { home.OnPlayPressed -= Play; home.OnModePressed -= OpenModes; home.OnDeckPressed -= OpenDecks; }
            CardDecks.SelectionChanged -= RefreshDecks;
            if (profile != null)
            {
                profile.OnSettingsPressed -= OpenSettings;
                profile.OnRegisterPressed -= OpenRegistration;
            }
            if (collection != null) collection.DecksPanel.OnDeckActionPressed -= SelectDeck;
            if (modes != null)
            {
                modes.OnSelectionChanged -= SelectionChanged;
                modes.OnVisibilityChanged -= ModeVisibilityChanged;
            }
            if (navigation != null) navigation.OnItemSelected -= Navigate;
            if (pager != null) { pager.OnPageChanged -= PageChanged; pager.CanNavigate = null; }
            if (auth != null)
            {
                auth.OnAuthReady -= RefreshProfile;
                if (auth.Profile != null)
                {
                    auth.Profile.OnProfileLoaded -= CloudProfileLoaded;
                    auth.Profile.OnProfileUpdated -= RefreshProfile;
                }
                if (auth.PlayFabAuth != null) auth.PlayFabAuth.OnDisplayNameChanged -= DisplayNameChanged;
            }
            if (progress != null) progress.OnExpChanged -= ExpChanged;
            if (authUI != null)
            {
                authUI.OnLoginSuccess -= ReloadAccountProfile;
                authUI.OnRegistrationSuccess -= ReloadAccountProfile;
            }
        }
    }
}
