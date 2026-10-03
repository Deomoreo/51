using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Testata della Home (UI51): avatar, nome, livello e barra XP dell'account, oppure ospite con
    /// Registrati. Niente valute: non esiste ancora un servizio autorevole di monete o energia.
    /// </summary>
    public class UIV2TopBar : MonoBehaviour
    {
        // Ritratto reale dentro il cerchio di avatar_frame (mascherato): spento finche'
        // PlayerSummaryViewData.Avatar e' null, cosi' resta la silhouette cotta nel frame.
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameLabel;
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

        public void SetProfile(PlayerSummaryViewData player)
        {
            if (player == null) return;
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
            if (xpBar != null) xpBar.gameObject.SetActive(player.XpMax > 0);
            if (levelLabel != null)
            {
                levelLabel.text = player.Level.ToString();
                levelLabel.transform.parent.gameObject.SetActive(player.XpMax > 0);
            }
            SetXp(player.XpCurrent, player.XpMax);
        }

        public void SetXp(int current, int max)
        {
            if (xpBar != null) xpBar.SetProgress(current, max, animate: false);
        }
    }
}
