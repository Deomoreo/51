using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project51.Core
{
    public sealed class CardDeckCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public string DisplayName;
            public string Subtitle;
            public Sprite Artwork;
            public string ResourcePath;
        }

        [SerializeField] private Entry[] entries = new Entry[0];
        public IReadOnlyList<Entry> Entries => entries;
        public Entry Find(string id)
        {
            foreach (var entry in entries)
                if (entry != null && entry.Id == id) return entry;
            return null;
        }
    }
}
