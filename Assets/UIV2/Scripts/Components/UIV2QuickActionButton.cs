using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Pulsante icona+label riusabile per le quick action della Home (Premio/Classifica/
    /// Posta), con badge di notifica opzionale. Icona/label sono strutturali (impostate una
    /// volta al build time in base a dove il pulsante viene usato, come i default icon di
    /// UIV2BottomNav) - solo il badge count e' dati mock/reali a runtime.
    /// </summary>
    public class UIV2QuickActionButton : UIV2AnimatedComponent
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TMP_Text badgeLabel;

        public Button Button => button;
        public event Action OnClicked;

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(PlayPress);
                button.onClick.AddListener(RaiseClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayPress);
                button.onClick.RemoveListener(RaiseClicked);
            }
        }

        private void RaiseClicked() => OnClicked?.Invoke();

        public void SetContent(Sprite iconSprite, string labelText)
        {
            if (icon != null && iconSprite != null) icon.sprite = iconSprite;
            if (label != null) label.text = labelText;
        }

        public void SetBadgeCount(int count)
        {
            if (badgeRoot == null) return;
            bool show = count > 0;
            badgeRoot.SetActive(show);
            if (show && badgeLabel != null) badgeLabel.text = count > 99 ? "99+" : count.ToString();
        }
    }
}
