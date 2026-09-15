using UnityEngine;

namespace Project51.UIV2.Data
{
    public enum ShopProductCategory
    {
        Offer,
        Coins,
        Gems,
        Decks
    }

    public enum ShopProductState
    {
        Available,
        Owned,
        Locked
    }

    /// <summary>
    /// Un prodotto del NEGOZIO V2 (offerta hero o card di sezione). Prezzi/quantita' sono testo gia'
    /// formattato dal chiamante: nessun contenuto negozio e' hardcoded nei prefab.
    /// </summary>
    public class ShopProductViewData
    {
        public string Id;
        public ShopProductCategory Category;
        public string Title;
        public string Subtitle;
        public Sprite Artwork;      // null = arte di default del prefab di sezione
        public string AmountText;   // "1.000", "50" (se vuoto la card mostra Title)
        public string PriceText;    // "0,99 EUR", "1.500", "250 gemme"
        public string BadgeText;    // "MIGLIORE", "-60%" (vuoto = nessun badge)
        public bool Highlighted;
        public ShopProductState State;
    }
}
