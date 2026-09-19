using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Auth;
using Project51.UIV2.Components;
using Project51.UIV2.Core;

namespace Project51.EditorTools
{
    /// <summary>
    /// Schermata Accesso V2 (mockup 23_login) dentro MainMenu. Ricostruisce il contenuto di
    /// Canvas_Login/LoginPanel lasciando intatti lo sfondo a gradiente e il velo di rumore che la
    /// schermata aveva gia', e ricollega i campi di AuthUIController ai nuovi oggetti: la logica di
    /// accesso resta quella esistente, cambia solo la grafica. I pulsanti che il controller storico
    /// non prevedeva (indietro, ospite, registrati, password dimenticata) passano da AuthScreensV2.
    /// Coordinate = pixel del mockup 1080x1920 con origine in alto a sinistra, dentro un
    /// contenitore DesignCanvasFit: la schermata resta centrata e proporzionata a ogni risoluzione.
    /// Rilanciabile: cancella e ricostruisce sempre da zero.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private static readonly Color AuthFieldFill = new Color32(20, 32, 58, 255);
        private static readonly Color AuthFieldBorder = new Color32(58, 86, 128, 255);
        private static readonly Color AuthPlaceholder = new Color32(126, 147, 180, 255);
        private static readonly Color AuthTitleGold = new Color32(245, 215, 156, 255);
        private static readonly Color AuthSubtitle = new Color32(147, 166, 194, 255);
        private static readonly Color AuthSoftLink = new Color32(159, 176, 204, 255);
        private static readonly Color AuthStrongLink = new Color32(240, 226, 192, 255);
        private static readonly Color AuthVersion = new Color32(90, 112, 149, 255);

        private static readonly Color AuthButtonGlow = new Color32(255, 186, 74, 150);
        private static readonly Color AuthStrengthOff = new Color32(30, 42, 68, 255);
        private static readonly Color AuthCheckFill = new Color32(236, 176, 62, 255);
        private static readonly Color AuthCheckBorder = new Color32(196, 138, 34, 255);
        private static readonly Color AuthCheckMark = new Color32(26, 38, 60, 255);

        private const string FlowBackdropPath = "Assets/UIV2/Art/FlowBackdrop.mat";

        [MenuItem("Tools/UIV2/Build Auth Screens")]
        private static void BuildAuthScreens()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var auth = Object.FindObjectOfType<AuthUIController>(true);
            if (auth == null) throw new System.Exception("AuthUIController (Canvas_Login) non trovato in MainMenu");

            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsBoldPath);
            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PoppinsExtraBoldPath);
            if (bold == null || extraBold == null) throw new System.Exception("Font Poppins Bold/ExtraBold non trovati");

            var screens = auth.GetComponent<AuthScreensV2>() ?? auth.gameObject.AddComponent<AuthScreensV2>();
            screens.Auth = auth;
            screens.StartScreen = Object.FindObjectOfType<StartScreenV2>(true);

            screens.Legal = BuildLegalModal(bold, extraBold);
            BuildLoginScreen(auth, screens, bold, extraBold);
            BuildRegisterScreen(auth, screens, bold, extraBold);
            BuildAccountScreen(auth, screens, bold, extraBold);

            EditorUtility.SetDirty(auth);
            EditorUtility.SetDirty(screens);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UIV2FoundationBuilder] Schermate Accesso (mockup 23), Registrazione (mockup 24) e finestra documenti legali ricostruite in MainMenu.");
        }

        /// <summary>
        /// Mockup 23: indietro, logo, titolo, due campi, password dimenticata, ACCEDI, separatore
        /// "oppure", accesso ospite, rimando alla registrazione e numero di versione.
        /// </summary>
        private static void BuildLoginScreen(AuthUIController auth, AuthScreensV2 screens, TMP_FontAsset bold, TMP_FontAsset extraBold)
        {
            var panel = auth.transform.Find("LoginPanel");
            if (panel == null) throw new System.Exception("LoginPanel non trovato sotto Canvas_Login");

            bool wasActive = panel.gameObject.activeSelf;
            panel.gameObject.SetActive(true);
            for (int i = panel.childCount - 1; i >= 0; i--) Object.DestroyImmediate(panel.GetChild(i).gameObject);

            // Fondale: lo stesso della schermata iniziale (blu notte con bagliore centrale), al
            // posto del gradiente verdastro legacy che questa schermata si portava dietro. E' il
            // fondo dei mockup 23-25 ed e' gia' quello del resto della UI V2.
            var background = panel.GetComponent<Image>();
            background.sprite = null;
            background.color = Color.white;
            background.material = AssetDatabase.LoadAssetAtPath<Material>(FlowBackdropPath);
            background.raycastTarget = true;

            var design = CreateUIObject("Design", panel);
            design.gameObject.AddComponent<DesignCanvasFit>();
            design.sizeDelta = new Vector2(1080f, 1920f);

            screens.LoginBack = BackButton(design, "Back", 78f, 96f, 88f);
            LogoBadge(design, 430f, 130f, 220f, 220f);

            // Titolo e logo del mockup sono oro pieno, senza bordo scuro: sul fondale blu notte
            // l'outline li ingrossava e sporcava le lettere.
            var title = MockText(design, "Title", "BENTORNATO", 90f, 372f, 900f, 58f, 35f, FontStyles.Bold, AuthTitleGold, TextAlignmentOptions.Center);
            UseFont(title, extraBold, extraBold.material);
            MockText(design, "Subtitle", "Accedi al tuo account", 90f, 432f, 900f, 40f, 21f, FontStyles.Normal, AuthSubtitle, TextAlignmentOptions.Center);

            var email = AuthField(design, "EmailInput", "Email o nome utente", 91f, 543f, 898f, 87f, false);
            var password = AuthField(design, "PasswordInput", "Password", 91f, 657f, 898f, 87f, true);

            var forgot = LinkButton(design, "ForgotPassword", "Password dimenticata?", 490f, 752f, 500f, 40f,
                20f, FontStyles.Normal, AuthSoftLink, TextAlignmentOptions.MidlineRight, null);

            var submit = MockButton(design, "LoginButton", "btn_gold_long", 91f, 848f, 898f, 104f, "ACCEDI", 30f, BrownOutlineMaterial());
            UseFont(submit.GetComponentInChildren<TMP_Text>(), extraBold, BrownOutlineMaterial());
            // Bagliore caldo dietro al pulsante principale, come nel mockup (stesso asset e stesso
            // aiutante del riquadro vincente della roulette).
            AddSoftRectGlow(submit.transform, LoadSprite(GlowSheetDir + "Bagliore morbido rettangolo.png", "Bagliore morbido rettangolo"), AuthButtonGlow);

            DividerLine(design, "OrLineLeft", 90f, 1043f, 380f);
            MockText(design, "OrLabel", "oppure", 470f, 1024f, 140f, 40f, 20f, FontStyles.Normal, AuthSubtitle, TextAlignmentOptions.Center);
            DividerLine(design, "OrLineRight", 610f, 1043f, 380f);

            var guest = MockButton(design, "GuestButton", "btn_blue_long", 91f, 1095f, 898f, 92f, "ACCEDI COME OSPITE", 23f, NavyOutlineMaterial());
            UseFont(guest.GetComponentInChildren<TMP_Text>(), bold, NavyOutlineMaterial());

            MockText(design, "RegisterHint", "Non hai un account?", 90f, 1248f, 900f, 38f, 20f, FontStyles.Normal, AuthSoftLink, TextAlignmentOptions.Center);
            var register = LinkButton(design, "RegisterLink", "REGISTRATI", 340f, 1288f, 400f, 46f,
                21f, FontStyles.Bold, AuthStrongLink, TextAlignmentOptions.Center, bold);

            // Area avvisi: nello spazio vuoto sotto al modulo, non nella fessura fra i campi e il
            // pulsante. Creata per ultima cosi' niente le finisce sopra, e su tre righe perche' la
            // conferma del recupero password e' una frase lunga che va letta per intero.
            var status = NoticeArea(design);

            var version = MockText(design, "Version", "v" + PlayerSettings.bundleVersion, 90f, 1846f, 900f, 38f, 17f, FontStyles.Normal, AuthVersion, TextAlignmentOptions.Center);
            version.gameObject.AddComponent<VersionLabelV2>();

            // Stessa cura alla schermata iniziale, dove il numero era rimasto a "v1.77".
            var startVersion = screens.StartScreen != null ? screens.StartScreen.transform.Find("DesignArea/Version") : null;
            if (startVersion != null && startVersion.GetComponent<VersionLabelV2>() == null)
            {
                startVersion.GetComponent<TMP_Text>().text = "v" + PlayerSettings.bundleVersion;
                startVersion.gameObject.AddComponent<VersionLabelV2>();
            }

            // AuthUIController continua a gestire accesso e messaggi: gli si ridanno i suoi campi.
            // loginBackButton resta vuoto di proposito: l'indietro del mockup torna alla schermata
            // iniziale (AuthScreensV2), non al vecchio pannello Ospite.
            SetPrivateField(auth, "loginEmailInput", email);
            SetPrivateField(auth, "loginPasswordInput", password);
            SetPrivateField(auth, "loginButton", submit);
            SetPrivateField(auth, "loginBackButton", null);
            SetPrivateField(auth, "loginStatusText", status);

            screens.LoginGuest = guest;
            // Dopo l'ingresso come ospite "oppure / ACCEDI COME OSPITE" spariscono e il rimando alla
            // registrazione sale al loro posto (AuthScreensV2.RefreshGuestRows).
            screens.LoginGuestOnly = new[]
            {
                design.Find("OrLineLeft").gameObject, design.Find("OrLabel").gameObject,
                design.Find("OrLineRight").gameObject, guest.gameObject
            };
            screens.LoginBelowGuest = new[] { (RectTransform)design.Find("RegisterHint"), (RectTransform)register.transform };
            screens.LoginGuestHiddenShift = 1248f - 1024f;
            screens.LoginToRegister = register;
            screens.LoginForgot = forgot;
            screens.LoginEmail = email;
            screens.LoginStatus = status;

            panel.gameObject.SetActive(wasActive);
        }

        /// <summary>
        /// Mockup 24: indietro, logo, titolo, quattro campi (nome utente, email, password,
        /// conferma), tacche di sicurezza della password, accettazione dei Termini, REGISTRATI e
        /// rimando all'accesso. La chiamata a PlayFab resta di AuthUIController; conferma, forza e
        /// Termini li gestisce AuthScreensV2, che tiene spento REGISTRATI finche' non torna tutto.
        /// </summary>
        private static void BuildRegisterScreen(AuthUIController auth, AuthScreensV2 screens, TMP_FontAsset bold, TMP_FontAsset extraBold)
        {
            var panel = auth.transform.Find("RegisterPanel");
            if (panel == null) throw new System.Exception("RegisterPanel non trovato sotto Canvas_Login");

            bool wasActive = panel.gameObject.activeSelf;
            panel.gameObject.SetActive(true);
            for (int i = panel.childCount - 1; i >= 0; i--) Object.DestroyImmediate(panel.GetChild(i).gameObject);

            var background = panel.GetComponent<Image>();
            background.sprite = null;
            background.color = Color.white;
            background.material = AssetDatabase.LoadAssetAtPath<Material>(FlowBackdropPath);
            background.raycastTarget = true;

            var design = CreateUIObject("Design", panel);
            design.gameObject.AddComponent<DesignCanvasFit>();
            design.sizeDelta = new Vector2(1080f, 1920f);

            screens.RegisterBack = BackButton(design, "Back", 78f, 96f, 88f);
            LogoBadge(design, 455f, 90f, 170f, 170f);

            var title = MockText(design, "Title", "CREA IL TUO ACCOUNT", 60f, 278f, 960f, 54f, 32f, FontStyles.Bold, AuthTitleGold, TextAlignmentOptions.Center);
            UseFont(title, extraBold, extraBold.material);
            MockText(design, "Subtitle", "Salva i tuoi progressi per sempre", 90f, 330f, 900f, 40f, 20f, FontStyles.Normal, AuthSubtitle, TextAlignmentOptions.Center);

            var username = AuthField(design, "UsernameInput", "Nome utente", 91f, 412f, 898f, 87f, false);
            var email = AuthField(design, "EmailInput", "Email", 91f, 558f, 898f, 87f, false);
            var password = AuthField(design, "PasswordInput", "Password", 91f, 705f, 898f, 87f, true);

            MockText(design, "StrengthLabel", "Sicurezza password:", 91f, 820f, 240f, 28f, 18f, FontStyles.Normal, AuthSoftLink, TextAlignmentOptions.MidlineLeft);
            screens.StrengthBars = StrengthBars(design, 320f, 822f, 92f, 21f, 97f);

            var confirm = AuthField(design, "ConfirmInput", "Conferma password", 91f, 851f, 898f, 87f, true);

            var termsToggle = TermsRow(design, 90f, 988f, out var check, out var termsLink, out var privacyLink);
            screens.TermsToggle = termsToggle;
            screens.TermsCheck = check;
            screens.TermsLink = termsLink;
            screens.PrivacyLink = privacyLink;

            var submit = MockButton(design, "RegisterButton", "btn_gold_long", 91f, 1088f, 898f, 104f, "REGISTRATI", 30f, BrownOutlineMaterial());
            UseFont(submit.GetComponentInChildren<TMP_Text>(), extraBold, BrownOutlineMaterial());
            screens.RegisterGlow = AddSoftRectGlow(submit.transform, LoadSprite(GlowSheetDir + "Bagliore morbido rettangolo.png", "Bagliore morbido rettangolo"), AuthButtonGlow);

            MockText(design, "LoginHint", "Hai già un account?", 90f, 1246f, 900f, 38f, 20f, FontStyles.Normal, AuthSoftLink, TextAlignmentOptions.Center);
            var loginLink = LinkButton(design, "LoginLink", "ACCEDI", 340f, 1286f, 400f, 46f,
                21f, FontStyles.Bold, AuthStrongLink, TextAlignmentOptions.Center, bold);

            // Area avvisi: nello spazio vuoto sotto al modulo, non nella fessura fra i campi e il
            // pulsante. Creata per ultima cosi' niente le finisce sopra, e su tre righe perche' la
            // conferma del recupero password e' una frase lunga che va letta per intero.
            var status = NoticeArea(design);

            // registerBackButton resta vuoto: l'indietro torna indietro davvero (AuthScreensV2),
            // non al vecchio pannello Ospite.
            SetPrivateField(auth, "registerUsernameInput", username);
            SetPrivateField(auth, "registerEmailInput", email);
            SetPrivateField(auth, "registerPasswordInput", password);
            SetPrivateField(auth, "registerButton", submit);
            SetPrivateField(auth, "registerBackButton", null);
            SetPrivateField(auth, "registerStatusText", status);

            screens.RegisterPanel = panel.gameObject;
            screens.RegisterToLogin = loginLink;
            screens.RegisterSubmit = submit;
            screens.RegisterUsername = username;
            screens.RegisterEmail = email;
            screens.RegisterPassword = password;
            screens.RegisterConfirm = confirm;
            screens.RegisterStatus = status;

            panel.gameObject.SetActive(wasActive);
        }

        /// <summary>Le quattro tacche di "Sicurezza password", tutte spente alla costruzione.</summary>
        private static Image[] StrengthBars(RectTransform design, float left, float top, float width, float height, float pitch)
        {
            var bars = new Image[4];
            for (int i = 0; i < bars.Length; i++)
            {
                var rect = MockRect(design, "StrengthBar" + i, left + i * pitch, top, width, height);
                var image = rect.gameObject.AddComponent<Image>();
                image.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 48f / (height * 0.5f);
                image.color = AuthStrengthOff;
                image.raycastTarget = false;
                bars[i] = image;
            }
            return bars;
        }

        /// <summary>
        /// Riga dei Termini: quadratino oro con la spunta (spenta all'inizio - l'accettazione deve
        /// essere un gesto dell'utente) e due righe di testo. Tutta la riga e' cliccabile, non solo
        /// il quadratino: e' un bersaglio da 43 px, troppo piccolo da centrare col dito.
        /// </summary>
        private static Button TermsRow(RectTransform design, float left, float top, out GameObject check,
            out Button terms, out Button privacy)
        {
            var rect = MockRect(design, "TermsRow", left, top, 700f, 74f);
            var surface = rect.gameObject.AddComponent<Image>();
            surface.color = new Color(0f, 0f, 0f, 0.001f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;

            // Spento = riquadro scuro con bordo oro (si vede che e' da spuntare); acceso = riquadro
            // oro pieno con la spunta scura, come nel mockup. Il riempimento oro sta sotto la spunta
            // e si accende insieme a lei.
            var box = MockRect(rect, "Box", 0f, 0f, 45f, 45f);
            AddRoundedPanel(box, "panel_fill_r24", 48f, 12f, AuthCheckBorder, 3f, AuthFieldFill, out _, out _);

            // Segno di spunta: ic_check del foglio icone e' verde acceso e la sua tinta e' cotta
            // nello sprite; qui serve scuro sull'oro, quindi si disegna con due barrette
            // arrotondate invece di storpiare un'icona fatta per un altro fondo.
            var mark = CreateUIObject("Check", box);
            mark.anchorMin = Vector2.zero;
            mark.anchorMax = Vector2.one;
            mark.offsetMin = new Vector2(3f, 3f);
            mark.offsetMax = new Vector2(-3f, -3f);
            var goldFill = mark.gameObject.AddComponent<Image>();
            goldFill.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            goldFill.type = Image.Type.Sliced;
            goldFill.pixelsPerUnitMultiplier = 48f / 9f;
            goldFill.color = AuthCheckFill;
            goldFill.raycastTarget = false;
            StrokeBar(mark, "Short", new Vector2(-7f, -3f), 14f, 6f, -45f, AuthCheckMark);
            StrokeBar(mark, "Long", new Vector2(4f, 2f), 26f, 6f, 50f, AuthCheckMark);
            check = mark.gameObject;
            check.SetActive(false);

            // "Termini di servizio" e "Privacy Policy" sono cliccabili e aprono il documento; il
            // resto della riga spunta la casella. I due link sono figli posti dopo, quindi
            // intercettano loro il tocco nella propria area: leggere non equivale ad accettare.
            terms = SentenceWithLink(rect, "Line1", "Accetto i ", "Termini di servizio", 62f, 0f, 20f, Color.white);
            privacy = SentenceWithLink(rect, "Line2", "e la ", "Privacy Policy", 62f, 34f, 20f, AuthSoftLink);
            return button;
        }

        /// <summary>
        /// Una riga "testo normale + parola cliccabile sottolineata". La parola viene messa subito
        /// dopo il testo misurandone la larghezza reale, cosi' la riga resta unita a qualunque
        /// lunghezza del testo.
        /// </summary>
        private static Button SentenceWithLink(RectTransform parent, string name, string prefix, string link,
            float left, float top, float size, Color prefixColor)
        {
            var label = MockText(parent, name, prefix, left, top, 620f, 36f, size, FontStyles.Normal, prefixColor, TextAlignmentOptions.MidlineLeft);
            label.enableWordWrapping = false;
            // TMP scarta lo spazio finale quando misura: misurando "prefisso" e basta il link
            // finirebbe attaccato alla parola prima. Si misura con una lettera in fondo e la si sottrae.
            float prefixWidth = label.GetPreferredValues(prefix + "x").x - label.GetPreferredValues("x").x;

            var linkLabel = MockText(parent, name + "Text", link, left + prefixWidth, top, 620f, 36f, size,
                FontStyles.Normal | FontStyles.Underline, AuthStrongLink, TextAlignmentOptions.MidlineLeft);
            linkLabel.enableWordWrapping = false;
            float linkWidth = linkLabel.GetPreferredValues(link).x;

            // Area di tocco un po' piu' alta della riga di testo, per non costringere a centrare il dito.
            var touch = MockRect(parent, name + "Link", left + prefixWidth - 6f, top - 6f, linkWidth + 12f, 48f);
            var surface = touch.gameObject.AddComponent<Image>();
            surface.color = new Color(0f, 0f, 0f, 0.001f);
            var button = touch.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            return button;
        }

        /// <summary>Barretta arrotondata inclinata, mattone per disegnare il segno di spunta.</summary>
        private static void StrokeBar(RectTransform parent, string name, Vector2 offset, float length, float thickness, float angle, Color color)
        {
            var bar = CreateUIObject(name, parent);
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0.5f);
            bar.anchoredPosition = offset;
            bar.sizeDelta = new Vector2(length, thickness);
            bar.localRotation = Quaternion.Euler(0f, 0f, angle);
            var image = bar.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(PanelsNeutralPath, "panel_fill_r24");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 48f / (thickness * 0.5f);
            image.color = color;
            image.raycastTarget = false;
        }

        private const string StartLogoPath = SheetsDir + "/logo_51.png";

        /// <summary>
        /// Logo del gioco, lo stesso della schermata iniziale (al posto del riquadro "51" dei mockup 23-24).
        /// </summary>
        private static void LogoBadge(RectTransform design, float left, float top, float width, float height)
        {
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(StartLogoPath);
            if (logo == null) throw new System.Exception("Logo non trovato: " + StartLogoPath);
            var image = MockRect(design, "Logo", left, top, width, height).gameObject.AddComponent<Image>();
            image.sprite = logo;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        /// <summary>
        /// Campo di testo del mockup: rettangolo arrotondato scuro con bordo sottile, segnaposto e
        /// testo in Poppins. Il bordo e' anche la superficie cliccabile del campo.
        /// </summary>
        private static TMP_InputField AuthField(RectTransform design, string name, string placeholder,
            float left, float top, float width, float height, bool isPassword)
        {
            var rect = MockRect(design, name, left, top, width, height);
            var border = AddRoundedPanel(rect, "panel_fill_r24", 48f, 24f, AuthFieldBorder, 2f, AuthFieldFill, out _, out _);
            border.raycastTarget = true;

            var viewport = CreateUIObject("TextArea", rect);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(28f, 8f);
            viewport.offsetMax = new Vector2(-28f, -8f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var placeholderText = AddText(Stretch(viewport, "Placeholder"), placeholder, 24f, FontStyles.Normal, AuthPlaceholder, TextAlignmentOptions.MidlineLeft);
            placeholderText.enableWordWrapping = false;
            var valueText = AddText(Stretch(viewport, "Text"), "", 24f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
            valueText.enableWordWrapping = false;

            var field = rect.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = border;
            field.textViewport = viewport;
            field.textComponent = valueText;
            field.placeholder = placeholderText;
            field.caretColor = AuthTitleGold;
            field.customCaretColor = true;
            field.selectionColor = new Color32(58, 86, 128, 160);
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            field.text = string.Empty;
            return field;
        }

        /// <summary>
        /// Testo cliccabile ("Password dimenticata?", "REGISTRATI"): nessuna cornice, solo un'area
        /// trasparente che riceve il tocco sotto all'etichetta.
        /// </summary>
        private static Button LinkButton(RectTransform design, string name, string label, float left, float top,
            float width, float height, float size, FontStyles style, Color color, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var rect = MockRect(design, name, left, top, width, height);
            var surface = rect.gameObject.AddComponent<Image>();
            surface.color = new Color(0f, 0f, 0f, 0.001f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            var text = AddText(Stretch(rect, "Label"), label, size, style, color, alignment);
            if (font != null) UseFont(text, font, font.material);
            return button;
        }

        /// <summary>
        /// Riquadro dei messaggi (errori di validazione, esito del recupero password). Sta nello
        /// spazio libero sotto al modulo: nella fessura fra l'ultimo campo e il pulsante principale
        /// non ci sta una frase lunga, e il bagliore del pulsante le passava sopra.
        /// </summary>
        private static TMP_Text NoticeArea(RectTransform design)
        {
            var status = MockText(design, "StatusText", "", 90f, 1370f, 900f, 120f, 20f,
                FontStyles.Normal, AuthSoftLink, TextAlignmentOptions.Top);
            status.enableWordWrapping = true;
            status.overflowMode = TextOverflowModes.Overflow;
            status.lineSpacing = 6f;
            return status;
        }

        private static void DividerLine(RectTransform design, string name, float left, float top, float width)
        {
            var line = MockRect(design, name, left, top, width, 2f).gameObject.AddComponent<Image>();
            line.color = OnLine;
            line.raycastTarget = false;
        }

        /// <summary>
        /// Pulsante indietro: lo sprite ic_arrow_left e' un pulsante tondo gia' completo (disco di
        /// legno con cornice oro e freccia dentro), non un'icona da mettere dentro una cornice - va
        /// usato da solo, altrimenti si vedrebbe un pulsante dentro un pulsante. Il riquadro che
        /// riceve il tocco e' piu' largo del disco visibile, per non avere un bersaglio minuscolo
        /// sui telefoni. Lo sprite ha margini trasparenti asimmetrici: ci pensa MockSprite a far
        /// coincidere la parte VISIBILE con il riquadro chiesto.
        /// </summary>
        private static Button BackButton(RectTransform design, string name, float centerX, float centerY, float visibleSize)
        {
            const float touchSize = 116f;
            var rect = MockRect(design, name, centerX - touchSize * 0.5f, centerY - touchSize * 0.5f, touchSize, touchSize);
            var surface = rect.gameObject.AddComponent<Image>();
            surface.color = new Color(0f, 0f, 0f, 0.001f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;

            float inset = (touchSize - visibleSize) * 0.5f;
            MockSprite(rect, "Art", LoadSprite(SheetsDir + "/ic_arrow_left.png", "ic_arrow_left"), inset, inset, visibleSize, visibleSize, false);
            return button;
        }
    }
}
