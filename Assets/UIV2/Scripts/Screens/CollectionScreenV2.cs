using System;
using UnityEngine;
using Project51.UI51;

namespace Project51.UIV2.Screens
{
    public enum CollectionTab
    {
        Decks = 0,
        Emoticons = 1,
        Accusi = 2
    }

    /// <summary>
    /// Pagina COLLEZIONE (voce "Carte" della bottom nav): UN solo screen con tab bar fissa
    /// MAZZI / EMOTICON / ACCUSI e un pannello attivo alla volta dentro ContentHost. TopBar e
    /// BottomNav restano quelli globali di UIV2_Root. Mockup: 20_collezione_carte.png,
    /// 21_collezione_emoticon (1).png, 22_collezione_accusi (1).png.
    /// </summary>
    public class CollectionScreenV2 : MonoBehaviour
    {
        [Serializable]
        public class TabRefs
        {
            public GameObject Panel;
        }

        [SerializeField] private TabRefs[] tabs; // indice = (int)CollectionTab
        [SerializeField] private CollectionDecksPanel decksPanel;
        [SerializeField] private CollectionEmoticonsPanel emoticonsPanel;
        [SerializeField] private CollectionAccusiPanel accusiPanel;
        [SerializeField] private CollectionTab initialTab = CollectionTab.Decks;

        [Header("UI51 (facoltativo): schede a segmenti, col conteggio accanto al nome")]
        [SerializeField] private SegmentedTabs segmentedTabs;
        [SerializeField] private string[] tabNames; // indice = (int)CollectionTab

        private readonly string[] tabCounts = new string[3];

        public CollectionDecksPanel DecksPanel => decksPanel;
        public CollectionEmoticonsPanel EmoticonsPanel => emoticonsPanel;
        public CollectionAccusiPanel AccusiPanel => accusiPanel;
        public CollectionTab CurrentTab { get; private set; }

        public event Action<CollectionTab> OnTabChanged;

        private void Awake()
        {
            if (segmentedTabs != null) segmentedTabs.onTabChanged.AddListener(index => SetTab((CollectionTab)index));
            ApplyTab(initialTab);
        }

        /// <summary>Conteggio accanto al nome della scheda ("4", "2/3"); vuoto = solo il nome.</summary>
        public void SetTabCount(CollectionTab tab, string count)
        {
            tabCounts[(int)tab] = count;
            PaintSegments();
        }

        /// <summary>Nome + conteggio piccolo: oro .75 sulla scheda attiva, crema .4 sulle altre (mockup Collezione).</summary>
        public static string TabLabel(string name, string count, bool selected)
        {
            if (string.IsNullOrEmpty(count)) return name;
            var color = selected ? UI51Tokens.GoldA(0.75f) : UI51Tokens.CreamA(0.4f);
            return $"{name}<space=6><size=10><color=#{ColorUtility.ToHtmlStringRGBA(color)}>{count}</color></size>";
        }

        private void PaintSegments()
        {
            if (segmentedTabs == null || tabNames == null) return;
            var labels = new string[tabNames.Length];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = TabLabel(tabNames[i], i < tabCounts.Length ? tabCounts[i] : null, i == (int)CurrentTab);
            segmentedTabs.SetLabels(labels);
            segmentedTabs.Select((int)CurrentTab, false);
        }

        public void SetTab(CollectionTab tab)
        {
            bool changed = tab != CurrentTab;
            ApplyTab(tab);
            if (changed) OnTabChanged?.Invoke(tab);
        }

        private void ApplyTab(CollectionTab tab)
        {
            CurrentTab = tab;
            PaintSegments();
            if (tabs == null) return;

            for (int i = 0; i < tabs.Length; i++)
                if (tabs[i]?.Panel != null) tabs[i].Panel.SetActive(i == (int)tab);
        }
    }
}
