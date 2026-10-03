using System;
using System.Collections.Generic;
using UnityEngine;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Pannello MAZZI della pagina COLLEZIONE: griglia dei mazzi spawnata da Bind().
    /// </summary>
    public class CollectionDecksPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private DeckCardView deckCardPrefab;

        private readonly List<DeckCardView> _spawned = new List<DeckCardView>();

        public event Action<DeckViewData> OnDeckActionPressed;

        public void Bind(IReadOnlyList<DeckViewData> decks)
        {
            ClearSpawned();
            if (decks == null) return;
            foreach (var deck in decks)
                if (deck != null) SpawnCard(deck);
        }

        private void SpawnCard(DeckViewData deck)
        {
            if (deckCardPrefab == null || gridContainer == null) return;
            var card = Instantiate(deckCardPrefab, gridContainer);
            card.Bind(deck);
            card.OnActionPressed += data => OnDeckActionPressed?.Invoke(data);
            _spawned.Add(card);
        }

        private void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                {
                    _spawned[i].gameObject.SetActive(false);
                    Destroy(_spawned[i].gameObject);
                }
            }
            _spawned.Clear();
        }
    }
}
