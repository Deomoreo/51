using Project51.UIV2.Components;
using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Pagina Novita' (C3, mockup 25) in MainMenu: canvas proprio sopra alla schermata iniziale (1100,
    /// sotto all'accesso 2000 e alle finestre legali 2200), fondale e pulsante indietro delle schermate
    /// Accesso/Registrazione, elenco scorrevole di schede. Collega il pulsante "Novita'" della schermata
    /// iniziale, che prima era disattivato. Rilanciabile.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private static readonly Color NewsTitleCream = new Color32(255, 236, 190, 255);
        private static readonly Color NewsCardFill = new Color32(12, 33, 54, 255);
        private static readonly Color NewsIconFill = new Color32(20, 44, 68, 255);
        private static readonly Color NewsBadgeRed = new Color32(214, 58, 58, 255);
        private static readonly Color NewsBody = new Color32(170, 195, 220, 255);
        private static readonly Color NewsTime = new Color32(120, 148, 178, 255);
        private static readonly Color NewsEnd = new Color32(128, 152, 184, 255);

        private const float NewsCardHeight = 230f;

        [MenuItem("Tools/UIV2/Build News Screen")]
        private static void BuildNewsScreen()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var start = Object.FindObjectOfType<StartScreenV2>(true);
            if (start == null) throw new System.Exception("StartScreenV2 non trovato in MainMenu");
            var startCanvas = start.GetComponent<Canvas>();
            var startScaler = start.GetComponent<CanvasScaler>();

            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsRegularPath);
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsBoldPath);
            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            if (regular == null || bold == null || extraBold == null) throw new System.Exception("Font Poppins non trovati");

            var old = GameObject.Find("NewsV2");
            if (old != null) Object.DestroyImmediate(old);

            var rootGo = new GameObject("NewsV2", typeof(RectTransform));
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = startCanvas.renderMode;
            canvas.worldCamera = startCanvas.worldCamera;
            canvas.sortingOrder = 1100;
            var scaler = rootGo.AddComponent<CanvasScaler>();
            if (startScaler != null) EditorUtility.CopySerialized(startScaler, scaler);
            rootGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)rootGo.transform;
            var screen = rootGo.AddComponent<NewsScreenV2>();
            screen.View = rootGo.AddComponent<CanvasGroup>();

            // Fondale di Accesso/Registrazione; blocca i tocchi verso la schermata iniziale.
            var background = Stretch(root, "Background").gameObject.AddComponent<Image>();
            background.color = Color.white;
            background.material = AssetDatabase.LoadAssetAtPath<Material>(FlowBackdropPath);
            background.raycastTarget = true;

            var design = CreateUIObject("Design", root);
            design.gameObject.AddComponent<DesignCanvasFit>();
            design.sizeDelta = new Vector2(1080f, 1920f);

            screen.Back = BackButton(design, "Back", 78f, 96f, 88f);
            var title = MockText(design, "Title", "NOVITÀ", 190f, 71f, 700f, 50f, 35f, FontStyles.Normal, NewsTitleCream, TextAlignmentOptions.Center);
            UseFont(title, extraBold, extraBold.material);

            // Elenco: viewport ritagliato dal bordo alto delle schede al fondo della pagina.
            var viewport = MockRect(design, "Viewport", 0f, 180f, 1080f, 1740f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f); // superficie per il trascinamento
            viewportImage.raycastTarget = true;

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 0, 60);
            layout.spacing = 26f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 40f;
            scroll.decelerationRate = 0.12f;
            screen.Scroll = scroll;
            screen.Content = content;

            screen.ItemTemplate = BuildNewsCard(content, regular, bold, extraBold);

            var end = CreateUIObject("End", content);
            end.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            var endText = AddText(end, "Hai visto tutte le novità", 19f, FontStyles.Normal, NewsEnd, TextAlignmentOptions.Center);
            UseFont(endText, regular, null);
            screen.EndLabel = end.gameObject;
            end.gameObject.SetActive(false);

            screen.Status = MockText(design, "Status", NewsScreenV2.LoadingText, 90f, 560f, 900f, 200f, 26f, FontStyles.Normal, NewsBody, TextAlignmentOptions.Center);
            UseFont(screen.Status, regular, null);
            screen.Status.enableWordWrapping = true;

            // Pulsante "Novita'" della schermata iniziale: era disattivato in attesa di questa pagina.
            Button newsButton = null;
            foreach (var b in start.GetComponentsInChildren<Button>(true)) if (b.name == "NewsButton") newsButton = b;
            if (newsButton == null) throw new System.Exception("NewsButton non trovato nella schermata iniziale");
            newsButton.interactable = true;
            start.NewsButton = newsButton;
            start.News = screen;

            screen.View.alpha = 0f;
            screen.View.blocksRaycasts = false;
            screen.View.interactable = false;

            EditorUtility.SetDirty(start);
            EditorUtility.SetDirty(newsButton);
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Pagina Novità (mockup 25) costruita in MainMenu e collegata al pulsante della schermata iniziale.");
        }

        /// <summary>Scheda del mockup 25, coordinate relative alla scheda (960x230).</summary>
        private static NewsItemViewV2 BuildNewsCard(RectTransform content, TMP_FontAsset regular, TMP_FontAsset bold, TMP_FontAsset extraBold)
        {
            var card = CreateUIObject("ItemTemplate", content);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0f, 1f);
            card.sizeDelta = new Vector2(960f, NewsCardHeight);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = NewsCardHeight;
            var border = AddRoundedPanel(card, "panel_fill_r24", 48f, 24f, new Color32(232, 178, 74, 255), 3f, NewsCardFill, out var fill, out _);

            var iconBox = MockRect(card, "IconBox", 24f, 24f, 180f, 130f);
            AddRoundedPanel(iconBox, "panel_fill_r24", 48f, 18f, NewsIconFill, 0f, NewsIconFill, out _, out _);
            MockSprite(iconBox, "Icon", LoadSprite(IconsPath, "ic_calendar"), 62f, 37f, 56f, 56f, false);

            var badge = MockRect(card, "Badge", 240f, 24f, 104f, 38f);
            AddRoundedPanel(badge, "panel_fill_r24", 48f, 10f, NewsBadgeRed, 0f, NewsBadgeRed, out _, out _);
            var badgeText = AddText(Stretch(badge, "Label"), "NUOVO", 16f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            UseFont(badgeText, bold, null);

            // Blocco testo: sale di BadgeShift quando manca l'etichetta (vedi NewsItemViewV2.Bind).
            var text = MockRect(card, "Text", 240f, 0f, 700f, NewsCardHeight);
            var title = MockText(text, "Title", "Titolo", 0f, 59f, 700f, 40f, 24f, FontStyles.Normal, NewsTitleCream, TextAlignmentOptions.MidlineLeft);
            UseFont(title, extraBold, extraBold.material);
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.enableWordWrapping = false;
            title.richText = false;
            var body = MockText(text, "Body", "Testo", 0f, 105f, 700f, 68f, 19f, FontStyles.Normal, NewsBody, TextAlignmentOptions.TopLeft);
            UseFont(body, regular, null);
            body.enableWordWrapping = true;
            body.overflowMode = TextOverflowModes.Ellipsis;
            body.richText = false;
            var time = MockText(text, "Time", "oggi", 0f, 141f, 700f, 30f, 17f, FontStyles.Normal, NewsTime, TextAlignmentOptions.MidlineLeft);
            UseFont(time, regular, null);

            var view = card.gameObject.AddComponent<NewsItemViewV2>();
            view.Border = border;
            view.Fill = fill;
            view.Badge = badge.gameObject;
            view.TextBlock = text;
            view.Title = title;
            view.Body = body;
            view.Time = time;
            card.gameObject.SetActive(false);
            return view;
        }
    }
}
