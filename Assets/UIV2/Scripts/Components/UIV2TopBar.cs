using System;
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
        // Numero livello dentro la stella accanto alla barra XP (il genitore e' la stella).
        [SerializeField] private TMP_Text levelLabel;
        // UI51 (Fase 3): testata account/ospite, "Livello N" e pulsante Registrati. Vuoti = UIV2 classica.
        [SerializeField] private TMP_Text levelCaption;
        [SerializeField] private GameObject accountGroup;
        [SerializeField] private GameObject guestGroup;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button[] avatarButtons; // avatar account e ospite: aprono la pagina Profilo

        public event Action OnRegisterPressed;
        public event Action OnProfilePressed;

        private void Awake()
        {
            if (registerButton != null) registerButton.onClick.AddListener(() => OnRegisterPressed?.Invoke());
            if (avatarButtons != null)
                foreach (var b in avatarButtons) if (b != null) b.onClick.AddListener(() => OnProfilePressed?.Invoke());
        }

        public void SetGuest(bool guest)
        {
            if (accountGroup != null) accountGroup.SetActive(!guest);
            if (guestGroup != null) guestGroup.SetActive(guest);
        }

        private readonly List<UIV2ResourcePill> _spawned = new List<UIV2ResourcePill>();
        private readonly List<string> _currencies = new List<string>();

        public void SetProfile(PlayerSummaryViewData player)
        {
            if (player == null) return;
            if (profileAvatar != null) profileAvatar.SetAvatar(player.Avatar, player.Level);
            if (portraitImage != null)
            {
                portraitImage.sprite = player.Avatar;
                portraitImage.enabled = player.Avatar != null;
                // Sprite avatar non quadrati: ritaglio nel cerchio invece di stirarli.
                var circle = portraitImage.rectTransform.parent as RectTransform;
                if (player.Avatar != null && circle != null) Project51.UI51.AvatarFrame.FitPortrait(portraitImage, circle.rect.width);
            }
            if (nameLabel != null) nameLabel.text = player.DisplayName;
            if (levelCaption != null) levelCaption.text = "Livello " + player.Level;
            if (energyBar != null) energyBar.gameObject.SetActive(player.EnergyMax > 0);
            if (xpBar != null) xpBar.gameObject.SetActive(player.XpMax > 0);
            if (levelLabel != null)
            {
                levelLabel.text = player.Level.ToString();
                levelLabel.transform.parent.gameObject.SetActive(player.XpMax > 0);
            }
            SetEnergy(player.EnergyCurrent, player.EnergyMax);
            SetXp(player.XpCurrent, player.XpMax);
        }

        public void SetResources(IReadOnlyList<ResourceViewData> resources)
        {
            if (resourcesContainer == null || resourcePillPrefab == null) return;
            resourcesContainer.gameObject.SetActive(resources != null && resources.Count > 0);

            int count = resources != null ? resources.Count : 0;
            for (int i = _spawned.Count - 1; i >= count; i--)
            {
                if (_spawned[i] != null) { _spawned[i].gameObject.SetActive(false); Destroy(_spawned[i].gameObject); }
                _spawned.RemoveAt(i);
                _currencies.RemoveAt(i);
            }

            if (resources == null) return;

            for (int i = 0; i < resources.Count; i++)
            {
                if (i >= _spawned.Count)
                {
                    _spawned.Add(Instantiate(resourcePillPrefab, resourcesContainer));
                    _currencies.Add(resources[i].CurrencyId);
                }
                else if (_spawned[i] == null || _currencies[i] != resources[i].CurrencyId)
                {
                    if (_spawned[i] != null) { _spawned[i].gameObject.SetActive(false); Destroy(_spawned[i].gameObject); }
                    _spawned[i] = Instantiate(resourcePillPrefab, resourcesContainer);
                    _currencies[i] = resources[i].CurrencyId;
                }
                var pill = _spawned[i];
                pill.transform.SetSiblingIndex(i);
                pill.SetResource(resources[i].Icon, resources[i].Amount);
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
