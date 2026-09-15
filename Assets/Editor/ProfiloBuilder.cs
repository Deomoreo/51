using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Ricostruisce il contenuto di "ProfilePage" (Assets/Mockup/17_profilo.png), stessa
    /// tecnica di NegozioBuilder/DeckPageBuilder (trova la pagina placeholder, ripulisce,
    /// ricostruisce dentro un ContentArea centrato + ScrollRect). Nessun sistema di
    /// autenticazione/statistiche/trofei reale collegato: dati statici dal mockup,
    /// "Registrati"/"Condividi profilo" sono StubActionButton. Il gear icon apre il vero
    /// ImpostazioniOverlay gia' costruito (OpenPanelShortcut), non un pannello duplicato.
    /// </summary>
    public static class ProfiloBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private const float ContentAreaY0 = 254f;
        private const float ContentWidth = 980f;

        private static readonly Color PanelDark = new Color(0.06f, 0.08f, 0.12f, 1f);
        private static readonly Color CardFill = new Color(0.086f, 0.157f, 0.235f);
        private static readonly Color RingMuted = new Color(0.18f, 0.31f, 0.42f);
        private static readonly Color RingHighlight = new Color(0.91f, 0.70f, 0.29f);

        private struct Stat
        {
            public string Value;
            public string Label;
        }

        private static readonly Stat[] Stats =
        {
            new Stat { Value = "12", Label = "Partite" },
            new Stat { Value = "5", Label = "Vittorie" },
            new Stat { Value = "42%", Label = "% vittorie" },
            new Stat { Value = "38", Label = "Scope totali" },
            new Stat { Value = "7", Label = "Settebello" },
            new Stat { Value = "44", Label = "Record punti" },
        };

        private struct Trophy
        {
            public string Icon;
            public bool Unlocked;
        }

        private static readonly Trophy[] Trophies =
        {
            new Trophy { Icon = "ic_trophy", Unlocked = true },
            new Trophy { Icon = "ic_cup", Unlocked = true },
            new Trophy { Icon = "ic_gamepad", Unlocked = false },
            new Trophy { Icon = "ic_gem_green", Unlocked = false },
            new Trophy { Icon = "ic_clover_circle", Unlocked = false },
        };

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Profilo")]
        private static void Build()
        {
            if (!LoadContext()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ProfiloBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;
            var profilePage = FindDeepChild(canvasRect, "ProfilePage") as RectTransform;
            if (profilePage == null)
            {
                Debug.LogError("[ProfiloBuilder] Nessun 'ProfilePage' trovato sotto PagesViewport: esegui prima Tools/Dragons Hoard/Build Home Screen.");
                return;
            }

            profilePage.gameObject.SetActive(true);

            for (int i = profilePage.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(profilePage.GetChild(i).gameObject);
            }

            var bg = profilePage.GetComponent<Image>();
            if (bg == null) bg = profilePage.gameObject.AddComponent<Image>();
            bg.color = PanelDark;
            bg.raycastTarget = true;

            var contentArea = CreateUIObject("ContentArea", profilePage);
            SetTopCenter(contentArea, ContentAreaY0, ContentWidth, 1080f);

            BuildScrollContent(contentArea);

            profilePage.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ProfiloBuilder] ProfilePage ricostruita in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // ScrollView
        // ------------------------------------------------------------------

        private static void BuildScrollContent(RectTransform contentArea)
        {
            var scrollViewRt = CreateUIObject("ScrollView", contentArea);
            StretchFill(scrollViewRt);
            var scrollRect = scrollViewRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateUIObject("Viewport", scrollViewRt);
            StretchFill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            var content = CreateUIObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            float y = 0f;
            y += CreateHeader(content, y) + 40f;
            y += CreateStatsSection(content, y) + 40f;
            y += CreateTrophiesSection(content, y) + 50f;
            y += CreateActionButtons(content, y) + 20f;

            content.sizeDelta = new Vector2(0f, y);
        }

        // ------------------------------------------------------------------
        // Header: avatar + nome + livello + XP bar
        // ------------------------------------------------------------------

        private static float CreateHeader(RectTransform content, float yTop)
        {
            const float height = 330f;

            var avatarFrameRt = CreateUIObject("AvatarFrame", content);
            SetAnchoredRect(avatarFrameRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(170f, 170f), new Vector2(0f, -yTop));
            var avatarBg = avatarFrameRt.gameObject.AddComponent<Image>();
            avatarBg.sprite = LoadSprite("panel_fill_r24");
            avatarBg.type = Image.Type.Sliced;
            avatarBg.color = CardFill;
            var avatarRingRt = CreateUIObject("Ring", avatarFrameRt);
            StretchFill(avatarRingRt);
            AddSpriteImage(avatarRingRt, "panel_ring_r24", sliced: true).color = _theme.Gold;
            var avatarIconRt = CreateUIObject("Icon", avatarFrameRt);
            SetAnchoredRect(avatarIconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(96f, 96f), Vector2.zero);
            AddSpriteImage(avatarIconRt, "ic_person", preserveAspect: true).color = Hex("#5D8FD6");

            var gearRt = CreateUIObject("SettingsShortcut", content);
            SetAnchoredRect(gearRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(64f, 64f), new Vector2(120f, -yTop - 10f));
            var gearBg = AddSpriteImage(gearRt, "sq_blue", raycastTarget: true, sliced: true);
            gearRt.gameObject.AddComponent<Button>().targetGraphic = gearBg;
            var gearIconRt = CreateUIObject("Icon", gearRt);
            SetAnchoredRect(gearIconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(38f, 38f), Vector2.zero);
            AddSpriteImage(gearIconRt, "ic_gear", preserveAspect: true);
            var shortcut = gearRt.gameObject.AddComponent<OpenPanelShortcut>();
            var shortcutSo = new SerializedObject(shortcut);
            shortcutSo.FindProperty("targetControllerTypeName").stringValue = nameof(PanelImpostazioniController);
            shortcutSo.ApplyModifiedPropertiesWithoutUndo();

            var nameRt = CreateUIObject("Name", content);
            SetTopCenter(nameRt, yTop + 180f, ContentWidth, 44f);
            AddText(nameRt, "Deomoreo", 32f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            var levelRt = CreateUIObject("LevelText", content);
            SetTopCenter(levelRt, yTop + 228f, ContentWidth, 32f);
            AddText(levelRt, "Livello 1 · Guest_66B9B973", 21f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);

            var xpBarRt = CreateUIObject("XpBarTrack", content);
            SetTopCenter(xpBarRt, yTop + 272f, ContentWidth - 200f, 24f);
            // bar_empty ha un blu acceso "cotto dentro" lo sprite (e' pensato come sfondo
            // pieno per i badge del TopBar, non come track "vuoto" di una progress bar) -
            // tinto scuro qui cosi' il riempimento oro sopra si distingue davvero, invece di
            // sembrare una barra gia' piena al 100% con "0/100 XP" scritto sotto.
            AddSpriteImage(xpBarRt, "bar_empty", sliced: true).color = new Color(0.086f, 0.157f, 0.235f);
            var xpFillRt = CreateUIObject("Fill", xpBarRt);
            xpFillRt.anchorMin = new Vector2(0f, 0f);
            xpFillRt.anchorMax = new Vector2(0.02f, 1f);
            xpFillRt.pivot = new Vector2(0f, 0.5f);
            xpFillRt.offsetMin = Vector2.zero;
            xpFillRt.offsetMax = Vector2.zero;
            AddSpriteImage(xpFillRt, "bar_energy", sliced: true).color = _theme.Gold;

            var xpCaptionRt = CreateUIObject("XpCaption", content);
            SetTopCenter(xpCaptionRt, yTop + 302f, ContentWidth, 30f);
            AddText(xpCaptionRt, "0 / 100 XP al livello 2", 19f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);

            return height;
        }

        // ------------------------------------------------------------------
        // Statistiche: 2 righe x 3
        // ------------------------------------------------------------------

        private static float CreateStatsSection(RectTransform content, float yTop)
        {
            const float labelHeight = 40f;
            const float boxHeight = 132f;
            const float rowGap = 20f;
            const float colGap = 24f;

            var labelRt = CreateUIObject("Label_STATISTICHE", content);
            SetTopStretch(labelRt, yTop, labelHeight);
            AddText(labelRt, "STATISTICHE", 23f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineLeft);

            float boxWidth = (ContentWidth - colGap * 2f) / 3f;
            for (int i = 0; i < Stats.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;
                float x = col * (boxWidth + colGap) - ContentWidth * 0.5f + boxWidth * 0.5f;
                float y = yTop + labelHeight + row * (boxHeight + rowGap);
                CreateStatBox(content, Stats[i], x, y, boxWidth, boxHeight);
            }

            return labelHeight + boxHeight * 2f + rowGap;
        }

        private static void CreateStatBox(RectTransform content, Stat stat, float xCenter, float yTop, float width, float height)
        {
            var box = CreateUIObject("Stat_" + stat.Label, content);
            SetAnchoredRect(box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width, height), new Vector2(xCenter, -yTop));

            var fill = box.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = CardFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", box);
            StretchFill(ringRt);
            AddSpriteImage(ringRt, "panel_ring_r24", sliced: true).color = RingMuted;

            var valueRt = CreateUIObject("Value", box);
            SetAnchoredRect(valueRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width - 16f, 44f), new Vector2(0f, -28f));
            AddText(valueRt, stat.Value, 30f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Center);

            var labelRt = CreateUIObject("Label", box);
            SetAnchoredRect(labelRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width - 16f, 30f), new Vector2(0f, -80f));
            AddText(labelRt, stat.Label, 18f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // Trofei: riga di 5 slot
        // ------------------------------------------------------------------

        private static float CreateTrophiesSection(RectTransform content, float yTop)
        {
            const float labelHeight = 40f;
            const float boxHeight = 132f;
            const float gap = 16f;

            var labelRt = CreateUIObject("Label_TROFEI", content);
            SetTopStretch(labelRt, yTop, labelHeight);
            AddText(labelRt, "TROFEI", 23f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineLeft);

            float boxWidth = (ContentWidth - gap * 4f) / 5f;
            for (int i = 0; i < Trophies.Length; i++)
            {
                float x = i * (boxWidth + gap) - ContentWidth * 0.5f + boxWidth * 0.5f;
                CreateTrophyBox(content, Trophies[i], x, yTop + labelHeight, boxWidth, boxHeight);
            }

            return labelHeight + boxHeight;
        }

        private static void CreateTrophyBox(RectTransform content, Trophy trophy, float xCenter, float yTop, float width, float height)
        {
            var box = CreateUIObject("Trophy", content);
            SetAnchoredRect(box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(width, height), new Vector2(xCenter, -yTop));

            var fill = box.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = CardFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", box);
            StretchFill(ringRt);
            AddSpriteImage(ringRt, "panel_ring_r24", sliced: true).color = trophy.Unlocked ? RingHighlight : RingMuted;

            var iconRt = CreateUIObject("Icon", box);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(56f, 56f), Vector2.zero);
            var iconImg = AddSpriteImage(iconRt, trophy.Icon, preserveAspect: true);
            if (!trophy.Unlocked) iconImg.color = new Color(0.35f, 0.35f, 0.35f);
        }

        // ------------------------------------------------------------------
        // Bottoni azione
        // ------------------------------------------------------------------

        private static float CreateActionButtons(RectTransform content, float yTop)
        {
            var registerRt = CreateUIObject("RegisterButton", content);
            SetTopCenter(registerRt, yTop, ContentWidth, 86f);
            var registerBg = AddSpriteImage(registerRt, "btn_gold_long", raycastTarget: true, sliced: true);
            registerRt.gameObject.AddComponent<Button>().targetGraphic = registerBg;
            var registerTextRt = CreateUIObject("Text", registerRt);
            StretchFill(registerTextRt);
            var registerText = AddText(registerTextRt, "REGISTRATI PER SALVARE", 27f, FontStyles.Bold, Hex("#3A2208"), TextAlignmentOptions.Center);
            registerText.outlineWidth = 0.15f;
            registerText.outlineColor = Hex("#241608");
            AddStub(registerRt.gameObject, "Registrati per salvare: nessun sistema di autenticazione reale collegato ancora.");

            var shareRt = CreateUIObject("ShareButton", content);
            SetTopCenter(shareRt, yTop + 106f, ContentWidth, 74f);
            var shareBg = AddSpriteImage(shareRt, "btn_blue_long", raycastTarget: true, sliced: true);
            shareRt.gameObject.AddComponent<Button>().targetGraphic = shareBg;
            var shareTextRt = CreateUIObject("Text", shareRt);
            StretchFill(shareTextRt);
            AddText(shareTextRt, "CONDIVIDI PROFILO", 24f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            AddStub(shareRt.gameObject, "Condividi profilo: nessuna integrazione share nativa collegata ancora.");

            return 106f + 74f;
        }

        private static void AddStub(GameObject go, string message)
        {
            var stub = go.AddComponent<StubActionButton>();
            var so = new SerializedObject(stub);
            so.FindProperty("logMessage").stringValue = message;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // Contesto / helper generici
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[ProfiloBuilder] UITheme non trovato in {ThemePath}.");
                return false;
            }
            return true;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

        private static void SetTopCenter(RectTransform rt, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(w, h), new Vector2(0f, -y0));
        }

        private static void SetTopStretch(RectTransform rt, float y0, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, h);
            rt.anchoredPosition = new Vector2(0f, -y0);
        }

        private static Sprite LoadSprite(string fileNameNoExt)
        {
            return DragonsHoardSprites.Find(fileNameNoExt);
        }

        private static Image AddSpriteImage(RectTransform rt, string fileNameNoExt,
            bool raycastTarget = false, bool preserveAspect = false, bool sliced = false)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(fileNameNoExt);
            image.raycastTarget = raycastTarget;
            image.preserveAspect = preserveAspect;
            if (sliced) image.type = Image.Type.Sliced;
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
