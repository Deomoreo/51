using System;
using System.Collections.Generic;
using DG.Tweening;
using Project51.Auth;
using Project51.UI51;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 8, pagina Notizie (mockup Notizie e NotizieArticolo), aperta dal pulsante Notizie della Home.
    /// Contenuto da PlayFab Title News (NewsService): in alto il carosello delle notizie in evidenza, sotto ULTIME NOTIZIE,
    /// il tocco apre l'articolo dal basso. Etichetta, sottotitolo, evidenza e pulsante si scrivono in testa al testo
    /// della notizia (NewsService.Parse). Il pallino rosso sul pulsante della Home = ci sono notizie non ancora viste.
    /// Costruita da UI51SocialBuilder.
    /// </summary>
    public sealed class UI51NewsView : MonoBehaviour
    {
        public const string LoadingText = "Caricamento…";
        public const string EmptyText = "Nessuna notizia per ora.\nTorna a trovarci presto!";
        public const string ErrorText = "Non è stato possibile caricare le notizie.\nControlla la connessione e riprova.";

        /// <summary>Aspetto di un'etichetta (mockup: TAG, classi art-*, miniature dell'elenco).</summary>
        public struct TagStyle
        {
            public Color TagBackground, TagText;
            public int Art;
            public Gradient ThumbFill;
            public Sprite Icon, Pic;
            public float IconWidth, PicWidth, PicRight, PicTop, PicRotation;
        }

        [SerializeField] private CanvasGroup view;
        [SerializeField] private Button back;
        [SerializeField] private HomeScreenV2 home;
        [SerializeField] private UIV2Pager pager;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text status;

        [Header("Carosello")]
        [SerializeField] private GameObject featuredBlock;
        [SerializeField] private RectTransform cards;
        [SerializeField] private UI51NewsItem cardTemplate;
        [SerializeField] private Button prev, next;
        [SerializeField] private RectTransform[] dots = new RectTransform[0]; // MaxFeatured

        [Header("Elenco")]
        [SerializeField] private GameObject listBlock;
        [SerializeField] private RectTransform rows;
        [SerializeField] private UI51NewsItem rowTemplate;

        [Header("Articolo")]
        [SerializeField] private BottomSheet sheet;
        [SerializeField] private UI51NewsItem article;
        [SerializeField] private ScrollRect articleScroll;
        [SerializeField] private RectTransform paragraphs;
        [SerializeField] private TMP_Text paragraphTemplate;
        [SerializeField] private Button cta;
        [SerializeField] private TMP_Text ctaLabel;

        [Header("Icone per etichetta (ordine di NewsService.Tags)")]
        [SerializeField] private Sprite[] icons = new Sprite[0];
        [SerializeField] private Sprite[] pics = new Sprite[0];

        private readonly List<UI51NewsItem> spawned = new List<UI51NewsItem>();
        private readonly List<TMP_Text> paragraphPool = new List<TMP_Text>();
        private readonly List<NewsStory> featured = new List<NewsStory>(), rest = new List<NewsStory>();
        private readonly List<UI51NewsItem> featuredCards = new List<UI51NewsItem>();
        private NewsStory open;
        private int index;
        private bool waitingForAuth;
        private Tween fade;

        public bool IsOpen => view != null && view.blocksRaycasts;

        private void Awake()
        {
            back.onClick.AddListener(Close);
            prev.onClick.AddListener(() => Show(index - 1));
            next.onClick.AddListener(() => Show(index + 1));
            for (int i = 0; i < dots.Length; i++)
            {
                int k = i;
                dots[i].GetComponent<Button>().onClick.AddListener(() => Show(k));
            }
            cta.onClick.AddListener(Cta);
            cardTemplate.gameObject.SetActive(false);
            rowTemplate.gameObject.SetActive(false);
            paragraphTemplate.gameObject.SetActive(false);
            sheet.Hide();
            if (home != null) home.OnNewsPressed += Open;
            SetVisible(false, true);
        }

        // Pallino sulla Home appena la sessione PlayFab e' pronta.
        private void Start() => Load();

        public void Open()
        {
            SetVisible(true, false);
            scroll.verticalNormalizedPosition = 1f;
            Load();
        }

        public void Close()
        {
            if (!IsOpen) return;
            NewsService.MarkAllSeen(DateTime.UtcNow);
            if (home != null) home.SetNewsBadge(0);
            sheet.Hide();
            SetVisible(false, false);
        }

        private void Load()
        {
            if (IsOpen && spawned.Count == 0) ShowStatus(LoadingText);
            var auth = AuthBootstrapper.Instance;
            if (auth != null && !auth.IsReady && !auth.HasError)
            {
                if (!waitingForAuth) { waitingForAuth = true; auth.OnAuthReady += OnAuthReady; }
                return;
            }
            NewsService.Fetch(Fill, () => { if (this != null && IsOpen && spawned.Count == 0) ShowStatus(ErrorText); });
        }

        private void OnAuthReady()
        {
            StopWaitingForAuth();
            Load();
        }

        private void StopWaitingForAuth()
        {
            if (!waitingForAuth) return;
            waitingForAuth = false;
            if (AuthBootstrapper.Instance != null) AuthBootstrapper.Instance.OnAuthReady -= OnAuthReady;
        }

        private void Fill(List<NewsEntry> news)
        {
            if (this == null) return;
            var now = DateTime.UtcNow;
            var lastSeen = NewsService.LastSeenUtc;
            bool anyNew = false;
            foreach (var n in news) anyNew |= NewsService.IsNew(n.TimestampUtc, lastSeen, now);
            if (home != null) home.SetNewsBadge(anyNew && !IsOpen ? 1 : 0);
            if (!IsOpen) return;

            Clear();
            var stories = new List<NewsStory>();
            foreach (var n in news) stories.Add(NewsService.Parse(n));
            NewsService.Split(stories, featured, rest);
            status.gameObject.SetActive(stories.Count == 0);
            status.text = EmptyText;
            featuredBlock.SetActive(featured.Count > 0);
            listBlock.SetActive(rest.Count > 0);

            foreach (var s in featured)
            {
                var card = Spawn(cardTemplate, cards, s, NewsService.Date(s.TimestampUtc.ToLocalTime(), false), false);
                featuredCards.Add(card);
            }
            foreach (var s in rest)
                Spawn(rowTemplate, rows, s, NewsService.Date(s.TimestampUtc.ToLocalTime(), true), NewsService.IsNew(s.TimestampUtc, lastSeen, now));
            prev.gameObject.SetActive(featured.Count > 1);
            next.gameObject.SetActive(featured.Count > 1);
            for (int i = 0; i < dots.Length; i++) dots[i].gameObject.SetActive(featured.Count > 1 && i < featured.Count);
            Show(0, instant: true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = 1f;
        }

        private UI51NewsItem Spawn(UI51NewsItem template, RectTransform parent, NewsStory story, string date, bool isNew)
        {
            var item = Instantiate(template, parent);
            item.name = "News_" + story.Id;
            item.gameObject.SetActive(true);
            item.Bind(story, date, isNew, Style(story.Tag));
            item.button.onClick.AddListener(() => OpenArticle(story));
            spawned.Add(item);
            return item;
        }

        /// <summary>Carosello: una scheda alla volta, dissolvenza .35 s (mockup .fade).</summary>
        private void Show(int i, bool instant = false)
        {
            if (featuredCards.Count == 0) return;
            index = (i % featuredCards.Count + featuredCards.Count) % featuredCards.Count;
            for (int k = 0; k < featuredCards.Count; k++) featuredCards[k].gameObject.SetActive(k == index);
            var group = featuredCards[index].GetComponent<CanvasGroup>();
            group.DOKill();
            group.alpha = instant ? 1f : 0f;
            if (!instant) group.DOFade(1f, 0.35f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(group.gameObject);
            for (int k = 0; k < dots.Length; k++)
            {
                bool on = k == index;
                dots[k].GetComponent<LayoutElement>().preferredWidth = on ? 22f : 6f;
                dots[k].GetComponent<UI51Shape>().fill = UI51Shape.Solid(on ? UI51Tokens.Gold : UI51Tokens.CreamA(0.3f));
            }
        }

        private void OpenArticle(NewsStory story)
        {
            open = story;
            article.Bind(story, NewsService.Date(story.TimestampUtc.ToLocalTime(), false), false, Style(story.Tag));
            for (int i = 0; i < Mathf.Max(story.Paragraphs.Length, paragraphPool.Count); i++)
            {
                if (i >= paragraphPool.Count) paragraphPool.Add(Instantiate(paragraphTemplate, paragraphs));
                bool used = i < story.Paragraphs.Length;
                paragraphPool[i].gameObject.SetActive(used);
                if (used) paragraphPool[i].text = story.Paragraphs[i];
            }
            ctaLabel.text = story.Cta;
            sheet.Open();
            LayoutRebuilder.ForceRebuildLayoutImmediate(articleScroll.content);
            articleScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Pulsante dell'articolo: GIOCA porta alla Home, COLLEZIONE e PROFILO alla pagina; il resto chiude l'articolo.</summary>
        private void Cta()
        {
            string label = open != null ? open.Cta : string.Empty;
            int page = label.Contains("GIOCA") ? 0 : label.Contains("COLLEZIONE") ? 1 : label.Contains("PROFILO") ? 3 : -1;
            if (page < 0) { sheet.Close(); return; }
            Close();
            if (pager != null) pager.Select(page);
        }

        private void ShowStatus(string text)
        {
            Clear();
            featuredBlock.SetActive(false);
            listBlock.SetActive(false);
            status.text = text;
            status.gameObject.SetActive(true);
        }

        private void Clear()
        {
            foreach (var item in spawned) if (item != null) Destroy(item.gameObject);
            spawned.Clear();
            featuredCards.Clear();
        }

        private void SetVisible(bool visible, bool instant)
        {
            fade?.Kill();
            view.blocksRaycasts = visible;
            view.interactable = visible;
            if (instant) view.alpha = visible ? 1f : 0f;
            else fade = view.DOFade(visible ? 1f : 0f, 0.2f).SetUpdate(true).SetLink(gameObject);
        }

        private void Update()
        {
            if (!IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (sheet.isOpen) sheet.Close();
            else Close();
        }

        private void OnDestroy()
        {
            fade?.Kill();
            StopWaitingForAuth();
            if (home != null) home.OnNewsPressed -= Open;
        }

        // --- Colori del mockup (const TAG e miniature)

        private TagStyle Style(string tag)
        {
            int i = Mathf.Max(0, Array.IndexOf(NewsService.Tags, tag));
            var s = new TagStyle
            {
                Icon = i < icons.Length ? icons[i] : null,
                Pic = i < pics.Length ? pics[i] : null,
            };
            switch (i)
            {
                case 0: Set(ref s, UI51Tokens.Gold, UI51Tokens.OnGold, 0, UI51Tokens.GoldA(0.12f), 26f); break;                  // NOVITÀ
                case 1: Set(ref s, UI51Tokens.TeamBlue, Color.white, 1, UI51Tokens.Rgba(79, 128, 232, 0.15f), 36f);                 // TORNEO
                    s.PicWidth = 96f; s.PicRight = 26f; s.PicTop = 26f; s.PicRotation = 8f; break;
                case 2: Set(ref s, UI51Tokens.Success, UI51Tokens.Hex("#062519"), 2, UI51Tokens.Rgba(39, 181, 133, 0.12f), 28f);    // COLLEZIONE
                    s.PicWidth = 84f; s.PicRight = 30f; s.PicTop = 18f; s.PicRotation = 10f; break;
                case 3: Set(ref s, UI51Tokens.GoldA(0.18f), UI51Tokens.Gold, 0, Color.clear, 48f);                                  // AGGIORNAMENTO
                    s.ThumbFill = UI51Shape.Linear((UI51Tokens.Navy, 0f), (UI51Tokens.Hex("#1B6B8F"), 0.33f), (UI51Tokens.Success, 0.66f),
                        (UI51Tokens.Hex("#6B3FA0"), 1f));
                    break;
                case 4: Set(ref s, UI51Tokens.Rgba(79, 128, 232, 0.22f), UI51Tokens.TeamBlueText, 1, UI51Tokens.Rgba(79, 128, 232, 0.15f), 38f); break; // EVENTO
                case 5: Set(ref s, UI51Tokens.Rgba(229, 72, 77, 0.18f), UI51Tokens.DangerText, 1, UI51Tokens.Rgba(229, 72, 77, 0.12f), 26f); break;      // AVVISO
                default: Set(ref s, UI51Tokens.Rgba(39, 181, 133, 0.2f), UI51Tokens.SuccessText, 2, UI51Tokens.Rgba(39, 181, 133, 0.12f), 32f); break;   // CONSIGLI
            }
            return s;
        }

        private static void Set(ref TagStyle s, Color tagBg, Color tagText, int art, Color thumb, float iconWidth)
        {
            s.TagBackground = tagBg;
            s.TagText = tagText;
            s.Art = art;
            s.ThumbFill = UI51Shape.Solid(thumb);
            s.IconWidth = iconWidth;
        }
    }
}
