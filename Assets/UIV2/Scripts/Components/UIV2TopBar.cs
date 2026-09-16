using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Top bar data-driven: genera una UIV2ResourcePill per ogni valuta passata invece di
    /// avere slot fissi hardcoded (oro/gemme/energia...), cosi' aggiungere una valuta non
    /// richiede toccare il prefab. Energia/XP restano invece slot FISSI (come l'avatar) -
    /// non variano in numero come le valute, quindi non passano da SetResources.
    /// Estesa in fase di visual calibration 2026-09-13 (nameplate sotto l'avatar, energy/XP
    /// bar) senza cambiare la sua API concettuale: SetProfile/SetResources restano gli
    /// stessi due metodi, semplicemente SetProfile ora legge anche i campi
    /// Energy*/Xp* di PlayerSummaryViewData.
    /// </summary>
    public class UIV2TopBar : MonoBehaviour
    {
        [SerializeField] private UIV2AvatarBadge profileAvatar;
        // Ritratto reale dentro il cerchio di avatar_frame (mascherato): spento finche'
        // PlayerSummaryViewData.Avatar e' null, cosi' resta la silhouette cotta nel frame come
        // nel mockup home_B2. profileAvatar resta opzionale (null nel layout Home attuale).
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private RectTransform resourcesContainer;
        [SerializeField] private UIV2ResourcePill resourcePillPrefab;
        [SerializeField] private UIV2ProgressBar energyBar;
        [SerializeField] private UIV2ProgressBar xpBar;

        private readonly List<UIV2ResourcePill> _spawned = new List<UIV2ResourcePill>();

        public void SetProfile(PlayerSummaryViewData player)
        {
            if (player == null) return;
            if (profileAvatar != null) profileAvatar.SetAvatar(player.Avatar, player.Level);
            if (portraitImage != null)
            {
                portraitImage.sprite = player.Avatar;
                portraitImage.enabled = player.Avatar != null;
            }
            if (nameLabel != null) nameLabel.text = player.DisplayName;
            if (energyBar != null) energyBar.gameObject.SetActive(player.EnergyMax > 0);
            if (xpBar != null) xpBar.gameObject.SetActive(player.XpMax > 0);
            SetEnergy(player.EnergyCurrent, player.EnergyMax);
            SetXp(player.XpCurrent, player.XpMax);
        }

        public void SetResources(IReadOnlyList<ResourceViewData> resources)
        {
            if (resourcesContainer == null || resourcePillPrefab == null) return;
            resourcesContainer.gameObject.SetActive(resources != null && resources.Count > 0);

            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();

            if (resources == null) return;

            for (int i = 0; i < resources.Count; i++)
            {
                var pill = Instantiate(resourcePillPrefab, resourcesContainer);
                pill.SetResource(resources[i].Icon, resources[i].Amount);
                _spawned.Add(pill);
            }
        }

        public void SetEnergy(int current, int max)
        {
            if (energyBar != null) energyBar.SetProgress(current, max, animate: false);
        }

        public void SetXp(int current, int max)
        {
            if (xpBar != null) xpBar.SetProgress(current, max, animate: false);
        }
    }
}
