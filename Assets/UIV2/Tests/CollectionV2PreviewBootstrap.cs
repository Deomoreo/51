using System.Collections.Generic;
using UnityEngine;
using Project51.UIV2.Components;
using Project51.UIV2.Data;
using Project51.UIV2.Screens;

namespace Project51.UIV2.Tests
{
    /// <summary>
    /// Solo per Assets/UIV2/Tests/UIV2_CollectionV2_Preview.unity: dati demo dei mockup
    /// 20_collezione_carte.png (mazzi, prezzi, conteggi) SOLO qui, mai nei prefab.
    /// </summary>
    public class CollectionV2PreviewBootstrap : MonoBehaviour
    {
        [SerializeField] private UIV2TopBar topBar;
        [SerializeField] private UIV2BottomNav bottomNav;
        [SerializeField] private CollectionScreenV2 collectionScreen;
        [SerializeField] private Sprite deckArtDemo;
        [SerializeField] private Sprite[] emoticonSprites; // risata, arrabbiato, sorpreso, pensieroso, triste, furbo
        [SerializeField] private CollectionTab startTab = CollectionTab.Decks;

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
                bottomNav.SelectIndex(1);
            }

            if (collectionScreen == null) return;

            collectionScreen.SetTab(startTab);
            collectionScreen.OnTabChanged += tab => Debug.Log($"[CollectionV2PreviewBootstrap] OnTabChanged {tab}");

            var decks = new List<DeckViewData>
            {
                new DeckViewData { Id = "napoletano", Name = "Napoletano", Subtitle = "Mazzo standard \u00B7 40 carte", Artwork = deckArtDemo, Unlocked = true, Equipped = true },
                new DeckViewData { Id = "classico", Name = "Classico", Artwork = deckArtDemo, Unlocked = true },
                new DeckViewData { Id = "reale", Name = "Reale", PriceText = "1.500" },
                new DeckViewData { Id = "smeraldo", Name = "Smeraldo", PriceText = "250 gemme" },
                new DeckViewData { Id = "antico", Name = "Antico", PriceText = "3.000" },
                new DeckViewData { Id = "drago", Name = "Drago", RequirementText = "Evento" },
            };
            collectionScreen.DecksPanel.Bind(decks, 9);
            collectionScreen.DecksPanel.OnDeckActionPressed += deck => Debug.Log($"[CollectionV2PreviewBootstrap] OnDeckActionPressed {deck.Id}");

            if (collectionScreen.EmoticonsPanel != null)
            {
                // Mockup 21: 6 sbloccate (prime 2 equipaggiate) + 6 bloccate, catalogo da 12.
                string[] emoticonNames = { "Risata", "Arrabbiato", "Sorpreso", "Pensieroso", "Triste", "Furbo" };
                var emoticons = new List<CollectionItemViewData>();
                for (int i = 0; i < emoticonNames.Length; i++)
                {
                    emoticons.Add(new CollectionItemViewData
                    {
                        Id = "emo_" + i,
                        Title = emoticonNames[i],
                        Icon = emoticonSprites != null && i < emoticonSprites.Length ? emoticonSprites[i] : null,
                        Unlocked = true,
                        Equipped = i < 2,
                    });
                }
                for (int i = 0; i < 6; i++)
                {
                    emoticons.Add(new CollectionItemViewData { Id = "emo_locked_" + i, Title = "?", Unlocked = false });
                }
                collectionScreen.EmoticonsPanel.Bind(emoticons, 12);
                collectionScreen.EmoticonsPanel.OnEmoticonPressed += item => Debug.Log($"[CollectionV2PreviewBootstrap] OnEmoticonPressed {item.Id}");
                collectionScreen.EmoticonsPanel.OnRemovePressed += item => Debug.Log($"[CollectionV2PreviewBootstrap] OnRemovePressed {item.Id}");
                collectionScreen.EmoticonsPanel.OnEmptySlotPressed += index => Debug.Log($"[CollectionV2PreviewBootstrap] OnEmptySlotPressed {index}");
            }

            if (collectionScreen.AccusiPanel != null)
            {
                // Mockup 22: 1 accuso posseduto ed equipaggiato + 3 bloccati, catalogo da 6. Nessuna arte
                // accuso esiste ancora nel progetto: Artwork null -> placeholder neutro nel prefab.
                var accusi = new List<AccusoViewData>
                {
                    new AccusoViewData { Id = "pugno", Title = "Pugno sul tavolo", Subtitle = "Standard \u00B7 sbloccata",
                        Description = "Batti il pugno e fai saltare tutte le carte sul tavolo.", ShortDescription = "Le carte saltano in aria",
                        Unlocked = true, Equipped = true },
                    new AccusoViewData { Id = "tuono", Title = "Tuono", ShortDescription = "Un lampo illumina il tavolo", PriceText = "2.000" },
                    new AccusoViewData { Id = "pioggia", Title = "Pioggia d'oro", ShortDescription = "Monete cadono sul tavolo", PriceText = "300 gemme" },
                    new AccusoViewData { Id = "vortice", Title = "Vortice", ShortDescription = "Le carte turbinano", RequirementText = "Livello 15" },
                };
                collectionScreen.AccusiPanel.Bind(accusi, 6);
                collectionScreen.AccusiPanel.OnPreviewPressed += a => Debug.Log($"[CollectionV2PreviewBootstrap] OnPreviewPressed {a.Id}");
                collectionScreen.AccusiPanel.OnAccusoActionPressed += a => Debug.Log($"[CollectionV2PreviewBootstrap] OnAccusoActionPressed {a.Id}");
            }
        }
    }
}
