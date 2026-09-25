using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project51.UI51
{
    public enum CosmeticType { Banner, Frame, CardBack, Emoticon }
    public enum CosmeticRarity { Comune, Rara, Epica, Leggendaria }
    public enum UnlockSource { Base, Livello, Forziere, Negozio, Missione, Evento, Pass }

    [Serializable]
    public sealed class CosmeticItem
    {
        [Tooltip("Id stabile: e' il valore salvato nei dati giocatore PlayFab (BannerId, FrameId...) e il futuro ItemId del catalogo PlayFab.")]
        public string id;
        public string displayName;
        public CosmeticType type;
        public CosmeticRarity rarity;
        public UnlockSource unlockSource;
        [Tooltip("Solo per UnlockSource.Livello.")]
        public int unlockLevel;
        [Tooltip("0 = usa il default della rarita' (20/50/120/300).")]
        public int fragmentsRequired;
        [Tooltip("Icona/anteprima (dorso, emoticon). Vuoto = segnaposto neutro.")]
        public Sprite sprite;
        [Tooltip("Materiale UI51/Banner per i banner animati (SPEC §7).")]
        public Material material;

        public int Fragments => fragmentsRequired > 0 ? fragmentsRequired : CosmeticCatalog.DefaultFragments(rarity);
    }

    /// <summary>
    /// Catalogo cosmetici locale. Proprieta' (cosa possiedi) arrivera' dall'inventario PlayFab:
    /// qui c'e' solo la definizione degli oggetti, cosi' il client puo' disegnarli.
    /// </summary>
    [CreateAssetMenu(menuName = "Project51/UI51/Cosmetic Catalog", fileName = "CosmeticCatalog")]
    public sealed class CosmeticCatalog : ScriptableObject
    {
        public List<CosmeticItem> items = new List<CosmeticItem>();

        public static int DefaultFragments(CosmeticRarity r) =>
            r == CosmeticRarity.Leggendaria ? 300 : r == CosmeticRarity.Epica ? 120 : r == CosmeticRarity.Rara ? 50 : 20;

        public CosmeticItem Find(string id) => items.Find(i => i.id == id);

        public IEnumerable<CosmeticItem> OfType(CosmeticType type)
        {
            foreach (var i in items) if (i.type == type) yield return i;
        }
    }
}
