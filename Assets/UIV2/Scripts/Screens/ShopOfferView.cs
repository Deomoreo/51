using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Offerta hero in cima al NEGOZIO V2 (es. Pacchetto Starter): arte, titolo, contenuti, badge
    /// sconto e bottone acquisto.
    /// </summary>
    public class ShopOfferView : MonoBehaviour
    {
        [SerializeField] private Image artwork;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TMP_Text badgeLabel;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private bool uppercaseTitle = true;

        private ShopProductViewData _data;

        public ShopProductViewData Data => _data;
        public event Action<ShopProductViewData> OnPurchasePressed;

        private void Awake()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(() =>
                {
                    if (_data != null) OnPurchasePressed?.Invoke(_data);
                });
            }
        }

        public void Bind(ShopProductViewData data)
        {
            _data = data;
            if (data == null) return;

            if (artwork != null && data.Artwork != null) artwork.sprite = data.Artwork;
            if (titleLabel != null) titleLabel.text = uppercaseTitle && data.Title != null ? data.Title.ToUpperInvariant() : data.Title;
            if (subtitleLabel != null) subtitleLabel.text = data.Subtitle;
            if (priceLabel != null) priceLabel.text = data.PriceText;

            bool hasBadge = !string.IsNullOrEmpty(data.BadgeText);
            if (badgeRoot != null) badgeRoot.SetActive(hasBadge);
            if (hasBadge && badgeLabel != null) badgeLabel.text = data.BadgeText;
        }
    }
}
