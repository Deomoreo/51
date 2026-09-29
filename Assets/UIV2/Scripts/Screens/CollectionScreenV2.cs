using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
            public Button Button;
            public Image Background;
            public TMP_Text Label;
            public GameObject Panel;
        }

        [SerializeField] private TabRefs[] tabs; // indice = (int)CollectionTab
        [SerializeField] private CollectionDecksPanel decksPanel;
        [SerializeField] private CollectionEmoticonsPanel emoticonsPanel;
        [SerializeField] private CollectionAccusiPanel accusiPanel;

        [Header("Stato tab: btn_teal selezionata, btn_gray_small normale")]
        [SerializeField] private Sprite selectedTabSprite;
        [SerializeField] private Sprite normalTabSprite;
        [SerializeField] private float selectedTabPixelsPerUnit = 1f;
        [SerializeField] private float normalTabPixelsPerUnit = 1f;
        [SerializeField] private Color selectedLabelColor = Color.white;
        [SerializeField] private Color normalLabelColor = Color.white;
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
            if (tabs != null)
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    var tab = (CollectionTab)i;
                    if (tabs[i] != null && tabs[i].Button != null) tabs[i].Button.onClick.AddListener(() => SetTab(tab));
                }
            }
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

        public void SetTabInteractable(CollectionTab tab, bool interactable)
        {
            int index = (int)tab;
            if (tabs != null && index >= 0 && index < tabs.Length && tabs[index]?.Button != null)
                tabs[index].Button.interactable = interactable;
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
            {
                var refs = tabs[i];
                if (refs == null) continue;

                bool selected = i == (int)tab;
                if (refs.Background != null)
                {
                    var sprite = selected ? selectedTabSprite : normalTabSprite;
                    if (sprite != null) refs.Background.sprite = sprite;
                    refs.Background.pixelsPerUnitMultiplier = selected ? selectedTabPixelsPerUnit : normalTabPixelsPerUnit;
                }
                if (refs.Label != null) refs.Label.color = selected ? selectedLabelColor : normalLabelColor;
                if (refs.Panel != null) refs.Panel.SetActive(selected);
            }
        }
    }
}
