using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Components;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Pannello MAZZI della pagina COLLEZIONE: sezione IN USO (mazzo equipaggiato a ventaglio),
    /// sezione COLLEZIONE (conteggio + barra) e griglia mazzi spawnata da Bind().
    /// </summary>
    public class CollectionDecksPanel : MonoBehaviour
    {
        [Header("IN USO")]
        [SerializeField] private Image[] heroArtCards;
        [SerializeField] private TMP_Text heroNameLabel;
        [SerializeField] private TMP_Text heroSubtitleLabel;

        [Header("COLLEZIONE")]
        [SerializeField] private TMP_Text collectionCountLabel;
        [SerializeField] private UIV2ProgressBar collectionProgress;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private DeckCardView deckCardPrefab;
        [SerializeField] private TMP_Text footerLabel;

        private readonly List<DeckCardView> _spawned = new List<DeckCardView>();

        public event Action<DeckViewData> OnDeckActionPressed;

        /// <summary>
        /// totalDeckCount = mazzi esistenti nel catalogo (anche quelli non in griglia), per "2 / 9".
        /// </summary>
        public void Bind(IReadOnlyList<DeckViewData> decks, int totalDeckCount)
        {
            ClearSpawned();

            int owned = 0;
            DeckViewData equipped = null;
            if (decks != null)
            {
                foreach (var deck in decks)
                {
                    if (deck == null) continue;
                    if (deck.Unlocked || deck.Equipped) owned++;
                    if (deck.Equipped && equipped == null) equipped = deck;
                    SpawnCard(deck);
                }
            }

            SetEquippedDeck(equipped);
            SetCollectionProgress(owned, Mathf.Max(totalDeckCount, decks != null ? decks.Count : 0));
        }

        public void SetEquippedDeck(DeckViewData deck)
        {
            if (heroArtCards != null && deck != null && deck.Artwork != null)
            {
                foreach (var card in heroArtCards)
                {
                    if (card != null) { card.sprite = deck.Artwork; card.preserveAspect = true; }
                }
            }
            if (heroNameLabel != null) heroNameLabel.text = deck != null ? deck.Name : "-";
            if (heroSubtitleLabel != null) heroSubtitleLabel.text = deck != null ? deck.Subtitle : string.Empty;
        }

        public void SetCollectionProgress(int owned, int total)
        {
            if (collectionCountLabel != null) collectionCountLabel.text = $"{owned} / {total}";
            if (collectionProgress != null) collectionProgress.SetProgress(total > 0 ? (float)owned / total : 0f, animate: false);
        }

        public void SetFooter(string text)
        {
            if (footerLabel == null) return;
            footerLabel.text = text;
            footerLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
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
