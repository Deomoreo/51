using System;
using System.Collections.Generic;
using Project51.Auth;
using Project51.UIV2.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// Fase 2 (Accesso): ricostruisce in stile UI51 Login, Registrazione, Termini/Privacy e Caricamento dentro
    /// MainMenu.unity, riusando gli script esistenti (AuthUIController, AuthScreensV2, LegalModalV2,
    /// AnimatedModalV2, AppLoadingView). Solo grafica e ricollegamento dei campi serializzati.
    /// Idempotente: i nodi sono ritrovati per nome. Coordinate = px del mockup su 390x844.
    /// </summary>
    public static class UI51AccessBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string Tag = "[UI51 Accesso]";

        [MenuItem("Tools/UI51/Build Fase 2 (Accesso)")]
        private static void Menu() => Build();

        public static void Build()
        {
            if (HasDirtyScene()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var auth = UnityEngine.Object.FindObjectOfType<AuthUIController>(true);
            var screens = UnityEngine.Object.FindObjectOfType<AuthScreensV2>(true);
            var legal = UnityEngine.Object.FindObjectOfType<LegalModalV2>(true);
            if (auth == null || screens == null || legal == null)
            {
                Debug.LogError($"{Tag} AuthUIController, AuthScreensV2 o LegalModalV2 non trovati in {ScenePath}. Non tocco nulla.");
                return;
            }

            var authSo = new SerializedObject(auth);
            var loginPanel = PanelRef(authSo, "loginPanel", scene, "LoginPanel");
            var registerPanel = PanelRef(authSo, "registerPanel", scene, "RegisterPanel");
            if (loginPanel == null || registerPanel == null)
            {
                Debug.LogError($"{Tag} LoginPanel o RegisterPanel non trovati. Non tocco nulla.");
                return;
            }

            var login = BuildLogin(loginPanel.transform);
            var register = BuildRegister(registerPanel.transform);
            var legalUi = BuildLegal(legal);
            bool loadingOk = BuildLoading(scene);

            WireAuthScreens(screens, login, register);
            WireAuthController(auth, login, register);
            WireLegal(legal, legalUi);

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene, ScenePath))
                Debug.Log($"{Tag} Scena salvata: {ScenePath}" + (loadingOk ? "" : " (Caricamento lasciato legacy, vedi errore sopra)"));
            else Debug.LogError($"{Tag} Salvataggio fallito: {ScenePath}");
        }

        // --- Riferimenti costruiti

        sealed class LoginUi
        {
            public Button Back, Globe, Forgot, Submit, ToRegister, Guest;
            public TMP_InputField Email, Password;
            public TextMeshProUGUI Status;
        }

        sealed class RegisterUi
        {
            public Button Back, Globe, Submit, ToLogin, TermsToggle, TermsLink, PrivacyLink;
            public TMP_InputField Username, Email, Password, Confirm;
            public GameObject TermsCheck;
            public TextMeshProUGUI Status;
        }

        sealed class LegalUi
        {
            public RectTransform Frame;
            public Button Back, Understood;
            public TextMeshProUGUI Title, Subtitle, Body;
            public ScrollRect Scroll;
            public RectTransform IndexRow, Section;
        }

        // --- Main (login)

        static LoginUi BuildLogin(Transform panel)
        {
            HideChild(panel, "Design");
            var overlay = UI51Shape.Linear(
                (UI51Tokens.Rgba(6, 14, 28, 0.22f), 0f), (UI51Tokens.Rgba(6, 14, 28, 0.08f), 0.26f),
                (UI51Tokens.Rgba(5, 11, 24, 0.52f), 0.54f), (UI51Tokens.Rgba(4, 9, 20, 0.86f), 0.78f),
                (UI51Tokens.Rgba(3, 7, 16, 0.96f), 1f));
            var safe = BuildScreen(panel, UI51Build.Sprite("Backgrounds", "home_bg_base"), overlay);
            var ui = new LoginUi();

            ui.Globe = RoundButton(safe, "Globe", true, 44f, 22f, UI51Tokens.Rgba(11, 29, 58, 0.55f), UI51Build.Sprite("Common", "ic_globe_cream"), 22f);
            ui.Back = RoundButton(safe, "Back", false, 44f, 22f, UI51Tokens.Rgba(11, 29, 58, 0.55f), UI51Build.Sprite("Common", "ic_nav_back_cream"), 16f);

            var glow = UI51Build.Child(safe, "Glow");
            UI51Build.Image(glow, UI51Build.Sprite("Common", "Bagliore_morbido"), new Color(1f, 1f, 1f, 0.85f));
            CenterAt(glow, 195f, 260f, 320f, 320f);
            Logo(safe, 195f, 260f, 182f);

            var sheet = Sheet(safe, UI51Build.Pad(24, 22, 22, 22), 13f, UI51Shape.Linear(
                (UI51Tokens.Rgba(8, 17, 34, 0.40f), 0f), (UI51Tokens.Rgba(7, 15, 30, 0.88f), 0.22f),
                (UI51Tokens.Rgba(5, 11, 23, 0.96f), 1f)));

            ui.Email = BuildInput(sheet, "Email", "Email o nome utente", UI51Build.Sprite("Common", "ic_mail"), TMP_InputField.ContentType.Standard, 50f);
            ui.Password = BuildInput(sheet, "Password", "Password", UI51Build.Sprite("Common", "ic_lock_cream"), TMP_InputField.ContentType.Password, 50f);

            ui.Forgot = Link(sheet, "Forgot", "Password dimenticata?", FontFace.NunitoSemiBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.Right, 14f);
            ui.Status = Status(sheet, "Status");
            ui.Submit = GoldButton(sheet, "Submit", "ACCEDI", 54f, 15f);

            var orRow = UI51Build.Child(sheet, "Or");
            UI51Build.Row(orRow, 10f, null, TextAnchor.MiddleCenter, true, false);
            UI51Build.Layout(orRow, -1f, 14f);
            OrLine(orRow, "LineL");
            var orText = UI51Build.Child(orRow, "Label");
            UI51Build.Size(orText, 0f, 14f);
            NoWrap(UI51Build.Text(orText, "OPPURE", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center, 1f));
            OrLine(orRow, "LineR");

            ui.ToRegister = GhostButton(sheet, "Register", "Registrati", 50f);

            ui.Guest = Link(sheet, "Guest", "Continua come ospite", FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.8f), TextAlignmentOptions.Center, 18f);
            ui.Guest.GetComponent<TMP_Text>().fontStyle = FontStyles.Underline;

            Version(sheet, 0.4f);

            ui.Back.gameObject.SetActive(false);   // AuthScreensV2 lo accende solo quando si arriva dalla Home
            ui.Status.gameObject.SetActive(false);
            return ui;
        }

        // --- Registrazione

        static RegisterUi BuildRegister(Transform panel)
        {
            HideChild(panel, "Design");
            var overlay = UI51Shape.Linear(
                (UI51Tokens.Rgba(6, 14, 28, 0.35f), 0f), (UI51Tokens.Rgba(5, 11, 24, 0.62f), 0.3f),
                (UI51Tokens.Rgba(4, 9, 20, 0.88f), 0.62f), (UI51Tokens.Rgba(3, 7, 16, 0.97f), 1f));
            var safe = BuildScreen(panel, UI51Build.Sprite("Backgrounds", "home_bg_base"), overlay);
            var ui = new RegisterUi();

            ui.Back = RoundButton(safe, "Back", false, 44f, 22f, UI51Tokens.Rgba(11, 29, 58, 0.55f), UI51Build.Sprite("Common", "ic_nav_back_cream"), 16f);
            ui.Globe = RoundButton(safe, "Globe", true, 44f, 22f, UI51Tokens.Rgba(11, 29, 58, 0.55f), UI51Build.Sprite("Common", "ic_globe_cream"), 22f);

            // Header: box 210x135 da top 78
            var glow = UI51Build.Child(safe, "Glow");
            UI51Build.Image(glow, UI51Build.Sprite("Common", "Bagliore_morbido"), new Color(1f, 1f, 1f, 0.8f));
            CenterAt(glow, 195f, 145.5f, 210f, 210f);
            Logo(safe, 195f, 145.5f, 100f);

            var title = UI51Build.Child(safe, "Title");
            TopBand(title, 20f, 20f, 227f, 26f);
            NoWrap(UI51Build.Text(title, "Crea il tuo account", FontFace.CinzelBold, 20f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            var subtitle = UI51Build.Child(safe, "Subtitle");
            TopBand(subtitle, 20f, 20f, 257f, 16f);
            NoWrap(UI51Build.Text(subtitle, "Unisciti alla sfida di Cirulla-51", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Center));

            var sheet = Sheet(safe, UI51Build.Pad(24, 22, 22, 22), 12f, UI51Shape.Linear(
                (UI51Tokens.Rgba(8, 17, 34, 0.35f), 0f), (UI51Tokens.Rgba(7, 15, 30, 0.88f), 0.2f),
                (UI51Tokens.Rgba(5, 11, 23, 0.97f), 1f)));

            ui.Username = BuildInput(sheet, "Username", "Nome utente", UI51Build.Sprite("Common", "ic_person_cream"), TMP_InputField.ContentType.Standard, 48f);
            ui.Email = BuildInput(sheet, "Email", "Email", UI51Build.Sprite("Common", "ic_mail"), TMP_InputField.ContentType.EmailAddress, 48f);
            ui.Password = BuildInput(sheet, "Password", "Password", UI51Build.Sprite("Common", "ic_lock_cream"), TMP_InputField.ContentType.Password, 48f);
            ui.Confirm = BuildInput(sheet, "Confirm", "Conferma password", UI51Build.Sprite("Common", "ic_lock_cream"), TMP_InputField.ContentType.Password, 48f);

            // Riga termini: casella + "Accetto i [Termini di Servizio] e la [Privacy Policy]"
            var terms = UI51Build.Child(sheet, "Terms");
            UI51Build.Row(terms, 9f, null, TextAnchor.MiddleLeft, true, false);
            UI51Build.Layout(terms, -1f, 20f);
            var box = UI51Build.Child(terms, "Box");
            UI51Build.Size(box, 18f, 18f);
            UI51Build.Layout(box, 18f);
            var boxShape = UI51Build.Solid(box, UI51Tokens.WhiteA(0.06f), 5f, 1f, UI51Tokens.GoldA(0.5f), true);
            ui.TermsToggle = UI51Build.Button(boxShape, boxShape);
            UI51Build.GetOrAdd<UI51Press>(box);
            var check = UI51Build.Child(box, "Check");
            UI51Build.Solid(UI51Build.Stretch(check), UI51Tokens.Gold, 5f);
            var tick = UI51Build.Child(check, "Tick");
            UI51Build.Image(UI51Build.Center(tick, 12f, 12f), UI51Build.Sprite("Common", "ic_check_cream"), UI51Tokens.OnGold);
            ui.TermsCheck = check.gameObject;

            var line = UI51Build.Child(terms, "Line");
            UI51Build.Row(line, 3f, null, TextAnchor.MiddleLeft, true, false);
            UI51Build.Size(line, 0f, 18f);
            TermsPiece(line, "Accept", "Accetto i", UI51Tokens.CreamA(0.75f));
            ui.TermsLink = TermsLinkPiece(line, "TermsLink", "Termini di Servizio");
            TermsPiece(line, "And", "e la", UI51Tokens.CreamA(0.75f));
            ui.PrivacyLink = TermsLinkPiece(line, "PrivacyLink", "Privacy Policy");

            ui.Status = Status(sheet, "Status");
            ui.Submit = GoldButton(sheet, "Submit", "REGISTRATI", 54f, 15f);

            // Footer "Hai gia' un account? Accedi": tutta la riga e' il bottone
            var footer = UI51Build.Child(sheet, "ToLogin");
            UI51Build.Row(footer, 4f, null, TextAnchor.MiddleCenter, true, false);
            UI51Build.Layout(footer, -1f, 18f);
            var footerHit = UI51Build.Image(footer, null, Color.clear, true, false);
            ui.ToLogin = UI51Build.Button(footerHit, footerHit);
            UI51Build.GetOrAdd<UI51Press>(footer);
            var ask = UI51Build.Child(footer, "Ask");
            UI51Build.Size(ask, 0f, 18f);
            NoWrap(UI51Build.Text(ask, "Hai gi\u00e0 un account?", FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.8f)));
            var go = UI51Build.Child(footer, "Accedi");
            UI51Build.Size(go, 0f, 18f);
            NoWrap(UI51Build.Text(go, "Accedi", FontFace.NunitoBold, 13f, UI51Tokens.Gold));

            ui.TermsCheck.SetActive(false);   // AuthScreensV2.ToggleTerms lo accende
            ui.Status.gameObject.SetActive(false);
            return ui;
        }

        static void TermsPiece(Transform parent, string name, string text, Color color)
        {
            var rt = UI51Build.Child(parent, name);
            UI51Build.Size(rt, 0f, 18f);
            NoWrap(UI51Build.Text(rt, text, FontFace.NunitoRegular, 11.5f, color));
        }

        static Button TermsLinkPiece(Transform parent, string name, string text)
        {
            var rt = UI51Build.Child(parent, name);
            UI51Build.Size(rt, 0f, 18f);
            var t = NoWrap(UI51Build.Text(rt, text, FontFace.NunitoBold, 11.5f, UI51Tokens.Gold));
            t.raycastTarget = true;
            UI51Build.GetOrAdd<UI51Press>(rt);
            return UI51Build.Button(t, t);
        }

        // --- Termini / Privacy (un solo LegalModalV2 per entrambi)

        static LegalUi BuildLegal(LegalModalV2 legal)
        {
            var root = legal.transform;
            HideChild(root, "Dim");
            HideChild(root, "Design");
            var safe = BuildScreen(root, UI51Build.Sprite("Backgrounds", "home_bg_blur"), null);
            var ui = new LegalUi();

            ui.Frame = UI51Build.Stretch(UI51Build.Child(safe, "Frame"));

            // Header: indietro 40 + titolo/sottotitolo
            var header = UI51Build.Child(ui.Frame, "Header");
            TopBand(header, 20f, 20f, 22f, 40f);
            UI51Build.Row(header, 12f, null, TextAnchor.MiddleLeft, true, false);
            ui.Back = RoundButton(header, "Back", false, 40f, 20f, UI51Tokens.Rgba(11, 29, 58, 0.6f), UI51Build.Sprite("Common", "ic_nav_back_cream"), 15f);
            UI51Build.Layout(ui.Back, 40f);
            var titles = UI51Build.Child(header, "Titles");
            UI51Build.Size(titles, 0f, 40f);
            UI51Build.Layout(titles, -1f, -1f, 1f);
            UI51Build.Column(titles, 2f, null, TextAnchor.MiddleLeft, true, true);
            var title = UI51Build.Child(titles, "Title");
            UI51Build.Layout(title, -1f, 23f);
            // NoWrap: con Ellipsis TMP svuota il testo se la riga (line-height del font) supera l'altezza di layout.
            ui.Title = NoWrap(UI51Build.Text(title, "Termini di servizio", FontFace.CinzelBold, 20f, UI51Tokens.Cream));
            var subtitle = UI51Build.Child(titles, "Subtitle");
            UI51Build.Layout(subtitle, -1f, 14f);
            ui.Subtitle = NoWrap(UI51Build.Text(subtitle, "", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f)));

            // Scroller: top 84, left 20, right 12, bottom 104 (padding destro 8 nel contenuto)
            var scrollRt = UI51Build.Stretch(UI51Build.Child(ui.Frame, "Scroll"), 20f, 104f, 12f, 84f);
            ui.Scroll = UI51Build.GetOrAdd<ScrollRect>(scrollRt);
            ui.Scroll.horizontal = false;
            ui.Scroll.vertical = true;
            ui.Scroll.movementType = ScrollRect.MovementType.Elastic;
            ui.Scroll.scrollSensitivity = 30f;
            var viewport = UI51Build.Stretch(UI51Build.Child(scrollRt, "Viewport"));
            UI51Build.Image(viewport, null, Color.clear, true, false);
            UI51Build.GetOrAdd<RectMask2D>(viewport);
            var content = UI51Build.Child(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            UI51Build.Column(content, 0f, UI51Build.Pad(0, 8, 16, 0), TextAnchor.UpperLeft, true, true).childForceExpandWidth = true;
            UI51Build.Fit(content, false, true);
            ui.Scroll.viewport = viewport;
            ui.Scroll.content = content;

            // Indice
            var card = UI51Build.Child(content, "IndexCard");
            UI51Build.Shape(card, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(16f), 1f, UI51Tokens.BorderGoldSoft);
            UI51Build.Column(card, 0f, UI51Build.Pad(0, 0, 4, 0), TextAnchor.UpperLeft, true, true).childForceExpandWidth = true;
            var indexLabel = UI51Build.Child(card, "Label");
            var il = NoWrap(UI51Build.Text(indexLabel, "INDICE", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.Left, 2f));
            il.margin = new Vector4(12f, 12f, 12f, 6f);

            var row = UI51Build.Child(card, "IndexRowTemplate");
            UI51Build.Layout(row, -1f, 36f);
            UI51Build.Row(row, 10f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleLeft, true, false);
            var rowHit = UI51Build.Image(row, null, Color.clear, true, false);
            UI51Build.Button(rowHit, rowHit);
            UI51Build.GetOrAdd<UI51Press>(row);
            var num = UI51Build.Child(row, "Num");
            UI51Build.Size(num, 16f, 20f);
            UI51Build.Layout(num, 16f);
            NoWrap(UI51Build.Text(num, "1", FontFace.CinzelSemiBold, 11f, UI51Tokens.Gold));
            var label = UI51Build.Child(row, "Label");
            UI51Build.Size(label, 0f, 20f);
            UI51Build.Layout(label, 0f, -1f, 1f);
            UI51Build.Text(label, "Sezione", FontFace.NunitoBold, 13f, UI51Tokens.Cream).enableWordWrapping = false;
            var rowLine = UI51Build.Child(row, "Line");
            rowLine.anchorMin = new Vector2(0f, 0f);
            rowLine.anchorMax = new Vector2(1f, 0f);
            rowLine.pivot = new Vector2(0.5f, 0f);
            rowLine.offsetMin = new Vector2(12f, 0f);
            rowLine.offsetMax = new Vector2(-12f, 1f);
            UI51Build.Layout(rowLine, -1f, -1f, -1f, -1f, true);
            UI51Build.Image(rowLine, null, UI51Tokens.GoldA(0.08f), false, false);
            ui.IndexRow = row;

            // Introduzione
            var body = UI51Build.Child(content, "Body");
            ui.Body = Paragraph(body, "");
            ui.Body.margin = new Vector4(0f, 14f, 0f, 0f);

            // Sezione modello: pt 18, titolo 15 + testo 13
            var section = UI51Build.Child(content, "SectionTemplate");
            UI51Build.Column(section, 8f, UI51Build.Pad(18, 0, 8, 0), TextAnchor.UpperLeft, true, true).childForceExpandWidth = true;
            var heading = UI51Build.Child(section, "Heading");
            var h = UI51Build.Text(heading, "1. Sezione", FontFace.CinzelBold, 15f, UI51Tokens.Cream);
            h.enableWordWrapping = true;
            h.overflowMode = TextOverflowModes.Overflow;
            Paragraph(UI51Build.Child(section, "Text"), "");
            ui.Section = section;

            // Sfumatura sotto e HO CAPITO
            var fade = UI51Build.Child(ui.Frame, "Fade");
            fade.anchorMin = new Vector2(0f, 0f);
            fade.anchorMax = new Vector2(1f, 0f);
            fade.pivot = new Vector2(0.5f, 0f);
            // scende di 80 oltre la safe area come Sheet(), o sotto resta una fascia di sfondo chiaro.
            fade.anchoredPosition = new Vector2(0f, -80f);
            fade.sizeDelta = new Vector2(0f, 184f);
            UI51Build.Shape(fade, UI51Shape.Linear(
                (UI51Tokens.Rgba(5, 8, 15, 0f), 0f), (UI51Tokens.Rgba(5, 8, 15, 0.9f), 0.35f),
                (UI51Tokens.Rgba(5, 8, 15, 0.9f), 1f)), 180f, Vector4.zero, 0f, Color.clear);

            var ok = UI51Build.Child(ui.Frame, "Understood");
            UI51PrefabBuilder.GoldBody(ok.gameObject, 350f, 52f, UI51Tokens.RadiusButton, FontFace.CinzelBold, 14f, 2f, "HO CAPITO",
                new UI51Shadow(0f, 8f, 18f, UI51Tokens.BlackA(0.35f)));
            ok.anchorMin = new Vector2(0f, 0f);
            ok.anchorMax = new Vector2(1f, 0f);
            ok.pivot = new Vector2(0.5f, 0f);
            ok.offsetMin = new Vector2(20f, 30f);
            ok.offsetMax = new Vector2(-20f, 82f);
            ui.Understood = ok.GetComponent<Button>();

            row.gameObject.SetActive(false);
            section.gameObject.SetActive(false);
            return ui;
        }

        static TextMeshProUGUI Paragraph(RectTransform rt, string text)
        {
            var t = UI51Build.Text(rt, text, FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.72f));
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            t.lineSpacing = 20f;   // ~line-height 1.6 del mockup
            return t;
        }

        // --- Caricamento (AppLoadingV2/LoadingView)

        static bool BuildLoading(Scene scene)
        {
            var view = FindPath(scene, "AppLoadingV2", "LoadingView");
            if (view == null) { Debug.LogError($"{Tag} AppLoadingV2/LoadingView non trovato: Caricamento non ricostruito."); return false; }
            MonoBehaviour loading = null;
            foreach (var mb in view.GetComponentsInParent<MonoBehaviour>(true))
                if (mb != null && mb.GetType().Name == "AppLoadingView") loading = mb;

            var shade = UI51Shape.Linear(
                (UI51Tokens.Rgba(4, 9, 20, 0.35f), 0f), (UI51Tokens.Rgba(4, 9, 20, 0.1f), 0.3f),
                (UI51Tokens.Rgba(4, 9, 20, 0.55f), 0.62f), (UI51Tokens.Rgba(3, 7, 16, 0.95f), 1f));
            var safe = BuildScreen(view, UI51Build.Sprite("Backgrounds", "home_bg_base"), shade);
            var ui51 = safe.parent.gameObject;

            var glow = UI51Build.Child(safe, "Glow");
            UI51Build.Image(glow, UI51Build.Sprite("Common", "Bagliore_morbido"), new Color(1f, 1f, 1f, 0.75f));
            CenterAt(glow, 195f, 260f, 360f, 360f);
            Logo(safe, 195f, 260f, 196f);

            // Ventaglio di 5 dorsi 40x60, r5, ombra. Posizioni fisse, niente LayoutGroup: LoadingWave cattura
            // anchoredPosition a vista ancora non impaginata e le carte finirebbero tutte nello stesso punto.
            var cards = UI51Build.Child(safe, "Cards");
            TopBand(cards, 0f, 0f, 530f, 80f);
            var oldRow = cards.GetComponent<HorizontalLayoutGroup>();   // lasciato dalle build precedenti
            if (oldRow != null) UnityEngine.Object.DestroyImmediate(oldRow);
            var cardRts = WaveBacks(cards, 40f, 60f, 50f);

            // Avanzamento: left/right 40, top 640, gap 10
            var progress = UI51Build.Child(safe, "Progress");
            TopBand(progress, 40f, 40f, 640f, 76f);
            UI51Build.Column(progress, 10f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var labels = UI51Build.Child(progress, "Labels");
            UI51Build.Layout(labels, -1f, 16f);
            UI51Build.Row(labels, 8f, null, TextAnchor.MiddleLeft, true, false);
            var stepRt = UI51Build.Child(labels, "Step");
            UI51Build.Size(stepRt, 0f, 16f);
            UI51Build.Layout(stepRt, 0f, -1f, 1f);
            var step = UI51Build.Text(stepRt, "CARICAMENTO", FontFace.CinzelSemiBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.Left, 2f);
            step.enableWordWrapping = false;
            var pctRt = UI51Build.Child(labels, "Percent");
            UI51Build.Size(pctRt, 0f, 16f);
            var pct = NoWrap(UI51Build.Text(pctRt, "0%", FontFace.CinzelBold, 12f, UI51Tokens.Cream, TextAlignmentOptions.Right));

            var bar = UI51Build.Child(progress, "Bar");
            UI51Build.Layout(bar, -1f, 10f);
            UI51Build.Solid(bar, UI51Tokens.WhiteA(0.12f), 5f, 1f, UI51Tokens.GoldA(0.3f));
            var fill = UI51Build.Stretch(UI51Build.Child(bar, "Fill"), 1f, 1f, 1f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(1f, 1f);
            fill.offsetMax = new Vector2(-1f, -1f);
            // Fill porta solo misura e valore (AppLoadingView.Progress, immagine spenta); il ritaglio tondo e' Clip. Prima la maschera
            // era lo sprite di Unity stirato: le estremita' si deformavano mentre la barra cresceva (utente, 01/10).
            var fillImg = UI51Build.Image(fill, null, Color.white, false, false);
            fillImg.enabled = false;
            UI51Build.Remove<Mask>(fill.gameObject);
            foreach (var stale in new[] { "Gradient", "Shine" })
            {
                var old = fill.Find(stale);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var clip = UI51Build.Stretch(UI51Build.Child(fill, "Clip"));
            UI51Build.Solid(clip, Color.white, 4f);
            UI51Build.GetOrAdd<Mask>(clip).showMaskGraphic = false;
            UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(clip, "Gradient")),
                UI51Shape.Linear((UI51Tokens.Hex("#C4922F"), 0f), (UI51Tokens.Hex("#FCE29A"), 1f)), 90f, UI51Tokens.Radii(4f), 0f, Color.clear);
            var shine = UI51Build.Stretch(UI51Build.Child(clip, "Shine"));
            UI51Build.Shape(shine, UI51Shape.Linear((UI51Tokens.WhiteA(0f), 0f), (UI51Tokens.WhiteA(0.45f), 0.5f), (UI51Tokens.WhiteA(0f), 1f)),
                90f, Vector4.zero, 0f, Color.clear);

            // Fuori dalla colonna per lo stesso motivo delle carte (UIAnim.Tip anima la Y).
            var oldTip = progress.Find("Tip");
            if (oldTip != null) UnityEngine.Object.DestroyImmediate(oldTip.gameObject);
            var tipRt = UI51Build.Child(safe, "Tip");
            TopBand(tipRt, 40f, 40f, 696f, 40f);
            var tip = UI51Build.Text(tipRt, "", FontFace.NunitoRegular, 12f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Top);
            tip.enableWordWrapping = true;
            tip.lineSpacing = 9f;   // ~line-height 1.45

            // Stato di errore (non nel mockup): Riprova/Annulla, spenti finche' AppLoadingView.Fail non li accende.
            var actions = UI51Build.Child(safe, "Actions");
            TopBand(actions, 40f, 40f, 740f, 98f);
            UI51Build.Column(actions, 10f, null, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            var retry = GoldButton(actions, "Retry", "RIPROVA", 48f, 15f);
            var cancel = GhostButton(actions, "Cancel", "Annulla", 40f);
            retry.gameObject.SetActive(false);
            cancel.gameObject.SetActive(false);

            var version = UI51Build.Child(safe, "Version");
            version.anchorMin = new Vector2(0f, 0f);
            version.anchorMax = new Vector2(1f, 0f);
            version.pivot = new Vector2(0.5f, 0f);
            version.offsetMin = new Vector2(20f, 22f);
            version.offsetMax = new Vector2(-20f, 36f);
            NoWrap(UI51Build.Text(version, "", FontFace.NunitoRegular, 10f, UI51Tokens.CreamA(0.35f), TextAlignmentOptions.Center));
            UI51Build.GetOrAdd<UI51VersionLabel>(version);

            if (loading == null)
            {
                ui51.SetActive(false);
                Debug.LogError($"{Tag} AppLoadingView non trovato su AppLoadingV2: resta la grafica legacy.");
                return false;
            }
            // La barra e' disegnata da Fill: AppLoadingView ne stira anchorMax.x = Progress.fillAmount (Image Simple, niente doppio taglio).
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = new Vector2(1f, 1f);
            fill.offsetMax = new Vector2(-1f, -1f);
            fillImg.type = Image.Type.Simple;
            fillImg.fillAmount = 0f;
            var so = new SerializedObject(loading);
            so.Update();
            so.FindProperty("Status").objectReferenceValue = step;
            so.FindProperty("Percent").objectReferenceValue = pct;
            so.FindProperty("Progress").objectReferenceValue = fillImg;
            so.FindProperty("ProgressFill").objectReferenceValue = fill;
            so.FindProperty("ProgressShine").objectReferenceValue = shine;
            so.FindProperty("Glow").objectReferenceValue = glow;
            so.FindProperty("Logo").objectReferenceValue = safe.Find("Logo");
            so.FindProperty("Tip").objectReferenceValue = tip;
            so.FindProperty("RetryButton").objectReferenceValue = retry;
            so.FindProperty("CancelButton").objectReferenceValue = cancel;
            var cardsProp = so.FindProperty("Cards");
            cardsProp.arraySize = cardRts.Length;
            for (int i = 0; i < cardRts.Length; i++) cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardRts[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            ui51.SetActive(true);
            HideChild(view, "Background");
            HideChild(view, "DesignArea");
            return true;
        }

        // --- Collegamenti agli script esistenti

        static void WireAuthScreens(AuthScreensV2 screens, LoginUi login, RegisterUi register)
        {
            UI51Build.Wire(screens, so =>
            {
                UI51Build.Ref(so, "LoginBack", login.Back);
                UI51Build.Ref(so, "LoginGuest", login.Guest);
                UI51Build.Ref(so, "LoginToRegister", login.ToRegister);
                UI51Build.Ref(so, "LoginForgot", login.Forgot);
                UI51Build.Ref(so, "LoginEmail", login.Email);
                UI51Build.Ref(so, "LoginStatus", login.Status);
                SetArray(so, "LoginGuestOnly", login.Guest.gameObject);
                SetArray(so, "LoginBelowGuest");
                UI51Build.Float(so, "LoginGuestHiddenShift", 0f);   // il foglio e' un layout: si richiude da solo

                UI51Build.Ref(so, "RegisterBack", register.Back);
                UI51Build.Ref(so, "RegisterToLogin", register.ToLogin);
                UI51Build.Ref(so, "RegisterSubmit", register.Submit);
                UI51Build.Ref(so, "RegisterUsername", register.Username);
                UI51Build.Ref(so, "RegisterEmail", register.Email);
                UI51Build.Ref(so, "RegisterPassword", register.Password);
                UI51Build.Ref(so, "RegisterConfirm", register.Confirm);
                UI51Build.Ref(so, "RegisterStatus", register.Status);
                UI51Build.Ref(so, "TermsToggle", register.TermsToggle);
                UI51Build.Ref(so, "TermsCheck", register.TermsCheck);
                UI51Build.Ref(so, "TermsLink", register.TermsLink);
                UI51Build.Ref(so, "PrivacyLink", register.PrivacyLink);
            });
        }

        // I bottoni indietro di AuthUIController restano sui vecchi oggetti: li governa AuthScreensV2.
        static void WireAuthController(AuthUIController auth, LoginUi login, RegisterUi register)
        {
            UI51Build.Wire(auth, so =>
            {
                UI51Build.Ref(so, "registerUsernameInput", register.Username);
                UI51Build.Ref(so, "registerEmailInput", register.Email);
                UI51Build.Ref(so, "registerPasswordInput", register.Password);
                UI51Build.Ref(so, "registerButton", register.Submit);
                UI51Build.Ref(so, "registerStatusText", register.Status);
                UI51Build.Ref(so, "loginEmailInput", login.Email);
                UI51Build.Ref(so, "loginPasswordInput", login.Password);
                UI51Build.Ref(so, "loginButton", login.Submit);
                UI51Build.Ref(so, "loginStatusText", login.Status);
            });
        }

        static void WireLegal(LegalModalV2 legal, LegalUi ui)
        {
            var modal = legal.Modal != null ? legal.Modal : legal.GetComponent<AnimatedModalV2>();
            if (modal != null)
            {
                var group = UI51Build.GetOrAdd<CanvasGroup>(modal);
                UI51Build.Wire(modal, so =>
                {
                    UI51Build.Ref(so, "Group", group);
                    UI51Build.Ref(so, "Frame", ui.Frame);
                    UI51Build.Ref(so, "CloseButton", ui.Back);
                    UI51Build.Ref(so, "Dimmer", null);
                    UI51Build.Bool(so, "HandleEscape", true);
                });
            }
            else Debug.LogError($"{Tag} AnimatedModalV2 del LegalModalV2 non trovato: apertura/chiusura non ricollegate.");

            UI51Build.Wire(legal, so =>
            {
                if (modal != null) UI51Build.Ref(so, "Modal", modal);
                UI51Build.Ref(so, "Title", ui.Title);
                UI51Build.Ref(so, "Subtitle", ui.Subtitle);
                UI51Build.Ref(so, "Body", ui.Body);
                UI51Build.Ref(so, "Scroll", ui.Scroll);
                UI51Build.Ref(so, "OpenOnWeb", null);
                UI51Build.Ref(so, "IndexRowTemplate", ui.IndexRow);
                UI51Build.Ref(so, "SectionTemplate", ui.Section);
                UI51Build.Ref(so, "Understood", ui.Understood);
            });
        }

        internal static void SetArray(SerializedObject so, string field, params UnityEngine.Object[] values)
        {
            var p = so.FindProperty(field);
            if (p == null || !p.isArray) { Debug.LogError($"{Tag} Campo array {field} non trovato su {so.targetObject.GetType().Name}."); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        // --- Mattoni

        /// <summary>UI51 (stretch) -> Bg (envelope) -> Overlay (opzionale) -> Safe (DesignCanvasFit 390x844). Ritorna Safe.</summary>
        internal static RectTransform BuildScreen(Transform panel, Sprite bgSprite, Gradient overlay)
        {
            var root = UI51Build.Stretch(UI51Build.Child(panel, "UI51"));
            var bg = UI51Build.Stretch(UI51Build.Child(root, "Bg"));
            UI51Build.Image(bg, bgSprite, Color.white, true, false);
            var fitter = UI51Build.GetOrAdd<AspectRatioFitter>(bg);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = bgSprite != null ? bgSprite.rect.width / bgSprite.rect.height : 1153f / 2048f;

            var old = root.Find("Overlay");
            if (overlay != null)
                UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(root, "Overlay")), overlay, 180f, Vector4.zero, 0f, Color.clear);
            else if (old != null) old.gameObject.SetActive(false);

            var safe = UI51Build.Child(root, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;
            return safe;
        }

        /// <summary>
        /// Cinque dorsi (back_giada) in fila, centrati, raggio 5 e ombra 0 6 12 .5, passo step: le carte di UIAnim.LoadingWave.
        /// Posizioni fisse, niente LayoutGroup (l'onda cattura la posizione di riposo).
        /// </summary>
        internal static RectTransform[] WaveBacks(RectTransform parent, float w, float h, float step)
        {
            var back = UI51Build.Sprite("Cards", "back_giada");
            var cards = new RectTransform[5];
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i] = UI51Build.Child(parent, "Card" + i);
                UI51Build.Size(c, w, h);
                c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
                c.anchoredPosition = new Vector2((i - 2) * step, 0f);
                UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(c, "Shadow")), UI51Tokens.Navy, 5f, 0f, default, false,
                    new UI51Shadow(0f, 6f, 12f, UI51Tokens.BlackA(0.5f)));
                var face = UI51Build.Stretch(UI51Build.Child(c, "Face"));
                UI51Build.Solid(face, Color.white, 5f);
                UI51Build.GetOrAdd<Mask>(face).showMaskGraphic = false;
                UI51Build.Image(UI51Build.Stretch(UI51Build.Child(face, "Art")), back, Color.white, false, false);
            }
            return cards;
        }

        /// <summary>Centro in px del mockup (x da sinistra, y dall'alto), ancorato in alto al centro.</summary>
        internal static RectTransform CenterAt(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x - 195f, -y);
            return rt;
        }

        /// <summary>Fascia larga quanto il genitore meno i margini, a top px dall'alto, alta h.</summary>
        internal static RectTransform TopBand(RectTransform rt, float left, float right, float top, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - h);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        internal static void Logo(RectTransform safe, float x, float y, float width)
        {
            var logo = UI51Build.Child(safe, "Logo");
            var sprite = UI51Build.Sprite("Common", "logo_51");
            UI51Build.Image(logo, sprite, Color.white);
            float h = sprite != null ? width * sprite.rect.height / sprite.rect.width : width;
            CenterAt(logo, x, y, width, h);
        }

        /// <summary>Foglio in basso a tutta larghezza (bordo solo in alto: i lati escono di 1px), altezza dal contenuto.</summary>
        internal static RectTransform Sheet(RectTransform safe, RectOffset padding, float spacing, Gradient fill)
        {
            var sheet = UI51Build.Child(safe, "Sheet");
            sheet.anchorMin = new Vector2(0f, 0f);
            sheet.anchorMax = new Vector2(1f, 0f);
            sheet.pivot = new Vector2(0.5f, 0f);
            sheet.offsetMin = new Vector2(-1f, -1f);
            sheet.offsetMax = new Vector2(1f, sheet.offsetMax.y);
            // Safe finisce al bordo della safe area: il foglio scende di Bleed oltre, cosi' copre anche
            // la fascia dell'indicatore home (il contenuto resta dov'e', compensato nel padding).
            const int Bleed = 80;
            sheet.anchoredPosition = new Vector2(0f, -1f - Bleed);
            padding.left += 1; padding.right += 1; padding.bottom += 1 + Bleed;
            // Il foglio prende i tocchi: senza, un tocco sul fondo arrivava al velo dietro e chiudeva il pannello (utente, 01/10).
            UI51Build.Shape(sheet, fill, 180f, UI51Tokens.RadiiTop(26f), 1f, UI51Tokens.GoldA(0.3f), true);
            UI51Build.Column(sheet, spacing, padding, TextAnchor.UpperCenter, true, true).childForceExpandWidth = true;
            UI51Build.Fit(sheet, false, true);
            return sheet;
        }

        internal static Button RoundButton(RectTransform parent, string name, bool right, float size, float radius, Color fill, Sprite icon, float iconSize)
        {
            var rt = UI51Build.Child(parent, name);
            var anchor = new Vector2(right ? 1f : 0f, 1f);
            UI51Build.Place(rt, anchor, new Vector2(size, size), new Vector2(right ? -20f : 20f, -20f));
            var shape = UI51Build.Solid(rt, fill, radius, 1f, UI51Tokens.GoldA(0.45f), true);
            var b = UI51Build.Button(shape, shape);
            UI51Build.GetOrAdd<UI51Press>(rt);
            UI51Build.Image(UI51Build.Center(UI51Build.Child(rt, "Icon"), iconSize, iconSize), icon, Color.white);
            return b;
        }

        internal static TMP_InputField BuildInput(RectTransform parent, string name, string placeholder, Sprite icon, TMP_InputField.ContentType type, float height)
        {
            var rt = UI51Build.Child(parent, name);
            UI51Build.Layout(rt, -1f, height);
            var box = UI51Build.Solid(rt, UI51Tokens.WhiteA(0.06f), 14f, 1f, UI51Tokens.GoldA(0.3f), true);

            var iconRt = UI51Build.Child(rt, "Icon");
            UI51Build.Place(iconRt, new Vector2(0f, 0.5f), new Vector2(18f, 18f), Vector2.zero);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(22f, 0f);
            UI51Build.Image(iconRt, icon, new Color(1f, 1f, 1f, 0.9f));

            var area = UI51Build.Stretch(UI51Build.Child(rt, "TextArea"), 44f, 0f, 14f, 0f);
            UI51Build.GetOrAdd<RectMask2D>(area);
            var ph = UI51Build.Text(UI51Build.Stretch(UI51Build.Child(area, "Placeholder")), placeholder, FontFace.NunitoRegular, 14f,
                UI51Tokens.CreamA(0.45f), TextAlignmentOptions.MidlineLeft);
            ph.enableWordWrapping = false;
            var text = UI51Build.Text(UI51Build.Stretch(UI51Build.Child(area, "Text")), "", FontFace.NunitoRegular, 14f,
                UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;

            var field = UI51Build.GetOrAdd<TMP_InputField>(rt);
            field.transition = Selectable.Transition.None;
            field.targetGraphic = box;
            var nav = field.navigation;
            nav.mode = Navigation.Mode.None;
            field.navigation = nav;
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = ph;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = type;
            field.richText = false;
            field.customCaretColor = true;
            field.caretColor = UI51Tokens.Gold;
            field.selectionColor = UI51Tokens.GoldA(0.35f);

            var input = UI51Build.GetOrAdd<UI51Input>(rt);
            UI51Build.Wire(input, so =>
            {
                UI51Build.Ref(so, "m_Field", field);
                UI51Build.Ref(so, "m_Box", box);
            });
            return field;
        }

        internal static Button GoldButton(RectTransform parent, string name, string label, float height, float fontSize)
        {
            var rt = UI51Build.Child(parent, name);
            UI51PrefabBuilder.GoldBody(rt.gameObject, 346f, height, UI51Tokens.RadiusButton, FontFace.CinzelBold, fontSize, 2f, label,
                new UI51Shadow(0f, 8f, 18f, UI51Tokens.BlackA(0.35f)));
            UI51Build.Layout(rt, -1f, height);
            return rt.GetComponent<Button>();
        }

        internal static Button GhostButton(RectTransform parent, string name, string label, float height)
        {
            var rt = UI51Build.Child(parent, name);
            UI51PrefabBuilder.ButtonBody(rt.gameObject, 346f, height, UI51Shape.Solid(UI51Tokens.WhiteA(0.07f)), UI51Tokens.Radii(14f), 1f,
                UI51Tokens.WhiteA(0.22f), FontFace.NunitoBold, 14f, 0f, UI51Tokens.Cream, label);
            UI51Build.Layout(rt, -1f, height);
            return rt.GetComponent<Button>();
        }

        /// <summary>Testo cliccabile (il testo stesso fa da bersaglio del raycast).</summary>
        internal static Button Link(RectTransform parent, string name, string label, FontFace face, float size, Color color, TextAlignmentOptions align, float height)
        {
            var rt = UI51Build.Child(parent, name);
            UI51Build.Layout(rt, -1f, height);
            var t = NoWrap(UI51Build.Text(rt, label, face, size, color, align));
            t.raycastTarget = true;
            UI51Build.GetOrAdd<UI51Press>(rt);
            return UI51Build.Button(t, t);
        }

        internal static TextMeshProUGUI Status(RectTransform parent, string name)
        {
            var rt = UI51Build.Child(parent, name);
            var t = UI51Build.Text(rt, "", FontFace.NunitoSemiBold, 12f, UI51Tokens.DangerText, TextAlignmentOptions.Center);
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            UI51Build.Layout(rt, -1f, -1f);
            return t;
        }

        internal static void Version(RectTransform parent, float alpha)
        {
            var rt = UI51Build.Child(parent, "Version");
            UI51Build.Layout(rt, -1f, 14f);
            NoWrap(UI51Build.Text(rt, "", FontFace.NunitoRegular, 10f, UI51Tokens.CreamA(alpha), TextAlignmentOptions.Center));
            UI51Build.GetOrAdd<UI51VersionLabel>(rt);
        }

        internal static void OrLine(RectTransform row, string name)
        {
            var rt = UI51Build.Child(row, name);
            UI51Build.Size(rt, 0f, 1f);
            UI51Build.Layout(rt, 0f, -1f, 1f);
            UI51Build.Image(rt, null, UI51Tokens.WhiteA(0.18f), false, false);
        }

        internal static TextMeshProUGUI NoWrap(TextMeshProUGUI t)
        {
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        // --- Scena

        internal static void HideChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }

        internal static GameObject PanelRef(SerializedObject authSo, string field, Scene scene, string fallbackName)
        {
            var p = authSo.FindProperty(field);
            if (p != null && p.objectReferenceValue is GameObject go) return go;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == fallbackName) return t.gameObject;
            return null;
        }

        internal static Transform FindPath(Scene scene, string parentName, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name && t.parent != null && t.parent.name == parentName) return t;
            return null;
        }

        internal static bool HasDirtyScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isDirty) continue;
                Debug.LogError($"{Tag} La scena aperta '{(string.IsNullOrEmpty(s.path) ? s.name : s.path)}' ha modifiche non salvate: " +
                               "salvala o scartala e riesegui. Non la tocco.");
                return true;
            }
            return false;
        }
    }
}
