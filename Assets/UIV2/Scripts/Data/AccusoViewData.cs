using UnityEngine;

namespace Project51.UIV2.Data
{
    /// <summary>
    /// Un accuso nella pagina COLLEZIONE > ACCUSI. Tutti i testi/valori arrivano dal chiamante.
    /// </summary>
    public class AccusoViewData
    {
        public string Id;
        public string Title;
        public string Subtitle;         // riga sotto il titolo nel box IN USO (es. categoria + stato)
        public string Description;      // descrizione lunga (box IN USO)
        public string ShortDescription; // riga della lista; se vuota si usa Description
        public Sprite Artwork;
        public bool Unlocked;
        public bool Equipped;
        public string RequirementText;  // es. "Livello 15"
        public string PriceText;        // es. "2.000", "300 gemme"
    }
}
