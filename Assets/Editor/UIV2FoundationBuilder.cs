using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Core;
using Project51.UIV2.Components;
using Project51.UIV2.Screens;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce la FOUNDATION della UI V2 (Assets/UIV2): shell UIV2_Root + component
    /// library base + l'esempio data-driven Friends, come prefab isolati. A differenza dei
    /// builder Dragon's Hoard legacy (coordinate pixel fisse su una scena precisa), qui la
    /// geometria e' sempre anchor/LayoutGroup based cosi' i prefab restano validi a
    /// qualunque risoluzione e possono essere droppati in qualunque scena futura. Lavora in
    /// una scena scratch dedicata (mai una scena esistente) e non la salva: alla fine
    /// restano solo gli asset prefab su disco.
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string ThemePath = "Assets/UIV2/Art/UIV2Theme.asset";
        private const string CorePrefabDir = "Assets/UIV2/Prefabs/Core";
        private const string ComponentsPrefabDir = "Assets/UIV2/Prefabs/Components";
        private const string ScreensPrefabDir = "Assets/UIV2/Prefabs/Screens";

        private const string SheetsDir = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity";
        private const string IconsPath = SheetsDir + "/Icons.png";
        private const string Icons2Path = SheetsDir + "/Icons2.png";
        private const string PanelsNeutralPath = SheetsDir + "/PanelsNeutral_v2.png";
        private const string AvatarsPath = SheetsDir + "/Avatars.png";
        private const string EmoticonsPath = SheetsDir + "/14_emoticon_set.png";

        private static UIV2Theme _theme;
        private static readonly Dictionary<string, Sprite[]> _sheetCache = new Dictionary<string, Sprite[]>();

        [MenuItem("Tools/UIV2/Build Foundation")]
        private static void Build()
        {
            EnsureFolders();
            _theme = LoadOrCreateTheme();
            WireThemeSprites(_theme);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- Component library (leaf components first: altri prefab le referenziano) ----
            var goldButton = BuildButton("UIV2_PrimaryGoldButton", UIV2Button.Style.PrimaryGold, "GIOCA");
            BuildButton("UIV2_SecondaryBlueButton", UIV2Button.Style.SecondaryBlue, "ANNULLA");
            BuildButton("UIV2_TealButton", UIV2Button.Style.Teal, "INVITA");
            var greenSmallButton = BuildButton("UIV2_GreenSmallButton", UIV2Button.Style.GreenSmall, "OK");

            var avatarBadge = BuildAvatarBadge();
            var resourcePill = BuildResourcePill();
            BuildSectionHeader();
            var progressBar = BuildProgressBar();
            BuildStatTile();
            BuildListRow();
            BuildCollectionCard();
            BuildModalFrame();
            BuildPlayerRow(avatarBadge);
            BuildTopBar(avatarBadge, resourcePill, progressBar);
            BuildBottomNav();

            // ---- Screens (esempio data-driven Friends) ----
            var friendRow = BuildFriendRow(avatarBadge, greenSmallButton);
            BuildFriendsScreen(friendRow);

            // ---- App shell ----
            BuildRoot();
            AssetDatabase.SaveAssets();

            Debug.Log("[UIV2FoundationBuilder] Foundation UIV2 costruita in Assets/UIV2/Prefabs. " +
                      "Scena scratch NON salvata (nessuna scena esistente e' stata toccata).");
        }

        /// <summary>
        /// Costruisce la prima schermata reale (HomeScreenV2) SENZA ricostruire la
        /// foundation: carica il theme e i prefab gia' esistenti (UIV2_PrimaryGoldButton)
        /// da disco invece di rigenerarli, cosi' la foundation ormai congelata (vedi
        /// BuildProgressBar/BuildButton/ecc. sopra) resta intatta.
        /// </summary>
        [MenuItem("Tools/UIV2/Build Home Screen V2")]
        private static void BuildHomeScreenV2Entry()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UIV2Theme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError("[UIV2FoundationBuilder] UIV2Theme non trovato - esegui prima 'Tools/UIV2/Build Foundation'.");
                return;
            }

            var goldButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ComponentsPrefabDir}/UIV2_PrimaryGoldButton.prefab");
            if (goldButtonPrefab == null)
            {
                Debug.LogError("[UIV2FoundationBuilder] UIV2_PrimaryGoldButton.prefab non trovato - esegui prima 'Tools/UIV2/Build Foundation'.");
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var quickActionButtonPrefab = BuildQuickActionButtonPrefab();
            var modeSelectorPrefab = BuildSelectorChipPrefab("UIV2_ModeSelector", "MODALIT\u00C0",
                LoadSprite(IconsPath, "btn_blue_long"), _theme.ButtonSecondaryBlue, LoadSprite(IconsPath, "ic_gamepad"));
            var deckSelectorPrefab = BuildSelectorChipPrefab("UIV2_DeckSelector", "MAZZO",
                LoadSprite(IconsPath, "btn_teal"), _theme.ButtonTeal, LoadSprite(IconsPath, "ic_cards"));

            BuildHomeScreenV2Prefab(quickActionButtonPrefab, modeSelectorPrefab, deckSelectorPrefab, goldButtonPrefab);

            Debug.Log("[UIV2FoundationBuilder] HomeScreenV2 costruita in " +
                      $"{ScreensPrefabDir}/HomeScreenV2.prefab. Scena scratch NON salvata.");
        }

        private static void EnsureFolders()
        {
            CreateFolderRecursive(CorePrefabDir);
            CreateFolderRecursive(ComponentsPrefabDir);
            CreateFolderRecursive(ScreensPrefabDir);
        }

        private static void CreateFolderRecursive(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static UIV2Theme LoadOrCreateTheme()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UIV2Theme>(ThemePath);
            if (existing != null) return existing;

            var theme = ScriptableObject.CreateInstance<UIV2Theme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
            AssetDatabase.SaveAssets();
            return theme;
        }

        /// <summary>
        /// Collega gli sprite reali gia' presenti nel progetto (Icons/Icons2/PanelsNeutral_v2/
        /// Avatars, vedi Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity) invece dei
        /// placeholder a colore piatto. PanelsNeutral_v2 fornisce forme neutre da tintare
        /// (fill/ring) per pannello e bordo modal; i bottoni/bar/icone sono asset gia' colorati
        /// (nessuna tinta - vedi ApplySpriteOrColor).
        /// </summary>
        private static void WireThemeSprites(UIV2Theme theme)
        {
            theme.PanelBackground = LoadSprite(PanelsNeutralPath, "panel_fill_r30");
            theme.PanelBorder = LoadSprite(PanelsNeutralPath, "panel_ring_r30");
            theme.RibbonSprite = LoadSprite(IconsPath, "ribbon_teal");
            theme.ButtonPrimarySprite = LoadSprite(IconsPath, "btn_gold_long");
            theme.ButtonSecondarySprite = LoadSprite(IconsPath, "btn_blue_long");
            theme.ButtonTealSprite = LoadSprite(IconsPath, "btn_teal");
            theme.ButtonGreenSmallSprite = LoadSprite(IconsPath, "btn_green_small");
            theme.PillBackground = LoadSprite(IconsPath, "bar_empty");
            theme.AvatarFrame = LoadSprite(PanelsNeutralPath, "panel_ring_r24");
            theme.DefaultAvatarSilhouette = LoadSprite(IconsPath, "ic_person");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }

        private static Sprite _neutralCircleSprite;

        /// <summary>
        /// Pallino di stato online/offline: primitiva UI, non un asset del mockup - un cerchio
        /// neutro (lo sprite "Knob" incluso in Unity) tinto verde/grigio a runtime, come
        /// richiesto esplicitamente invece di generare un nuovo PNG per questo.
        /// </summary>
        private static Sprite NeutralCircleSprite =>
            _neutralCircleSprite ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        private static Sprite LoadSprite(string sheetPath, string spriteName)
        {
            if (!_sheetCache.TryGetValue(sheetPath, out var sprites))
            {
                sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
                _sheetCache[sheetPath] = sprites;
            }

            var found = sprites.FirstOrDefault(s => s.name == spriteName);
            if (found == null)
            {
                Debug.LogError($"[UIV2FoundationBuilder] Sprite '{spriteName}' non trovato in {sheetPath}.");
            }
            return found;
        }

        /// <summary>
        /// Sprite reale se disponibile (Sliced e non tintato, l'arte e' gia' colorata) altrimenti
        /// fallback sul colore piatto placeholder - stesso pattern di UIV2Button.Apply().
        /// </summary>
        private static void ApplySpriteOrColor(Image image, Sprite sprite, Color fallbackColor, bool sliced = true)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                image.color = Color.white;
            }
            else
            {
                image.color = fallbackColor;
            }
        }

        /// <summary>
        /// Come ApplySpriteOrColor ma tinge lo sprite col colore del tema (per le forme neutre
        /// di PanelsNeutral_v2, pensate apposta per essere colorate via Image.color).
        /// </summary>
        private static void ApplyTintedSpriteOrColor(Image image, Sprite sprite, Color themeColor, bool sliced = true)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            image.color = themeColor;
        }

        // ------------------------------------------------------------------
        // App shell: UIV2_Root
        // ------------------------------------------------------------------

        private static void BuildRoot()
        {
            var rootGo = new GameObject("UIV2_Root", typeof(RectTransform));
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            rootGo.AddComponent<GraphicRaycaster>();
            var rootRect = (RectTransform)rootGo.transform;
            StretchFill(rootRect);

            var background = CreateUIObject("BackgroundLayer", rootRect);
            StretchFill(background);
            var bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = _theme.PanelBlue;
            bgImage.raycastTarget = false;

            var safeArea = CreateUIObject("SafeArea", rootRect);
            StretchFill(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            var safeLayout = safeArea.gameObject.AddComponent<VerticalLayoutGroup>();
            safeLayout.childControlWidth = true;
            safeLayout.childControlHeight = true;
            safeLayout.childForceExpandWidth = true;
            safeLayout.childForceExpandHeight = false;

            var topBarHost = CreateUIObject("TopBarHost", safeArea);
            // 262 (non 160): calibrato 2026-09-13 su home_B2 (1).png per ospitare
            // avatar(190)+cartiglio nome(44)+padding TopBar(24) senza tagliarli - vedi
            // BuildTopBar/ProfileGroup.
            AddLayoutElement(topBarHost, preferredHeight: 262f, flexibleHeight: 0f);

            var screenHost = CreateUIObject("ScreenHost", safeArea);
            AddLayoutElement(screenHost, flexibleHeight: 1f);

            var bottomNavHost = CreateUIObject("BottomNavHost", safeArea);
            AddLayoutElement(bottomNavHost, preferredHeight: 170f, flexibleHeight: 0f);

            var modalHost = CreateUIObject("ModalHost", rootRect);
            StretchFill(modalHost);

            var overlayHost = CreateUIObject("OverlayHost", rootRect);
            StretchFill(overlayHost);

            var fxHost = CreateUIObject("FXHost", rootRect);
            StretchFill(fxHost);
            fxHost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            var uiv2Root = rootGo.AddComponent<UIV2Root>();
            SetPrivateField(uiv2Root, "backgroundLayer", background);
            SetPrivateField(uiv2Root, "safeArea", safeArea);
            SetPrivateField(uiv2Root, "topBarHost", topBarHost);
            SetPrivateField(uiv2Root, "screenHost", screenHost);
            SetPrivateField(uiv2Root, "bottomNavHost", bottomNavHost);
            SetPrivateField(uiv2Root, "modalHost", modalHost);
            SetPrivateField(uiv2Root, "overlayHost", overlayHost);
            SetPrivateField(uiv2Root, "fxHost", fxHost);

            var modalHostComp = rootGo.AddComponent<UIV2ModalHost>();
            SaveAsPrefab(rootGo, $"{CorePrefabDir}/UIV2_Root.prefab");
        }

        // ------------------------------------------------------------------
        // Buttons (un solo script, 4 prefab/stili)
        // ------------------------------------------------------------------

        private static UIV2Button BuildButton(string prefabName, UIV2Button.Style style, string sampleLabel)
        {
            bool small = style == UIV2Button.Style.GreenSmall;
            var go = new GameObject(prefabName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = small ? new Vector2(160f, 88f) : new Vector2(420f, 128f);

            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var labelRect = CreateUIObject("Label", rect);
            StretchFill(labelRect);
            var label = AddText(labelRect, sampleLabel, small ? 32f : 40f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            var comp = go.AddComponent<UIV2Button>();
            SetPrivateField(comp, "theme", _theme);
            SetPrivateField(comp, "style", style);
            SetPrivateField(comp, "button", button);
            SetPrivateField(comp, "background", image);
            SetPrivateField(comp, "label", label);
            SetPrivateField(comp, "visualRoot", rect);
            comp.Apply();

            return SaveAsPrefab(go, $"{ComponentsPrefabDir}/{prefabName}.prefab").GetComponent<UIV2Button>();
        }

        // ------------------------------------------------------------------
        // Avatar badge
        // ------------------------------------------------------------------

        private static UIV2AvatarBadge BuildAvatarBadge()
        {
            var go = new GameObject("UIV2_AvatarBadge", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(120f, 120f);

            var frame = go.AddComponent<Image>();
            ApplyTintedSpriteOrColor(frame, _theme.AvatarFrame, _theme.BorderGold);

            var avatarRect = CreateUIObject("Avatar", rect);
            SetAnchoredStretchWithMargin(avatarRect, 8f);
            var avatarImage = avatarRect.gameObject.AddComponent<Image>();
            // Placeholder generico (nessun avatar reale assegnato ancora): sagoma silhouette
            // del kit, non un avatar specifico - i veri avatar_01..08 arrivano da SetAvatar().
            ApplySpriteOrColor(avatarImage, _theme.DefaultAvatarSilhouette, Color.white, sliced: false);

            var levelRoot = CreateUIObject("LevelBadge", rect);
            levelRoot.anchorMin = levelRoot.anchorMax = new Vector2(1f, 0f);
            levelRoot.pivot = new Vector2(1f, 0f);
            levelRoot.sizeDelta = new Vector2(44f, 44f);
            levelRoot.anchoredPosition = Vector2.zero;
            var levelBg = levelRoot.gameObject.AddComponent<Image>();
            levelBg.color = _theme.RibbonGreen;
            var levelLabelRect = CreateUIObject("Label", levelRoot);
            StretchFill(levelLabelRect);
            var levelLabel = AddText(levelLabelRect, "1", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            var onlineDot = CreateUIObject("OnlineDot", rect);
            onlineDot.anchorMin = onlineDot.anchorMax = new Vector2(0f, 0f);
            onlineDot.pivot = new Vector2(0f, 0f);
            onlineDot.sizeDelta = new Vector2(28f, 28f);
            var dotImage = onlineDot.gameObject.AddComponent<Image>();
            dotImage.sprite = NeutralCircleSprite;
            dotImage.color = new Color(0.25f, 0.85f, 0.45f);
            onlineDot.gameObject.SetActive(false);

            var comp = go.AddComponent<UIV2AvatarBadge>();
            SetPrivateField(comp, "avatarImage", avatarImage);
            SetPrivateField(comp, "frameImage", frame);
            SetPrivateField(comp, "levelBadgeRoot", levelRoot.gameObject);
            SetPrivateField(comp, "levelLabel", levelLabel);
            SetPrivateField(comp, "onlineDot", onlineDot.gameObject);

            return SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_AvatarBadge.prefab").GetComponent<UIV2AvatarBadge>();
        }

        // ------------------------------------------------------------------
        // Resource pill
        // ------------------------------------------------------------------

        private static UIV2ResourcePill BuildResourcePill()
        {
            var go = new GameObject("UIV2_ResourcePill", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            // Pass fedelta' home_B2 (1).png 2026-09-13: bar_coin mostrato INTERO ad aspect nativo
            // (322x80 -> 292x72.5, zero stretch). Moneta, track e "+" verde sono gia' cotti nello
            // sprite ed e' esattamente la pillola del mockup: sopra ci vanno solo testo e hit area.
            rect.sizeDelta = new Vector2(292f, 72.5f);
            var bg = go.AddComponent<Image>();
            bg.sprite = LoadSprite(IconsPath, "bar_coin");
            bg.type = Image.Type.Simple;
            bg.preserveAspect = true;
            bg.raycastTarget = false;

            // Icona opzionale per valute diverse dall'oro (coprirebbe la moneta cotta): spenta di
            // default, UIV2ResourcePill.SetResource la accende solo se riceve uno sprite.
            var iconRect = CreateUIObject("Icon", rect);
            Place(iconRect, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(31f, 0f), new Vector2(58f, 58f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            var labelRect = CreateUIObject("AmountLabel", rect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(84f, 0f);
            labelRect.offsetMax = new Vector2(-60f, 0f);
            var label = AddText(labelRect, "0", 30f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.MidlineLeft);
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 30f;
            ApplyOutline(label, NavyOutlineMaterial());

            // Hit area invisibile sopra il "+" cotto in bar_coin (hook futuro: apri negozio).
            var addRect = CreateUIObject("AddButton", rect);
            Place(addRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(56f, 62f));
            var addImage = addRect.gameObject.AddComponent<Image>();
            addImage.color = new Color(0f, 0f, 0f, 0f);
            var addButton = addRect.gameObject.AddComponent<Button>();
            addButton.targetGraphic = addImage;
            addButton.transition = Selectable.Transition.None;

            var comp = go.AddComponent<UIV2ResourcePill>();
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "amountLabel", label);
            SetPrivateField(comp, "addButton", addButton);

            return SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_ResourcePill.prefab").GetComponent<UIV2ResourcePill>();
        }

        // ------------------------------------------------------------------
        // Section header
        // ------------------------------------------------------------------

        private static void BuildSectionHeader()
        {
            var go = new GameObject("UIV2_SectionHeader", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(600f, 72f);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var titleRect = CreateUIObject("Title", rect);
            AddLayoutElement(titleRect, preferredWidth: 260f);
            var title = AddText(titleRect, "SEZIONE", 36f, FontStyles.Bold, _theme.TextCream, TextAlignmentOptions.MidlineLeft);

            var lineRect = CreateUIObject("Line", rect);
            AddLayoutElement(lineRect, flexibleWidth: 1f, preferredHeight: 4f);
            var line = lineRect.gameObject.AddComponent<Image>();
            line.color = _theme.BorderGold;

            var comp = go.AddComponent<UIV2SectionHeader>();
            SetPrivateField(comp, "titleLabel", title);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_SectionHeader.prefab");
        }

        // ------------------------------------------------------------------
        // Progress bar
        // ------------------------------------------------------------------

        private static UIV2ProgressBar BuildProgressBar()
        {
            var go = new GameObject("UIV2_ProgressBar", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(500f, 44f);

            var track = go.AddComponent<Image>();
            ApplySpriteOrColor(track, LoadSprite(IconsPath, "bar_empty"), _theme.PanelBlue);

            // FillArea: contenitore inset a margine fisso, rappresenta lo spazio DISPONIBILE
            // del track (sempre 4px di margine su ogni lato, a qualunque risoluzione perche'
            // e' anchor-stretch, non pixel assoluti rispetto allo schermo). Il vero Fill vive
            // dentro e la sua larghezza e' una frazione (anchorMax.x, 0..1) di QUESTO spazio -
            // mai Image.Type.Filled, che ignorerebbe il 9-slice e taglierebbe male i tappi
            // arrotondati durante il riempimento.
            var fillAreaRect = CreateUIObject("FillArea", rect);
            SetAnchoredStretchWithMargin(fillAreaRect, 4f);

            var fillRect = CreateUIObject("Fill", fillAreaRect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f); // preview 50% nel prefab
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);

            // panel_fill_r30 tinto (non bar_energy): scoperto in fase di calibrazione
            // visiva 2026-09-13 che bar_energy non e' una fill continua ma un'illustrazione
            // statica (fulmine + 2 segmenti fissi bakeati nella zona centrale, pensata per
            // essere mostrata intera, mai stirata) - sliced a percentuali/larghezze diverse
            // distorce fulmine e segmenti in una macchia irriconoscibile. panel_fill_r30 e'
            // la forma neutra gia' usata altrove (AvatarFrame/pannelli) apposta per essere
            // tinta e ridimensionata senza artefatti.
            var fill = fillRect.gameObject.AddComponent<Image>();
            var fillSprite = LoadSprite(PanelsNeutralPath, "panel_fill_r30");
            ApplyTintedSpriteOrColor(fill, fillSprite, _theme.RibbonGreen);

            var labelRect = CreateUIObject("ValueLabel", rect);
            StretchFill(labelRect);
            var label = AddText(labelRect, "0/0", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            var comp = go.AddComponent<UIV2ProgressBar>();
            SetPrivateField(comp, "fillRect", fillRect);
            SetPrivateField(comp, "valueLabel", label);

            return SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_ProgressBar.prefab").GetComponent<UIV2ProgressBar>();
        }

        // ------------------------------------------------------------------
        // Stat tile
        // ------------------------------------------------------------------

        private static void BuildStatTile()
        {
            var go = new GameObject("UIV2_StatTile", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(200f, 200f);
            var bg = go.AddComponent<Image>();
            bg.color = _theme.PanelBlue;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 12, 12);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            var iconRect = CreateUIObject("Icon", rect);
            AddLayoutElement(iconRect, preferredHeight: 80f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            // Icona di default generica: SetStat() la sovrascrive per-statistica a runtime.
            ApplySpriteOrColor(icon, LoadSprite(IconsPath, "ic_trophy"), _theme.BorderGold, sliced: false);

            var valueRect = CreateUIObject("ValueLabel", rect);
            AddLayoutElement(valueRect, preferredHeight: 40f);
            var value = AddText(valueRect, "0", 32f, FontStyles.Bold, _theme.TextCream, TextAlignmentOptions.Center);

            var captionRect = CreateUIObject("CaptionLabel", rect);
            AddLayoutElement(captionRect, preferredHeight: 28f);
            var caption = AddText(captionRect, "Etichetta", 20f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);

            var comp = go.AddComponent<UIV2StatTile>();
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "valueLabel", value);
            SetPrivateField(comp, "captionLabel", caption);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_StatTile.prefab");
        }

        // ------------------------------------------------------------------
        // List row
        // ------------------------------------------------------------------

        private static void BuildListRow()
        {
            var go = new GameObject("UIV2_ListRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(900f, 120f);
            var bg = go.AddComponent<Image>();
            bg.color = _theme.PanelBlue;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var iconRect = CreateUIObject("Icon", rect);
            AddLayoutElement(iconRect, preferredWidth: 88f, preferredHeight: 88f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.color = _theme.BorderGold;

            var textColumn = CreateUIObject("TextColumn", rect);
            AddLayoutElement(textColumn, flexibleWidth: 1f);
            var textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.spacing = 4f;

            var titleRect = CreateUIObject("Title", textColumn);
            AddLayoutElement(titleRect, preferredHeight: 44f);
            var title = AddText(titleRect, "Titolo", 32f, FontStyles.Bold, _theme.TextCream, TextAlignmentOptions.MidlineLeft);

            var subtitleRect = CreateUIObject("Subtitle", textColumn);
            AddLayoutElement(subtitleRect, preferredHeight: 32f);
            var subtitle = AddText(subtitleRect, "Sottotitolo", 22f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            var trailingSlot = CreateUIObject("TrailingSlot", rect);
            AddLayoutElement(trailingSlot, preferredWidth: 180f, preferredHeight: 88f);

            var comp = go.AddComponent<UIV2ListRow>();
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "titleLabel", title);
            SetPrivateField(comp, "subtitleLabel", subtitle);
            SetPrivateField(comp, "trailingSlot", trailingSlot);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_ListRow.prefab");
        }

        // ------------------------------------------------------------------
        // Collection card
        // ------------------------------------------------------------------

        private static void BuildCollectionCard()
        {
            var go = new GameObject("UIV2_CollectionCard", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(220f, 260f);

            var border = go.AddComponent<Image>();
            border.color = Color.white;

            var innerRect = CreateUIObject("Inner", rect);
            SetAnchoredStretchWithMargin(innerRect, 6f);
            var innerBg = innerRect.gameObject.AddComponent<Image>();
            innerBg.color = _theme.PanelBlue;

            var innerLayout = innerRect.gameObject.AddComponent<VerticalLayoutGroup>();
            innerLayout.padding = new RectOffset(8, 8, 10, 10);
            innerLayout.spacing = 6f;
            innerLayout.childAlignment = TextAnchor.MiddleCenter;
            innerLayout.childControlWidth = true;
            innerLayout.childControlHeight = true;
            innerLayout.childForceExpandWidth = true;

            var iconRect = CreateUIObject("Icon", innerRect);
            AddLayoutElement(iconRect, preferredHeight: 150f);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.color = _theme.BorderGold;

            var lockIcon = CreateUIObject("LockIcon", innerRect);
            AddLayoutElement(lockIcon, preferredHeight: 150f);
            var lockImage = lockIcon.gameObject.AddComponent<Image>();
            // ic_lock e' gia' colorato (oro): non tingerlo di TextMuted o si spegne il colore
            // dell'arte originale (vedi feedback_visual_quality_bar in memoria).
            ApplySpriteOrColor(lockImage, LoadSprite(IconsPath, "ic_lock"), _theme.TextMuted, sliced: false);
            lockIcon.gameObject.SetActive(false);

            var nameRect = CreateUIObject("NameLabel", innerRect);
            AddLayoutElement(nameRect, preferredHeight: 36f);
            var nameLabel = AddText(nameRect, "Nome", 22f, FontStyles.Normal, _theme.TextCream, TextAlignmentOptions.Center);

            var badgeRect = CreateUIObject("EquippedBadge", rect);
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.sizeDelta = new Vector2(40f, 40f);
            var badgeImage = badgeRect.gameObject.AddComponent<Image>();
            badgeImage.color = _theme.RibbonGreen;
            var badgeIconRect = CreateUIObject("CheckIcon", badgeRect);
            SetAnchoredStretchWithMargin(badgeIconRect, 6f);
            var badgeIconImage = badgeIconRect.gameObject.AddComponent<Image>();
            ApplySpriteOrColor(badgeIconImage, LoadSprite(IconsPath, "ic_check"), Color.white, sliced: false);
            badgeRect.gameObject.SetActive(false);

            var comp = go.AddComponent<UIV2CollectionCard>();
            SetPrivateField(comp, "border", border);
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "lockIcon", lockIcon.gameObject);
            SetPrivateField(comp, "nameLabel", nameLabel);
            SetPrivateField(comp, "equippedBadge", badgeRect.gameObject);
            SetPrivateField(comp, "visualRoot", innerRect);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_CollectionCard.prefab");
        }

        // ------------------------------------------------------------------
        // Modal frame
        // ------------------------------------------------------------------

        private static void BuildModalFrame()
        {
            var go = new GameObject("UIV2_ModalFrame", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);

            var dimmer = CreateUIObject("Dimmer", rect);
            StretchFill(dimmer);
            var dimmerImage = dimmer.gameObject.AddComponent<Image>();
            dimmerImage.color = new Color(0f, 0f, 0f, 0.6f);
            var dimmerButton = dimmer.gameObject.AddComponent<Button>();
            dimmerButton.targetGraphic = dimmerImage;

            var panel = CreateUIObject("Panel", rect);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(920f, 1400f);
            var panelBg = panel.gameObject.AddComponent<Image>();
            ApplyTintedSpriteOrColor(panelBg, _theme.PanelBackground, _theme.PanelBlue);

            var border = CreateUIObject("Border", panel);
            SetAnchoredStretchWithMargin(border, -8f);
            var borderImage = border.gameObject.AddComponent<Image>();
            ApplyTintedSpriteOrColor(borderImage, _theme.PanelBorder, _theme.BorderGold);
            border.SetAsFirstSibling();

            // ribbon_teal ha un aspect ratio fisso (non 9-sliced): dimensione nativa 290x92,
            // qui scalata 1.65x mantenendo il rapporto per non deformarla (policy: mai
            // stirare/deformare uno sprite per farlo combaciare col layout).
            var ribbonSprite = LoadSprite(IconsPath, "ribbon_teal");
            var ribbon = CreateUIObject("Ribbon", panel);
            ribbon.anchorMin = new Vector2(0.5f, 1f);
            ribbon.anchorMax = new Vector2(0.5f, 1f);
            ribbon.pivot = new Vector2(0.5f, 1f);
            ribbon.sizeDelta = ribbonSprite != null ? new Vector2(478f, 151f) : new Vector2(600f, 120f);
            ribbon.anchoredPosition = new Vector2(0f, 20f);
            var ribbonImage = ribbon.gameObject.AddComponent<Image>();
            if (ribbonSprite != null)
            {
                ribbonImage.sprite = ribbonSprite;
                ribbonImage.type = Image.Type.Simple;
                ribbonImage.preserveAspect = true;
                ribbonImage.color = Color.white;
            }
            else
            {
                ribbonImage.color = _theme.RibbonGreen;
            }

            var titleRect = CreateUIObject("Title", ribbon);
            StretchFill(titleRect);
            var title = AddText(titleRect, "TITOLO", 40f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            var closeRect = CreateUIObject("CloseButton", panel);
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(88f, 88f);
            closeRect.anchoredPosition = new Vector2(-8f, 16f);
            var closeImage = closeRect.gameObject.AddComponent<Image>();
            closeImage.color = _theme.ButtonSecondaryBlue;
            var closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var closeLabelRect = CreateUIObject("Label", closeRect);
            StretchFill(closeLabelRect);
            AddText(closeLabelRect, "X", 36f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);

            var contentArea = CreateUIObject("ContentArea", panel);
            contentArea.anchorMin = Vector2.zero;
            contentArea.anchorMax = Vector2.one;
            contentArea.offsetMin = new Vector2(24f, 24f);
            contentArea.offsetMax = new Vector2(-24f, -140f);

            var canvasGroup = go.AddComponent<CanvasGroup>();

            var comp = go.AddComponent<UIV2ModalFrame>();
            SetPrivateField(comp, "root", go);
            SetPrivateField(comp, "canvasGroup", canvasGroup);
            SetPrivateField(comp, "closeButton", closeButton);
            SetPrivateField(comp, "dimmerButton", dimmerButton);
            SetPrivateField(comp, "ribbonTitle", title);
            SetPrivateField(comp, "contentArea", contentArea);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_ModalFrame.prefab");
        }

        // ------------------------------------------------------------------
        // Player row
        // ------------------------------------------------------------------

        private static void BuildPlayerRow(UIV2AvatarBadge avatarBadgePrefab)
        {
            var go = new GameObject("UIV2_PlayerRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(900f, 140f);
            var bg = go.AddComponent<Image>();
            bg.color = _theme.PanelBlue;

            var highlight = CreateUIObject("CurrentPlayerHighlight", rect);
            SetAnchoredStretchWithMargin(highlight, -4f);
            var highlightImage = highlight.gameObject.AddComponent<Image>();
            highlightImage.color = _theme.BorderGold;
            highlight.SetAsFirstSibling();
            highlight.gameObject.SetActive(false);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var avatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(avatarBadgePrefab.gameObject, rect);
            AddLayoutElement((RectTransform)avatarInstance.transform, preferredWidth: 100f, preferredHeight: 100f);

            var nameRect = CreateUIObject("NameLabel", rect);
            AddLayoutElement(nameRect, flexibleWidth: 1f);
            var nameLabel = AddText(nameRect, "Nome Giocatore", 32f, FontStyles.Bold, _theme.TextCream, TextAlignmentOptions.MidlineLeft);

            var trailingRect = CreateUIObject("TrailingLabel", rect);
            AddLayoutElement(trailingRect, preferredWidth: 160f);
            var trailingLabel = AddText(trailingRect, "0", 32f, FontStyles.Bold, _theme.BorderGold, TextAlignmentOptions.MidlineRight);

            var comp = go.AddComponent<UIV2PlayerRow>();
            SetPrivateField(comp, "avatar", avatarInstance.GetComponent<UIV2AvatarBadge>());
            SetPrivateField(comp, "nameLabel", nameLabel);
            SetPrivateField(comp, "trailingLabel", trailingLabel);
            SetPrivateField(comp, "currentPlayerHighlight", highlight.gameObject);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_PlayerRow.prefab");
        }

        // ------------------------------------------------------------------
        // Top bar
        // ------------------------------------------------------------------

        /// <summary>
        /// Visual calibration 2026-09-13 su home_B2 (1).png: avatar+cartiglio nome
        /// (ProfileGroup), poi risorse dinamiche (pill), poi energy/XP - questi ultimi due
        /// sono slot FISSI (come l'avatar), non spawnati da SetResources - vedi UIV2TopBar.cs.
        /// </summary>
        private static void BuildTopBar(UIV2AvatarBadge avatarBadgePrefab, UIV2ResourcePill resourcePillPrefab,
            UIV2ProgressBar progressBarPrefab)
        {
            var go = new GameObject("UIV2_TopBar", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);

            // Pass fedelta' home_B2 (1).png 2026-09-13 (coordinate mockup 1080x1920): profilo
            // x18..218 / y35..241, riga risorse x245..1045 centrata a y=80 dal top.
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 35, 35, 0);
            layout.spacing = 27f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // ---- Profilo: avatar_frame intero ad aspect nativo (cornice oro + silhouette +
            // cartiglio cotti nello sprite = identico al mockup), emblema moneta-trifoglio in alto
            // a sinistra, nome dentro il cartiglio. UIV2AvatarBadge non si usa piu' qui: la sua
            // cornice neutra tinta non corrisponde al mockup. ----
            var profileRect = CreateUIObject("ProfileGroup", rect);
            AddLayoutElement(profileRect, preferredWidth: 200f, preferredHeight: 206f);

            var frameRect = CreateUIObject("AvatarFrame", profileRect);
            Place(frameRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(7f, -15f), new Vector2(190f, 191f));
            var frameImage = frameRect.gameObject.AddComponent<Image>();
            frameImage.sprite = LoadSprite(IconsPath, "avatar_frame");
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = false;

            // Ritratto reale opzionale: maschera circolare dentro l'anello, spento finche' SetProfile
            // non riceve un Avatar (stato mockup = silhouette del frame).
            var maskRect = CreateUIObject("PortraitMask", frameRect);
            Place(maskRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(95.5f, -88f), new Vector2(108f, 108f));
            var maskImage = maskRect.gameObject.AddComponent<Image>();
            maskImage.sprite = NeutralCircleSprite;
            maskImage.raycastTarget = false;
            maskRect.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portraitRect = CreateUIObject("Portrait", maskRect);
            Place(portraitRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(138f, 138f));
            var portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.enabled = false;

            var nameLabelRect = CreateUIObject("NameLabel", frameRect);
            Place(nameLabelRect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(95.5f, -163f), new Vector2(124f, 34f));
            var nameLabel = AddText(nameLabelRect, "Nome Giocatore", 24f, FontStyles.Bold, HomeNameBrown, TextAlignmentOptions.Center);
            nameLabel.enableWordWrapping = false;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 14f;
            nameLabel.fontSizeMax = 24f;
            ApplyOutline(nameLabel, GetOutlineMaterial("Outline Cream", OutlineCream, 0.3f, 0.2f));

            var emblemRect = CreateUIObject("Emblem", profileRect);
            Place(emblemRect, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(58f, 58f));
            var emblem = emblemRect.gameObject.AddComponent<Image>();
            emblem.sprite = LoadSprite(IconsPath, "ic_coin_clover");
            emblem.preserveAspect = true;
            emblem.raycastTarget = false;

            // ---- Riga risorse: [valute dinamiche] [energia] ... [xp] ----
            var rowRect = CreateUIObject("ResourceRow", rect);
            AddLayoutElement(rowRect, preferredHeight: 90f, flexibleWidth: 1f);
            var rowLayout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 13f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var resourcesContainer = CreateUIObject("ResourcesContainer", rowRect);
            resourcesContainer.sizeDelta = new Vector2(292f, 72.5f);
            var resourcesLayout = resourcesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            resourcesLayout.spacing = 13f;
            resourcesLayout.childAlignment = TextAnchor.MiddleLeft;
            // Le pill spawnate a runtime (SetResources) mantengono la propria dimensione da prefab.
            resourcesLayout.childControlWidth = false;
            resourcesLayout.childControlHeight = false;
            resourcesLayout.childForceExpandWidth = false;
            resourcesLayout.childForceExpandHeight = false;

            // ---- Energia: composizione senza stretch. bar_energy ha fulmine + 2 segmenti verdi
            // FISSI cotti (non e' una fill): qui se ne mostra SOLO il fulmine, ritagliato con
            // RectMask2D a scala uniforme, sopra un track bar_empty 9-slice + fill verde dinamica
            // btn_green_small. Workaround dichiarato - serve un fulmine standalone (ASSET MANCANTI).
            const float boltScale = 0.85f;
            var energyRect = CreateUIObject("EnergyBar", rowRect);
            AddLayoutElement(energyRect, preferredWidth: 223f);
            energyRect.sizeDelta = new Vector2(223f, 60f);

            var trackRect = CreateUIObject("Track", energyRect);
            Place(trackRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(13f, 0.4f), new Vector2(213f, 49f));
            AddSlicedImage(trackRect, LoadSprite(IconsPath, "bar_empty"), 49f).raycastTarget = false;

            var fillAreaRect = CreateUIObject("FillArea", trackRect);
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(35f, 13f);
            fillAreaRect.offsetMax = new Vector2(-10f, -13f);

            var fillRect = CreateUIObject("Fill", fillAreaRect);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
            AddSlicedImage(fillRect, LoadSprite(IconsPath, "btn_green_small"), 23f).raycastTarget = false;

            var energyLabelRect = CreateUIObject("ValueLabel", fillRect);
            StretchFill(energyLabelRect);
            var energyLabel = AddText(energyLabelRect, "0", 26f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.Center);
            energyLabel.enableWordWrapping = false;
            energyLabel.overflowMode = TextOverflowModes.Overflow;
            ApplyOutline(energyLabel, NavyOutlineMaterial());

            var boltClipRect = CreateUIObject("BoltClip", energyRect);
            Place(boltClipRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(54f * boltScale, 91f * boltScale));
            boltClipRect.gameObject.AddComponent<RectMask2D>();
            var boltRect = CreateUIObject("Bolt", boltClipRect);
            Place(boltRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 4f), new Vector2(239f * boltScale, 91f * boltScale));
            var bolt = boltRect.gameObject.AddComponent<Image>();
            bolt.sprite = LoadSprite(IconsPath, "bar_energy");
            bolt.type = Image.Type.Simple;
            bolt.raycastTarget = false;

            var energyBar = energyRect.gameObject.AddComponent<UIV2ProgressBar>();
            SetPrivateField(energyBar, "fillRect", fillRect);
            SetPrivateField(energyBar, "valueLabel", energyLabel);
            SetPrivateField(energyBar, "labelFormat", "{0}");

            var spacerRect = CreateUIObject("Spacer", rowRect);
            AddLayoutElement(spacerRect, flexibleWidth: 1f);

            // ---- XP: stesso UIV2_ProgressBar (bar_empty) a 225x48, 9-slice a scala uniforme ----
            var xpInstance = (GameObject)PrefabUtility.InstantiatePrefab(progressBarPrefab.gameObject, rowRect);
            xpInstance.name = "XpBar";
            var xpRect = (RectTransform)xpInstance.transform;
            AddLayoutElement(xpRect, preferredWidth: 225f);
            xpRect.sizeDelta = new Vector2(225f, 48f);
            var xpTrack = xpInstance.GetComponent<Image>();
            if (xpTrack != null)
            {
                xpTrack.pixelsPerUnitMultiplier = 63f / 48f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(xpTrack);
            }
            var xpBar = xpInstance.GetComponent<UIV2ProgressBar>();
            SetPrivateField(xpBar, "labelFormat", "{0} / {1}");
            PrefabUtility.RecordPrefabInstancePropertyModifications(xpBar);
            var xpLabel = xpInstance.GetComponentInChildren<TMP_Text>();
            if (xpLabel != null)
            {
                xpLabel.color = HomeXpLabel;
                xpLabel.fontSize = 24f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(xpLabel);
            }

            var comp = go.AddComponent<UIV2TopBar>();
            SetPrivateField(comp, "portraitImage", portrait);
            SetPrivateField(comp, "nameLabel", nameLabel);
            SetPrivateField(comp, "resourcesContainer", resourcesContainer);
            SetPrivateField(comp, "resourcePillPrefab", resourcePillPrefab);
            SetPrivateField(comp, "energyBar", energyBar);
            SetPrivateField(comp, "xpBar", xpBar);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_TopBar.prefab");
        }

        // ------------------------------------------------------------------
        // Bottom nav
        // ------------------------------------------------------------------

        private static void BuildBottomNav()
        {
            var go = new GameObject("UIV2_BottomNav", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);
            var bg = go.AddComponent<Image>();
            bg.color = NavBarBackground;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Linea sottile sopra il nav (3px, colore campionato dal mockup). Primo figlio cosi'
            // la linguetta oro della tab attiva le passa sopra, come nel mockup.
            var lineRect = CreateUIObject("TopLine", rect);
            lineRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = new Vector2(1f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = new Vector2(0f, 2f);
            lineRect.sizeDelta = new Vector2(0f, 3f);
            var lineImage = lineRect.gameObject.AddComponent<Image>();
            lineImage.color = NavBarTopLine;
            lineImage.raycastTarget = false;

            // "Gioca" (Home) usa il gamepad, non un'icona casa (confermato dal mockup).
            string[] tabNames = { "Gioca", "Cards", "Shop", "Profile" };
            string[] tabLabels = { "Gioca", "Carte", "Negozio", "Profilo" };
            string[] tabIconNames = { "ic_gamepad", "ic_cards", "ic_cart", "ic_person" };
            var slots = new UIV2NavItemRefs[tabNames.Length];

            for (int i = 0; i < tabNames.Length; i++)
            {
                var slotRect = CreateUIObject(tabNames[i] + "Slot", rect);
                var slotImage = slotRect.gameObject.AddComponent<Image>();
                slotImage.color = new Color(0f, 0f, 0f, 0f);
                var slotButton = slotRect.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slotImage;

                // Pass fedelta' home_B2 2026-09-13: le tab inattive NON hanno sfondo (solo icona +
                // label grigio-blu). La tab attiva ha la linguetta tab_gold a scala nativa
                // (231x116 -> 238x117) che sporge 5px sopra la linea del nav, label oro sotto.
                var indicator = CreateUIObject("SelectedIndicator", slotRect);
                Place(indicator, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(238f, 117f));
                AddSlicedImage(indicator, LoadSprite(IconsPath, "tab_gold"), 116f).raycastTarget = false;
                indicator.gameObject.SetActive(i == 0);

                // Dimensione icona gestita da UIV2BottomNav.ApplyVisualState (62 normale / 72 attiva).
                var iconRect = CreateUIObject("Icon", slotRect);
                Place(iconRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -57f), new Vector2(62f, 62f));
                var icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = LoadSprite(IconsPath, tabIconNames[i]);
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var labelRect = CreateUIObject("Label", slotRect);
                Place(labelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -124f), new Vector2(250f, 36f));
                var label = AddText(labelRect, tabLabels[i], 24f, FontStyles.Normal, HomeNavMuted, TextAlignmentOptions.Center);
                label.enableWordWrapping = false;

                slots[i] = new UIV2NavItemRefs
                {
                    Button = slotButton,
                    Icon = icon,
                    IconLayoutElement = null,
                    Label = label,
                    SelectedIndicator = indicator.gameObject
                };
            }

            var comp = go.AddComponent<UIV2BottomNav>();
            SetPrivateField(comp, "slots", slots);

            SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_BottomNav.prefab");
        }

        // ------------------------------------------------------------------
        // Friends (esempio data-driven)
        // ------------------------------------------------------------------

        private static FriendRowView BuildFriendRow(UIV2AvatarBadge avatarBadgePrefab, UIV2Button inviteButtonPrefab)
        {
            var go = new GameObject("FriendRow", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(900f, 140f);
            var bg = go.AddComponent<Image>();
            bg.color = _theme.PanelBlue;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var avatarInstance = (GameObject)PrefabUtility.InstantiatePrefab(avatarBadgePrefab.gameObject, rect);
            AddLayoutElement((RectTransform)avatarInstance.transform, preferredWidth: 100f, preferredHeight: 100f);

            var textColumn = CreateUIObject("TextColumn", rect);
            AddLayoutElement(textColumn, flexibleWidth: 1f);
            var textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.spacing = 4f;

            var nameRect = CreateUIObject("PlayerName", textColumn);
            AddLayoutElement(nameRect, preferredHeight: 44f);
            var nameLabel = AddText(nameRect, "Nome Giocatore", 32f, FontStyles.Bold, _theme.TextCream, TextAlignmentOptions.MidlineLeft);

            var statusRect = CreateUIObject("StatusText", textColumn);
            AddLayoutElement(statusRect, preferredHeight: 32f);
            var statusLabel = AddText(statusRect, "Stato", 22f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            var indicatorRect = CreateUIObject("StatusIndicator", rect);
            AddLayoutElement(indicatorRect, preferredWidth: 24f, preferredHeight: 24f);
            var indicatorImage = indicatorRect.gameObject.AddComponent<Image>();
            indicatorImage.sprite = NeutralCircleSprite;
            indicatorImage.color = new Color(0.25f, 0.85f, 0.45f);

            var inviteInstance = (GameObject)PrefabUtility.InstantiatePrefab(inviteButtonPrefab.gameObject, rect);
            AddLayoutElement((RectTransform)inviteInstance.transform, preferredWidth: 160f, preferredHeight: 88f);

            var comp = go.AddComponent<FriendRowView>();
            SetPrivateField(comp, "avatar", avatarInstance.GetComponent<UIV2AvatarBadge>());
            SetPrivateField(comp, "playerNameLabel", nameLabel);
            SetPrivateField(comp, "statusText", statusLabel);
            SetPrivateField(comp, "statusIndicator", indicatorImage);
            SetPrivateField(comp, "inviteButton", inviteInstance.GetComponent<UIV2Button>());

            return SaveAsPrefab(go, $"{ScreensPrefabDir}/FriendRow.prefab").GetComponent<FriendRowView>();
        }

        private static void BuildFriendsScreen(FriendRowView friendRowPrefab)
        {
            var go = new GameObject("FriendsScreenV2", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);

            var scrollRectGo = CreateUIObject("Scroll", rect);
            StretchFill(scrollRectGo);
            var scrollRect = scrollRectGo.gameObject.AddComponent<ScrollRect>();
            scrollRectGo.gameObject.AddComponent<RectMask2D>();

            var viewport = CreateUIObject("Viewport", scrollRectGo);
            StretchFill(viewport);

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 12f;
            contentLayout.padding = new RectOffset(24, 24, 24, 24);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var onlineHeader = CreateUIObject("OnlineHeader", content);
            AddLayoutElement(onlineHeader, preferredHeight: 48f);
            AddText(onlineHeader, "ONLINE", 26f, FontStyles.Bold, _theme.BorderGold, TextAlignmentOptions.MidlineLeft);

            var onlineList = CreateUIObject("OnlineList", content);
            var onlineListLayout = onlineList.gameObject.AddComponent<VerticalLayoutGroup>();
            onlineListLayout.spacing = 12f;
            onlineListLayout.childControlWidth = true;
            onlineListLayout.childControlHeight = false;
            onlineListLayout.childForceExpandWidth = true;
            // Nessun ContentSizeFitter qui: e' un LayoutGroup annidato dentro "content" (che lo
            // controlla gia' tramite childControlHeight=true sopra), non il contenuto diretto
            // della ScrollRect - un secondo sistema di resize in piu' e' solo ridondante.

            var offlineHeader = CreateUIObject("OfflineHeader", content);
            AddLayoutElement(offlineHeader, preferredHeight: 48f);
            AddText(offlineHeader, "OFFLINE", 26f, FontStyles.Bold, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            var offlineList = CreateUIObject("OfflineList", content);
            var offlineListLayout = offlineList.gameObject.AddComponent<VerticalLayoutGroup>();
            offlineListLayout.spacing = 12f;
            offlineListLayout.childControlWidth = true;
            offlineListLayout.childControlHeight = false;
            offlineListLayout.childForceExpandWidth = true;

            var comp = go.AddComponent<FriendsScreenController>();
            SetPrivateField(comp, "onlineListContainer", onlineList);
            SetPrivateField(comp, "offlineListContainer", offlineList);
            SetPrivateField(comp, "friendRowPrefab", friendRowPrefab);

            SaveAsPrefab(go, $"{ScreensPrefabDir}/FriendsScreenV2.prefab");
        }

        // ------------------------------------------------------------------
        // Home V2: quick action button, selector chip, screen
        // ------------------------------------------------------------------

        // ---- Token visivi Home, campionati da Assets/Mockup/home_B2 (1).png (2026-09-13) ----
        // Non nel UIV2Theme: sono calibrazioni di questa schermata (top bar/nav/home), non token
        // globali della foundation.
        private static readonly Color HomeTextLight = new Color32(255, 250, 238, 255);
        private static readonly Color HomeTextSoftBlue = new Color32(178, 198, 224, 255);
        private static readonly Color HomeQuickLabel = new Color32(196, 212, 232, 255);
        private static readonly Color HomeXpLabel = new Color32(185, 200, 222, 255);
        private static readonly Color HomeNameBrown = new Color32(92, 56, 14, 255);
        private static readonly Color HomeNavMuted = new Color32(148, 172, 202, 255);
        private static readonly Color HomePlaceholderTitle = new Color32(180, 200, 226, 255);
        private static readonly Color HomePlaceholderSubtitle = new Color32(132, 155, 185, 255);
        private static readonly Color NavBarBackground = new Color32(13, 22, 38, 255);
        private static readonly Color NavBarTopLine = new Color32(58, 88, 128, 255);
        private static readonly Color OutlineNavy = new Color32(8, 18, 34, 255);
        private static readonly Color OutlineBrown = new Color32(116, 60, 6, 255);
        private static readonly Color OutlineCream = new Color32(255, 240, 196, 255);

        private const string FontMaterialDir = "Assets/UIV2/Art/Fonts";

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        /// <summary>
        /// Material preset TMP (stesso atlas del font di default) con outline, salvato come asset
        /// cosi' il riferimento sopravvive nel prefab (un fontMaterial istanziato no).
        /// </summary>
        private static Material GetOutlineMaterial(string presetName, Color outlineColor, float outlineWidth, float faceDilate, TMP_FontAsset fontOverride = null)
        {
            var font = fontOverride != null ? fontOverride : TMP_Settings.defaultFontAsset;
            if (font == null || font.material == null) return null;

            CreateFolderRecursive(FontMaterialDir);
            string path = $"{FontMaterialDir}/{font.name} {presetName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(font.material);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = font.material.shader;
                mat.CopyPropertiesFromMaterial(font.material);
            }

            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat("_OutlineWidth", outlineWidth);
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_FaceDilate", faceDilate);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material NavyOutlineMaterial() => GetOutlineMaterial("Outline Navy", OutlineNavy, 0.38f, 0.28f);

        private static void ApplyOutline(TMP_Text text, Material material)
        {
            if (text != null && material != null) text.fontSharedMaterial = material;
        }

        /// <summary>
        /// 9-slice con pixelsPerUnitMultiplier = altezza nativa / altezza target: i bordi scalano
        /// nella stessa proporzione del corpo (tappi mai schiacciati o gonfiati), si allunga solo
        /// la fascia centrale orizzontale, che e' quella pensata per allungarsi.
        /// </summary>
        private static Image AddSlicedImage(RectTransform rect, Sprite sprite, float targetHeight)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            if (sprite != null && targetHeight > 0f) image.pixelsPerUnitMultiplier = sprite.rect.height / targetHeight;
            return image;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static GameObject BuildQuickActionButtonPrefab()
        {
            var go = new GameObject("UIV2_QuickActionButton", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            // Pass fedelta' home_B2 2026-09-13: visual 110x100 (sq_blue 9-slice a scala uniforme),
            // label 21px con centro a +119 dal top del visual. Icona in un box 76x66 a aspect
            // nativo (le istanze possono stringere il box, vedi BuildHomeScreenV2Prefab).
            rect.sizeDelta = new Vector2(130f, 134f);
            var button = go.AddComponent<Button>();

            var visualRect = CreateUIObject("Visual", rect);
            // sq_blue ha alpha trasparente (sx4 dx7 su3 giu6): rect 120x108 => visibile 110x100.
            Place(visualRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1.3f, 2.7f), new Vector2(120f, 108f));
            var visualImage = AddSlicedImage(visualRect, LoadSprite(IconsPath, "sq_blue"), 108f);
            button.targetGraphic = visualImage;

            var iconRect = CreateUIObject("Icon", visualRect);
            Place(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-1.3f, 4.3f), new Vector2(76f, 66f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.color = Color.white; // icona assegnata dal chiamante (HomeScreenV2Prefab) per posizione
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Badge notifica: pallino rosso Icons_52 (cerchio del kit, aspect nativo) centrato
            // sull'angolo in alto a destra del visual.
            var badgeRect = CreateUIObject("Badge", visualRect);
            Place(badgeRect, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-14.2f, -2.7f), new Vector2(38f, 38f));
            var badgeImage = badgeRect.gameObject.AddComponent<Image>();
            badgeImage.sprite = LoadSprite(IconsPath, "Icons_52");
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;

            var badgeLabelRect = CreateUIObject("Label", badgeRect);
            StretchFill(badgeLabelRect);
            var badgeLabel = AddText(badgeLabelRect, "1", 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            badgeRect.gameObject.SetActive(false); // nessun badge di default - SetBadgeCount() lo riattiva

            var labelRect = CreateUIObject("Label", rect);
            Place(labelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -119f), new Vector2(150f, 30f));
            var label = AddText(labelRect, "Label", 20f, FontStyles.Normal, HomeQuickLabel, TextAlignmentOptions.Center);
            label.enableWordWrapping = false;

            var comp = go.AddComponent<UIV2QuickActionButton>();
            SetPrivateField(comp, "button", button);
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "label", label);
            SetPrivateField(comp, "badgeRoot", badgeRect.gameObject);
            SetPrivateField(comp, "badgeLabel", badgeLabel);

            return SaveAsPrefab(go, $"{ComponentsPrefabDir}/UIV2_QuickActionButton.prefab");
        }

        /// <summary>
        /// SmallLabel/sfondo/icona sono strutturali (una prefab per posizione - Modalita' in
        /// blu, Mazzo in teal, come nel mockup) - il valore corrente resta l'unico dato
        /// scritto a runtime via SetValue().
        /// </summary>
        private static GameObject BuildSelectorChipPrefab(string prefabName, string smallLabelText,
            Sprite backgroundSprite, Color fallbackColor, Sprite iconSprite)
        {
            var go = new GameObject(prefabName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            // Rect 470x130.3 => parte VISIBILE 465x115 come nel mockup: btn_blue_long/btn_teal hanno
            // ~12px di alpha trasparente in altezza, compensati a scala uniforme (1.278).
            rect.sizeDelta = new Vector2(470f, 130.3f);

            var button = go.AddComponent<Button>();
            Image bg;
            if (backgroundSprite != null)
            {
                bg = AddSlicedImage(rect, backgroundSprite, 130.3f);
            }
            else
            {
                bg = go.AddComponent<Image>();
                bg.color = fallbackColor;
            }
            button.targetGraphic = bg;

            // Pass fedelta' home_B2 2026-09-13: icona direttamente sul bottone (nessun medaglione
            // con anello oro - nel mockup non c'e'), centro a x=56 dal bordo sinistro.
            var iconRect = CreateUIObject("Icon", rect);
            Place(iconRect, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58.6f, -0.5f), new Vector2(70f, 62f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var textColumn = CreateUIObject("TextColumn", rect);
            textColumn.anchorMin = Vector2.zero;
            textColumn.anchorMax = Vector2.one;
            textColumn.offsetMin = new Vector2(113.6f, 19.1f);
            textColumn.offsetMax = new Vector2(-26.6f, -24.2f);
            var textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            textLayout.spacing = 4f;

            var smallLabelRect = CreateUIObject("SmallLabel", textColumn);
            AddLayoutElement(smallLabelRect, preferredHeight: 26f);
            var smallLabel = AddText(smallLabelRect, smallLabelText, 22f, FontStyles.Normal, HomeTextSoftBlue, TextAlignmentOptions.MidlineLeft);
            smallLabel.characterSpacing = 2f;
            smallLabel.enableWordWrapping = false;

            var valueLabelRect = CreateUIObject("ValueLabel", textColumn);
            AddLayoutElement(valueLabelRect, preferredHeight: 40f);
            var valueLabel = AddText(valueLabelRect, "-", 32f, FontStyles.Bold, HomeTextLight, TextAlignmentOptions.MidlineLeft);
            valueLabel.enableWordWrapping = false;
            valueLabel.enableAutoSizing = true;
            valueLabel.fontSizeMin = 22f;
            valueLabel.fontSizeMax = 32f;
            ApplyOutline(valueLabel, NavyOutlineMaterial());

            var comp = go.AddComponent<UIV2SelectorChip>();
            SetPrivateField(comp, "icon", icon);
            SetPrivateField(comp, "smallLabel", smallLabel);
            SetPrivateField(comp, "valueLabel", valueLabel);
            SetPrivateField(comp, "button", button);

            return SaveAsPrefab(go, $"{ComponentsPrefabDir}/{prefabName}.prefab");
        }

        private static void BuildHomeScreenV2Prefab(GameObject quickActionButtonPrefab,
            GameObject modeSelectorPrefab, GameObject deckSelectorPrefab, GameObject goldButtonPrefab)
        {
            var go = new GameObject("HomeScreenV2", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            StretchFill(rect);

            // Coordinate del pass fedelta' 2026-09-13 riferite a home_B2 (1).png 1080x1920 con
            // ScreenHost da y=262 a y=1750.

            // ---- Sfondo/video placeholder: full-bleed, sotto tutto il resto ----
            var backgroundRect = CreateUIObject("BackgroundArea", rect);
            StretchFill(backgroundRect);
            var backgroundVideoSlot = backgroundRect.gameObject.AddComponent<RawImage>();
            backgroundVideoSlot.color = Color.white;
            backgroundVideoSlot.raycastTarget = false;
            backgroundVideoSlot.enabled = false; // nessuna texture finche' SetBackgroundVideoTexture non viene chiamata

            var placeholderRect = CreateUIObject("VideoPlaceholderMarker", backgroundRect);
            Place(placeholderRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 123f), new Vector2(760f, 80f));
            var placeholderLayout = placeholderRect.gameObject.AddComponent<VerticalLayoutGroup>();
            placeholderLayout.childAlignment = TextAnchor.MiddleCenter;
            placeholderLayout.spacing = 4f;
            placeholderLayout.childControlWidth = true;
            placeholderLayout.childControlHeight = true;
            placeholderLayout.childForceExpandWidth = true;
            placeholderLayout.childForceExpandHeight = false;

            var placeholderTitleRect = CreateUIObject("Title", placeholderRect);
            AddLayoutElement(placeholderTitleRect, preferredHeight: 40f);
            AddText(placeholderTitleRect, "VIDEO LOOP A TUTTO SCHERMO", 30f, FontStyles.Bold, HomePlaceholderTitle, TextAlignmentOptions.Center);

            var placeholderSubtitleRect = CreateUIObject("Subtitle", placeholderRect);
            AddLayoutElement(placeholderSubtitleRect, preferredHeight: 36f);
            AddText(placeholderSubtitleRect, "(scena animata dietro tutta la UI)", 23f, FontStyles.Normal, HomePlaceholderSubtitle, TextAlignmentOptions.Center);

            // ---- Quick action colonna destra (Premio/Classifica/Posta): visual top a y=250,
            // passo 146.5, centro x=1000 ----
            var quickActionsRect = CreateUIObject("QuickActionsColumn", rect);
            Place(quickActionsRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-15f, 12f), new Vector2(130f, 440f));
            var quickActionsLayout = quickActionsRect.gameObject.AddComponent<VerticalLayoutGroup>();
            quickActionsLayout.childAlignment = TextAnchor.UpperCenter;
            quickActionsLayout.spacing = 12.5f;
            quickActionsLayout.childControlWidth = false;
            quickActionsLayout.childControlHeight = false;
            quickActionsLayout.childForceExpandWidth = false;
            quickActionsLayout.childForceExpandHeight = false;

            var rewardsInstance = (GameObject)PrefabUtility.InstantiatePrefab(quickActionButtonPrefab, quickActionsRect);
            var rewardsButton = rewardsInstance.GetComponent<UIV2QuickActionButton>();
            rewardsButton.SetContent(LoadSprite(IconsPath, "chest_green"), "Premio");

            var rankingInstance = (GameObject)PrefabUtility.InstantiatePrefab(quickActionButtonPrefab, quickActionsRect);
            var rankingButton = rankingInstance.GetComponent<UIV2QuickActionButton>();
            rankingButton.SetContent(LoadSprite(IconsPath, "ic_trophy"), "Classifica");
            SetQuickActionIconBox(rankingInstance, new Vector2(60f, 58f));

            var mailInstance = (GameObject)PrefabUtility.InstantiatePrefab(quickActionButtonPrefab, quickActionsRect);
            var mailButton = mailInstance.GetComponent<UIV2QuickActionButton>();
            mailButton.SetContent(LoadSprite(IconsPath, "ic_mail"), "Posta");
            SetQuickActionIconBox(mailInstance, new Vector2(60f, 52f));

            // ---- Selettori + CTA ancorati in basso: parte visibile di GIOCA finisce 95px sopra il
            // nav, selettori visibili 72px sopra GIOCA (valori rect compensano l'alpha trasparente) (l'area centrale si allarga/stringe sopra di loro) ----
            var bottomGroupRect = CreateUIObject("BottomControlsGroup", rect);
            bottomGroupRect.anchorMin = new Vector2(0f, 0f);
            bottomGroupRect.anchorMax = new Vector2(1f, 0f);
            bottomGroupRect.pivot = new Vector2(0.5f, 0f);
            bottomGroupRect.anchoredPosition = new Vector2(0f, 82.3f);
            bottomGroupRect.sizeDelta = new Vector2(0f, 0f);
            var bottomGroupLayout = bottomGroupRect.gameObject.AddComponent<VerticalLayoutGroup>();
            bottomGroupLayout.padding = new RectOffset(60, 57, 0, 0);
            bottomGroupLayout.spacing = 35.6f;
            bottomGroupLayout.childAlignment = TextAnchor.UpperCenter;
            bottomGroupLayout.childControlWidth = true;
            bottomGroupLayout.childControlHeight = true;
            bottomGroupLayout.childForceExpandWidth = true;
            bottomGroupLayout.childForceExpandHeight = false;
            var bottomGroupFitter = bottomGroupRect.gameObject.AddComponent<ContentSizeFitter>();
            bottomGroupFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            bottomGroupFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var selectorsRowRect = CreateUIObject("SelectorsRow", bottomGroupRect);
            AddLayoutElement(selectorsRowRect, preferredHeight: 130.3f);
            var selectorsRowLayout = selectorsRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            selectorsRowLayout.spacing = 20.8f;
            selectorsRowLayout.childAlignment = TextAnchor.MiddleCenter;
            selectorsRowLayout.childControlWidth = true;
            selectorsRowLayout.childControlHeight = true;
            selectorsRowLayout.childForceExpandWidth = true;
            selectorsRowLayout.childForceExpandHeight = true;

            var modeSelectorInstance = (GameObject)PrefabUtility.InstantiatePrefab(modeSelectorPrefab, selectorsRowRect);
            var deckSelectorInstance = (GameObject)PrefabUtility.InstantiatePrefab(deckSelectorPrefab, selectorsRowRect);

            // GIOCA: 556x158 centrato (non a tutta larghezza), btn_gold_long 9-slice a scala
            // uniforme; label crema con gradiente verticale + outline marrone come nel mockup (solo
            // su QUESTA istanza, il prefab condiviso UIV2_PrimaryGoldButton resta invariato).
            var playButtonRect = CreateUIObject("PlayButtonSlot", bottomGroupRect);
            AddLayoutElement(playButtonRect, preferredHeight: 201.6f);
            var playButtonInstance = (GameObject)PrefabUtility.InstantiatePrefab(goldButtonPrefab, playButtonRect);
            var playRect = (RectTransform)playButtonInstance.transform;
            // btn_gold_long ha alpha trasparente asimmetrico (sx 24px, sopra 17px, dx 5, sotto 7): rect
            // 608.6x201.6 spostato di -17.3 => parte visibile 556x158 centrata come nel mockup.
            Place(playRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-17.3f, 0f), new Vector2(608.6f, 201.6f));
            var playUiv2 = playButtonInstance.GetComponent<UIV2Button>();
            playUiv2.SetLabel("GIOCA");
            SetPrivateField(playUiv2, "keepLabelColor", true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(playUiv2);
            var playBg = playButtonInstance.GetComponent<Image>();
            if (playBg != null)
            {
                playBg.pixelsPerUnitMultiplier = 111f / 201.6f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(playBg);
            }
            var playButtonLabel = playButtonInstance.GetComponentInChildren<TMP_Text>();
            if (playButtonLabel != null)
            {
                playButtonLabel.fontSize = 72f;
                playButtonLabel.rectTransform.offsetMin = new Vector2(12f, -6f);
                playButtonLabel.rectTransform.offsetMax = new Vector2(12f, -6f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(playButtonLabel.rectTransform);
                playButtonLabel.fontStyle = FontStyles.Bold;
                playButtonLabel.color = Color.white;
                playButtonLabel.enableVertexGradient = true;
                var top = new Color32(255, 253, 245, 255);
                var bottom = new Color32(255, 234, 186, 255);
                playButtonLabel.colorGradient = new VertexGradient(top, top, bottom, bottom);
                ApplyOutline(playButtonLabel, GetOutlineMaterial("Outline Brown", OutlineBrown, 0.36f, 0.3f));
                PrefabUtility.RecordPrefabInstancePropertyModifications(playButtonLabel);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(playRect);

            var comp = go.AddComponent<HomeScreenV2>();
            SetPrivateField(comp, "backgroundVideoSlot", backgroundVideoSlot);
            SetPrivateField(comp, "rewardsButton", rewardsButton);
            SetPrivateField(comp, "rankingButton", rankingButton);
            SetPrivateField(comp, "mailButton", mailButton);
            SetPrivateField(comp, "modeSelector", modeSelectorInstance.GetComponent<UIV2SelectorChip>());
            SetPrivateField(comp, "deckSelector", deckSelectorInstance.GetComponent<UIV2SelectorChip>());
            SetPrivateField(comp, "playButton", playButtonInstance.GetComponent<UIV2Button>());

            SaveAsPrefab(go, $"{ScreensPrefabDir}/HomeScreenV2.prefab");
            AssetDatabase.SaveAssets();
        }

        private static void SetQuickActionIconBox(GameObject quickActionInstance, Vector2 size)
        {
            var iconRect = quickActionInstance.transform.Find("Visual/Icon") as RectTransform;
            if (iconRect == null) return;
            iconRect.sizeDelta = size;
            PrefabUtility.RecordPrefabInstancePropertyModifications(iconRect);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void StretchFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredStretchWithMargin(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void AddLayoutElement(RectTransform rect, float preferredWidth = -1f, float preferredHeight = -1f,
            float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var le = rect.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f) le.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f) le.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) le.flexibleHeight = flexibleHeight;
        }

        private static TMP_Text AddText(RectTransform rect, string text, float size, FontStyles style, Color color,
            TextAlignmentOptions alignment)
        {
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject SaveAsPrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                Debug.LogError($"[UIV2FoundationBuilder] Campo '{fieldName}' non trovato su {target.GetType().Name}.");
                return;
            }
            field.SetValue(target, value);
        }
    }
}
