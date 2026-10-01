using Project51.UIV2.Components;
using Project51.UIV2.Core;
using Project51.UIV2.Screens;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 8 su MainMenu. Per ora la pagina Notizie (mockup Notizie e NotizieArticolo): radice UI51News con UI51NewsView
    /// (canvas sopra la Home e sotto i flussi online) e il pulsante Notizie sotto Posta nella colonna sinistra della Home.
    /// La vecchia pagina NewsV2 (Novita' della schermata iniziale) resta in scena, non piu' raggiungibile.
    /// Idempotente: rieseguirlo riusa i nodi per nome.
    /// </summary>
    public static class UI51SocialBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Fase 8]";
        const int SortingOrder = 700; // sopra UIV2_Home (0), sotto OnlineFlowV2 (800) e StartScreenV2 (1000)
        const float CardW = 350f, CardH = 230f, HeroH = 210f, SheetH = 700f, Bleed = 80f;

        [MenuItem("Tools/UI51/Build Fase 8 (Notizie)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (UI51AccessBuilder.HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var home = Object.FindObjectOfType<HomeScreenV2>(true);
            var pager = Object.FindObjectOfType<UIV2Pager>(true);
            var canvases = home != null ? home.GetComponentsInParent<Canvas>(true) : new Canvas[0];
            var homeCanvas = canvases.Length > 0 ? canvases[canvases.Length - 1] : null;
            if (home == null || pager == null || homeCanvas == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo della Home (HomeScreenV2, UIV2Pager o il suo canvas).");
                return;
            }

            var news = BuildNewsButton(home);
            BuildNews(home, pager, homeCanvas);
            UI51Build.Wire(home, so => UI51Build.Ref(so, "newsButton", news));

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        /// <summary>Colonna sinistra del mockup Home: Amici, Posta, Missioni, Notizie. Amici e Missioni restano nascosti: Notizie va sotto Posta.</summary>
        static UIV2QuickActionButton BuildNewsButton(HomeScreenV2 home)
        {
            var c = UI51HomeBuilder.Container(home.transform);
            return UI51HomeBuilder.QuickAction(c, "News", "ic_globe_cream", 22f, "Notizie", 14f, 118f - UI51HomeBuilder.TopBarH + 62f + 14f);
        }

        static void BuildNews(HomeScreenV2 home, UIV2Pager pager, Canvas homeCanvas)
        {
            // Non GameObject.Find: "UI51News" trova anche il percorso UI51/News (il pulsante della Home).
            GameObject rootGo = null;
            foreach (var go in home.gameObject.scene.GetRootGameObjects()) if (go.name == "UI51News") rootGo = go;
            if (rootGo == null) rootGo = new GameObject("UI51News", typeof(RectTransform));
            rootGo.layer = UI51Build.UILayer;
            var canvas = UI51Build.GetOrAdd<Canvas>(rootGo);
            canvas.renderMode = homeCanvas.renderMode;
            canvas.worldCamera = homeCanvas.worldCamera;
            canvas.sortingOrder = SortingOrder;
            var scaler = UI51Build.GetOrAdd<CanvasScaler>(rootGo);
            var homeScaler = homeCanvas.GetComponent<CanvasScaler>();
            if (homeScaler != null) EditorUtility.CopySerialized(homeScaler, scaler);
            UI51Build.GetOrAdd<GraphicRaycaster>(rootGo);
            var group = UI51Build.GetOrAdd<CanvasGroup>(rootGo);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var view = UI51Build.GetOrAdd<UI51NewsView>(rootGo);

            var safe = UI51AccessBuilder.BuildScreen(rootGo.transform, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);

            // Testata: indietro 40 a 20/22, titolo Cinzel 22
            var header = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Header"), 20f, 20f, 22f, 40f);
            UI51Build.Row(header, 12f, null, TextAnchor.MiddleLeft, true, false);
            var back = UI51AccessBuilder.RoundButton(header, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            UI51Build.Layout(back, 40f);
            var title = UI51Build.Size(UI51Build.Child(header, "Title"), 0f, 40f);
            UI51Build.Layout(title, -1f, -1f, 1f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(title, "Notizie", FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));

            // Tutto sotto la testata scorre: carosello (top 80), puntini (320), ULTIME NOTIZIE (344), elenco (366).
            var scrollRt = UI51Build.Stretch(UI51Build.Child(safe, "Scroll"), 0f, -Bleed, 0f, 80f);
            var scroll = ScrollArea(scrollRt, UI51Build.Pad(0, 20, 24 + (int)Bleed, 20), 0f);
            var content = scroll.content;

            var featured = UI51Build.Child(content, "Featured");
            UI51MetaBuilder.Stack(featured, 0f);
            var carousel = UI51Build.Child(featured, "Carousel");
            UI51Build.Layout(carousel, -1f, CardH);
            var cards = UI51Build.Stretch(UI51Build.Child(carousel, "Cards"));
            var card = Item(cards, "CardTemplate", true);
            UI51Build.Stretch((RectTransform)card.transform);
            UI51Build.GetOrAdd<CanvasGroup>(card);
            var prev = Arrow(carousel, "Prev", false);
            var next = Arrow(carousel, "Next", true);
            UI51MetaBuilder.Gap(featured, "GapDots", 10f);
            var dotRow = UI51Build.Child(featured, "Dots");
            UI51Build.Layout(dotRow, -1f, 6f);
            UI51Build.Row(dotRow, 6f, null, TextAnchor.MiddleCenter, true, true);
            var dots = new RectTransform[3];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = UI51Build.Child(dotRow, "Dot" + i);
                UI51Build.Layout(dots[i], i == 0 ? 22f : 6f, 6f);
                var shape = UI51Build.Solid(dots[i], i == 0 ? UI51Tokens.Gold : UI51Tokens.CreamA(0.3f), 3f, 0f, default, true);
                UI51Build.Button(shape, shape);
                // 6x6 e' piccolo per il dito: area di tocco 22x26, invisibile.
                UI51Build.Image(UI51Build.Center(UI51Build.Child(dots[i], "Hit"), 22f, 26f), null, Color.clear, true, false)
                    .canvasRenderer.cullTransparentMesh = false;
            }
            UI51MetaBuilder.Gap(featured, "GapList", 18f);

            var list = UI51Build.Child(content, "List");
            UI51MetaBuilder.Stack(list, 8f);
            var caption = UI51Build.Child(list, "Caption");
            UI51Build.Layout(caption, -1f, 14f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(caption, "ULTIME NOTIZIE", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold,
                TextAlignmentOptions.MidlineLeft, 2f));
            var panel = UI51Build.Child(list, "Panel");
            UI51Build.Shape(panel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51MetaBuilder.Stack(panel, 0f, UI51Build.Pad(4, 0, 4, 0));
            var row = Item(panel, "RowTemplate", false);
            UI51Build.Layout(row, -1f, 78f);

            var status = UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Status"), 30f, 30f, 360f, 80f), UI51NewsView.LoadingText,
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Top);
            UI51MetaBuilder.Wrap(status, 8f);

            var article = BuildArticle(rootGo.transform, out var articleItem, out var articleScroll, out var paragraphs, out var paragraph,
                out var cta, out var ctaLabel);

            var icons = new[] { "ic_globe_cream", "medal_trophy", "back_smeraldo", "avatar_1", "medal_sun", "ic_warn_cream", "pugno" };
            var iconAreas = new[] { "Common", "Common", "Cards", "Avatars", "Common", "Common", "Common" };
            var iconSprites = new Object[icons.Length];
            for (int i = 0; i < icons.Length; i++) iconSprites[i] = UI51Build.Sprite(iconAreas[i], icons[i]);
            var picSprites = new Object[] { null, UI51Build.Sprite("Common", "medal_trophy"), UI51Build.Sprite("Cards", "back_smeraldo"), null, null, null, null };

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "view", group);
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "home", home);
                UI51Build.Ref(so, "pager", pager);
                UI51Build.Ref(so, "scroll", scroll);
                UI51Build.Ref(so, "status", status);
                UI51Build.Ref(so, "featuredBlock", featured.gameObject);
                UI51Build.Ref(so, "cards", cards);
                UI51Build.Ref(so, "cardTemplate", card);
                UI51Build.Ref(so, "prev", prev);
                UI51Build.Ref(so, "next", next);
                UI51AccessBuilder.SetArray(so, "dots", dots);
                UI51Build.Ref(so, "listBlock", list.gameObject);
                UI51Build.Ref(so, "rows", panel);
                UI51Build.Ref(so, "rowTemplate", row);
                UI51Build.Ref(so, "sheet", article);
                UI51Build.Ref(so, "article", articleItem);
                UI51Build.Ref(so, "articleScroll", articleScroll);
                UI51Build.Ref(so, "paragraphs", paragraphs);
                UI51Build.Ref(so, "paragraphTemplate", paragraph);
                UI51Build.Ref(so, "cta", cta);
                UI51Build.Ref(so, "ctaLabel", ctaLabel);
                UI51AccessBuilder.SetArray(so, "icons", iconSprites);
                UI51AccessBuilder.SetArray(so, "pics", picSprites);
            });
        }

        /// <summary>Articolo dal basso: velo, foglio 390x700 (raggi 24 in alto) con testata illustrata 210, testo che scorre e il pulsante d'oro.</summary>
        static BottomSheet BuildArticle(Transform root, out UI51NewsItem item, out ScrollRect scroll, out RectTransform paragraphs,
            out TextMeshProUGUI paragraph, out Button cta, out TextMeshProUGUI ctaLabel)
        {
            var article = UI51Build.Stretch(UI51Build.Child(root.Find("UI51"), "Article"));
            var scrim = UI51Build.Stretch(UI51Build.Child(article, "Scrim"));
            var scrimShape = UI51Build.Solid(scrim, UI51Tokens.Scrim, 0f, 0f, default, true);
            UI51Build.Button(scrimShape, scrimShape);
            UI51Build.GetOrAdd<CanvasGroup>(scrim);

            var safe = UI51Build.Child(article, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;

            // Scende di Bleed oltre la safe area (fascia dell'indicatore home), il contenuto resta dov'e'.
            // Largo quanto l'area utile (431 su iPhone 12) piu' 1 per lato: i bordi laterali escono dallo schermo.
            var sheet = UI51Build.Child(safe, "Sheet");
            sheet.anchorMin = Vector2.zero;
            sheet.anchorMax = new Vector2(1f, 0f);
            sheet.pivot = new Vector2(0.5f, 0f);
            sheet.sizeDelta = new Vector2(2f, SheetH + Bleed);
            sheet.anchoredPosition = new Vector2(0f, -Bleed);
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(sheet, "Shadow")), UI51Tokens.Navy, 0f, 0f, default, false,
                new UI51Shadow(0f, -12f, 40f, UI51Tokens.BlackA(0.5f))).radii = UI51Tokens.RadiiTop(24f);
            var face = UI51Build.Stretch(UI51Build.Child(sheet, "Face"));
            UI51Build.Shape(face, UI51Shape.Linear((UI51Tokens.Rgba(12, 26, 50, 0.98f), 0f), (UI51Tokens.Rgba(6, 13, 27, 0.99f), 1f)), 180f,
                UI51Tokens.RadiiTop(24f), 0f, Color.clear, true);
            UI51Build.GetOrAdd<Mask>(face).showMaskGraphic = true;

            var hero = UI51AccessBuilder.TopBand(UI51Build.Child(face, "Hero"), 0f, 0f, 0f, HeroH);
            UI51Build.GetOrAdd<RectMask2D>(hero); // la foto copre la testata e ne uscirebbe sotto
            item = UI51Build.GetOrAdd<UI51NewsItem>(hero);
            Arts(hero, item, 390f, HeroH);

            // Testo: da sotto la testata a 12 sopra il pulsante (52, 24 dal fondo).
            var scrollRt = UI51Build.Stretch(UI51Build.Child(face, "Body"), 0f, Bleed + 24f + 52f + 12f, 0f, HeroH);
            scroll = ScrollArea(scrollRt, UI51Build.Pad(16, 20, 12, 20), 8f);
            var meta = UI51Build.Child(scroll.content, "Meta");
            UI51Build.Layout(meta, -1f, 20f);
            UI51Build.Row(meta, 8f, null, TextAnchor.MiddleLeft, true, false);
            Pill(meta, item, 20f, 8f, 9f, 1f);
            item.date = Label(meta, "Date", 11f, UI51Tokens.CreamA(0.5f), 20f);
            var head = UI51Build.Text(UI51Build.Child(scroll.content, "Title"), "Titolo", FontFace.CinzelBold, 21f, UI51Tokens.Cream);
            head.enableWordWrapping = true;
            head.overflowMode = TextOverflowModes.Overflow;
            item.title = head;
            paragraphs = UI51Build.Child(scroll.content, "Paragraphs");
            UI51MetaBuilder.Stack(paragraphs, 8f);
            paragraph = UI51Build.Text(UI51Build.Child(paragraphs, "ParagraphTemplate"), "", FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.75f));
            UI51MetaBuilder.Wrap(paragraph, 20f); // ~line-height 1.6 del mockup (come i Termini)

            var ctaRt = UI51Build.Child(face, "Cta");
            UI51PrefabBuilder.GoldBody(ctaRt.gameObject, 350f, 52f, UI51Tokens.RadiusButton, FontFace.CinzelBold, 14f, 2f, "HO CAPITO");
            ctaRt.anchorMin = Vector2.zero; // left/right 20 come nel mockup, anche sull'area larga 431
            ctaRt.anchorMax = new Vector2(1f, 0f);
            ctaRt.pivot = new Vector2(0.5f, 0f);
            ctaRt.offsetMin = new Vector2(21f, Bleed + 24f); // +1: il foglio esce di 1 per lato
            ctaRt.offsetMax = new Vector2(-21f, Bleed + 24f + 52f);
            cta = ctaRt.GetComponent<Button>();
            ctaLabel = ctaRt.Find("Label").GetComponent<TextMeshProUGUI>();

            UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(sheet, "Border")), UI51Shape.Solid(Color.clear), 180f, UI51Tokens.RadiiTop(24f), 1f,
                UI51Tokens.GoldA(0.4f));
            var close = UI51AccessBuilder.RoundButton(sheet, "Close", true, 34f, 17f, UI51Tokens.Rgba(6, 13, 27, 0.6f),
                UI51Build.Sprite("Common", "ic_close_cream"), 12f);
            close.GetComponent<UI51Shape>().borderColor = UI51Tokens.GoldA(0.35f);
            UI51Build.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(34f, 34f), new Vector2(-14f, -14f));

            var bottomSheet = UI51Build.GetOrAdd<BottomSheet>(article);
            var body = scroll.content;
            UI51Build.Wire(bottomSheet, so =>
            {
                UI51Build.Ref(so, "m_Scrim", scrim);
                UI51Build.Ref(so, "m_Sheet", sheet);
                UI51Build.Ref(so, "m_Content", body);
                UI51Build.Ref(so, "m_Title", null);
                UI51Build.Ref(so, "m_Subtitle", null);
                UI51Build.Ref(so, "m_CloseButton", close);
                UI51Build.Bool(so, "m_CloseOnScrim", true);
            });
            article.gameObject.SetActive(false);
            return bottomSheet;
        }

        /// <summary>Scheda del carosello (featured) o riga dell'elenco, con UI51NewsItem.</summary>
        static UI51NewsItem Item(RectTransform parent, string name, bool featured)
        {
            var rt = UI51Build.Child(parent, name);
            var item = UI51Build.GetOrAdd<UI51NewsItem>(rt);
            UI51Build.GetOrAdd<UI51Press>(rt);
            if (featured)
            {
                UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(rt, "Shadow")), UI51Tokens.Navy, 20f, 0f, default, false,
                    new UI51Shadow(0f, 12f, 28f, UI51Tokens.BlackA(0.5f)));
                var face = UI51Build.Stretch(UI51Build.Child(rt, "Face"));
                var faceShape = UI51Build.Solid(face, Color.white, 20f, 0f, default, true);
                UI51Build.GetOrAdd<Mask>(face).showMaskGraphic = false;
                item.button = UI51Build.Button(rt, faceShape);
                Arts(face, item, CardW, CardH);

                var texts = UI51Build.Child(face, "Texts");
                texts.anchorMin = Vector2.zero;
                texts.anchorMax = new Vector2(1f, 0f);
                texts.pivot = new Vector2(0.5f, 0f);
                texts.offsetMin = new Vector2(16f, 14f);
                texts.offsetMax = new Vector2(-16f, 14f);
                UI51MetaBuilder.Stack(texts, 6f);
                UI51Build.Fit(texts, false, true);
                var tagRow = UI51Build.Child(texts, "TagRow");
                UI51Build.Layout(tagRow, -1f, 20f);
                UI51Build.Row(tagRow, 0f, null, TextAnchor.MiddleLeft, true, false);
                Pill(tagRow, item, 20f, 8f, 9f, 1f);
                var title = UI51Build.Text(UI51Build.Child(texts, "Title"), "Titolo", FontFace.CinzelBold, 20f, UI51Tokens.Cream);
                title.enableWordWrapping = true;
                title.overflowMode = TextOverflowModes.Overflow;
                item.title = title;
                var sub = UI51Build.Child(texts, "Subtitle");
                UI51Build.Layout(sub, -1f, 17f);
                item.subtitle = UI51MetaBuilderClip(UI51Build.Text(sub, "", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.75f)));

                UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(rt, "Border")), Color.clear, 20f, 1f, UI51Tokens.GoldA(0.45f));
                return item;
            }

            var hit = UI51Build.Image(rt, null, Color.clear, true, false);
            item.button = UI51Build.Button(rt, hit);
            var line = UI51Build.Child(rt, "Line");
            line.anchorMin = Vector2.zero;
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.offsetMin = Vector2.zero;
            line.offsetMax = new Vector2(0f, 1f);
            UI51Build.Image(line, null, UI51Tokens.GoldA(0.08f), false, false);

            var thumb = UI51Build.Place(UI51Build.Child(rt, "Thumb"), new Vector2(0f, 0.5f), new Vector2(54f, 54f), new Vector2(12f, 0f));
            item.thumb = UI51Build.Shape(thumb, UI51Shape.Solid(UI51Tokens.GoldA(0.12f)), 120f, UI51Tokens.Radii(12f), 1f, UI51Tokens.GoldA(0.3f));
            UI51Build.GetOrAdd<Mask>(thumb).showMaskGraphic = true;
            item.thumbIcon = UI51Build.Image(UI51Build.Center(UI51Build.Child(thumb, "Icon"), 26f, 26f), UI51Build.Sprite("Common", "ic_globe_cream"), Color.white);

            var col = UI51Build.Stretch(UI51Build.Child(rt, "Texts"), 78f, 0f, 12f, 0f);
            UI51Build.Column(col, 4f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            var meta = UI51Build.Child(col, "Meta");
            UI51Build.Layout(meta, -1f, 16f);
            UI51Build.Row(meta, 6f, null, TextAnchor.MiddleLeft, true, false);
            Pill(meta, item, 16f, 6f, 8f, 0.8f);
            item.date = Label(meta, "Date", 10f, UI51Tokens.CreamA(0.45f), 16f);
            var dot = UI51Build.Size(UI51Build.Child(meta, "New"), 7f, 7f);
            UI51Build.Layout(dot, 7f, 7f);
            UI51Build.Solid(dot, UI51Tokens.Danger, 3.5f);
            item.newDot = dot.gameObject;
            var title2 = UI51Build.Child(col, "Title");
            UI51Build.Layout(title2, -1f, 20f);
            item.title = UI51MetaBuilderClip(UI51Build.Text(title2, "Titolo", FontFace.NunitoExtraBold, 14f, UI51Tokens.Cream));
            var sub2 = UI51Build.Child(col, "Subtitle");
            UI51Build.Layout(sub2, -1f, 16f);
            item.subtitle = UI51MetaBuilderClip(UI51Build.Text(sub2, "", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f)));
            return item;
        }

        /// <summary>
        /// Sfondi del mockup (art-2v2, art-torneo, art-dorso), la sfumatura scura in basso comune ai tre e l'immagine inclinata.
        /// I bagliori radiali del CSS sono Bagliore_morbido colorato, largo il doppio del raggio (45% / 50% del lato lontano).
        /// </summary>
        static void Arts(RectTransform host, UI51NewsItem item, float w, float h)
        {
            var photo = UI51Build.Stretch(UI51Build.Child(host, "ArtPhoto"));
            var bgSprite = UI51Build.Sprite("Backgrounds", "home_bg_base");
            var img = UI51Build.Image(UI51Build.Child(photo, "Image"), bgSprite, Color.white, false, false);
            var fitter = UI51Build.GetOrAdd<AspectRatioFitter>(img);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = bgSprite != null ? bgSprite.rect.width / bgSprite.rect.height : 1153f / 2048f;
            // background-position center 40%: sopra resta il 40% dell'eccedenza, sotto il 60% (pivot dal basso).
            img.rectTransform.pivot = new Vector2(0.5f, 0.6f);

            var torneo = UI51Build.Stretch(UI51Build.Child(host, "ArtTorneo"));
            UI51Build.Shape(torneo, UI51Shape.Linear((UI51Tokens.Navy, 0f), (UI51Tokens.Hex("#1B6B8F"), 0.5f), (UI51Tokens.Hex("#6B3FA0"), 1f)), 120f,
                Vector4.zero, 0f, Color.clear);
            Glow(torneo, w, h, 0.7f, 0.4f, 0.45f, UI51Tokens.GoldA(0.45f));

            var dorso = UI51Build.Stretch(UI51Build.Child(host, "ArtDorso"));
            UI51Build.Shape(dorso, UI51Shape.Linear((UI51Tokens.Hex("#0E4F3A"), 0f), (UI51Tokens.Hex("#062519"), 1f)), 135f, Vector4.zero, 0f, Color.clear);
            Glow(dorso, w, h, 0.7f, 0.45f, 0.5f, UI51Tokens.Rgba(39, 181, 133, 0.5f));

            // linear-gradient(180deg, transparent 30%, rgba(6,13,27,.92) 100%)
            var dark = UI51Tokens.Rgba(6, 13, 27, 0.92f);
            UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(host, "Fade")),
                UI51Shape.Linear((UI51Tokens.WithAlpha(dark, 0f), 0f), (UI51Tokens.WithAlpha(dark, 0f), 0.3f), (dark, 1f)), 180f, Vector4.zero, 0f, Color.clear);

            var pic = UI51Build.Child(host, "Pic");
            pic.anchorMin = pic.anchorMax = new Vector2(1f, 1f);
            pic.pivot = new Vector2(0.5f, 0.5f);
            item.pic = UI51Build.Image(pic, null, Color.white);
            item.pic.gameObject.SetActive(false);
            item.arts = new[] { photo.gameObject, torneo.gameObject, dorso.gameObject };
        }

        /// <summary>Freccia del carosello: tondo 32 a 8 dal bordo, 84 dall'alto; Next e' l'icona indietro specchiata.</summary>
        static Button Arrow(RectTransform carousel, string name, bool right)
        {
            var b = UI51AccessBuilder.RoundButton(carousel, name, right, 32f, 16f, UI51Tokens.Rgba(6, 13, 27, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 11f);
            b.GetComponent<UI51Shape>().borderColor = UI51Tokens.GoldA(0.35f);
            UI51Build.Place((RectTransform)b.transform, new Vector2(right ? 1f : 0f, 1f), new Vector2(32f, 32f), new Vector2(right ? -8f : 8f, -84f));
            b.transform.Find("Icon").localScale = new Vector3(right ? -1f : 1f, 1f, 1f);
            return b;
        }

        /// <summary>
        /// radial-gradient(circle at x% y%, color, transparent r%): r% della distanza dall'angolo piu' lontano. Fatto con l'ombra
        /// sfumata di un punto trasparente (UI51Shape), liscia come il CSS: Bagliore_morbido ha raggi e scintille.
        /// </summary>
        static void Glow(RectTransform parent, float w, float h, float fx, float fy, float r, Color color)
        {
            float cx = fx * w, cy = fy * h;
            float far = Mathf.Sqrt(Mathf.Pow(Mathf.Max(cx, w - cx), 2f) + Mathf.Pow(Mathf.Max(cy, h - cy), 2f));
            float radius = r * far, core = 0.2f; // l'ombra non si disegna sotto la forma: punto invisibile
            var glow = UI51Build.Child(parent, "Glow");
            UI51Build.Remove<Image>(glow.gameObject);
            glow.anchorMin = glow.anchorMax = new Vector2(0f, 1f);
            glow.pivot = new Vector2(0.5f, 0.5f);
            glow.sizeDelta = new Vector2(core, core);
            glow.anchoredPosition = new Vector2(cx, -cy);
            // Ombra di un punto: al centro vale meta' dell'alpha (bordo sfumato), quindi alpha doppia; sfuma a zero verso radius.
            var shadow = UI51Tokens.WithAlpha(color, Mathf.Min(1f, color.a * 2f));
            UI51Build.Solid(glow, Color.clear, core * 0.5f, 0f, default, false, new UI51Shadow(0f, 0f, radius * 2f, shadow));
        }

        /// <summary>Pillola dell'etichetta: alta h, raggio h/2, margini laterali pad, testo ExtraBold maiuscolo.</summary>
        static void Pill(RectTransform row, UI51NewsItem item, float h, float pad, float size, float spacing)
        {
            var pill = UI51Build.Size(UI51Build.Child(row, "Pill"), 0f, h);
            item.tagPill = UI51Build.Solid(pill, UI51Tokens.Gold, h * 0.5f);
            UI51Build.Row(pill, 0f, UI51Build.Pad(0, (int)pad, 0, (int)pad), TextAnchor.MiddleCenter, true, true);
            item.tagLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(pill, "Text"), "NOVITÀ", FontFace.NunitoExtraBold, size, UI51Tokens.OnGold,
                TextAlignmentOptions.Center, spacing));
        }

        static TextMeshProUGUI Label(RectTransform row, string name, float size, Color color, float h) =>
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(row, name), 0f, h), "", FontFace.NunitoRegular, size, color,
                TextAlignmentOptions.MidlineLeft));

        /// <summary>Una riga sola, con i puntini se non entra.</summary>
        static TextMeshProUGUI UI51MetaBuilderClip(TextMeshProUGUI t)
        {
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        /// <summary>ScrollRect verticale: Viewport (RectMask2D) e Content in colonna, altezza dai figli.</summary>
        static ScrollRect ScrollArea(RectTransform rt, RectOffset padding, float spacing)
        {
            var scroll = UI51Build.GetOrAdd<ScrollRect>(rt);
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            var viewport = UI51Build.Stretch(UI51Build.Child(rt, "Viewport"));
            UI51Build.Image(viewport, null, Color.clear, true, false);
            UI51Build.GetOrAdd<RectMask2D>(viewport);
            var content = UI51Build.Child(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            UI51MetaBuilder.Stack(content, spacing, padding);
            UI51Build.Fit(content, false, true);
            scroll.viewport = viewport;
            scroll.content = content;
            return scroll;
        }
    }
}
