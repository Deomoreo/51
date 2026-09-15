using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce la scena HomeScreen.unity da zero seguendo Assets/UI_SPEC_Home.md
    /// (sezione 3: ancoraggi e misure esatte). Non tocca MainMenu.unity.
    /// </summary>
    public static class HomeScreenBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Home Screen")]
        private static void Build()
        {
            if (!LoadContext())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));

            RectTransform canvasRect = CreateCanvas();
            CreateVideoBackground(canvasRect);
            RectTransform safeArea = CreateSafeArea(canvasRect);

            RectTransform topBar = CreateTopBar(safeArea);

            var pagesViewport = CreateUIObject("PagesViewport", safeArea);
            StretchFill(pagesViewport);

            var homePage = CreateHomePage(pagesViewport);
            var deckPage = CreatePlaceholderPage(pagesViewport, "DeckPage", "MAZZO (in arrivo)");
            var shopPage = CreatePlaceholderPage(pagesViewport, "ShopPage", "NEGOZIO (in arrivo)");
            var profilePage = CreatePlaceholderPage(pagesViewport, "ProfilePage", "PROFILO (in arrivo)");

            // TopBar deve disegnarsi SOPRA le pagine (DeckPage/ShopPage/ProfilePage hanno uno
            // sfondo opaco a schermo intero che altrimenti la coprirebbe, dato che l'ordine di
            // rendering degli UI siblings segue l'ordine nella hierarchy - bug segnalato
            // dall'utente su Collezione/Negozio/Profilo, assente su Gioca solo perche' quella
            // pagina ha lo sfondo trasparente). La NavBar in basso era gia' l'ultimo sibling
            // quindi non ne soffriva.
            topBar.SetAsLastSibling();

            var navBarRefs = CreateNavBar(safeArea);

            var pages = new[] { homePage, deckPage, shopPage, profilePage };
            WireNavigation(pagesViewport, pages, navBarRefs, topBar);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[HomeScreenBuilder] Scena creata: {ScenePath}");
        }

        // ------------------------------------------------------------------
        // PagesViewport / swipe pages
        // ------------------------------------------------------------------

        private class NavTabRefs
        {
            public Button Button;
            public RectTransform Rect;
            public Graphic Icon;
            public Graphic Label;
        }

        private class NavBarRefs
        {
            public RectTransform NavBar;
            public RectTransform Highlight;
            public Button[] TabButtons;
            public RectTransform[] TabRects;
            public Graphic[] TabIcons;
            public Graphic[] TabLabels;
        }

        private static RectTransform CreateHomePage(RectTransform pagesViewport)
        {
            var page = CreateUIObject("HomePage", pagesViewport);
            StretchFill(page);

            var catcher = page.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            CreateRightRail(page);
            CreateBottomSection(page);
            return page;
        }

        private static RectTransform CreatePlaceholderPage(RectTransform pagesViewport, string name, string label)
        {
            var page = CreateUIObject(name, pagesViewport);
            StretchFill(page);

            var bg = page.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            bg.raycastTarget = true;

            var textRt = CreateUIObject("Label", page);
            SetAnchoredRect(textRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(700f, 200f), Vector2.zero);
            AddText(textRt, label, 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            page.gameObject.SetActive(false);

            return page;
        }

        private static void WireNavigation(RectTransform pagesViewport, RectTransform[] pages, NavBarRefs navBarRefs, RectTransform topBar)
        {
            var swipeController = pagesViewport.gameObject.AddComponent<PanelSwipeController>();
            var soSwipe = new SerializedObject(swipeController);
            soSwipe.FindProperty("pagesViewport").objectReferenceValue = pagesViewport;
            var pagesProp = soSwipe.FindProperty("pages");
            pagesProp.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++)
                pagesProp.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            soSwipe.FindProperty("bottomBar").objectReferenceValue = navBarRefs.NavBar;
            soSwipe.FindProperty("topBar").objectReferenceValue = topBar;
            soSwipe.FindProperty("pageHorizontalOverflow").floatValue = 0f;
            soSwipe.ApplyModifiedPropertiesWithoutUndo();

            foreach (var page in pages)
            {
                var navigator = page.gameObject.AddComponent<SwipeSectionNavigator>();
                var soNav = new SerializedObject(navigator);
                soNav.FindProperty("swipeController").objectReferenceValue = swipeController;
                soNav.FindProperty("blockSwipeOver").objectReferenceValue = navBarRefs.NavBar;
                soNav.ApplyModifiedPropertiesWithoutUndo();
            }

            var navController = navBarRefs.NavBar.gameObject.AddComponent<HomeNavBarController>();
            var soNavBar = new SerializedObject(navController);
            soNavBar.FindProperty("swipeController").objectReferenceValue = swipeController;
            soNavBar.FindProperty("highlight").objectReferenceValue = navBarRefs.Highlight;
            AssignObjectArray(soNavBar, "tabButtons", navBarRefs.TabButtons);
            AssignObjectArray(soNavBar, "tabRects", navBarRefs.TabRects);
            AssignObjectArray(soNavBar, "tabIcons", navBarRefs.TabIcons);
            AssignObjectArray(soNavBar, "tabLabels", navBarRefs.TabLabels);
            soNavBar.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignObjectArray(SerializedObject so, string propertyName, UnityEngine.Object[] values)
        {
            var prop = so.FindProperty(propertyName);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        // ------------------------------------------------------------------
        // Contesto: manifest sprite + tema
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[HomeScreenBuilder] UITheme non trovato in {ThemePath}.");
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------------
        // Canvas / VideoBackground / SafeArea
        // ------------------------------------------------------------------

        private static RectTransform CreateCanvas()
        {
            var canvasGO = new GameObject("Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return (RectTransform)canvasGO.transform;
        }

        private static void CreateVideoBackground(RectTransform canvasRect)
        {
            var videoBg = CreateUIObject("VideoBackground", canvasRect);
            StretchFill(videoBg);

            var rawImage = videoBg.gameObject.AddComponent<RawImage>();
            rawImage.texture = Texture2D.whiteTexture;
            rawImage.color = new Color(0.06f, 0.08f, 0.12f, 1f); // segnaposto statico, no video loop
            rawImage.raycastTarget = false;
        }

        private static RectTransform CreateSafeArea(RectTransform canvasRect)
        {
            var safeArea = CreateUIObject("SafeArea", canvasRect);
            StretchFill(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            return safeArea;
        }

        // ------------------------------------------------------------------
        // TopBar
        // ------------------------------------------------------------------

        private static RectTransform CreateTopBar(RectTransform safeArea)
        {
            var topBar = CreateUIObject("TopBar", safeArea);
            SetAnchoredRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 150f), Vector2.zero);

            CreatePlayerBadge(topBar);
            CreateStatBar(topBar, "CoinBar", "bar_coin", new Vector2(0f, 1f), new Vector2(232f, -40f),
                new Vector2(280f, 70f), "1.250");
            CreateStatBar(topBar, "GemBar", "bar_energy", new Vector2(0f, 1f), new Vector2(530f, -40f),
                new Vector2(214f, 70f), "48");
            CreateStatBar(topBar, "XpBar", "bar_empty", new Vector2(1f, 1f), new Vector2(-22f, -40f),
                new Vector2(218f, 70f), "320/500");
            return topBar;
        }

        private static void CreatePlayerBadge(RectTransform topBar)
        {
            var badge = CreateUIObject("PlayerBadge", topBar);
            // Quadrata (168x168): la spec originale (180x150) stirava avatar_frame, che e' un'immagine quadrata.
            SetAnchoredRect(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(168f, 168f), new Vector2(24f, -30f));

            var avatarFrame = CreateUIObject("AvatarFrame", badge);
            StretchFill(avatarFrame);
            // preserveAspect come rete di sicurezza, anche se il contenitore e' gia' quadrato.
            AddSpriteImage(avatarFrame, "avatar_frame", preserveAspect: true);

            var levelBadge = CreateUIObject("LevelBadge", badge);
            SetAnchoredRect(levelBadge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(52f, 52f), new Vector2(-8f, 4f));
            AddSpriteImage(levelBadge, "ic_coin_clover", preserveAspect: true);

            var levelText = CreateUIObject("LevelText", levelBadge);
            StretchFill(levelText);
            AddText(levelText, "1", 20f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            // avatar_frame.png include gia' una targa dorata vuota nella fascia inferiore
            // (corona + cornice circolare + targa, tutto in un solo sprite): il testo va
            // dentro quella fascia, non sotto la cornice come se fosse nuda. Colore scuro
            // per contrasto sul fondo dorato (stessa logica del testo GIOCA su btn_gold_long).
            var nameText = CreateUIObject("NameText", badge);
            SetAnchoredRect(nameText, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(140f, 28f), new Vector2(0f, 6f));
            var name = AddText(nameText, "Giocatore", 24f, FontStyles.Bold, Hex("#3A2208"), TextAlignmentOptions.Center);
            name.enableAutoSizing = true;
            name.fontSizeMin = 15f;
            name.fontSizeMax = 24f;
            name.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void CreateStatBar(RectTransform topBar, string name, string spriteName,
            Vector2 anchor, Vector2 pos, Vector2 size, string placeholderValue)
        {
            var bar = CreateUIObject(name, topBar);
            SetAnchoredRect(bar, anchor, anchor, anchor, size, pos);

            var background = CreateUIObject("Background", bar);
            StretchFill(background);
            AddSpriteImage(background, spriteName, sliced: true);

            var valueText = CreateUIObject("ValueText", bar);
            SetAnchoredRect(valueText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(-24f, -8f), Vector2.zero);
            AddText(valueText, placeholderValue, 28f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // RightRail
        // ------------------------------------------------------------------

        private static void CreateRightRail(RectTransform safeArea)
        {
            var rail = CreateUIObject("RightRail", safeArea);
            SetAnchoredRect(rail, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(112f, 0f), new Vector2(-24f, -240f));

            var layout = rail.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 34f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            rail.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateRailButton(rail, "RewardButton", "chest_green", "Premi");
            CreateRailButton(rail, "LeaderboardButton", "ic_trophy", "Classifica");
            CreateRailButton(rail, "MailButton", "ic_mail", "Posta");
            // NON aggiungere qui un bottone "Personalizza": Mazzo/Accuso/Emoticon vivono
            // nella pagina CARTE (DeckPage, vedi DeckPageBuilder), navigata dalla bottom nav
            // bar. La scorciatoia su Home resta solo il DeckSelector (solo mazzo, vedi
            // PanelMazzoBuilder), niente entry point separato qui.

            LayoutRebuilder.ForceRebuildLayoutImmediate(rail);
        }

        private static void CreateRailButton(RectTransform rail, string name, string iconSprite, string label)
        {
            var button = CreateUIObject(name, rail);
            SetAnchoredRect(button, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(112f, 112f), Vector2.zero);

            var layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 112f;
            layoutElement.preferredHeight = 112f;

            var background = AddSpriteImage(button, "sq_blue", raycastTarget: true, sliced: true);
            button.gameObject.AddComponent<Button>().targetGraphic = background;

            var icon = CreateUIObject("Icon", button);
            SetAnchoredRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(66f, 66f), Vector2.zero);
            var iconImage = AddSpriteImage(icon, iconSprite, preserveAspect: true);

            var labelRt = CreateUIObject("Label", button);
            SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(112f, 24f), new Vector2(0f, -16f));
            AddText(labelRt, label, 17f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);

            AssignThemedButton(button.gameObject, background, null, ThemedButton.Variant.IconTab, iconImage: iconImage);
        }

        // ------------------------------------------------------------------
        // BottomSection
        // ------------------------------------------------------------------

        private static void CreateBottomSection(RectTransform safeArea)
        {
            var section = CreateUIObject("BottomSection", safeArea);
            SetAnchoredRect(section, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 420f), new Vector2(0f, 0f));

            // Larghezza 450 (non 465: uno scarto di 3px per lato era impercettibile nello
            // screenshot) per un gap di 60px, ben visibile, mantenendo i margini di 60px:
            // Valori 400x132 verificati a mano in editor (i precedenti 450/465/468 lasciavano
            // ancora i due bottoni troppo larghi/vicini nel confronto visivo con il mockup).
            CreateSelector(section, "ModeSelector", "btn_blue_long", "ic_gamepad", "MODALITÀ", "Allenamento",
                new Vector2(0f, 0f), new Vector2(60f, 214f), new Vector2(400f, 132f));
            CreateSelector(section, "DeckSelector", "btn_teal", "ic_cards", "MAZZO", "Napoletano",
                new Vector2(1f, 0f), new Vector2(-60f, 214f), new Vector2(400f, 132f));

            CreatePlayButton(section);
        }

        private static void CreateSelector(RectTransform parent, string name, string backgroundSprite,
            string iconSprite, string labelText, string valueText, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var selector = CreateUIObject(name, parent);
            SetAnchoredRect(selector, anchor, anchor, anchor, size, pos);

            var background = AddSpriteImage(selector, backgroundSprite, raycastTarget: true);
            // Simple invece di Sliced (default per "sliced" da manifest): verificato a mano in
            // editor, con Sliced questi due sprite non rendono bene a questa dimensione.
            background.type = Image.Type.Simple;
            selector.gameObject.AddComponent<Button>().targetGraphic = background;

            var icon = CreateUIObject("Icon", selector);
            SetAnchoredRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(68f, 68f), new Vector2(26f, 0f));
            AddSpriteImage(icon, iconSprite, preserveAspect: true);

            // Larghezza 270 (non piu' 320): con il bottone a 400px di larghezza, partendo da
            // X108, 320 avrebbe sforato il bordo destro di 28px (108+320=428 > 400).
            var labelRt = CreateUIObject("LabelText", selector);
            SetAnchoredRect(labelRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(270f, 28f), new Vector2(108f, -24f));
            AddText(labelRt, labelText, 20f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Left);

            var valueRt = CreateUIObject("ValueText", selector);
            SetAnchoredRect(valueRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(270f, 44f), new Vector2(108f, 24f));
            var value = AddText(valueRt, valueText, 27f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Left);

            AssignThemedButton(selector.gameObject, background, value, ThemedButton.Variant.Secondary,
                spriteOverride: background.sprite);
            // ApplyPill() forza sempre l'alignment a Midline: lo correggiamo per il layout "left" richiesto.
            value.alignment = TextAlignmentOptions.Left;
        }

        private static void CreatePlayButton(RectTransform section)
        {
            var playButton = CreateUIObject("PlayButton", section);
            SetAnchoredRect(playButton, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(560f, 148f), new Vector2(0f, 40f));
            playButton.gameObject.AddComponent<Button>();

            // Glow: niente glow_soft.png (non esiste piu' nel progetto). Invece di un rettangolo
            // pieno (che lascia sporgere angoli quadrati fuori dalla pillola arrotondata, il
            // difetto "scontornato" segnalato), riusiamo lo stesso sprite/border del bottone
            // leggermente ingrandito: stessa sagoma arrotondata, nessun angolo vivo che sporge.
            // E' il primo figlio quindi disegnato per primo, dietro a Background e Text.
            var glow = CreateUIObject("Glow", playButton);
            SetAnchoredRect(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(560f + 48f, 148f + 40f), Vector2.zero);
            var glowImage = AddSpriteImage(glow, "btn_gold_long", sliced: true);
            Color glowColor = _theme.Gold;
            glowColor.a = 0.4f;
            glowImage.color = glowColor;

            var backgroundRt = CreateUIObject("Background", playButton);
            StretchFill(backgroundRt);
            var background = AddSpriteImage(backgroundRt, "btn_gold_long", raycastTarget: true, sliced: true);
            playButton.GetComponent<Button>().targetGraphic = background;

            var textRt = CreateUIObject("Text", playButton);
            StretchFill(textRt);
            var text = AddText(textRt, "GIOCA", 54f, FontStyles.Bold, Hex("#3A2208"), TextAlignmentOptions.Midline);

            AssignThemedButton(playButton.gameObject, background, text, ThemedButton.Variant.Primary,
                spriteOverride: background.sprite, overrideLabelColor: true, labelColorOverride: Hex("#3A2208"));
        }

        // ------------------------------------------------------------------
        // NavBar
        // ------------------------------------------------------------------

        private static NavBarRefs CreateNavBar(RectTransform safeArea)
        {
            var navBar = CreateUIObject("NavBar", safeArea);
            SetAnchoredRect(navBar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 172f), Vector2.zero);

            CreateNavBarBackground(navBar);

            // Dimensione/posizione verificate a mano in editor (170 di altezza, sollevato di
            // 30px da terra) per coprire bene icona+etichetta del tab attivo.
            var highlight = CreateUIObject("ActiveTabHighlight", navBar);
            SetAnchoredRect(highlight, new Vector2(0.125f, 0f), new Vector2(0.125f, 0f), new Vector2(0.5f, 0f),
                new Vector2(248f, 150f), new Vector2(0f, 30f));
            AddSpriteImage(highlight, "tab_gold", sliced: true);

            string[] names = { "TabGioca", "TabCarte", "TabNegozio", "TabProfilo" };
            string[] icons = { "ic_gamepad", "ic_cards", "ic_cart", "ic_person" };
            string[] labels = { "GIOCA", "COLLEZIONE", "NEGOZIO", "PROFILO" };

            var refs = new NavBarRefs
            {
                NavBar = navBar,
                Highlight = highlight,
                TabButtons = new Button[4],
                TabRects = new RectTransform[4],
                TabIcons = new Graphic[4],
                TabLabels = new Graphic[4],
            };

            for (int i = 0; i < 4; i++)
            {
                var tabData = CreateNavTab(navBar, names[i], icons[i], labels[i], i, active: i == 0);
                refs.TabButtons[i] = tabData.Button;
                refs.TabRects[i] = tabData.Rect;
                refs.TabIcons[i] = tabData.Icon;
                refs.TabLabels[i] = tabData.Label;
            }

            return refs;
        }

        private static void CreateNavBarBackground(RectTransform navBar)
        {
            var background = CreateUIObject("Background", navBar);
            StretchFill(background);
            var bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = Hex("#0D1626");
            bgImage.raycastTarget = false;

            var topLine = CreateUIObject("TopLine", navBar);
            SetAnchoredRect(topLine, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 4f), Vector2.zero);
            var lineImage = topLine.gameObject.AddComponent<Image>();
            lineImage.color = Hex("#3A5880");
            lineImage.raycastTarget = false;
        }

        private static NavTabRefs CreateNavTab(RectTransform navBar, string name, string iconSprite, string label,
            int index, bool active)
        {
            var tab = CreateUIObject(name, navBar);
            float xMin = index * 0.25f;
            float xMax = xMin + 0.25f;
            SetAnchoredRect(tab, new Vector2(xMin, 0f), new Vector2(xMax, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            var hitArea = tab.gameObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);
            hitArea.raycastTarget = true;
            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = hitArea;

            Color color = Hex(active ? "#FFE49C" : "#94ACCA");

            var icon = CreateUIObject("Icon", tab);
            SetAnchoredRect(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(70f, 70f), new Vector2(0f, -20f));
            var iconImage = AddSpriteImage(icon, iconSprite, preserveAspect: true);
            iconImage.color = color;

            var labelRt = CreateUIObject("Label", tab);
            SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(260f, 28f), new Vector2(0f, 14f));
            var labelText = AddText(labelRt, label, 21f, FontStyles.Normal, color, TextAlignmentOptions.Center);
            // "COLLEZIONE" e' piu' lungo di GIOCA/NEGOZIO/PROFILO e a font fisso rischia di
            // uscire dalla tab (ogni tab e' larga solo 1/4 navbar, senza margine extra) -
            // auto-size cosi' si restringe da solo se serve, invariato per le altre etichette.
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 14f;
            labelText.fontSizeMax = 21f;

            return new NavTabRefs { Button = button, Rect = tab, Icon = iconImage, Label = labelText };
        }

        // ------------------------------------------------------------------
        // ThemedButton wiring
        // ------------------------------------------------------------------

        private static void AssignThemedButton(GameObject buttonGO, Image background, TMP_Text label,
            ThemedButton.Variant variant, Sprite spriteOverride = null,
            bool overrideLabelColor = false, Color labelColorOverride = default, Image iconImage = null)
        {
            // Il GameObject resta inattivo finche' i campi non sono assegnati, cosi' OnEnable()
            // non chiama Apply() con un UITheme nullo (eviterebbe un warning spurio in Console).
            buttonGO.SetActive(false);
            var themedButton = buttonGO.AddComponent<ThemedButton>();

            var so = new SerializedObject(themedButton);
            so.FindProperty("theme").objectReferenceValue = _theme;
            so.FindProperty("variant").enumValueIndex = (int)variant;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("backgroundSpriteOverride").objectReferenceValue = spriteOverride;
            so.FindProperty("overrideLabelColor").boolValue = overrideLabelColor;
            so.FindProperty("labelColorOverride").colorValue = labelColorOverride;
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            buttonGO.SetActive(true);
            themedButton.Apply();
        }

        // ------------------------------------------------------------------
        // Helper generici
        // ------------------------------------------------------------------

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void SetAnchoredRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string fileNameNoExt)
        {
            return DragonsHoardSprites.Find(fileNameNoExt);
        }

        /// <summary>
        /// preserveAspect vale per gli usi "icona" (icone/RightRail/Selector/NavBar), non per
        /// immagini strutturali come AvatarFrame che devono riempire per intero il loro rect.
        /// sliced e' deciso dal chiamante (prima veniva dal "type" in import_manifest.json,
        /// ora che gli sprite vengono dai fogli Multiple serve dirlo esplicitamente).
        /// </summary>
        private static Image AddSpriteImage(RectTransform rt, string fileNameNoExt,
            bool raycastTarget = false, bool preserveAspect = false, bool sliced = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(fileNameNoExt);
            image.raycastTarget = raycastTarget;
            image.preserveAspect = preserveAspect;
            if (sliced)
            {
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string content, float fontSize,
            FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
        }
    }
}
