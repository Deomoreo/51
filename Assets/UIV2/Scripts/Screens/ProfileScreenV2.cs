using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UI51;
using Project51.UIV2.Components;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// PROFILO V2 (voce "Profilo" della bottom nav, mockup 17_profilo.png): avatar grande + nome +
    /// livello/id + XP, 6 statistiche, trofei, bottoni REGISTRATI / CONDIVIDI. Solo eventi: nessuna
    /// registrazione, condivisione o modale impostazioni implementata qui.
    /// </summary>
    public class ProfileScreenV2 : MonoBehaviour
    {
        private const char MiddleDot = (char)0xB7;

        [Header("Profilo")]
        [SerializeField] private Image defaultAvatarFrame;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text infoLabel;
        [SerializeField] private UIV2ProgressBar xpBar;
        [SerializeField] private TMP_Text xpLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private string levelPrefix = "Livello";
        [SerializeField] private string xpSuffixFormat = "XP al livello {0}";

        [Header("Statistiche")]
        [SerializeField] private UIV2StatTile matchesTile;
        [SerializeField] private UIV2StatTile winsTile;
        [SerializeField] private UIV2StatTile winRateTile;
        [SerializeField] private UIV2StatTile scopasTile;
        [SerializeField] private UIV2StatTile settebelloTile;
        [SerializeField] private UIV2StatTile pointRecordTile;

        [Header("Trofei")]
        [SerializeField] private RectTransform trophyContainer;
        [SerializeField] private ProfileTrophyView trophyPrefab;

        [Header("Azioni")]
        [SerializeField] private UIV2Button registerButton;
        [SerializeField] private UIV2Button shareButton;

        [Header("UI51 (opzionali)")]
        [SerializeField] private GameObject accountGroup;
        [SerializeField] private GameObject guestGroup;
        [SerializeField] private TMP_Text guestNameLabel;
        [SerializeField] private AvatarFrame avatar;
        [SerializeField] private UI51Shape banner;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text idLabel;
        [SerializeField] private TMP_Text xpHintLabel;
        [SerializeField] private Button editButton;
        [SerializeField] private Button loginButton;

        private readonly List<ProfileTrophyView> _spawnedTrophies = new List<ProfileTrophyView>();

        public event Action OnSettingsPressed;
        public event Action OnRegisterPressed;
        public event Action OnSharePressed;
        public event Action<CollectionItemViewData> OnTrophyPressed;
        public event Action OnEditPressed;
        public event Action OnLoginPressed;

        private void Awake()
        {
            if (editButton != null) editButton.onClick.AddListener(() => OnEditPressed?.Invoke());
            if (loginButton != null) loginButton.onClick.AddListener(() => OnLoginPressed?.Invoke());
            if (settingsButton != null) settingsButton.onClick.AddListener(() => OnSettingsPressed?.Invoke());
            if (registerButton != null && registerButton.Button != null) registerButton.Button.onClick.AddListener(() => OnRegisterPressed?.Invoke());
            if (shareButton != null && shareButton.Button != null) shareButton.Button.onClick.AddListener(() => OnSharePressed?.Invoke());
        }

        public void Bind(ProfileViewData data)
        {
            if (data == null) return;

            bool hasPortrait = data.Avatar != null;
            if (portrait != null)
            {
                portrait.gameObject.SetActive(hasPortrait);
                if (hasPortrait) portrait.sprite = data.Avatar;
            }
            // I ritratti avatar_01..08 hanno gia' la propria cornice: niente doppia cornice.
            if (defaultAvatarFrame != null) defaultAvatarFrame.gameObject.SetActive(!hasPortrait);

            if (nameLabel != null) nameLabel.text = data.PlayerName;
            if (infoLabel != null)
            {
                string status = data.IsGuest ? "Ospite" : data.HasProgress ? $"{levelPrefix} {data.Level}" : "Account";
                infoLabel.text = string.IsNullOrEmpty(data.PlayerId) ? status : $"{status} {MiddleDot} {data.PlayerId}";
            }

            SetXp(data.XpCurrent, data.XpMax, data.Level + 1);
            if (xpBar != null) xpBar.gameObject.SetActive(!data.IsGuest && data.HasProgress && data.XpMax > 0);
            if (!data.HasProgress && xpLabel != null) xpLabel.text = "Progressi non disponibili";
            // Gli ospiti non guadagnano XP: al posto della barra, l'invito a registrarsi.
            if (data.IsGuest && xpLabel != null) xpLabel.text = "Registrati per guadagnare XP";
            if (xpLabel != null) xpLabel.gameObject.SetActive(true);
            SetStats(data);
            SetTrophies(data.Trophies);

            if (registerButton != null) registerButton.gameObject.SetActive(data.IsGuest);
            BindUI51(data);
        }

        private void BindUI51(ProfileViewData data)
        {
            if (accountGroup != null) accountGroup.SetActive(!data.IsGuest);
            if (guestGroup != null) guestGroup.SetActive(data.IsGuest);
            if (guestNameLabel != null) guestNameLabel.text = data.PlayerName;
            if (avatar != null)
            {
                avatar.SetAvatar(data.Avatar);
                ProfileCosmetics.ApplyFrame(avatar, ProfileCosmetics.FrameIndex(data.FrameId), 2f, 4f);
            }
            if (banner != null)
            {
                UI51Banners.Apply(banner, ProfileCosmetics.Banner(ProfileCosmetics.BannerIndex(data.BannerId)));
                banner.borderColor = UI51Tokens.BorderGoldSoft; // il bordo resta quello dei pannelli
            }
            if (levelLabel != null) levelLabel.text = data.Level.ToString();
            if (titleLabel != null) titleLabel.text = $"{levelPrefix} {data.Level}";
            if (idLabel != null) idLabel.text = string.IsNullOrEmpty(data.PlayerId) ? string.Empty : "ID " + data.PlayerId;
            if (xpHintLabel != null)
                xpHintLabel.text = data.HasProgress && data.XpMax > 0
                    ? $"Ancora {Mathf.Max(0, data.XpMax - data.XpCurrent)} XP per il livello {data.Level + 1}"
                    : string.Empty;
        }

        public void SetXp(int current, int max, int nextLevel)
        {
            if (xpBar != null) xpBar.SetProgress(max > 0 ? Mathf.Clamp01((float)current / max) : 0f, animate: false);
            if (xpLabel != null) xpLabel.text = $"{current} / {max} " + string.Format(xpSuffixFormat, nextLevel);
        }

        public void SetStats(ProfileViewData data)
        {
            if (data == null) return;
            float winRate = data.WinRate >= 0f ? data.WinRate
                : (data.MatchesPlayed > 0 ? (float)data.Wins / data.MatchesPlayed : 0f);

            SetTile(matchesTile, data.HasMatchStats ? data.MatchesPlayed.ToString() : "—");
            SetTile(winsTile, data.HasMatchStats ? data.Wins.ToString() : "—");
            SetTile(winRateTile, data.HasMatchStats ? Mathf.RoundToInt(winRate * 100f) + "%" : "—");
            SetTile(scopasTile, data.HasAdvancedStats ? data.TotalScopas.ToString() : "—");
            SetTile(settebelloTile, data.HasAdvancedStats ? data.SettebelloCount.ToString() : "—");
            SetTile(pointRecordTile, data.HasAdvancedStats ? data.PointRecord.ToString() : "—");
        }

        public void SetActionsAvailable(bool settings, bool register, bool share)
        {
            if (settingsButton != null) settingsButton.interactable = settings;
            if (registerButton != null && registerButton.Button != null) registerButton.Button.interactable = register;
            if (shareButton != null && shareButton.Button != null) shareButton.Button.interactable = share;
        }

        public void SetTrophies(IReadOnlyList<CollectionItemViewData> trophies)
        {
            for (int i = _spawnedTrophies.Count - 1; i >= 0; i--)
            {
                if (_spawnedTrophies[i] != null) Destroy(_spawnedTrophies[i].gameObject);
            }
            _spawnedTrophies.Clear();

            if (trophies == null || trophyPrefab == null || trophyContainer == null) return;
            foreach (var trophy in trophies)
            {
                if (trophy == null) continue;
                var view = Instantiate(trophyPrefab, trophyContainer);
                view.Bind(trophy);
                view.OnPressed += item => OnTrophyPressed?.Invoke(item);
                _spawnedTrophies.Add(view);
            }
        }

        private static void SetTile(UIV2StatTile tile, string value)
        {
            // Caption e icona restano quelle del prefab (null = non sovrascrivere).
            if (tile != null) tile.SetStat(value, null);
        }
    }
}
