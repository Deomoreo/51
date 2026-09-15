using UnityEngine;

namespace Project51.UIV2.Data
{
    public enum DeckCardState
    {
        Equipped,
        Owned,
        Locked
    }

    /// <summary>
    /// Un mazzo nella pagina COLLEZIONE > MAZZI. Tutti i valori (nomi, prezzi, requisiti) arrivano
    /// dal chiamante: nessun mazzo e' hardcoded nei prefab.
    /// </summary>
    public class DeckViewData
    {
        public string Id;
        public string Name;
        public string Subtitle;
        public Sprite Artwork;
        public bool Unlocked;
        public bool Equipped;
        public string PriceText;       // es. "1.500", "250 gemme" (mazzo acquistabile)
        public string RequirementText; // es. "Evento" (mazzo sbloccabile solo da requisito)

        public DeckCardState State =>
            Equipped ? DeckCardState.Equipped : (Unlocked ? DeckCardState.Owned : DeckCardState.Locked);
    }
}
