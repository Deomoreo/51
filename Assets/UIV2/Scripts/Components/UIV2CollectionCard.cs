using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Card generica di collezione (icona + nome; stati sbloccata / equipaggiata / bloccata). Usata
    /// dalla griglia EMOTICON di CollectionScreenV2. I campi "Stile esteso" sono opzionali: lasciati
    /// vuoti (null/false) il comportamento resta quello originale di UIV2_CollectionCard.
    /// </summary>
    public class UIV2CollectionCard : UIV2AnimatedComponent
    {
        [SerializeField] private Image border;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private Color unlockedBorderColor = Color.white;
        [SerializeField] private Color equippedBorderColor = new Color(0.91f, 0.70f, 0.29f);

        [Header("Stile esteso (opzionale)")]
        [SerializeField] private Button button;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fill;
        [SerializeField] private Color unlockedFillColor = Color.white;
        [SerializeField] private Color lockedFillColor = Color.white;
        [SerializeField] private float equippedBorderThickness = 4f;
        [SerializeField] private float normalBorderThickness = 3f;
        [SerializeField] private bool overrideNameColors;
        [SerializeField] private Color nameColor = Color.white;
        [SerializeField] private Color equippedNameColor = Color.white;
        [SerializeField] private Color lockedNameColor = Color.white;
        [SerializeField] private string lockedTitleText;

        private CollectionItemViewData _data;

        public CollectionItemViewData Data => _data;
        public event Action<CollectionItemViewData> OnClicked;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            PlayPress();
            OnClicked?.Invoke(_data);
        }

        public void Bind(CollectionItemViewData data)
        {
            _data = data;
            if (data == null) return;

            bool equipped = data.Unlocked && data.Equipped;

            if (nameLabel != null)
            {
                nameLabel.text = !data.Unlocked && !string.IsNullOrEmpty(lockedTitleText) ? lockedTitleText : data.Title;
                if (overrideNameColors)
                {
                    nameLabel.color = !data.Unlocked ? lockedNameColor : (equipped ? equippedNameColor : nameColor);
                }
            }

            if (fillRect != null)
            {
                float thickness = equipped ? equippedBorderThickness : normalBorderThickness;
                fillRect.offsetMin = new Vector2(thickness, thickness);
                fillRect.offsetMax = new Vector2(-thickness, -thickness);
            }
            if (fill != null) fill.color = data.Unlocked ? unlockedFillColor : lockedFillColor;

            if (data.Unlocked)
            {
                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    if (data.Icon != null) icon.sprite = data.Icon;
                    icon.color = data.TintColor;
                }
                if (lockIcon != null) lockIcon.SetActive(false);
                if (border != null) border.color = data.Equipped ? equippedBorderColor : unlockedBorderColor;
                if (equippedBadge != null) equippedBadge.SetActive(data.Equipped);
            }
            else
            {
                if (icon != null) icon.gameObject.SetActive(false);
                if (lockIcon != null) lockIcon.SetActive(true);
                if (border != null) border.color = unlockedBorderColor;
                if (equippedBadge != null) equippedBadge.SetActive(false);
            }
        }
    }
}
