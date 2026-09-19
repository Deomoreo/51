using System;
using UnityEngine;

namespace Project51.Core
{
    /// <summary>Local cosmetic selection. Definitions load only when a match needs its cards.</summary>
    public static class CardDecks
    {
        public const string PreferenceKey = "SelectedDeckId";
        public const string DefaultId = "napoletano";
        private static CardDeckCatalog catalog;
        private static CardDeckDefinition loadedDeck;
        public static event Action SelectionChanged;

        public static CardDeckCatalog Catalog => catalog != null ? catalog : (catalog = Resources.Load<CardDeckCatalog>("CardDeckCatalog"));
        public static string SelectedId
        {
            get
            {
                string stored = PlayerPrefs.GetString(PreferenceKey, DefaultId);
                return Catalog != null && Catalog.Find(stored) != null ? stored : DefaultId;
            }
        }

        public static bool Select(string id)
        {
            if (Catalog == null || Catalog.Find(id) == null) return false;
            if (SelectedId == id) return true;
            PlayerPrefs.SetString(PreferenceKey, id);
            PlayerPrefs.Save();
            SelectionChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Mazzo da usare al tavolo: sempre quello scelto dal giocatore. Il mazzo e' solo estetico e
        /// ognuno vede il proprio. Prima il tavolo leggeva MatchConfig.DeckBackId, salvato in PlayerPrefs
        /// da chi avviava la partita: valeva "default" (= napoletano) o un mazzo vecchio in ogni percorso
        /// che non lo riscriveva, quindi si vedeva il dorso napoletano qualunque mazzo avessi scelto.
        /// </summary>
        public static CardDeckDefinition LoadForMatch() => Load(SelectedId);

        public static CardDeckDefinition Load(string id)
        {
            var entry = Catalog != null ? Catalog.Find(id) ?? Catalog.Find(DefaultId) : null;
            if (entry == null) return null;
            if (loadedDeck == null || loadedDeck.Id != entry.Id)
                loadedDeck = Resources.Load<CardDeckDefinition>(entry.ResourcePath);
            return loadedDeck;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            catalog = null;
            loadedDeck = null;
            SelectionChanged = null;
        }
    }
}
