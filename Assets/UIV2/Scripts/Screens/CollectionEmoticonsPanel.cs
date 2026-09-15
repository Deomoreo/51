using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Project51.UIV2.Components;
using Project51.UIV2.Data;

namespace Project51.UIV2.Screens
{
    /// <summary>
    /// Pannello EMOTICON della pagina COLLEZIONE (mockup 21_collezione_emoticon (1).png): slot
    /// EQUIPAGGIATE (max = numero di slot nel prefab), conteggio + barra COLLEZIONE e griglia a 4
    /// colonne spawnata da Bind(). Nessun emoticon e' hardcoded nel prefab.
    /// </summary>
    public class CollectionEmoticonsPanel : MonoBehaviour
    {
        private const char MiddleDot = (char)0xB7;

        [Header("EQUIPAGGIATE")]
        [SerializeField] private TMP_Text equippedHeaderLabel;
        [SerializeField] private string equippedHeaderPrefix = "EQUIPAGGIATE";
        [SerializeField] private EmoticonSlotView[] equippedSlots;

        [Header("COLLEZIONE")]
        [SerializeField] private TMP_Text collectionCountLabel;
        [SerializeField] private UIV2ProgressBar collectionProgress;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private UIV2CollectionCard cardPrefab;

        private readonly List<UIV2CollectionCard> _spawned = new List<UIV2CollectionCard>();

        public int MaxEquipped => equippedSlots != null ? equippedSlots.Length : 0;

        public event Action<CollectionItemViewData> OnEmoticonPressed;
        public event Action<CollectionItemViewData> OnRemovePressed;
        public event Action<int> OnEmptySlotPressed;

        private void Awake()
        {
            if (equippedSlots == null) return;
            foreach (var slot in equippedSlots)
            {
                if (slot == null) continue;
                slot.OnRemovePressed += item => OnRemovePressed?.Invoke(item);
                slot.OnEmptyPressed += index => OnEmptySlotPressed?.Invoke(index);
            }
        }

        /// <summary>
        /// totalCount = emoticon esistenti nel catalogo (anche non in griglia), per "6 / 12". Gli
        /// equipaggiati sono gli item con Equipped && Unlocked, nell'ordine della lista.
        /// </summary>
        public void Bind(IReadOnlyList<CollectionItemViewData> emoticons, int totalCount)
        {
            ClearSpawned();

            var equipped = new List<CollectionItemViewData>();
            int owned = 0;
            if (emoticons != null)
            {
                foreach (var item in emoticons)
                {
                    if (item == null) continue;
                    if (item.Unlocked) owned++;
                    if (item.Unlocked && item.Equipped) equipped.Add(item);
                    SpawnCard(item);
                }
            }

            SetEquipped(equipped);
            SetCollectionProgress(owned, Mathf.Max(totalCount, emoticons != null ? emoticons.Count : 0));
        }

        public void SetEquipped(IReadOnlyList<CollectionItemViewData> equipped)
        {
            if (equippedHeaderLabel != null) equippedHeaderLabel.text = $"{equippedHeaderPrefix} {MiddleDot} {MaxEquipped} max";
            if (equippedSlots == null) return;

            for (int i = 0; i < equippedSlots.Length; i++)
            {
                if (equippedSlots[i] == null) continue;
                equippedSlots[i].Bind(equipped != null && i < equipped.Count ? equipped[i] : null, i);
            }
        }

        public void SetCollectionProgress(int owned, int total)
        {
            if (collectionCountLabel != null) collectionCountLabel.text = $"{owned} / {total}";
            if (collectionProgress != null) collectionProgress.SetProgress(total > 0 ? (float)owned / total : 0f, animate: false);
        }

        private void SpawnCard(CollectionItemViewData item)
        {
            if (cardPrefab == null || gridContainer == null) return;
            var card = Instantiate(cardPrefab, gridContainer);
            card.Bind(item);
            card.OnClicked += data => OnEmoticonPressed?.Invoke(data);
            _spawned.Add(card);
        }

        private void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();
        }
    }
}
