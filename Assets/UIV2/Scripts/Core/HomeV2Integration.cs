using System.Collections.Generic;
using DG.Tweening;
using Project51.Auth;
using Project51.Core;
using Project51.UIV2.Animations;
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
        [SerializeField] private HomeScreenV2 home;
        [SerializeField] private CollectionScreenV2 collection;
        [SerializeField] private ProfileScreenV2 profile;
        [SerializeField] private SettingsV2Integration settings;
        [SerializeField] private AuthUIController authUI;
        [SerializeField] private UIV2TopBar topBar;
        [SerializeField] private UIV2BottomNav navigation;
        [SerializeField] private GameLaunchController launcher;
        [SerializeField] private UIV2Pager pager;
        [SerializeField] private QuickSelectionPanels quickPanels;
        [SerializeField] private StartScreenV2 startScreen;
        [SerializeField] private CanvasGroup homeAmbient;
        [Header("UI51 (opzionali)")]
        [SerializeField] private ProfileEditorV2 profileEditor;
        [SerializeField] private CanvasGroup pagesBackground;
        [SerializeField] private CanvasGroup topBarGroup;
        [SerializeField] private CanvasGroup[] pageHeaders;

        private AuthBootstrapper auth;
        private PlayerProgressLocal progress;
        private bool showingHome;
        private string profileOwnerId;
        private bool loadingProfile;
        private Tween ambientFade;

        private void Start()
        {
            if (home == null || launcher == null || homeCanvas == null || navigation == null || quickPanels == null)
            {
                Debug.LogError("[HomeV2Integration] MainMenu references are incomplete.", this);
                enabled = false;
                return;
            }
            home.OnPlayPressed += Play;
            home.OnModePressed += OpenModes;
            home.OnDeckPressed += OpenDecks;
            home.OnSettingsPressed += OpenSettings;
            CardDecks.SelectionChanged += RefreshDecks;
            if (topBar != null) { topBar.OnRegisterPressed += OpenRegistration; topBar.OnProfilePressed += OpenProfile; }
            if (profile != null)
            {
                profile.OnSettingsPressed += OpenSettings;
                profile.OnRegisterPressed += OpenRegistration;
                profile.OnLoginPressed += OpenLogin;
                profile.OnEditPressed += OpenProfileEditor;
                profile.SetActionsAvailable(settings != null, authUI != null, false);
            }
            if (profileEditor != null) profileEditor.OnSave += SaveCosmetics;
            if (collection != null)
            {
                collection.DecksPanel.OnDeckActionPressed += SelectDeck;
                collection.DecksPanel.SetFooter(string.Empty);
            }
            quickPanels.SelectionChanged += SelectionChanged;
            navigation.OnItemSelected += Navigate;
            pager.CanNavigate = CanNavigate;
            pager.OnPageChanged += PageChanged;
            navigation.SetItems(new List<UIV2NavItemData>
            {
                new UIV2NavItemData { Id = "gioca", Label = "Gioca" },
                new UIV2NavItemData { Id = "cards", Label = "Collezione" },
                new UIV2NavItemData { Id = "shop", Label = "Negozio" },
                new UIV2NavItemData { Id = "profile", Label = "Profilo" }
            });
            home.SetMode(quickPanels.ModeOption(quickPanels.Selection));
            home.SetPendingActionsInteractable(false);
            home.SetDeckInteractable(collection != null);
            RefreshDecks();
            home.SetRankingBadge(0); // Premi e Posta: il pallino lo mettono UI51RewardsView e UI51MailView
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
            PageChanged(0);
        }

        private void Play()
        {
            if (!showingHome || !CanNavigate() || pager.IsMoving) return;
            var config = quickPanels.Selection.Clone();
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

        // Avatar della testata: stessa pagina della voce "Profilo" della barra in basso (ultima voce).
        private void OpenProfile()
        {
            if (profile != null && !pager.IsMoving) Navigate(3);
        }

        private void OpenLogin()
        {
            if (authUI == null) return;
            authUI.ShowAuthUI();
            authUI.ShowLoginPanel();
        }

        private void OpenProfileEditor()
        {
            // CloudReady: durante il ricaricamento dopo un accesso la cache e' ancora del profilo precedente.
            if (profileEditor == null || !CloudReady() || !HasRealLogin()) return;
            if (!CanNavigate() || pager.IsMoving) return;
            var cloud = auth.Profile;
            profileEditor.Open(cloud.AvatarId, cloud.FrameId, cloud.BannerId, PlayerXp.LevelOf(cloud.XP),
                auth.PlayFabAuth.GetBestDisplayName());
        }

        private void SaveCosmetics(string avatarId, string frameId, string bannerId)
        {
            var cloud = auth?.Profile;
            if (cloud == null || !HasRealLogin() || !CloudReady()) { profileEditor.SaveFinished(false); return; }
            if (avatarId == cloud.AvatarId && frameId == cloud.FrameId && bannerId == cloud.BannerId)
            {
                profileEditor.SaveFinished(true); // niente da scrivere: il cloud ha gia' questo aspetto
                return;
            }
            // Una scrittura sola: la carta si aggiorna una volta, con OnProfileUpdated.
            cloud.SetCosmetics(avatarId, frameId, bannerId,
                () => { if (this != null && profileEditor != null) profileEditor.SaveFinished(true); },
                error => { if (this != null && profileEditor != null) profileEditor.SaveFinished(false); });
        }

        private bool HasRealLogin() => auth?.PlayFabAuth != null && auth.PlayFabAuth.HasRealLogin;

        private bool CloudReady() => !loadingProfile && auth?.Profile != null && auth.Profile.IsLoaded
            && profileOwnerId == auth.PlayFabAuth?.PlayFabId;

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
            if (entry != null) home.SetDeck(new SelectorOptionViewData { Id = entry.Id, DisplayName = entry.DisplayName, Icon = entry.Artwork });
            if (collection == null) return;
            var decks = new List<DeckViewData>();
            foreach (var item in catalog.Entries)
                decks.Add(new DeckViewData { Id = item.Id, Name = item.DisplayName, Subtitle = item.Subtitle,
                    Artwork = item.Artwork, Unlocked = true, Equipped = item.Id == selected });
            collection.DecksPanel.Bind(decks, decks.Count);
            collection.SetTabCount(CollectionTab.Decks, decks.Count.ToString());
        }

        private void SelectionChanged(MatchConfig config) => home.SetMode(quickPanels.ModeOption(config));

        private void Navigate(int index)
        {
            if (!CanNavigate()) { navigation.SelectIndex(pager.CurrentIndex, false); return; }
            pager.Select(index);
        }

        private bool CanNavigate()
        {
            if(quickPanels.RoomFlow!=null&&quickPanels.RoomFlow.IsOpen)return false;
            if (quickPanels.IsOpen || (settings != null && settings.IsOpen)) return false;
            if (profileEditor != null && profileEditor.IsOpen) return false;
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
            ShowAmbient(showingHome);
            ShowPageChrome(index);
        }

        // Pagine UI51: sfondo sfocato al posto di quello della Home e intestazione propria al posto della
        // barra del giocatore (voce vuota in pageHeaders = su quella pagina resta la barra).
        private void ShowPageChrome(int index)
        {
            if (pagesBackground != null) Fade(pagesBackground, !showingHome);
            if (pageHeaders == null || pageHeaders.Length == 0) return;
            bool ownHeader = index < pageHeaders.Length && pageHeaders[index] != null;
            for (int i = 0; i < pageHeaders.Length; i++)
                if (pageHeaders[i] != null) Fade(pageHeaders[i], i == index);
            if (topBarGroup != null) Fade(topBarGroup, !ownHeader);
        }

        private static void Fade(CanvasGroup group, bool on)
        {
            group.DOKill();
            group.blocksRaycasts = on;
            group.DOFade(on ? 1f : 0f, UIV2Motion.Page).SetUpdate(true);
        }

        // Sfondo animato solo sulla pagina Gioca: spento (non solo trasparente) altrove, cosi' i tween si fermano.
        private void ShowAmbient(bool on)
        {
            if (homeAmbient == null) return;
            UIV2Motion.Cancel(ref ambientFade);
            if (on) homeAmbient.gameObject.SetActive(true);
            ambientFade = homeAmbient.DOFade(on ? 1f : 0f, UIV2Motion.Page).SetUpdate(true)
                .OnComplete(() => { ambientFade = null; homeAmbient.gameObject.SetActive(on); });
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
                auth.PublishLook(); // anche se il caricamento fallisce: niente aspetto dell'account di prima al tavolo
                RefreshProfile();
            });
        }

        /// <summary>Avatar scelto nel profilo (null per l'ospite o profilo non caricato): lo usano i posti della ricerca partita.</summary>
        public static Sprite LocalAvatar { get; private set; }

        private void RefreshProfile()
        {
            if (topBar == null) return;
            string displayName = auth?.PlayFabAuth?.GetBestDisplayName();
            var cloud = auth?.Profile;
            bool isGuest = !HasRealLogin();
            bool cloudLoaded = CloudReady();
            // Gli ospiti non guadagnano XP (spinta a registrarsi): livello 1 fisso.
            int totalXp = isGuest ? 0 : cloudLoaded ? cloud.XP : progress != null ? progress.Exp : 0;
            string playFabId = auth?.PlayFabAuth?.PlayFabId;
            int level = PlayerXp.LevelOf(totalXp);
            int xp = PlayerXp.XpInLevel(totalXp);
            int maxXp = PlayerXp.XpToNext(level);
            bool cosmetics = !isGuest && cloudLoaded && profileEditor != null;
            Sprite avatar = cosmetics ? profileEditor.AvatarFor(cloud.AvatarId) : null;
            LocalAvatar = avatar;
            if (profile != null)
            {
                profile.Bind(new ProfileViewData
                {
                    PlayerName = string.IsNullOrEmpty(displayName) ? "Ospite" : displayName,
                    // ID breve solo per gli account; quello completo resta in "Il tuo account".
                    PlayerId = isGuest || string.IsNullOrEmpty(playFabId) ? null
                        : "#" + playFabId.Substring(0, Mathf.Min(8, playFabId.Length)).ToUpperInvariant(),
                    IsGuest = isGuest,
                    Avatar = avatar,
                    FrameId = cosmetics ? cloud.FrameId : null,
                    BannerId = cosmetics ? cloud.BannerId : null,
                    Level = level, XpCurrent = xp, XpMax = maxXp,
                    HasProgress = cloudLoaded || progress != null,
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
                Avatar = avatar,
                XpCurrent = xp,
                XpMax = isGuest ? 0 : maxXp, // 0 = esagono livello e barra XP nascosti per gli ospiti
                EnergyCurrent = 0,
                EnergyMax = 0
            });
            // No authoritative currency/energy service exists yet; do not display preview balances.
            topBar.SetResources(null);
            topBar.SetGuest(isGuest);
            home.SetGuest(isGuest);
        }

        private void OnDestroy()
        {
            if (home != null) { home.OnPlayPressed -= Play; home.OnModePressed -= OpenModes; home.OnDeckPressed -= OpenDecks; home.OnSettingsPressed -= OpenSettings; }
            CardDecks.SelectionChanged -= RefreshDecks;
            if (topBar != null) { topBar.OnRegisterPressed -= OpenRegistration; topBar.OnProfilePressed -= OpenProfile; }
            if (profile != null)
            {
                profile.OnSettingsPressed -= OpenSettings;
                profile.OnRegisterPressed -= OpenRegistration;
                profile.OnLoginPressed -= OpenLogin;
                profile.OnEditPressed -= OpenProfileEditor;
            }
            if (profileEditor != null) profileEditor.OnSave -= SaveCosmetics;
            if (pagesBackground != null) pagesBackground.DOKill();
            if (topBarGroup != null) topBarGroup.DOKill();
            if (pageHeaders != null)
                foreach (var header in pageHeaders)
                    if (header != null) header.DOKill();
            if (collection != null) collection.DecksPanel.OnDeckActionPressed -= SelectDeck;
            if (quickPanels != null) quickPanels.SelectionChanged -= SelectionChanged;
            if (navigation != null) navigation.OnItemSelected -= Navigate;
            if (pager != null) { pager.OnPageChanged -= PageChanged; pager.CanNavigate = null; }
            UIV2Motion.Cancel(ref ambientFade);
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
