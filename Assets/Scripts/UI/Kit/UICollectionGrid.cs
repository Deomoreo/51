using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UIKit
{
    public struct UICollectionCardData
    {
        public bool Unlocked;
        public bool Equipped;
        public string Label;
        public Color Color;

        public UICollectionCardData(bool unlocked, bool equipped, string label, Color color)
        {
            Unlocked = unlocked;
            Equipped = equipped;
            Label = label;
            Color = color;
        }
    }

    /// <summary>
    /// Griglia di collezione riutilizzabile (UI_CollectionGrid4): il GameObject porta un
    /// GridLayoutGroup nativo (colonne/cellSize/spacing configurabili li', mai duplicati qui).
    /// Genera le card da un prefab (UI_CollectionCard) invece di card scritte a mano - qualunque
    /// altra schermata con una griglia di collezione (mazzi, accusi, skin...) riusa lo stesso
    /// componente passando solo dati diversi.
    /// </summary>
    public class UICollectionGrid : MonoBehaviour
    {
        [SerializeField] private UICollectionCard cardPrefab;
        [SerializeField] private Transform container;

        private readonly List<UICollectionCard> _spawned = new List<UICollectionCard>();

        public void Populate(IList<UICollectionCardData> data)
        {
            var parent = container != null ? container : transform;
            foreach (var c in _spawned)
            {
                if (c != null) Destroy(c.gameObject);
            }
            _spawned.Clear();

            for (int i = 0; i < data.Count; i++)
            {
                var card = Instantiate(cardPrefab, parent);
                card.name = "Card" + (i + 1).ToString("00");
                var d = data[i];
                if (d.Unlocked) card.SetUnlocked(d.Color, d.Label, d.Equipped);
                else card.SetLocked();
                _spawned.Add(card);
            }
        }
    }
}
