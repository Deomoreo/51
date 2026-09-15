using System.Collections.Generic;
using UnityEngine;
using Project51.UIV2.Components;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;

namespace Project51.UIV2.Tests
{
    /// <summary>
    /// Solo per Assets/UIV2/Tests/UIV2_ShopV2_Preview.unity: prodotti/prezzi demo del mockup
    /// 16_negozio.png SOLO qui, mai nei prefab. Nessun pagamento reale.
    /// </summary>
    public class ShopV2PreviewBootstrap : MonoBehaviour
    {
        private const char Euro = (char)0x20AC;
        private const char NewLine = (char)10;

        [SerializeField] private UIV2TopBar topBar;
        [SerializeField] private UIV2BottomNav bottomNav;
        [SerializeField] private ShopScreenV2 shopScreen;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite gemIcon;
        [SerializeField] private Sprite chestGreen;
        [SerializeField] private Sprite chestPurple;

        private static string EuroPrice(string amount) => amount + " " + Euro;

        private void Start()
        {
            if (topBar != null)
            {
                topBar.SetProfile(new PlayerSummaryViewData
                {
                    DisplayName = "Deomoreo",
                    EnergyCurrent = 50,
                    EnergyMax = 100,
                    XpCurrent = 0,
                    XpMax = 100,
                });
                topBar.SetResources(new List<ResourceViewData>
                {
                    new ResourceViewData { CurrencyId = "gold", Amount = 1000 },
                });
            }

            if (bottomNav != null)
            {
                bottomNav.SetItems(new List<UIV2NavItemData>
                {
                    new UIV2NavItemData { Id = "gioca", Label = "Gioca" },
                    new UIV2NavItemData { Id = "cards", Label = "Carte" },
                    new UIV2NavItemData { Id = "shop", Label = "Negozio" },
                    new UIV2NavItemData { Id = "profile", Label = "Profilo" },
                });
                bottomNav.SelectIndex(2);
            }

            if (shopScreen == null) return;

            var products = new List<ShopProductViewData>
            {
                new ShopProductViewData
                {
                    Id = "starter", Category = ShopProductCategory.Offer, Title = "Pacchetto starter",
                    Subtitle = "5.000 monete + 100 gemme" + NewLine + "+ 1 mazzo esclusivo",
                    Artwork = chestPurple, PriceText = EuroPrice("4,99"), BadgeText = "-60%",
                },

                new ShopProductViewData { Id = "coins_1000", Category = ShopProductCategory.Coins, AmountText = "1.000", Artwork = coinIcon, PriceText = EuroPrice("0,99") },
                new ShopProductViewData { Id = "coins_5500", Category = ShopProductCategory.Coins, AmountText = "5.500", Artwork = chestGreen, PriceText = EuroPrice("4,99"), BadgeText = "MIGLIORE", Highlighted = true },
                new ShopProductViewData { Id = "coins_12000", Category = ShopProductCategory.Coins, AmountText = "12.000", Artwork = chestPurple, PriceText = EuroPrice("9,99") },

                new ShopProductViewData { Id = "gems_50", Category = ShopProductCategory.Gems, AmountText = "50", Artwork = gemIcon, PriceText = EuroPrice("1,99") },
                new ShopProductViewData { Id = "gems_300", Category = ShopProductCategory.Gems, AmountText = "300", Artwork = gemIcon, PriceText = EuroPrice("7,99") },
                new ShopProductViewData { Id = "gems_800", Category = ShopProductCategory.Gems, AmountText = "800", Artwork = gemIcon, PriceText = EuroPrice("19,99") },

                new ShopProductViewData { Id = "deck_reale", Category = ShopProductCategory.Decks, Title = "Reale", PriceText = "1.500", State = ShopProductState.Locked },
                new ShopProductViewData { Id = "deck_smeraldo", Category = ShopProductCategory.Decks, Title = "Smeraldo", PriceText = "250 gemme", State = ShopProductState.Locked },
                new ShopProductViewData { Id = "deck_antico", Category = ShopProductCategory.Decks, Title = "Antico", PriceText = "3.000", State = ShopProductState.Locked },
            };

            shopScreen.Bind(products);
            shopScreen.OnProductPressed += product => Debug.Log($"[ShopV2PreviewBootstrap] OnProductPressed {product.Id}");
        }
    }
}
