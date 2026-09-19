using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Core;

namespace Project51.EditorTools
{
    /// <summary>
    /// Finestra di lettura dei documenti legali (Termini di servizio, Privacy Policy), costruita
    /// dentro il ModalHost gia' esistente della UI V2 e basata su AnimatedModalV2 come gli altri
    /// pannelli: non introduce una seconda architettura di modal.
    ///
    /// Ha un Canvas proprio con ordine 2200 perche' deve poter comparire sopra alla schermata di
    /// accesso, che mentre e' visibile si alza a 2000 (sotto al caricamento, che sta a 3000).
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string LegalTermsPath = "Assets/Legal/TerminiDiServizio.txt";
        private const string LegalPrivacyPath = "Assets/Legal/PrivacyPolicy.txt";

        private static readonly Color LegalBodyText = new Color32(212, 223, 240, 255);

        /// <summary>Costruisce la finestra e la restituisce, pronta da collegare alle schermate auth.</summary>
        private static LegalModalV2 BuildLegalModal(TMP_FontAsset bold, TMP_FontAsset extraBold)
        {
            var host = Object.FindObjectOfType<UIV2ModalHost>(true);
            if (host == null) throw new System.Exception("UIV2ModalHost non trovato in MainMenu");

            var old = host.transform.Find("LegalV2");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = CreateUIObject("LegalV2", host.transform);
            StretchFill(root);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2200;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            // Si apre sopra alla schermata di accesso, che mentre e' visibile rende non interattivi gli
            // altri Canvas (AuthUIController.GateGameUI) compreso UIV2_Home che contiene il ModalHost:
            // senza questo la X e il velo ereditano interactable=false e funziona solo Esc.
            group.ignoreParentGroups = true;

            var dim = CreateUIObject("Dim", root);
            StretchFill(dim);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = OnDim;
            dimImage.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.targetGraphic = dimImage;

            var design = CreateUIObject("Design", root);
            design.gameObject.AddComponent<DesignCanvasFit>();
            design.sizeDelta = new Vector2(1080f, 1920f);

            const float frameTop = 210f;
            const float frameBottom = 1720f;
            var frame = MockRect(design, "Frame", 45f, frameTop, 990f, frameBottom - frameTop);
            AddRoundedPanel(frame, "panel_fill_r24", 48f, 44f, OnFrameBorder, 9f, OnFrameFill, out _, out _);
            frame.GetComponent<Image>().raycastTarget = true;

            MockSprite(design, "Ribbon", LoadSprite(IconsPath, "ribbon_teal"), 240f, frameTop - 44f, 600f, 190f, false);
            var title = MockText(design, "Title", "TERMINI DI SERVIZIO", 280f, frameTop + 14f, 520f, 58f, 30f,
                FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            UseFont(title, extraBold, NavyOutlineMaterial());
            title.enableAutoSizing = true;
            title.fontSizeMin = 20f;
            title.fontSizeMax = 30f;

            var close = MockButton(design, "Close", "sq_blue", 935f, frameTop + 36f, 80f, 72f, "", 0f, null);
            MockSprite(close.transform, "Icon", LoadSprite(IconsPath, "ic_x"), 21f, 17f, 38f, 38f, false);

            // Area di lettura: viewport ritagliato + contenuto che cresce con il testo. Comincia
            // sotto le code del nastro, che scendono dentro alla cornice e coprirebbero la prima riga.
            const float readingTop = 178f;
            var viewport = MockRect(frame, "Viewport", 40f, readingTop, 910f, frameBottom - frameTop - readingTop - 130f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f); // superficie per il trascinamento
            viewportImage.raycastTarget = true;

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            var body = AddText(content, "", 22f, FontStyles.Normal, LegalBodyText, TextAlignmentOptions.TopLeft);
            body.enableWordWrapping = true;
            body.lineSpacing = 8f;
            body.paragraphSpacing = 14f;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;

            var openOnWeb = MockButton(design, "OpenOnWeb", "btn_blue_long", 300f, frameBottom - 112f, 480f, 68f,
                "APRI SUL SITO", 20f, NavyOutlineMaterial());
            UseFont(openOnWeb.GetComponentInChildren<TMP_Text>(), bold, NavyOutlineMaterial());

            var animated = root.gameObject.AddComponent<AnimatedModalV2>();
            animated.Group = group;
            animated.Frame = frame;
            animated.CloseButton = close;
            animated.Dimmer = dimButton;

            var legal = root.gameObject.AddComponent<LegalModalV2>();
            legal.Modal = animated;
            legal.Title = title;
            legal.Body = body;
            legal.Scroll = scroll;
            legal.OpenOnWeb = openOnWeb;
            legal.Terms = AssetDatabase.LoadAssetAtPath<TextAsset>(LegalTermsPath);
            legal.Privacy = AssetDatabase.LoadAssetAtPath<TextAsset>(LegalPrivacyPath);
            if (legal.Terms == null || legal.Privacy == null)
                Debug.LogError("[UIV2FoundationBuilder] Testi legali non trovati in Assets/Legal - la finestra si aprira' vuota.");

            root.gameObject.SetActive(false); // AnimatedModalV2 considera aperto = oggetto attivo
            return legal;
        }
    }
}
