using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Riga della lista ACCUSI: equipaggiato (bordo oro, "IN USO"), posseduto (bottone "USA"),
    /// bloccato (cerchio scuro + lucchetto, bottone blu con prezzo o requisito). Colori di default
    /// campionati da 22_collezione_accusi (1).png.
    /// </summary>
    public class AccusoRowView : MonoBehaviour
    {
        [SerializeField] private Image border;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fill;
        [SerializeField] private Image artwork;
        [SerializeField] private GameObject artworkPlaceholder;
        [SerializeField] private GameObject lockedBadge;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private GameObject equippedLabel;
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionBackground;
        [SerializeField] private TMP_Text actionLabel;

        [Header("Stili")]
        [SerializeField] private Sprite ownedActionSprite;
        [SerializeField] private Sprite lockedActionSprite;
        [SerializeField] private Color equippedBorderColor = new Color32(232, 178, 74, 255);
        [SerializeField] private Color normalBorderColor = new Color32(70, 102, 142, 255);
        [SerializeField] private float equippedBorderThickness = 4f;
        [SerializeField] private float normalBorderThickness = 3f;
        [SerializeField] private Color unlockedFillColor = new Color32(26, 44, 68, 255);
        [SerializeField] private Color lockedFillColor = new Color32(16, 28, 46, 255);
        [SerializeField] private Color titleColor = new Color32(255, 250, 238, 255);
        [SerializeField] private Color lockedTitleColor = new Color32(140, 162, 192, 255);
        [SerializeField] private Color descriptionColor = new Color32(186, 205, 228, 255);
        [SerializeField] private Color lockedDescriptionColor = new Color32(110, 132, 162, 255);
        [SerializeField] private string useActionText = "USA";

        private AccusoViewData _data;

        public AccusoViewData Data => _data;
        public event Action<AccusoViewData> OnActionPressed;

        private void Awake()
        {
            if (actionButton != null) actionButton.onClick.AddListener(() => OnActionPressed?.Invoke(_data));
        }

        public void Bind(AccusoViewData data)
        {
            _data = data;
            if (data == null) return;

            bool locked = !data.Unlocked;
            bool equipped = data.Unlocked && data.Equipped;

            if (border != null) border.color = equipped ? equippedBorderColor : normalBorderColor;
            if (fillRect != null)
            {
                float thickness = equipped ? equippedBorderThickness : normalBorderThickness;
                fillRect.offsetMin = new Vector2(thickness, thickness);
                fillRect.offsetMax = new Vector2(-thickness, -thickness);
            }
            if (fill != null) fill.color = locked ? lockedFillColor : unlockedFillColor;

            bool hasArtwork = !locked && data.Artwork != null;
            if (lockedBadge != null) lockedBadge.SetActive(locked);
            if (artwork != null)
            {
                artwork.gameObject.SetActive(hasArtwork);
                if (hasArtwork) artwork.sprite = data.Artwork;
            }
            if (artworkPlaceholder != null) artworkPlaceholder.SetActive(!locked && !hasArtwork);

            if (titleLabel != null)
            {
                titleLabel.text = data.Title;
                titleLabel.color = locked ? lockedTitleColor : titleColor;
            }
            if (descriptionLabel != null)
            {
                descriptionLabel.text = !string.IsNullOrEmpty(data.ShortDescription) ? data.ShortDescription : data.Description;
                descriptionLabel.color = locked ? lockedDescriptionColor : descriptionColor;
            }

            if (equippedLabel != null) equippedLabel.SetActive(equipped);
            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(!equipped);
                if (!equipped)
                {
                    var sprite = locked ? lockedActionSprite : ownedActionSprite;
                    if (actionBackground != null && sprite != null) actionBackground.sprite = sprite;
                    if (actionLabel != null)
                    {
                        actionLabel.text = !locked ? useActionText
                            : (!string.IsNullOrEmpty(data.PriceText) ? data.PriceText : data.RequirementText);
                    }
                }
            }
        }
    }
}
