using UnityEngine;

namespace Project51.UIV2.Data
{
    public class CollectionItemViewData
    {
        public string Id;
        public string Title;
        public Sprite Icon;
        public Color TintColor = Color.white;
        public bool Unlocked;
        public bool Equipped;
        public int Order; // posto fra quelle in uso (1, 2, 3); 0 = non in uso
    }
}
