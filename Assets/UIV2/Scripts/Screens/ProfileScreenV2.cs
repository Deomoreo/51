using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UI51;
using Project51.UIV2.Components;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// PROFILO (voce "Profilo" della bottom nav, UI51): avatar, nome, livello/id, XP, 3 statistiche,
    /// REGISTRATI per l'ospite. Solo eventi: nessuna registrazione o modale impostazioni implementata qui.
    /// </summary>
    public class ProfileScreenV2 : MonoBehaviour
    {
        [Header("Profilo")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private UIV2ProgressBar xpBar;
        [SerializeField] private TMP_Text xpLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private string levelPrefix = "Livello";
        [SerializeField] private string xpSuffixFormat = "XP al livello {0}";

        [Header("Statistiche")]
        [SerializeField] private UIV2StatTile matchesTile;
        [SerializeField] private UIV2StatTile winsTile;
        [SerializeField] private UIV2StatTile winRateTile;

        [Header("Azioni")]
        [SerializeField] private UIV2Button registerButton;

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
        [SerializeField] private Button retryButton; // B19: caricamento dell'account fallito

        public event Action OnSettingsPressed;
        public event Action OnRegisterPressed;
        public event Action OnEditPressed;
        public event Action OnLoginPressed;
        public event Action OnRetryPressed;

        private void Awake()
        {
            if (editButton != null) editButton.onClick.AddListener(() => OnEditPressed?.Invoke());
            if (loginButton != null) loginButton.onClick.AddListener(() => OnLoginPressed?.Invoke());
            if (retryButton != null) retryButton.onClick.AddListener(() => OnRetryPressed?.Invoke());
            if (settingsButton != null) settingsButton.onClick.AddListener(() => OnSettingsPressed?.Invoke());
            if (registerButton != null && registerButton.Button != null) registerButton.Button.onClick.AddListener(() => OnRegisterPressed?.Invoke());
        }

        public void Bind(ProfileViewData data)
        {
            if (data == null) return;

            if (nameLabel != null) nameLabel.text = data.PlayerName;

            SetXp(data.XpCurrent, data.XpMax, data.Level + 1);
            if (xpBar != null) xpBar.gameObject.SetActive(!data.IsGuest && data.HasProgress && data.XpMax > 0);
            if (!data.HasProgress && xpLabel != null) xpLabel.text = "Progressi non disponibili";
            // Gli ospiti non guadagnano XP: al posto della barra, l'invito a registrarsi.
            if (data.IsGuest && xpLabel != null) xpLabel.text = "Registrati per guadagnare XP";
            if (xpLabel != null) xpLabel.gameObject.SetActive(true);
            SetStats(data);

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
            string level = data.HasProgress ? data.Level.ToString() : "—"; // B19: progressi dell'account non caricati
            if (levelLabel != null) levelLabel.text = level;
            if (titleLabel != null) titleLabel.text = $"{levelPrefix} {level}";
            if (idLabel != null) idLabel.text = string.IsNullOrEmpty(data.PlayerId) ? string.Empty : "ID " + data.PlayerId;
            if (xpHintLabel != null)
                xpHintLabel.text = data.HasProgress && data.XpMax > 0
                    ? $"Ancora {Mathf.Max(0, data.XpMax - data.XpCurrent)} XP per il livello {data.Level + 1}"
                    : string.Empty;
            // B19: caricamento fallito -> RIPROVA al posto del testo a destra, il motivo sotto; in caricamento resta il testo.
            bool retry = retryButton != null && data.CanRetry && !data.IsGuest;
            if (retryButton != null) retryButton.gameObject.SetActive(retry);
            if (retry)
            {
                if (xpLabel != null) xpLabel.text = string.Empty;
                if (xpHintLabel != null) xpHintLabel.text = "Progressi non caricati";
            }
            else if (!data.HasProgress && !data.IsGuest && xpLabel != null) xpLabel.text = "Caricamento…";
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
        }

        public void SetActionsAvailable(bool settings, bool register)
        {
            if (settingsButton != null) settingsButton.interactable = settings;
            if (registerButton != null && registerButton.Button != null) registerButton.Button.interactable = register;
        }

        private static void SetTile(UIV2StatTile tile, string value)
        {
            // Caption e icona restano quelle del prefab (null = non sovrascrivere).
            if (tile != null) tile.SetStat(value, null);
        }
    }
}
