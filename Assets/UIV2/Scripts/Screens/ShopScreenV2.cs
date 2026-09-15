using System;
using System.Collections.Generic;
using UnityEngine;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// NEGOZIO V2 (voce "Negozio" della bottom nav, mockup 16_negozio.png): offerta hero + sezioni
    /// MONETE / GEMME / MAZZI in uno ScrollRect verticale. Bind() smista i prodotti per Category; il
    /// primo prodotto Offer va nell'hero. Nessun pagamento/IAP qui: solo l'evento OnProductPressed.
    /// </summary>
    public class ShopScreenV2 : MonoBehaviour
    {
        [Serializable]
        public class SectionRefs
        {
            public ShopProductCategory Category;
            public RectTransform Container;
            public ShopProductCardView CardPrefab;
        }

        [SerializeField] private ShopOfferView offerView;
        [SerializeField] private SectionRefs[] sections;

        private readonly List<ShopProductCardView> _spawned = new List<ShopProductCardView>();

        public event Action<ShopProductViewData> OnProductPressed;

        private void Awake()
        {
            if (offerView != null) offerView.OnPurchasePressed += data => OnProductPressed?.Invoke(data);
        }

        public void Bind(IReadOnlyList<ShopProductViewData> products)
        {
            ClearSpawned();

            ShopProductViewData offer = null;
            if (products != null)
            {
                foreach (var product in products)
                {
                    if (product == null) continue;
                    if (product.Category == ShopProductCategory.Offer)
                    {
                        if (offer == null) offer = product;
                        continue;
                    }
                    var section = FindSection(product.Category);
                    if (section == null || section.Container == null || section.CardPrefab == null) continue;

                    var card = Instantiate(section.CardPrefab, section.Container);
                    card.Bind(product);
                    card.OnPurchasePressed += data => OnProductPressed?.Invoke(data);
                    _spawned.Add(card);
                }
            }

            if (offerView != null)
            {
                var offerSlot = offerView.transform.parent != null ? offerView.transform.parent.gameObject : offerView.gameObject;
                offerSlot.SetActive(offer != null);
                offerView.Bind(offer);
            }
        }

        private SectionRefs FindSection(ShopProductCategory category)
        {
            if (sections == null) return null;
            foreach (var section in sections)
            {
                if (section != null && section.Category == category) return section;
            }
            return null;
        }

        private void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();
        }
    }
}
