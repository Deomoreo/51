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
    /// UI51 Fase 8 su MainMenu, ogni pagina con la sua radice (canvas sopra la Home e sotto i flussi online):
    /// Notizie (mockup Notizie e NotizieArticolo, radice UI51News con UI51NewsView, pulsante Notizie sotto Posta nella Home) e
    /// Posta (mockup Posta e PostaMessaggio, radice UI51Mail con UI51MailView, aperta dal pulsante Posta che c'era gia') e
    /// Premi giornalieri (mockup Premi e PremiRiscattato, radice UI51Rewards con UI51RewardsView, dal pulsante Premi).
    /// Idempotente: rieseguirlo riusa i nodi per nome.
    /// </summary>
    public static class UI51SocialBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Fase 8]";
        const int SortingOrder = 700; // sopra UIV2_Home (0), sotto OnlineFlowV2 (800) e StartScreenV2 (1000)
        const float CardW = 350f, CardH = 230f, HeroH = 210f, SheetH = 700f, Bleed = 80f;

        [MenuItem("Tools/UI51/Build Fase 8 (Notizie, Posta, Premi, Amici)")]
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

            var news = BuildNewsButton(home, out var friendsButton);
            BuildNews(home, pager, homeCanvas);
            BuildMail(home, homeCanvas);
            BuildRewards(home, homeCanvas);
            BuildFriends(home, homeCanvas);
            UI51Build.Wire(home, so =>
            {
                UI51Build.Ref(so, "newsButton", news);
                UI51Build.Ref(so, "friendsButton", friendsButton);
            });

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath)) Debug.Log($"{Tag} Scena salvata: {ScenePath}");
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        /// <summary>
        /// Colonna sinistra del mockup Home: Amici, Posta, Missioni, Notizie (passo 76). Missioni resta nascosto: Amici in cima,
        /// Posta sotto (spostata qui e in UI51HomeBuilder), Notizie sotto Posta.
        /// </summary>
        static UIV2QuickActionButton BuildNewsButton(HomeScreenV2 home, out UIV2QuickActionButton friends)
        {
            var c = UI51HomeBuilder.Container(home.transform);
            float top = 118f - UI51HomeBuilder.TopBarH;
            friends = UI51HomeBuilder.QuickAction(c, "Friends", "ic_person_cream", 21f, "Amici", 14f, top);
            UI51Build.Size((RectTransform)friends.transform.Find("Circle/Icon"), 18f, 21f); // 18x21 nel mockup
            var mail = c.Find("Mail") as RectTransform;
            if (mail != null) mail.anchoredPosition = new Vector2(14f, -(top + 76f));
            return UI51HomeBuilder.QuickAction(c, "News", "ic_globe_cream", 22f, "Notizie", 14f, top + 152f);
        }

        /// <summary>Radice di una pagina: canvas come quello della Home (ordine SortingOrder), CanvasGroup spento.</summary>
        internal static GameObject Root(HomeScreenV2 home, Canvas homeCanvas, string name, out CanvasGroup group, int order = SortingOrder)
        {
            // Non GameObject.Find: "UI51News" trova anche il percorso UI51/News (il pulsante della Home).
            GameObject rootGo = null;
            foreach (var go in home.gameObject.scene.GetRootGameObjects()) if (go.name == name) rootGo = go;
            if (rootGo == null) rootGo = new GameObject(name, typeof(RectTransform));
            rootGo.layer = UI51Build.UILayer;
            var canvas = UI51Build.GetOrAdd<Canvas>(rootGo);
            canvas.renderMode = homeCanvas.renderMode;
            canvas.worldCamera = homeCanvas.worldCamera;
            canvas.sortingOrder = order;
            var scaler = UI51Build.GetOrAdd<CanvasScaler>(rootGo);
            var homeScaler = homeCanvas.GetComponent<CanvasScaler>();
            if (homeScaler != null) EditorUtility.CopySerialized(homeScaler, scaler);
            UI51Build.GetOrAdd<GraphicRaycaster>(rootGo);
            group = UI51Build.GetOrAdd<CanvasGroup>(rootGo);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return rootGo;
        }

        static void BuildNews(HomeScreenV2 home, UIV2Pager pager, Canvas homeCanvas)
        {
            var rootGo = Root(home, homeCanvas, "UI51News", out var group);
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

        // --- Posta (mockup Posta e PostaMessaggio)

        static void BuildMail(HomeScreenV2 home, Canvas homeCanvas)
        {
            var rootGo = Root(home, homeCanvas, "UI51Mail", out var group);
            var view = UI51Build.GetOrAdd<UI51MailView>(rootGo);
            var safe = UI51AccessBuilder.BuildScreen(rootGo.transform, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);

            // Testata: indietro 40 a 20/22, "Posta" Cinzel 22 col conteggio sotto, Raccogli tutto (pillola d'oro alta 36).
            var header = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Header"), 20f, 20f, 22f, 40f);
            UI51Build.Row(header, 12f, null, TextAnchor.MiddleLeft, true, false);
            var back = UI51AccessBuilder.RoundButton(header, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            UI51Build.Layout(back, 40f);
            var titles = UI51Build.Size(UI51Build.Child(header, "Titles"), 0f, 43f);
            UI51Build.Layout(titles, -1f, -1f, 1f);
            UI51Build.Column(titles, 2f, null, TextAnchor.MiddleLeft, true, true);
            var title = UI51Build.Child(titles, "Title");
            UI51Build.Layout(title, -1f, 24f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(title, "Posta", FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var unreadRt = UI51Build.Child(titles, "Unread");
            UI51Build.Layout(unreadRt, -1f, 17f);
            var unread = UI51AccessBuilder.NoWrap(UI51Build.Text(unreadRt, "Tutto letto", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.55f),
                TextAlignmentOptions.MidlineLeft));
            var claimAllRt = UI51Build.Child(header, "ClaimAll");
            UI51PrefabBuilder.GoldBody(claimAllRt.gameObject, 112f, 36f, 18f, FontFace.NunitoExtraBold, 12f, 0f, "Raccogli tutto");
            // Larga quanto il testo + 14 per lato (padding del mockup), anche quando dice "Presto in arrivo".
            UI51Build.Row(claimAllRt, 0f, UI51Build.Pad(0, 14, 0, 14), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            var claimAllLabel = claimAllRt.Find("Label").GetComponent<TextMeshProUGUI>();

            // Elenco da top 88, largo 350 (20 per lato), scorre fino a sopra la nota in fondo.
            var scrollRt = UI51Build.Stretch(UI51Build.Child(safe, "Scroll"), 0f, 53f, 0f, 88f);
            var scroll = ScrollArea(scrollRt, UI51Build.Pad(0, 20, 12, 20), 0f);
            var panel = UI51Build.Child(scroll.content, "Panel");
            UI51Build.Shape(panel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51MetaBuilder.Stack(panel, 0f, UI51Build.Pad(4, 0, 4, 0));
            var row = MailRow(panel);
            UI51Build.Layout(row, -1f, 76f);

            var foot = UI51Build.Child(safe, "Footer");
            foot.anchorMin = Vector2.zero;
            foot.anchorMax = new Vector2(1f, 0f);
            foot.pivot = new Vector2(0.5f, 0f);
            foot.offsetMin = new Vector2(0f, 30f);
            foot.offsetMax = new Vector2(0f, 46f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(foot, "I messaggi vengono eliminati dopo 30 giorni", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.4f), TextAlignmentOptions.Center));

            var status = UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Status"), 30f, 30f, 140f, 80f), UI51MailView.LoadingText,
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Top);
            UI51MetaBuilder.Wrap(status, 8f);

            var empty = BuildMailEmpty(safe, out var backToPlay);
            var message = BuildMessage(rootGo.transform, out var detail, out var giftsBlock, out var giftsGroup, out var slots, out var slotIcons,
                out var slotLabels, out var claim, out var claimLabel, out var claimed, out var close);

            var kindIcons = new Object[]
            {
                UI51Build.Sprite("Common", "ic_mail"), UI51Build.Sprite("Common", "chest_purple"), UI51Build.Sprite("Avatars", "av_2"),
                UI51Build.Sprite("Common", "ic_warn_cream"), UI51Build.Sprite("Common", "medal_trophy"),
            };
            var giftIcons = new Object[] { UI51Build.Sprite("Common", "ic_coin"), UI51Build.Sprite("Common", "ic_gem"), UI51Build.Sprite("Common", "chest_purple") };

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "view", group);
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "home", home);
                UI51Build.Ref(so, "scroll", scroll);
                UI51Build.Ref(so, "status", status);
                UI51Build.Ref(so, "unreadLabel", unread);
                UI51Build.Ref(so, "claimAll", claimAllRt.GetComponent<Button>());
                UI51Build.Ref(so, "claimAllLabel", claimAllLabel);
                UI51Build.Ref(so, "listBlock", panel.gameObject);
                UI51Build.Ref(so, "rows", panel);
                UI51Build.Ref(so, "rowTemplate", row);
                UI51Build.Ref(so, "emptyBlock", empty);
                UI51Build.Ref(so, "backToPlay", backToPlay);
                UI51Build.Ref(so, "sheet", message);
                UI51Build.Ref(so, "detail", detail);
                UI51Build.Ref(so, "giftsBlock", giftsBlock);
                UI51Build.Ref(so, "giftsGroup", giftsGroup);
                UI51AccessBuilder.SetArray(so, "giftSlots", slots);
                UI51AccessBuilder.SetArray(so, "giftImages", slotIcons);
                UI51AccessBuilder.SetArray(so, "giftLabels", slotLabels);
                UI51Build.Ref(so, "claim", claim);
                UI51Build.Ref(so, "claimLabel", claimLabel);
                UI51Build.Ref(so, "claimedBadge", claimed);
                UI51Build.Ref(so, "closeButton", close);
                UI51AccessBuilder.SetArray(so, "kindIcons", kindIcons);
                UI51AccessBuilder.SetArray(so, "giftIcons", giftIcons);
            });
        }

        // --- Premi giornalieri (mockup Premi e PremiRiscattato)

        static void BuildRewards(HomeScreenV2 home, Canvas homeCanvas)
        {
            var rootGo = Root(home, homeCanvas, "UI51Rewards", out var group);
            var view = UI51Build.GetOrAdd<UI51RewardsView>(rootGo);
            var safe = UI51AccessBuilder.BuildScreen(rootGo.transform, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);

            var header = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Header"), 20f, 20f, 22f, 40f);
            UI51Build.Row(header, 12f, null, TextAnchor.MiddleLeft, true, false);
            var back = UI51AccessBuilder.RoundButton(header, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            UI51Build.Layout(back, 40f);
            var title = UI51Build.Size(UI51Build.Child(header, "Title"), 0f, 40f);
            UI51Build.Layout(title, -1f, -1f, 1f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(title, "Premi giornalieri", FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));

            // Serie di accessi (top 82): a sinistra la serie, a destra il tempo che resta.
            var streak = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Streak"), 20f, 20f, 82f, 62f);
            UI51Build.Shape(streak, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            var left = UI51Build.Stretch(UI51Build.Child(streak, "Left"), 16f, 0f, 16f, 0f);
            UI51Build.Column(left, 3f, null, TextAnchor.MiddleLeft, true, true);
            UI51Build.Layout(UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(left, "Caption"), "SERIE DI ACCESSI", FontFace.CinzelSemiBold, 10f,
                UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f)), -1f, 14f);
            var streakLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(left, "Value"), "0 giorni di fila", FontFace.NunitoExtraBold, 15f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(streakLabel, -1f, 20f);
            var right = UI51Build.Stretch(UI51Build.Child(streak, "Right"), 16f, 0f, 16f, 0f);
            UI51Build.Column(right, 3f, null, TextAnchor.MiddleRight, true, true);
            var timerCaption = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(right, "Caption"), "Il premio scade tra", FontFace.NunitoRegular, 10f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.MidlineRight));
            UI51Build.Layout(timerCaption, -1f, 14f);
            var timer = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(right, "Timer"), "00:00:00", FontFace.CinzelBold, 15f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineRight));
            UI51Build.Layout(timer, -1f, 20f);

            // Giorni 1-6: due file da 3 (top 168), tessere alte 136 a 10 l'una dall'altra.
            var grid = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Days"), 20f, 20f, 168f, 282f);
            UI51Build.Column(grid, 10f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var days = new Object[6];
            for (int r = 0; r < 2; r++)
            {
                var row = UI51Build.Child(grid, "Row" + r);
                UI51Build.Layout(row, -1f, 136f);
                var hg = UI51Build.Row(row, 10f, null, TextAnchor.MiddleCenter, true, true);
                hg.childForceExpandWidth = hg.childForceExpandHeight = true;
                for (int c = 0; c < 3; c++) days[r * 3 + c] = DayTile(row, r * 3 + c + 1);
            }

            BuildGrandPrize(safe, out var grandPulse, out var grandChest, out var grandNote);

            // Avviso (top 640): bordo tratteggiato nel mockup, qui pieno e tenue (UI51Shape non fa il tratteggio).
            var note = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Note"), 20f, 20f, 640f, 56f);
            UI51Build.Solid(note, UI51Tokens.Rgba(6, 13, 27, 0.5f), 14f, 1f, UI51Tokens.GoldA(0.3f));
            UI51Build.Image(UI51Build.Place(UI51Build.Child(note, "Icon"), new Vector2(0f, 0.5f), new Vector2(20f, 18f), new Vector2(14f, 0f)),
                UI51Build.Sprite("Common", "ic_warn_cream"), UI51Tokens.WhiteA(0.8f));
            var noteText = UI51Build.Text(UI51Build.Stretch(UI51Build.Child(note, "Text"), 44f, 0f, 14f, 0f),
                "Se salti un giorno la serie ricomincia dal giorno 1. Dopo il giorno 7 si riparte con premi più ricchi.", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.65f), TextAlignmentOptions.MidlineLeft);
            UI51MetaBuilder.Wrap(noteText, 4f); // ~line-height 1.45

            // In fondo (bottom 30, alto 56): RISCATTA IL PREMIO DI OGGI oppure, riscattato, TORNA A GIOCARE.
            var claimRt = UI51Build.Child(safe, "Claim");
            UI51PrefabBuilder.GoldBody(claimRt.gameObject, 350f, 56f, 16f, FontFace.CinzelBold, 15f, 2f, "RISCATTA IL PREMIO DI OGGI",
                new UI51Shadow(0f, 10f, 24f, UI51Tokens.BlackA(0.45f)));
            BottomBand(claimRt, 30f, 56f);
            var again = UI51Build.Child(safe, "PlayAgain");
            UI51PrefabBuilder.ButtonBody(again.gameObject, 350f, 56f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(16f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.CinzelBold, 14f, 2f, UI51Tokens.Gold, "TORNA A GIOCARE");
            BottomBand(again, 30f, 56f);

            // Riscatto: bagliore 420 al 42% dell'altezza, moneta 74 e "+150" che salgono dal 34%.
            var party = UI51Build.Stretch(UI51Build.Child(safe, "Celebration"));
            var burst = UI51Build.Child(party, "Burst");
            burst.anchorMin = burst.anchorMax = new Vector2(0.5f, 1f);
            burst.pivot = new Vector2(0.5f, 0.5f);
            burst.sizeDelta = new Vector2(420f, 420f);
            burst.anchoredPosition = new Vector2(0f, -0.42f * UI51Tokens.ReferenceResolution.y);
            UI51Build.Image(burst, UI51Build.Sprite("Common", "Bagliore_morbido"), Color.white, false, false);
            var rise = UI51Build.Child(party, "Rise");
            rise.anchorMin = rise.anchorMax = rise.pivot = new Vector2(0.5f, 1f);
            rise.sizeDelta = new Vector2(300f, 126f);
            rise.anchoredPosition = new Vector2(0f, -0.34f * UI51Tokens.ReferenceResolution.y);
            UI51Build.GetOrAdd<CanvasGroup>(rise).blocksRaycasts = false;
            UI51Build.Column(rise, 8f, null, TextAnchor.UpperCenter, false, false);
            var riseIcon = UI51Build.Image(UI51Build.Size(UI51Build.Child(rise, "Icon"), 74f, 74f), UI51Build.Sprite("Common", "ic_coin"), Color.white);
            var riseLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(rise, "Label"), 300f, 44f), "+150", FontFace.CinzelBold, 34f,
                UI51Tokens.GoldLight, TextAlignmentOptions.Center));
            UI51Build.GetOrAdd<CanvasGroup>(party).blocksRaycasts = false;
            party.gameObject.SetActive(false);

            var rewardIcons = new Object[] { UI51Build.Sprite("Common", "ic_coin"), UI51Build.Sprite("Common", "ic_gem"), UI51Build.Sprite("Common", "chest_green"),
                UI51Build.Sprite("Common", "chest_purple") };

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "view", group);
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "home", home);
                UI51Build.Ref(so, "streakLabel", streakLabel);
                UI51Build.Ref(so, "timerCaption", timerCaption);
                UI51Build.Ref(so, "timerLabel", timer);
                UI51AccessBuilder.SetArray(so, "days", days);
                UI51Build.Ref(so, "grandPulse", grandPulse);
                UI51Build.Ref(so, "grandChest", grandChest);
                UI51Build.Ref(so, "grandNote", grandNote);
                UI51Build.Ref(so, "claim", claimRt.GetComponent<Button>());
                UI51Build.Ref(so, "claimLabel", claimRt.Find("Label").GetComponent<TextMeshProUGUI>());
                UI51Build.Ref(so, "playAgain", again.GetComponent<Button>());
                UI51Build.Ref(so, "celebration", party.gameObject);
                UI51Build.Ref(so, "burst", burst);
                UI51Build.Ref(so, "rise", rise);
                UI51Build.Ref(so, "riseIcon", riseIcon);
                UI51Build.Ref(so, "riseLabel", riseLabel);
                UI51AccessBuilder.SetArray(so, "rewardIcons", rewardIcons);
            });
        }

        // --- Amici (mockup Amici e AmiciRichieste)

        static readonly string[] FriendAvatars = { "avatar_1", "av_2", "av_3", "av_4", "av_5", "av_6", "av_7", "av_8" };

        static void BuildFriends(HomeScreenV2 home, Canvas homeCanvas)
        {
            var roomFlow = Object.FindObjectOfType<RoomFlowV2>(true);
            if (roomFlow == null) Debug.LogError($"{Tag} RoomFlowV2 non trovato: Invita ed ENTRA non funzioneranno.");
            var rootGo = Root(home, homeCanvas, "UI51Friends", out var group);
            var view = UI51Build.GetOrAdd<UI51FriendsView>(rootGo);
            var safe = UI51AccessBuilder.BuildScreen(rootGo.transform, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);

            // Testata: indietro, "Amici", pallino verde + "N online" (12 ExtraBold crema .6).
            var header = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Header"), 20f, 20f, 22f, 40f);
            UI51Build.Row(header, 12f, null, TextAnchor.MiddleLeft, true, false);
            var back = UI51AccessBuilder.RoundButton(header, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f),
                UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            UI51Build.Layout(back, 40f);
            var title = UI51Build.Size(UI51Build.Child(header, "Title"), 0f, 40f);
            UI51Build.Layout(title, -1f, -1f, 1f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(title, "Amici", FontFace.CinzelBold, 22f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var online = UI51Build.Size(UI51Build.Child(header, "Online"), 0f, 40f);
            UI51Build.Row(online, 5f, null, TextAnchor.MiddleRight, true, false);
            UI51Build.Solid(UI51Build.Size(UI51Build.Child(online, "Dot"), 8f, 8f), UI51Tokens.Success, 4f);
            UI51Build.Layout(online.Find("Dot"), 8f, 8f);
            var onlineLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(online, "Label"), 0f, 17f), "0 online",
                FontFace.NunitoExtraBold, 12f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineRight));
            UI51Build.Layout(onlineLabel, -1f, 17f);

            // Aggiungi (top 80): AGGIUNGI PER NOME e "Il tuo: nome" con copia; casella + AGGIUNGI alti 42.
            var addPanel = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Add"), 20f, 20f, 80f, 100f);
            UI51Build.Shape(addPanel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Column(addPanel, 10f, UI51Build.Pad(12, 14, 12, 14), TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var top = UI51Build.Child(addPanel, "Top");
            UI51Build.Layout(top, -1f, 24f).flexibleHeight = 0f;
            UI51Build.Row(top, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = true;
            var caption = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(top, "Caption"), "AGGIUNGI PER NOME", FontFace.CinzelSemiBold, 10f,
                UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f));
            UI51Build.Layout(caption, 0f, -1f, 1f);
            var mine = UI51MetaBuilderClip(UI51Build.Text(UI51Build.Child(top, "Mine"), "Il tuo: —", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.55f), TextAlignmentOptions.MidlineRight));
            mine.richText = true;
            UI51Build.Layout(mine, -1f, -1f, 0f, 0f);
            var copyRt = UI51Build.Child(top, "Copy");
            UI51Build.Layout(copyRt, 24f, 24f);
            var copyShape = UI51Build.Solid(copyRt, UI51Tokens.GoldA(0.1f), 7f, 0f, default, true);
            var copy = UI51Build.Button(copyShape, copyShape);
            UI51Build.GetOrAdd<UI51Press>(copyRt);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(copyRt, "Icon"), 12f, 12f), UI51Build.Sprite("Common", "ic_copy_cream"), Color.white);

            var inputRow = UI51Build.Child(addPanel, "InputRow");
            UI51Build.Layout(inputRow, -1f, 42f).flexibleHeight = 0f;
            UI51Build.Row(inputRow, 8f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = true;
            var input = UI51AccessBuilder.BuildInput(inputRow, "Name", "Nome del giocatore", null, TMP_InputField.ContentType.Standard, 42f);
            UI51Build.Layout(input, 0f, 42f, 1f, 0f);
            input.GetComponent<UI51Shape>().radii = UI51Tokens.Radii(12f);
            input.transform.Find("Icon").gameObject.SetActive(false); // il mockup non ha l'icona
            UI51Build.Stretch((RectTransform)input.transform.Find("TextArea"), 12f, 0f, 12f, 0f);
            input.characterLimit = 25; // nomi PlayFab 3-25
            foreach (var t in new[] { (TextMeshProUGUI)input.textComponent, (TextMeshProUGUI)input.placeholder })
                UI51Tokens.Style(t, FontFace.NunitoExtraBold, 14f, t == input.textComponent ? UI51Tokens.Cream : UI51Tokens.CreamA(0.4f), 1f);
            var addRt = UI51Build.Child(inputRow, "AddButton");
            UI51PrefabBuilder.GoldBody(addRt.gameObject, 110f, 42f, 12f, FontFace.CinzelBold, 12f, 1f, "AGGIUNGI");
            UI51Build.Row(addRt, 0f, UI51Build.Pad(0, 16, 0, 16), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;

            // Schede (top 194, alte 46): Amici e Richieste col numero.
            var tabsRt = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Tabs"), 20f, 20f, 194f, 46f);
            UI51Build.Solid(tabsRt, UI51Tokens.Rgba(6, 13, 27, 0.7f), 12f, 1f, UI51Tokens.GoldA(0.18f));
            var tg = UI51Build.Row(tabsRt, 4f, UI51Build.Pad(4, 4, 4, 4), TextAnchor.MiddleCenter, true, true);
            tg.childForceExpandWidth = tg.childForceExpandHeight = true;
            string[] tabNames = { "Amici", "Richieste" };
            var tabButtons = new Object[2];
            var tabShapes = new Object[2];
            var tabLabels = new Object[2];
            var tabCounts = new Object[2];
            for (int i = 0; i < 2; i++)
            {
                var tab = UI51Build.Child(tabsRt, "Tab" + i);
                UI51Build.Layout(tab, 0f, -1f, 1f);
                var shape = UI51Build.Solid(tab, UI51Tokens.GoldA(0.16f), 9f, 1f, UI51Tokens.GoldA(0.7f), true);
                shape.color = i == 0 ? Color.white : Color.clear; // la tinta spegne anche il bordo
                tabShapes[i] = shape;
                tabButtons[i] = UI51Build.Button(shape, shape);
                UI51Build.Row(tab, 6f, null, TextAnchor.MiddleCenter, true, true);
                var label = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(tab, "Label"), tabNames[i], FontFace.NunitoExtraBold, 13f, UI51Tokens.Gold,
                    TextAlignmentOptions.Center));
                UI51Build.Layout(label, -1f, 18f);
                tabLabels[i] = label;
                var badge = UI51Build.Child(tab, "Badge");
                UI51Build.Layout(badge, -1f, 18f, -1f, 18f);
                UI51Build.Solid(badge, UI51Tokens.WhiteA(0.1f), 9f);
                UI51Build.Row(badge, 0f, UI51Build.Pad(0, 5, 0, 5), TextAnchor.MiddleCenter, true, true);
                var count = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(badge, "Count"), "0", FontFace.NunitoExtraBold, 10f,
                    i == 0 ? UI51Tokens.Cream : Color.white, TextAlignmentOptions.Center));
                UI51Build.Layout(count, -1f, 18f);
                tabCounts[i] = count;
            }

            // Elenco (top 250): pannello con righe alte 64, scorre fino in fondo.
            var scrollRt = UI51Build.Stretch(UI51Build.Child(safe, "Scroll"), 0f, 0f, 0f, 250f);
            var scroll = ScrollArea(scrollRt, UI51Build.Pad(0, 20, 24, 20), 0f);
            var panel = UI51Build.Child(scroll.content, "Panel");
            UI51Build.Shape(panel, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51MetaBuilder.Stack(panel, 0f, UI51Build.Pad(4, 0, 4, 0));
            var row = FriendRow(panel);
            UI51Build.Layout(row, -1f, 64f);

            var status = UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Status"), 30f, 30f, 270f, 80f), UI51FriendsView.LoadingText,
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Top);
            UI51MetaBuilder.Wrap(status, 8f);

            var empty = BuildFriendsEmpty(safe, out var share);
            var banner = BuildInviteBanner(home, homeCanvas);
            var avatars = new Object[FriendAvatars.Length];
            for (int i = 0; i < avatars.Length; i++) avatars[i] = UI51Build.Sprite("Avatars", FriendAvatars[i]);

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "view", group);
                UI51Build.Ref(so, "back", back);
                UI51Build.Ref(so, "home", home);
                UI51Build.Ref(so, "roomFlow", roomFlow);
                UI51Build.Ref(so, "quickPanels", Object.FindObjectOfType<QuickSelectionPanels>(true));
                UI51Build.Ref(so, "onlineLabel", onlineLabel);
                UI51Build.Ref(so, "myLabel", mine);
                UI51Build.Ref(so, "copy", copy);
                UI51Build.Ref(so, "nameInput", input);
                UI51Build.Ref(so, "add", addRt.GetComponent<Button>());
                UI51AccessBuilder.SetArray(so, "tabs", tabButtons);
                UI51AccessBuilder.SetArray(so, "tabShapes", tabShapes);
                UI51AccessBuilder.SetArray(so, "tabLabels", tabLabels);
                UI51AccessBuilder.SetArray(so, "tabCounts", tabCounts);
                UI51Build.Ref(so, "scroll", scroll);
                UI51Build.Ref(so, "listBlock", panel.gameObject);
                UI51Build.Ref(so, "rows", panel);
                UI51Build.Ref(so, "rowTemplate", row);
                UI51Build.Ref(so, "status", status);
                UI51AccessBuilder.SetArray(so, "avatars", avatars);
                UI51Build.Ref(so, "emptyBlock", empty);
                UI51Build.Ref(so, "share", share);
                UI51Build.Ref(so, "inviteBanner", banner);
            });
        }

        /// <summary>
        /// Riga alta 64 (padding 0 12, stacco 12): avatar 44 con anello (oro online, crema .25 offline) e pallino di stato 12,
        /// nome 14 + "Liv. N", stato 11; a destra Invita (oro), Invitato (contorno) oppure Occupato / Offline.
        /// </summary>
        static UI51FriendItem FriendRow(RectTransform parent)
        {
            var rt = UI51Build.Child(parent, "RowTemplate");
            var item = UI51Build.GetOrAdd<UI51FriendItem>(rt);
            var line = UI51Build.Child(rt, "Line");
            line.anchorMin = Vector2.zero;
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.offsetMin = Vector2.zero;
            line.offsetMax = new Vector2(0f, 1f);
            UI51Build.Image(line, null, UI51Tokens.GoldA(0.08f), false, false);
            UI51Build.Layout(line, -1f, -1f, -1f, -1f, true);
            UI51Build.Row(rt, 12f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleLeft, true, false);

            var slot = UI51Build.Child(rt, "AvatarSlot");
            UI51Build.Layout(slot, 44f, 44f);
            UI51Build.Size(slot, 44f, 44f);
            var avatarRt = UI51Build.Child(slot, "Avatar");
            item.avatar = UI51PrefabBuilder.BuildAvatar(avatarRt.gameObject, 44f, 2f, FrameStyle.Oro, UI51Build.Sprite("Avatars", "av_2"), 30f);
            var dot = UI51Build.Place(UI51Build.Child(slot, "Dot"), new Vector2(1f, 0f), new Vector2(12f, 12f), new Vector2(1f, 0f));
            item.dot = UI51Build.Solid(dot, Color.white, 6f, 2f, UI51Tokens.BadgeRing);
            item.dot.color = UI51Tokens.Success;

            var texts = UI51Build.Size(UI51Build.Child(rt, "Texts"), 0f, 44f);
            UI51Build.Layout(texts, 0f, 44f, 1f, 0f);
            UI51Build.Column(texts, 3f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            var topRow = UI51Build.Child(texts, "Top");
            UI51Build.Layout(topRow, -1f, 20f).flexibleHeight = 0f;
            UI51Build.Row(topRow, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = false;
            item.title = UI51MetaBuilderClip(UI51Build.Text(UI51Build.Child(topRow, "Name"), "Giulia", FontFace.NunitoExtraBold, 14f, UI51Tokens.Cream,
                TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(item.title, -1f, 20f, 0f, 0f);
            var pill = UI51Build.Child(topRow, "Level");
            UI51Build.Layout(pill, -1f, 16f);
            UI51Build.Solid(pill, UI51Tokens.GoldA(0.15f), 8f);
            UI51Build.Row(pill, 0f, UI51Build.Pad(0, 5, 0, 5), TextAnchor.MiddleCenter, true, true);
            item.level = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(pill, "Label"), "Liv. 15", FontFace.NunitoExtraBold, 9f, UI51Tokens.Gold,
                TextAlignmentOptions.Center));
            UI51Build.Layout(item.level, -1f, 16f);
            var st = UI51Build.Child(texts, "Status");
            UI51Build.Layout(st, -1f, 16f);
            item.status = UI51MetaBuilderClip(UI51Build.Text(st, "Online", FontFace.NunitoRegular, 11f, UI51Tokens.SuccessText, TextAlignmentOptions.MidlineLeft));

            var actions = UI51Build.Size(UI51Build.Child(rt, "Actions"), 0f, 32f); // la riga non controlla le altezze
            UI51Build.Layout(actions, -1f, 32f);
            UI51Build.Row(actions, 0f, null, TextAnchor.MiddleRight, true, true).childForceExpandHeight = true;
            var inviteRt = UI51Build.Child(actions, "Invite");
            UI51PrefabBuilder.GoldBody(inviteRt.gameObject, 70f, 32f, 16f, FontFace.NunitoExtraBold, 12f, 0f, "Invita");
            UI51Build.Row(inviteRt, 0f, UI51Build.Pad(0, 14, 0, 14), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            item.invite = inviteRt.GetComponent<Button>();
            var invitedRt = UI51Build.Child(actions, "Invited");
            UI51Build.Solid(invitedRt, Color.clear, 16f, 1f, UI51Tokens.GoldA(0.5f));
            UI51Build.Row(invitedRt, 0f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(invitedRt, "Label"), "Invitato", FontFace.NunitoExtraBold, 12f, UI51Tokens.Gold,
                TextAlignmentOptions.Center));
            item.invited = invitedRt.gameObject;
            var busyRt = UI51Build.Child(actions, "Busy");
            UI51Build.Solid(busyRt, UI51Tokens.WhiteA(0.05f), 16f);
            UI51Build.Row(busyRt, 0f, UI51Build.Pad(0, 10, 0, 10), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            item.busyLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(busyRt, "Label"), "Offline", FontFace.NunitoBold, 11f,
                UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center));
            item.busy = busyRt.gameObject;
            item.invited.SetActive(false);
            item.busy.SetActive(false);
            return item;
        }

        /// <summary>
        /// Invito ricevuto (mockup InvitoRicevuto), su un canvas suo (ordine 900) sopra Home, pagine e flusso online: banner a 12 dal
        /// bordo alto e dai lati, alto 146, raggio 20, padding 12 14. Righe a 14 di stacco: avatar 48 + nome / stanza + secondi, barra 4,
        /// Rifiuta (contorno crema) e ACCETTA (oro) alti 42 in proporzione 1 : 1.4.
        /// </summary>
        static UI51InviteBanner BuildInviteBanner(HomeScreenV2 home, Canvas homeCanvas)
        {
            var rootGo = Root(home, homeCanvas, "UI51Invite", out var group);
            rootGo.GetComponent<Canvas>().sortingOrder = 900;
            group.alpha = 1f; // il banner si spegne da solo: la radice resta visibile e prende i tocchi solo sul banner
            group.blocksRaycasts = group.interactable = true;
            var banner = UI51Build.GetOrAdd<UI51InviteBanner>(rootGo);
            var ui = UI51Build.Stretch(UI51Build.Child(rootGo.transform, "UI51"));
            var safe = UI51Build.Child(ui, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;
            var oldDialog = safe.Find("Dialog"); // fino alla 2.42 l'invito era un dialog
            if (oldDialog != null) Object.DestroyImmediate(oldDialog.gameObject);

            var panel = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Banner"), 12f, 12f, 12f, 146f);
            UI51Build.Shape(panel, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(20f), 1f, UI51Tokens.GoldA(0.5f), true, UI51Tokens.ShadowDialog);
            UI51Build.Column(panel, 14f, UI51Build.Pad(12, 14, 12, 14), TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;

            var top = UI51Build.Child(panel, "Top");
            UI51Build.Layout(top, -1f, 48f);
            UI51Build.Row(top, 12f, null, TextAnchor.MiddleLeft, true, true);
            var slot = UI51Build.Child(top, "AvatarSlot");
            UI51Build.Layout(slot, 48f, 48f);
            var avatar = UI51PrefabBuilder.BuildAvatar(UI51Build.Child(slot, "Avatar").gameObject, 48f, 2f, FrameStyle.Oro,
                UI51Build.Sprite("Avatars", "av_2"), 30f);
            var texts = UI51Build.Child(top, "Texts");
            UI51Build.Layout(texts, 0f, 48f, 1f, 0f);
            UI51Build.Column(texts, 2f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            var title = UI51MetaBuilderClip(UI51Build.Text(UI51Build.Child(texts, "Title"), "Giulia ti invita a giocare", FontFace.NunitoExtraBold, 14f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(title, -1f, 20f); // Ellipsis: almeno 14 x 1.37
            var sub = UI51MetaBuilderClip(UI51Build.Text(UI51Build.Child(texts, "Subtitle"), "Stanza privata · 2 vs 2", FontFace.NunitoRegular, 12f,
                UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(sub, -1f, 17f);
            var secs = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(top, "Seconds"), "20s", FontFace.CinzelBold, 13f, UI51Tokens.Gold,
                TextAlignmentOptions.MidlineRight));
            UI51Build.Layout(secs, -1f, 18f);

            var bar = UI51Build.Child(panel, "Bar");
            UI51Build.Layout(bar, -1f, 4f);
            UI51Build.Solid(bar, UI51Tokens.WhiteA(0.12f), 2f);
            var fill = UI51Build.Stretch(UI51Build.Child(bar, "Fill"));
            fill.pivot = new Vector2(0f, 0.5f);
            UI51Build.Shape(fill, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)), 90f, UI51Tokens.Radii(2f), 0f, Color.clear);

            var buttons = UI51Build.Child(panel, "Buttons");
            UI51Build.Layout(buttons, -1f, 42f);
            UI51Build.Row(buttons, 8f, null, TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            var declineRt = UI51Build.Child(buttons, "Decline");
            UI51PrefabBuilder.ButtonBody(declineRt.gameObject, 100f, 42f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(12f), 1f, UI51Tokens.CreamA(0.3f),
                FontFace.NunitoExtraBold, 13f, 0f, UI51Tokens.Cream, "Rifiuta");
            UI51Build.Layout(declineRt, 0f, 42f, 1f);
            var acceptRt = UI51Build.Child(buttons, "Accept");
            UI51PrefabBuilder.GoldBody(acceptRt.gameObject, 140f, 42f, 12f, FontFace.CinzelBold, 12f, 2f, "ACCETTA");
            UI51Build.Layout(acceptRt, 0f, 42f, 1.4f);
            panel.gameObject.SetActive(false);

            UI51Build.Wire(banner, so =>
            {
                UI51Build.Ref(so, "panel", panel);
                UI51Build.Ref(so, "avatar", avatar);
                UI51Build.Ref(so, "title", title);
                UI51Build.Ref(so, "subtitle", sub);
                UI51Build.Ref(so, "countdown", secs);
                UI51Build.Ref(so, "fill", fill);
                UI51Build.Ref(so, "accept", acceptRt.GetComponent<Button>());
                UI51Build.Ref(so, "decline", declineRt.GetComponent<Button>());
            });
            return banner;
        }

        /// <summary>
        /// Stato vuoto (mockup AmiciVuoto e PostaVuota): colonna centrata a 30 dai lati, da top, figli a 10 di stacco: disegno artW x artH,
        /// titolo Cinzel 18, testo 13 (line-height 1.5). Il pulsante lo aggiunge chi chiama (EmptyButtonRow). Parte spento.
        /// </summary>
        static RectTransform EmptyColumn(RectTransform safe, float top, float artW, float artH, string title, string text, out RectTransform art)
        {
            var rt = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Empty"), 30f, 30f, top, 330f);
            UI51Build.Column(rt, 10f, null, TextAnchor.UpperCenter, true, true);
            art = UI51Build.Child(rt, "Art");
            UI51Build.Layout(art, artW, artH);
            var t = UI51Build.Text(UI51Build.Child(rt, "Title"), title, FontFace.CinzelBold, 18f, UI51Tokens.Cream, TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(t, 0f);
            UI51Build.Layout(t, -1f, -1f, 1f);
            var b = UI51Build.Text(UI51Build.Child(rt, "Text"), text, FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(b, 13.6f); // line-height 1.5
            UI51Build.Layout(b, -1f, -1f, 1f);
            return rt;
        }

        /// <summary>Riga del pulsante dello stato vuoto: margin-top del mockup come padding, pulsante largo quanto il contenuto.</summary>
        static RectTransform EmptyButtonRow(RectTransform empty, float marginTop, float h)
        {
            var row = UI51Build.Child(empty, "ButtonRow");
            UI51Build.Layout(row, -1f, marginTop + h, 1f).flexibleHeight = 0f; // la riga con childForceExpandHeight allargherebbe la colonna
            UI51Build.Row(row, 0f, UI51Build.Pad((int)marginTop, 0, 0, 0), TextAnchor.LowerCenter, true, true).childForceExpandHeight = true;
            return row;
        }

        /// <summary>Cerchio tratteggiato (border dashed del CSS, che UI51Shape non fa): trattini 3x lo spessore, passo 6x.</summary>
        static void DashedRing(RectTransform rt, float diameter, float stroke, Color color)
        {
            float r = (diameter - stroke) * 0.5f;
            int n = Mathf.Max(8, Mathf.RoundToInt(2f * Mathf.PI * r / (stroke * 6f)));
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                var d = UI51Build.Place(UI51Build.Child(rt, "Dash" + i), new Vector2(0.5f, 0.5f), new Vector2(stroke * 3f, stroke),
                    new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * r);
                d.localRotation = Quaternion.Euler(0f, 0f, a + 90f);
                UI51Build.Image(d, null, color, false, false);
            }
            for (int i = n; rt.Find("Dash" + i) != null; i++) Object.DestroyImmediate(rt.Find("Dash" + i).gameObject);
        }

        /// <summary>Amici senza amici: due cerchi sovrapposti (tratteggiato con la persona, oro col +), testi e "Condividi il tuo nome".</summary>
        static RectTransform BuildFriendsEmpty(RectTransform safe, out Button share)
        {
            var empty = EmptyColumn(safe, 270f, 130f, 110f, "Il tavolo è più bello in compagnia",
                "Non hai ancora amici. Condividi il tuo nome oppure aggiungi chi incontri in partita dal suo profilo.", out var art);
            var dashed = UI51Build.Place(UI51Build.Child(art, "Dashed"), new Vector2(0f, 1f), new Vector2(70f, 70f), new Vector2(10f, -20f));
            DashedRing(dashed, 70f, 1.5f, UI51Tokens.GoldA(0.45f));
            var person = UI51Build.Sprite("Common", "ic_person_cream");
            float ph = person != null ? 30f * person.rect.height / person.rect.width : 30f;
            UI51Build.Image(UI51Build.Center(UI51Build.Child(dashed, "Person"), 30f, ph), person, UI51Tokens.WhiteA(0.55f));
            var plus = UI51Build.Place(UI51Build.Child(art, "Plus"), new Vector2(0f, 1f), new Vector2(70f, 70f), new Vector2(52f, -8f));
            UI51Build.Solid(plus, UI51Tokens.GoldA(0.12f), 35f, 1.5f, UI51Tokens.GoldA(0.55f));
            // "+" Nunito 300 a 30 px: due tratti di 1.8, lunghi 17.
            UI51Build.Solid(UI51Build.Center(UI51Build.Child(plus, "H"), 17f, 1.8f), UI51Tokens.Gold, 0.9f);
            UI51Build.Solid(UI51Build.Center(UI51Build.Child(plus, "V"), 1.8f, 17f), UI51Tokens.Gold, 0.9f);

            var row = EmptyButtonRow(empty, 6f, 44f);
            var shareRt = UI51Build.Child(row, "Share");
            UI51PrefabBuilder.ButtonBody(shareRt.gameObject, 180f, 44f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.NunitoExtraBold, 13f, 0f, UI51Tokens.Gold, "Condividi il tuo nome");
            UI51Build.Row(shareRt, 8f, UI51Build.Pad(0, 18, 0, 18), TextAnchor.MiddleCenter, true, true);
            var icon = UI51Build.Child(shareRt, "Icon");
            icon.SetAsFirstSibling();
            UI51Build.Image(icon, UI51Build.Sprite("Common", "ic_share_cream"), Color.white);
            UI51Build.Layout(icon, 15f, 15f);
            UI51Build.Layout(shareRt.Find("Label"), -1f, 18f);
            share = shareRt.GetComponent<Button>();
            empty.gameObject.SetActive(false);
            return empty;
        }

        /// <summary>Posta vuota: busta 70 (alpha .8) sul bagliore 200 (alpha .4), testi e TORNA A GIOCARE (oro, 48, padding 24).</summary>
        static RectTransform BuildMailEmpty(RectTransform safe, out Button backToPlay)
        {
            var empty = EmptyColumn(safe, 220f, 120f, 110f, "Nessun messaggio",
                "Qui arrivano regali degli amici, premi delle stagioni e avvisi del Team 51.", out var art);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(art, "Glow"), 200f, 200f), UI51Build.Sprite("Common", "Bagliore_morbido"), UI51Tokens.WhiteA(0.4f));
            UI51Build.Image(UI51Build.Center(UI51Build.Child(art, "Icon"), 70f, 70f), UI51Build.Sprite("Common", "ic_mail"), UI51Tokens.WhiteA(0.8f));

            var row = EmptyButtonRow(empty, 8f, 48f);
            var btn = UI51Build.Child(row, "BackToPlay");
            UI51PrefabBuilder.GoldBody(btn.gameObject, 200f, 48f, 14f, FontFace.CinzelBold, 13f, 2f, "TORNA A GIOCARE");
            UI51Build.Row(btn, 0f, UI51Build.Pad(0, 24, 0, 24), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            backToPlay = btn.GetComponent<Button>();
            empty.gameObject.SetActive(false);
            return empty;
        }

        /// <summary>Fascia a left/right 20 (si allarga con l'area utile), a bottom px dal fondo della safe area, alta h.</summary>
        static void BottomBand(RectTransform rt, float bottom, float h)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(20f, bottom);
            rt.offsetMax = new Vector2(-20f, bottom + h);
        }

        /// <summary>
        /// Tessera di un giorno: anello che pulsa (oggi), faccia (colori dalla vista), GIORNO n / OGGI in alto a 8, icona (in un posto
        /// della colonna, cosi' fluttua) e premio, pillola RISCATTA che esce di 11 sotto, spunta verde 24 a 8 dall'angolo.
        /// </summary>
        static UI51RewardDay DayTile(RectTransform row, int n)
        {
            var rt = UI51Build.Child(row, "Day" + n);
            UI51Build.Layout(rt, 0f, -1f, 1f);
            var d = UI51Build.GetOrAdd<UI51RewardDay>(rt);
            d.button = UI51Build.Button(rt, UI51Build.Image(rt, null, Color.clear, true, false));
            UI51Build.GetOrAdd<UI51Press>(rt);
            d.pulse = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(rt, "Pulse")), Color.white, 16f);
            d.pulse.color = UI51Tokens.WithAlpha(UI51Tokens.Gold, 0f); // UIAnim.Pulse anima la tinta
            d.face = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(rt, "Face")), UI51Tokens.Rgba(6, 13, 27, 0.6f), 16f, 1f, UI51Tokens.GoldA(0.2f));
            d.caption = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(rt, "Caption"), 0f, 0f, 8f, 14f), "GIORNO " + n,
                FontFace.CinzelSemiBold, 10f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.Center, 1.5f));

            var content = UI51Build.Stretch(UI51Build.Child(rt, "Content"));
            UI51Build.Column(content, 6f, UI51Build.Pad(10, 0, 0, 0), TextAnchor.MiddleCenter, true, true);
            var slot = UI51Build.Child(content, "IconSlot");
            UI51Build.Layout(slot, 40f, 40f);
            d.icon = UI51Build.Image(UI51Build.Center(UI51Build.Child(slot, "Icon"), 40f, 40f), UI51Build.Sprite("Common", "ic_coin"), Color.white);
            d.label = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(content, "Label"), "50", FontFace.CinzelBold, 13f, UI51Tokens.Cream,
                TextAlignmentOptions.Center));
            UI51Build.Layout(d.label, -1f, 18f);

            var pill = UI51Build.Child(rt, "Claim");
            UI51PrefabBuilder.GoldBody(pill.gameObject, 90f, 24f, 12f, FontFace.CinzelBold, 10f, 1f, "RISCATTA");
            pill.GetComponent<UI51Shape>().raycastTarget = false; // il tocco va alla tessera
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0f);
            pill.anchoredPosition = new Vector2(0f, 1f); // bottom:-11, alta 24
            UI51Build.Row(pill, 0f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Fit(pill, true, false);
            d.claimPill = pill.gameObject;

            var check = UI51Build.Place(UI51Build.Child(rt, "Check"), new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-8f, 8f));
            UI51Build.Solid(check, UI51Tokens.Success, 12f);
            UI51HomeBuilder.CheckMark(check, 12f, 3.4f, Color.white);
            d.check = check.gameObject;
            return d;
        }

        /// <summary>
        /// Giorno 7 (top 470, alto 150): fondo viola a 135 gradi col bagliore al 75%, bordo viola 2, testi a sinistra, forziere viola 118
        /// che fluttua a destra. L'anello pulsa solo quando il gran premio e' quello di oggi.
        /// </summary>
        static RectTransform BuildGrandPrize(RectTransform safe, out UI51Shape pulse, out RectTransform chest, out TextMeshProUGUI note)
        {
            const float W = 350f, H = 150f;
            var violet = UI51Tokens.Rgba(167, 139, 250, 1f);
            var grand = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Grand"), 20f, 20f, 470f, H);
            pulse = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(grand, "Pulse")), Color.white, 20f);
            pulse.color = UI51Tokens.WithAlpha(violet, 0f);
            var face = UI51Build.Stretch(UI51Build.Child(grand, "Face"));
            UI51Build.Shape(face, UI51Shape.Linear((UI51Tokens.Rgba(40, 20, 80, 0.95f), 0f), (UI51Tokens.Rgba(10, 14, 40, 0.97f), 1f)), 135f,
                UI51Tokens.Radii(20f), 0f, Color.clear);
            // overflow:hidden: il bagliore non esce, nemmeno dagli angoli tondi (RectMask2D li lasciava squadrati).
            UI51Build.Remove<RectMask2D>(face.gameObject);
            UI51Build.GetOrAdd<Mask>(face).showMaskGraphic = true;
            Glow(face, W, H, 0.75f, 0.5f, 0.55f, UI51Tokens.WithAlpha(violet, 0.35f));
            UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(grand, "Border")), Color.clear, 20f, 2f, UI51Tokens.WithAlpha(violet, 0.7f));

            var texts = UI51Build.Stretch(UI51Build.Child(grand, "Texts"), 20f, 0f, 20f + 118f + 14f, 0f);
            UI51Build.Column(texts, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            UI51Build.Layout(UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(texts, "Caption"), "GIORNO 7 · GRAN PREMIO", FontFace.CinzelBold, 10f,
                UI51Tokens.Hex("#C4B5FD"), TextAlignmentOptions.MidlineLeft, 2f)), -1f, 14f);
            var prize = UI51Build.Text(UI51Build.Child(texts, "Prize"), "Forziere viola\n+ 25 gemme", FontFace.CinzelBold, 19f, UI51Tokens.Cream,
                TextAlignmentOptions.TopLeft);
            UI51MetaBuilder.Wrap(prize, 0f);
            UI51Build.Layout(prize, -1f, 46f);
            note = UI51MetaBuilderClip(UI51Build.Text(UI51Build.Child(texts, "Note"), "Mancano 6 giorni · non saltare un giorno!", FontFace.NunitoRegular, 11f,
                UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineLeft));
            UI51Build.Layout(note, -1f, 16f);

            var sprite = UI51Build.Sprite("Common", "chest_purple");
            float ch = sprite != null ? 118f * sprite.rect.height / sprite.rect.width : 100f;
            var slot = UI51Build.Place(UI51Build.Child(grand, "ChestSlot"), new Vector2(1f, 0.5f), new Vector2(118f, ch), new Vector2(-20f, 0f));
            chest = UI51Build.Center(UI51Build.Child(slot, "Chest"), 118f, ch);
            UI51Build.Image(chest, sprite, Color.white);
            return grand;
        }

        /// <summary>Riga alta 76: tessera 46 a 12 (pallino rosso se non letto), titolo e ora, anteprima, pillole degli allegati.</summary>
        static UI51MailItem MailRow(RectTransform parent)
        {
            var rt = UI51Build.Child(parent, "RowTemplate");
            var item = UI51Build.GetOrAdd<UI51MailItem>(rt);
            UI51Build.GetOrAdd<UI51Press>(rt);
            item.button = UI51Build.Button(rt, UI51Build.Image(rt, null, Color.clear, true, false));
            var line = UI51Build.Child(rt, "Line");
            line.anchorMin = Vector2.zero;
            line.anchorMax = new Vector2(1f, 0f);
            line.pivot = new Vector2(0.5f, 0f);
            line.offsetMin = Vector2.zero;
            line.offsetMax = new Vector2(0f, 1f);
            UI51Build.Image(line, null, UI51Tokens.GoldA(0.08f), false, false);

            var tile = UI51Build.Place(UI51Build.Child(rt, "Tile"), new Vector2(0f, 0.5f), new Vector2(46f, 46f), new Vector2(12f, 0f));
            Tile(tile, item);
            // 11 + bordo 2 per lato (content-box del CSS) = 15, sporge di 3 in alto a destra.
            var dot = UI51Build.Place(UI51Build.Child(tile, "Unread"), new Vector2(1f, 1f), new Vector2(15f, 15f), new Vector2(3f, 3f));
            UI51Build.Solid(dot, UI51Tokens.Danger, 7.5f, 2f, UI51Tokens.BadgeRing);
            item.unread = dot.gameObject;

            var col = UI51Build.Stretch(UI51Build.Child(rt, "Texts"), 70f, 0f, 12f, 0f);
            UI51Build.Column(col, 3f, null, TextAnchor.MiddleLeft, true, true).childForceExpandWidth = true;
            var top = UI51Build.Child(col, "Top");
            UI51Build.Layout(top, -1f, 20f).flexibleHeight = 0f; // il gruppo con forceExpand si dichiarerebbe flessibile
            UI51Build.Row(top, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = true;
            var t = UI51Build.Child(top, "Title");
            UI51Build.Layout(t, 0f, -1f, 1f);
            item.title = UI51MetaBuilderClip(UI51Build.Text(t, "Titolo", FontFace.NunitoExtraBold, 14f, UI51Tokens.Cream,
                TextAlignmentOptions.BottomLeft));
            item.time = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(top, "Time"), "oggi", FontFace.NunitoRegular, 10f,
                UI51Tokens.CreamA(0.45f), TextAlignmentOptions.BottomRight)); // in basso tutti e due: linee di base quasi allineate (baseline del mockup)
            var sn = UI51Build.Child(col, "Snippet");
            UI51Build.Layout(sn, -1f, 16f);
            item.snippet = UI51MetaBuilderClip(UI51Build.Text(sn, "", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f),
                TextAlignmentOptions.MidlineLeft));

            var chipsRow = UI51Build.Child(col, "Chips");
            UI51Build.Layout(chipsRow, -1f, 20f).flexibleHeight = 0f;
            UI51Build.Row(chipsRow, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = true;
            var gifts = UI51Build.Child(chipsRow, "Gifts");
            UI51Build.Row(gifts, 6f, null, TextAnchor.MiddleLeft, true, true).childForceExpandHeight = true;
            item.chipsGroup = UI51Build.GetOrAdd<CanvasGroup>(gifts);
            item.chips = new GameObject[3];
            item.chipIcons = new Image[3];
            item.chipLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                var chip = UI51Build.Child(gifts, "Chip" + i);
                UI51Build.Solid(chip, UI51Tokens.GoldA(0.12f), 10f);
                UI51Build.Row(chip, 4f, UI51Build.Pad(0, 7, 0, 4), TextAnchor.MiddleLeft, true, true);
                var icon = UI51Build.Image(UI51Build.Child(chip, "Icon"), UI51Build.Sprite("Common", "ic_coin"), Color.white);
                UI51Build.Layout(icon, 14f, 14f);
                item.chips[i] = chip.gameObject;
                item.chipIcons[i] = icon;
                item.chipLabels[i] = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Label"), "+200", FontFace.NunitoExtraBold, 11f,
                    UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft));
            }
            // La spunta disegnata: Nunito e Cinzel non hanno il glifo ✓.
            var claimedRt = UI51Build.Child(chipsRow, "Claimed");
            UI51Build.Remove<TextMeshProUGUI>(claimedRt.gameObject); // prima versione: "✓ Riscattato" in un testo solo
            UI51Build.Row(claimedRt, 3f, null, TextAnchor.MiddleLeft, true, true);
            Check(claimedRt, 9f, 3.4f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(claimedRt, "Label"), "Riscattato", FontFace.NunitoExtraBold, 10f,
                UI51Tokens.SuccessText, TextAlignmentOptions.MidlineLeft));
            item.claimed = claimedRt.gameObject;
            item.expiry = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(chipsRow, "Expiry"), "Scade tra 5 giorni", FontFace.NunitoBold, 10f,
                UI51Tokens.DangerText, TextAlignmentOptions.MidlineLeft));
            return item;
        }

        /// <summary>Spunta verde (SuccessText) in un quadrato size, figlio di una riga.</summary>
        static void Check(RectTransform row, float size, float strokeSvg)
        {
            var box = UI51Build.Child(row, "Check");
            UI51Build.Layout(box, size, size);
            UI51HomeBuilder.CheckMark(box, size, strokeSvg, UI51Tokens.SuccessText);
            box.SetAsFirstSibling();
        }

        /// <summary>Tessera 46 r14 col bordo (colori per tipo da UI51MailView) e l'icona al centro.</summary>
        static void Tile(RectTransform tile, UI51MailItem item)
        {
            item.tile = UI51Build.Solid(tile, UI51Tokens.GoldA(0.12f), 14f, 1f, UI51Tokens.GoldA(0.45f));
            item.icon = UI51Build.Image(UI51Build.Center(UI51Build.Child(tile, "Icon"), 26f, 26f), UI51Build.Sprite("Common", "ic_mail"), Color.white);
        }

        /// <summary>
        /// Messaggio aperto dal basso: velo, foglio alto quanto il contenuto (raggi 24, bordo oro .4 in alto): maniglia, tessera con titolo e
        /// mittente, chiudi 32, testo, ALLEGATI (fino a 3 riquadri alti 88), RISCATTA / RISCATTATO / Chiudi.
        /// </summary>
        static BottomSheet BuildMessage(Transform root, out UI51MailItem detail, out GameObject giftsBlock, out CanvasGroup giftsGroup,
            out Object[] slots, out Object[] slotIcons, out Object[] slotLabels, out Button claim, out TextMeshProUGUI claimLabel,
            out GameObject claimed, out Button close)
        {
            var message = UI51Build.Stretch(UI51Build.Child(root.Find("UI51"), "Message"));
            var scrim = UI51Build.Stretch(UI51Build.Child(message, "Scrim"));
            var scrimShape = UI51Build.Solid(scrim, UI51Tokens.Scrim, 0f, 0f, default, true);
            UI51Build.Button(scrimShape, scrimShape);
            UI51Build.GetOrAdd<CanvasGroup>(scrim);

            var safe = UI51Build.Child(message, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;

            // Opaco: senza il blur del mockup il .97 lascerebbe trasparire l'elenco.
            var fill = UI51Shape.Linear((UI51Tokens.Rgba(12, 26, 50, 1f), 0f), (UI51Tokens.Rgba(6, 13, 27, 1f), 1f));
            var sheet = UI51AccessBuilder.Sheet(safe, UI51Build.Pad(12, 20, 24, 20), 0f, fill);
            UI51Build.Shape(sheet, fill, 180f, UI51Tokens.RadiiTop(24f), 1f, UI51Tokens.GoldA(0.4f), true,
                new UI51Shadow(0f, -12f, 40f, UI51Tokens.BlackA(0.5f)));

            var handle = UI51Build.Child(sheet, "Handle");
            UI51Build.Layout(handle, -1f, 4f);
            UI51Build.Solid(UI51Build.Center(UI51Build.Child(handle, "Bar"), 40f, 4f), UI51Tokens.CreamA(0.25f), 2f);
            UI51MetaBuilder.Gap(sheet, "GapHead", 14f);

            // Testata: tessera 46, titolo Cinzel 17 (va a capo) e "mittente · quando", chiudi 32; allineati in alto.
            var head = UI51Build.Child(sheet, "Head");
            UI51Build.Row(head, 12f, null, TextAnchor.UpperLeft, true, true);
            detail = UI51Build.GetOrAdd<UI51MailItem>(head);
            var tile = UI51Build.Child(head, "Tile");
            UI51Build.Layout(tile, 46f, 46f);
            Tile(tile, detail);
            var titles = UI51Build.Child(head, "Titles");
            UI51Build.Layout(titles, 0f, -1f, 1f);
            UI51MetaBuilder.Stack(titles, 3f);
            var title = UI51Build.Text(UI51Build.Child(titles, "Title"), "Titolo", FontFace.CinzelBold, 17f, UI51Tokens.Cream);
            title.enableWordWrapping = true;
            title.overflowMode = TextOverflowModes.Overflow;
            detail.title = title;
            var from = UI51Build.Child(titles, "From");
            UI51Build.Layout(from, -1f, 16f);
            detail.time = UI51MetaBuilderClip(UI51Build.Text(from, "Team 51 · oggi", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f)));
            var closeRt = UI51Build.Child(head, "Close");
            UI51Build.Layout(closeRt, 32f, 32f);
            var closeShape = UI51Build.Solid(closeRt, UI51Tokens.WhiteA(0.06f), 16f, 0f, default, true);
            var x = UI51Build.Button(closeShape, closeShape);
            UI51Build.GetOrAdd<UI51Press>(closeRt);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(closeRt, "Icon"), 12f, 12f), UI51Build.Sprite("Common", "ic_close_cream"), Color.white);

            UI51MetaBuilder.Gap(sheet, "GapBody", 14f);
            var body = UI51Build.Text(UI51Build.Child(sheet, "Body"), "", FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.75f));
            UI51MetaBuilder.Wrap(body, 16f); // ~line-height 1.55 del mockup
            detail.snippet = body;

            var gifts = UI51Build.Child(sheet, "Gifts");
            UI51MetaBuilder.Stack(gifts, 0f, UI51Build.Pad(16, 0, 0, 0));
            giftsBlock = gifts.gameObject;
            var caption = UI51Build.Child(gifts, "Caption");
            UI51Build.Layout(caption, -1f, 14f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(caption, "ALLEGATI", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f));
            UI51MetaBuilder.Gap(gifts, "GapSlots", 8f);
            var slotRow = UI51Build.Child(gifts, "Slots");
            UI51Build.Layout(slotRow, -1f, 88f);
            var rowGroup = UI51Build.Row(slotRow, 10f, null, TextAnchor.MiddleLeft, true, true);
            rowGroup.childForceExpandWidth = rowGroup.childForceExpandHeight = true;
            giftsGroup = UI51Build.GetOrAdd<CanvasGroup>(slotRow);
            slots = new Object[3];
            slotIcons = new Object[3];
            slotLabels = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = UI51Build.Child(slotRow, "Slot" + i);
                UI51Build.Layout(slot, 0f, -1f, 1f);
                UI51Build.Solid(slot, UI51Tokens.GoldA(0.08f), 14f, 1f, UI51Tokens.GoldA(0.35f));
                // Icona e numero centrati, a 6 l'una dall'altro; le dimensioni dell'icona le decide la vista.
                UI51Build.Column(slot, 6f, null, TextAnchor.MiddleCenter, false, false);
                var icon = UI51Build.Image(UI51Build.Size(UI51Build.Child(slot, "Icon"), 34f, 34f), UI51Build.Sprite("Common", "ic_coin"), Color.white);
                var label = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Size(UI51Build.Child(slot, "Label"), 100f, 18f), "+200", FontFace.CinzelBold, 13f,
                    UI51Tokens.Cream, TextAlignmentOptions.Center));
                slots[i] = slot.gameObject;
                slotIcons[i] = icon;
                slotLabels[i] = label;
            }

            // Un solo pulsante alla volta, 18 sotto: RISCATTA (oro 52), RISCATTATO (verde 52) o Chiudi (contorno oro 48, senza allegati).
            var actions = UI51Build.Child(sheet, "Actions");
            UI51MetaBuilder.Stack(actions, 0f, UI51Build.Pad(18, 0, 0, 0));
            var claimRt = UI51Build.Child(actions, "Claim");
            UI51PrefabBuilder.GoldBody(claimRt.gameObject, 350f, 52f, 16f, FontFace.CinzelBold, 14f, 2f, "RISCATTA");
            UI51Build.Layout(claimRt, -1f, 52f);
            claim = claimRt.GetComponent<Button>();
            claimLabel = claimRt.Find("Label").GetComponent<TextMeshProUGUI>();
            var done = UI51Build.Child(actions, "Claimed");
            UI51Build.Layout(done, -1f, 52f);
            UI51Build.Solid(done, UI51Tokens.Rgba(39, 181, 133, 0.1f), 16f, 1f, UI51Tokens.WithAlpha(UI51Tokens.SuccessText, 0.5f));
            UI51Build.Row(done, 6f, null, TextAnchor.MiddleCenter, true, true);
            Check(done, 13f, 3.2f);
            var doneLabel = UI51Build.Child(done, "Label");
            UI51Build.Layout(doneLabel, -1f, 20f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(doneLabel, "RISCATTATO", FontFace.CinzelBold, 14f, UI51Tokens.SuccessText, TextAlignmentOptions.Center, 1f));
            claimed = done.gameObject;
            var closeBtn = UI51Build.Child(actions, "Close");
            UI51PrefabBuilder.ButtonBody(closeBtn.gameObject, 350f, 48f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.NunitoExtraBold, 14f, 0f, UI51Tokens.Gold, "Chiudi");
            UI51Build.Layout(closeBtn, -1f, 48f);
            close = closeBtn.GetComponent<Button>();

            var bottomSheet = UI51Build.GetOrAdd<BottomSheet>(message);
            UI51Build.Wire(bottomSheet, so =>
            {
                UI51Build.Ref(so, "m_Scrim", scrim);
                UI51Build.Ref(so, "m_Sheet", sheet);
                UI51Build.Ref(so, "m_Content", sheet);
                UI51Build.Ref(so, "m_Title", null);
                UI51Build.Ref(so, "m_Subtitle", null);
                UI51Build.Ref(so, "m_CloseButton", x);
                UI51Build.Bool(so, "m_CloseOnScrim", true);
            });
            message.gameObject.SetActive(false);
            return bottomSheet;
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
        internal static ScrollRect ScrollArea(RectTransform rt, RectOffset padding, float spacing)
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
