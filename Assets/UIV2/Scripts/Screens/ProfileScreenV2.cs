using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        private readonly List<ProfileTrophyView> _spawnedTrophies = new List<ProfileTrophyView>();

        public event Action OnSettingsPressed;
        public event Action OnRegisterPressed;
        public event Action OnSharePressed;
        public event Action<CollectionItemViewData> OnTrophyPressed;

        private void Awake()
        {
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
                string status = data.HasProgress ? $"{levelPrefix} {data.Level}" : (data.IsGuest ? "Ospite" : "Account");
                infoLabel.text = string.IsNullOrEmpty(data.PlayerId) ? status : $"{status} {MiddleDot} {data.PlayerId}";
            }

            SetXp(data.XpCurrent, data.XpMax, data.Level + 1);
            if (xpBar != null) xpBar.gameObject.SetActive(data.HasProgress && data.XpMax > 0);
            if (!data.HasProgress && xpLabel != null) xpLabel.text = "Progressi non disponibili";
            if (xpLabel != null) xpLabel.gameObject.SetActive(!data.IsGuest);
            if (data.IsGuest && xpBar != null) xpBar.gameObject.SetActive(false);
            SetStats(data);
            SetTrophies(data.Trophies);

            if (registerButton != null) registerButton.gameObject.SetActive(data.IsGuest);
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
            if (tile != null) tile.SetStat(null, value, null);
        }
    }
}
