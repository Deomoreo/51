using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Card prodotto del NEGOZIO V2 (monete / gemme / mazzi: una prefab per sezione, stesso script).
    /// Evidenziata = bordo oro + badge; Locked = lucchetto sull'arte; Owned = bottone disattivato.
    /// </summary>
    public class ShopProductCardView : MonoBehaviour
    {
        [SerializeField] private Image border;
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image artwork;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TMP_Text badgeLabel;
        [SerializeField] private Button priceButton;
        [SerializeField] private TMP_Text priceLabel;

        [Header("Stili")]
        [SerializeField] private Color normalBorderColor = new Color32(70, 102, 142, 255);
        [SerializeField] private Color highlightedBorderColor = new Color32(232, 178, 74, 255);
        [SerializeField] private float normalBorderThickness = 3f;
        [SerializeField] private float highlightedBorderThickness = 5f;
        [SerializeField] private string ownedText = "Posseduto";

        private ShopProductViewData _data;

        public ShopProductViewData Data => _data;
        public event Action<ShopProductViewData> OnPurchasePressed;

        private void Awake()
        {
            if (priceButton != null)
            {
                priceButton.onClick.AddListener(() =>
                {
                    if (_data != null) OnPurchasePressed?.Invoke(_data);
                });
            }
        }

        public void Bind(ShopProductViewData data)
        {
            _data = data;
            if (data == null) return;

            if (border != null) border.color = data.Highlighted ? highlightedBorderColor : normalBorderColor;
            if (fillRect != null)
            {
                float thickness = data.Highlighted ? highlightedBorderThickness : normalBorderThickness;
                fillRect.offsetMin = new Vector2(thickness, thickness);
                fillRect.offsetMax = new Vector2(-thickness, -thickness);
            }

            if (artwork != null && data.Artwork != null) artwork.sprite = data.Artwork;
            if (lockIcon != null) lockIcon.SetActive(data.State == ShopProductState.Locked);
            if (label != null) label.text = !string.IsNullOrEmpty(data.AmountText) ? data.AmountText : data.Title;

            bool hasBadge = !string.IsNullOrEmpty(data.BadgeText);
            if (badgeRoot != null) badgeRoot.SetActive(hasBadge);
            if (hasBadge && badgeLabel != null) badgeLabel.text = data.BadgeText;

            bool owned = data.State == ShopProductState.Owned;
            if (priceLabel != null) priceLabel.text = owned ? ownedText : data.PriceText;
            if (priceButton != null) priceButton.interactable = !owned;
        }
    }
}
