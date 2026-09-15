using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            ApplyTab(initialTab);
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
