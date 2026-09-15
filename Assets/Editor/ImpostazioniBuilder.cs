using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce l'overlay "Impostazioni" (Assets/Mockup/05_impostazioni.png) come figlio
    /// disattivato del Canvas in HomeScreen.unity. Stessa architettura collaudata di
    /// PanelMazzoBuilder/PanelModalitaBuilder: PanelFrame ancorato al centro con coordinate
    /// PanelFrame-relative, controller su GameObject sempre attivo, niente ScrollRect (il
    /// contenuto ci sta per intero come nel mockup, niente scroll necessario).
    ///
    /// Aggiunge anche il bottone di ingresso "SettingsButton" (ic_gear) alla RightRail di
    /// Home, che prima non esisteva (RewardButton/LeaderboardButton/MailButton c'erano gia').
    ///
    /// Nota onesta sul knob del toggle: il manifest sprite non contiene un pallino
    /// circolare dedicato (bar_knob.png e' in realta' una barra 350x78, non un cerchio).
    /// Uso sq_gold in slice a dimensione ridotta (bordo 9-slice 35px su un target di ~40px
    /// lo fa leggere come un pallino arrotondato) - primo tentativo ragionevole, da
    /// verificare a schermo appena Unity e' collegato, non uno sprite dedicato "giusto".
    /// </summary>
    public static class ImpostazioniBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private const float FrameX0 = 44f;
        private const float FrameY0 = 210f;
        private const float FrameX1 = 1036f;
        private const float FrameY1 = 1830f;
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        private const float RowX0 = 74f;
        private const float RowX1 = 1006f;
        private static readonly Color RowFill = new Color(0.086f, 0.157f, 0.235f); // #16283C
        private static readonly Color RowRing = new Color(0.18f, 0.31f, 0.42f);    // #2E4F6C

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Impostazioni")]
        private static void Build()
        {
            if (!LoadContext()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ImpostazioniBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            DestroyIfExists(canvasRect, "ImpostazioniOverlay");
            DestroyIfExists(canvasRect, "ImpostazioniManager");

            var settingsButton = EnsureSettingsRailButton(canvasRect);

            var overlay = CreateUIObject("ImpostazioniOverlay", canvasRect);
            StretchFill(overlay);
            var overlayCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            var dimmer = CreateDimBackground(overlay);
            var panelFrame = CreatePanelFrame(overlay);
            CreateTitleRibbon(panelFrame);
            var closeButton = CreateCloseButton(panelFrame);

            float y = 390f;
            CreateSectionLabel(panelFrame, "AUDIO", y);
            y += 46f;
            var musicaToggle = CreateToggleRow(panelFrame, "Row_Musica", "Musica", "Musica di sottofondo", y);
            y += 146f;
            var effettiToggle = CreateToggleRow(panelFrame, "Row_Effetti", "Effetti sonori", "Carte, vittorie, click", y);
            y += 146f;
            var vibrazioneToggle = CreateToggleRow(panelFrame, "Row_Vibrazione", "Vibrazione", "Feedback aptico", y);
            y += 176f;

            CreateSectionLabel(panelFrame, "GIOCO", y);
            y += 46f;
            var animazioniToggle = CreateToggleRow(panelFrame, "Row_AnimazioniVeloci", "Animazioni veloci", "Riduce i tempi delle animazioni", y);
            y += 146f;
            var notificheToggle = CreateToggleRow(panelFrame, "Row_Notifiche", "Notifiche", "Turno, inviti, premi", y);
            y += 176f;

            CreateSectionLabel(panelFrame, "ACCOUNT", y);
            y += 46f;
            CreateInfoRow(panelFrame, "Row_Account", "ic_person", "Guest_66B9B973", "Tocca per registrarti", y, 130f);
            y += 146f;
            CreateInfoRow(panelFrame, "Row_Lingua", "ic_gear", "Lingua", "Italiano", y, 110f);
            y += 126f;
            CreateInfoRow(panelFrame, "Row_Supporto", "ic_mail", "Supporto", "Segnala un problema", y, 110f);
            y += 150f;

            var logoutButton = CreateLogoutButton(panelFrame, y);
            y += 100f;
            CreateFooterText(panelFrame, y);

            CreateController(canvasRect, overlay, overlayCanvasGroup, closeButton, dimmer, settingsButton,
                logoutButton, musicaToggle, effettiToggle, vibrazioneToggle, animazioniToggle, notificheToggle);

            overlay.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ImpostazioniBuilder] ImpostazioniOverlay creato in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // RightRail entry point
        // ------------------------------------------------------------------

        private static Button EnsureSettingsRailButton(RectTransform canvasRect)
        {
            var rail = FindDeepChild(canvasRect, "RightRail");
            if (rail == null)
            {
                Debug.LogWarning("[ImpostazioniBuilder] RightRail non trovata in Home: il bottone Impostazioni non viene creato, il pannello resta apribile solo a mano.");
                return null;
            }

            var existing = FindDeepChild(rail, "SettingsButton");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var button = CreateUIObject("SettingsButton", (RectTransform)rail);
            SetAnchoredRect(button, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(112f, 112f), Vector2.zero);

            var layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 112f;
            layoutElement.preferredHeight = 112f;

            var background = AddSpriteImage(button, "sq_blue", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = background;

            var icon = CreateUIObject("Icon", button);
            SetAnchoredRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(66f, 66f), Vector2.zero);
            var iconImage = AddSpriteImage(icon, "ic_gear", preserveAspect: true);

            var labelRt = CreateUIObject("Label", button);
            SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(112f, 24f), new Vector2(0f, -16f));
            AddText(labelRt, "Opzioni", 17f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);

            AssignThemedButton(button.gameObject, background, iconImage);

            // Ordine nella VerticalLayoutGroup della rail: metto Impostazioni per ultima
            // (dopo Premi/Classifica/Posta), cosi' l'ordine esistente non cambia visivamente.
            button.SetAsLastSibling();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)rail);

            return btn;
        }

        /// <summary>
        /// ThemedButton.Variant.IconTab richiede il campo iconImage assegnato via
        /// SerializedObject (altrimenti FindIconImage cade sul fallback a tag, che spamma
        /// "Tag: icon is not defined" in Console - bug gia' visto e fixato altrove in questo
        /// progetto per la RightRail esistente).
        /// </summary>
        private static void AssignThemedButton(GameObject go, Image background, Image iconImage)
        {
            var themedButton = go.AddComponent<ThemedButton>();
            var so = new SerializedObject(themedButton);
            so.FindProperty("theme").objectReferenceValue = _theme;
            so.FindProperty("variant").enumValueIndex = (int)ThemedButton.Variant.IconTab;
            var iconProp = so.FindProperty("iconImage");
            if (iconProp != null) iconProp.objectReferenceValue = iconImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            themedButton.Apply();
        }

        // ------------------------------------------------------------------
        // Controller wiring
        // ------------------------------------------------------------------

        private static void CreateController(RectTransform canvasRect, RectTransform overlay, CanvasGroup overlayCanvasGroup,
            Button closeButton, Button dimmerButton, Button openButton, Button logoutButton,
            SimpleToggleSwitch musica, SimpleToggleSwitch effetti, SimpleToggleSwitch vibrazione,
            SimpleToggleSwitch animazioniVeloci, SimpleToggleSwitch notifiche)
        {
            var managerGO = CreateUIObject("ImpostazioniManager", canvasRect).gameObject;
            var controller = managerGO.AddComponent<PanelImpostazioniController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
            so.FindProperty("canvasGroup").objectReferenceValue = overlayCanvasGroup;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("dimmerButton").objectReferenceValue = dimmerButton;
            so.FindProperty("openButton").objectReferenceValue = openButton;
            so.FindProperty("logoutButton").objectReferenceValue = logoutButton;
            so.FindProperty("musicaToggle").objectReferenceValue = musica;
            so.FindProperty("effettiToggle").objectReferenceValue = effetti;
            so.FindProperty("vibrazioneToggle").objectReferenceValue = vibrazione;
            so.FindProperty("animazioniVelociToggle").objectReferenceValue = animazioniVeloci;
            so.FindProperty("notificheToggle").objectReferenceValue = notifiche;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private static void DestroyIfExists(RectTransform canvasRect, string name)
        {
            var existing = canvasRect.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        // ------------------------------------------------------------------
        // Chrome: dim, frame, ribbon, close
        // ------------------------------------------------------------------

        private static Button CreateDimBackground(RectTransform overlay)
        {
            var dim = CreateUIObject("DimBackground", overlay);
            StretchFill(dim);
            var img = dim.gameObject.AddComponent<Image>();
            var dimColor = Hex("#040A14");
            img.color = new Color(dimColor.r, dimColor.g, dimColor.b, 220f / 255f);
            img.raycastTarget = true;
            return dim.gameObject.AddComponent<Button>();
        }

        private static RectTransform CreatePanelFrame(RectTransform overlay)
        {
            float w = FrameX1 - FrameX0;
            float h = FrameY1 - FrameY0;
            float boxCenterX = (FrameX0 + FrameX1) * 0.5f;
            float boxCenterY = (FrameY0 + FrameY1) * 0.5f;
            float offsetX = boxCenterX - RefWidth * 0.5f;
            float offsetY = RefHeight * 0.5f - boxCenterY;

            var outer = CreateUIObject("PanelFrame", overlay);
            SetAnchoredRect(outer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(w, h), new Vector2(offsetX, offsetY));
            var outerImg = outer.gameObject.AddComponent<Image>();
            outerImg.color = Hex("#785018");
            outerImg.raycastTarget = true;

            var gold = CreateUIObject("GoldBorder", outer);
            StretchFill(gold);
            gold.offsetMin = new Vector2(14f, 14f);
            gold.offsetMax = new Vector2(-14f, -14f);
            var goldImg = gold.gameObject.AddComponent<Image>();
            goldImg.color = Hex("#E8B24A");
            goldImg.raycastTarget = false;

            var fill = CreateUIObject("Fill", gold);
            StretchFill(fill);
            fill.offsetMin = new Vector2(8f, 8f);
            fill.offsetMax = new Vector2(-8f, -8f);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = Hex("#182838");
            fillImg.raycastTarget = false;

            return outer;
        }

        private static void CreateTitleRibbon(RectTransform panelFrame)
        {
            var ribbon = CreateUIObject("TitleRibbon", panelFrame);
            SetTopCenter(ribbon, 154f - FrameY0, 520f, 188f);
            AddSpriteImage(ribbon, "ribbon_teal", raycastTarget: false, sliced: true);

            var textRt = CreateUIObject("Text", ribbon);
            StretchFill(textRt);
            var text = AddText(textRt, "IMPOSTAZIONI", 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            text.outlineWidth = 0.2f;
            text.outlineColor = Hex("#241608");
        }

        private static Button CreateCloseButton(RectTransform panelFrame)
        {
            var button = CreateUIObject("CloseButton", panelFrame);
            SetTopLeft(button, 932f - FrameX0, 236f - FrameY0, 86f, 86f);
            var bg = AddSpriteImage(button, "sq_blue", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var icon = CreateUIObject("Icon", button);
            SetAnchoredRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(46f, 46f), Vector2.zero);
            AddSpriteImage(icon, "ic_x", preserveAspect: true);

            return btn;
        }

        // ------------------------------------------------------------------
        // Sezioni / righe
        // ------------------------------------------------------------------

        private static void CreateSectionLabel(RectTransform panelFrame, string label, float yAbs)
        {
            var rt = CreateUIObject("Section_" + label, panelFrame);
            SetTopLeft(rt, RowX0 - FrameX0, yAbs - FrameY0, RowX1 - RowX0, 30f);
            AddText(rt, label, 22f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineLeft);
        }

        private static void CreateRowBackground(RectTransform row)
        {
            // "list_row" esiste nel foglio ma border=(0,0,0,0) (non sliceizzato): stretchato su
            // una riga larga 938px si sarebbe distorto. panel_fill_r24/panel_ring_r24 hanno un
            // bordo 9-slice vero di 48px (sicuro su righe alte 110-130px, 2x48=96), stessa
            // tecnica fill+ring gia' usata per PanelFrame (gold border) in questo file.
            var fill = row.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = RowFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", row);
            StretchFill(ringRt);
            var ring = AddSpriteImage(ringRt, "panel_ring_r24", sliced: true);
            ring.color = RowRing;
        }

        private static SimpleToggleSwitch CreateToggleRow(RectTransform panelFrame, string name, string title, string subtitle, float yAbs)
        {
            var row = CreateUIObject(name, panelFrame);
            SetTopLeft(row, RowX0 - FrameX0, yAbs - FrameY0, RowX1 - RowX0, 130f);
            CreateRowBackground(row);

            var titleRt = CreateUIObject("Title", row);
            SetTopLeft(titleRt, 28f, 22f, RowX1 - RowX0 - 200f, 36f);
            AddText(titleRt, title, 28f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.MidlineLeft);

            var subtitleRt = CreateUIObject("Subtitle", row);
            SetTopLeft(subtitleRt, 28f, 66f, RowX1 - RowX0 - 200f, 32f);
            AddText(subtitleRt, subtitle, 21f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            return CreateToggleSwitch(row);
        }

        private static SimpleToggleSwitch CreateToggleSwitch(RectTransform row)
        {
            var toggleRt = CreateUIObject("Toggle", row);
            SetAnchoredRect(toggleRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(92f, 50f), new Vector2(-28f, 0f));

            var track = AddSpriteImage(toggleRt, "bar_empty", raycastTarget: true, sliced: true);
            var button = toggleRt.gameObject.AddComponent<Button>();
            button.targetGraphic = track;

            var knobRt = CreateUIObject("Knob", toggleRt);
            SetAnchoredRect(knobRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(40f, 40f), new Vector2(-22f, 0f));
            var knobImg = knobRt.gameObject.AddComponent<Image>();
            // sq_gold ha un colore dorato "cotto dentro" lo sprite (tintarlo di bianco non lo
            // schiarisce, un moltiplicatore non puo' schiarire) e non e' un vero cerchio -
            // uso lo sprite circolare nativo di Unity (stesso usato da LobbyPrefabBuilder per
            // lo stesso scopo) per un pallino bianco pulito come nel mockup.
            knobImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            knobImg.color = Color.white;
            knobImg.raycastTarget = false;

            var toggle = toggleRt.gameObject.AddComponent<SimpleToggleSwitch>();
            var so = new SerializedObject(toggle);
            so.FindProperty("trackImage").objectReferenceValue = track;
            so.FindProperty("knob").objectReferenceValue = knobRt;
            so.FindProperty("knobOffsetX").floatValue = 22f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        private static void CreateInfoRow(RectTransform panelFrame, string name, string iconSprite, string title, string subtitle, float yAbs, float height)
        {
            var row = CreateUIObject(name, panelFrame);
            SetTopLeft(row, RowX0 - FrameX0, yAbs - FrameY0, RowX1 - RowX0, height);
            CreateRowBackground(row);

            var iconRt = CreateUIObject("Icon", row);
            SetAnchoredRect(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(48f, 48f), new Vector2(28f, 0f));
            AddSpriteImage(iconRt, iconSprite, preserveAspect: true);

            var titleRt = CreateUIObject("Title", row);
            SetTopLeft(titleRt, 96f, height * 0.5f - 34f, RowX1 - RowX0 - 130f, 34f);
            AddText(titleRt, title, 27f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.MidlineLeft);

            var subtitleRt = CreateUIObject("Subtitle", row);
            SetTopLeft(subtitleRt, 96f, height * 0.5f + 2f, RowX1 - RowX0 - 130f, 30f);
            AddText(subtitleRt, subtitle, 20f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);
        }

        private static Button CreateLogoutButton(RectTransform panelFrame, float yAbs)
        {
            float w = 580f;
            var button = CreateUIObject("LogoutButton", panelFrame);
            SetTopCenter(button, yAbs - FrameY0, w, 74f);
            var bg = AddSpriteImage(button, "btn_blue_long", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var textRt = CreateUIObject("Text", button);
            StretchFill(textRt);
            AddText(textRt, "ESCI DALL'ACCOUNT", 26f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            return btn;
        }

        private static void CreateFooterText(RectTransform panelFrame, float yAbs)
        {
            var rt = CreateUIObject("FooterText", panelFrame);
            SetTopCenter(rt, yAbs - FrameY0, 600f, 30f);
            AddText(rt, "51 Cirulla · v1.0", 20f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // Contesto / helper generici (stessi di PanelMazzoBuilder)
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[ImpostazioniBuilder] UITheme non trovato in {ThemePath}.");
                return false;
            }
            return true;
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

        private static void SetTopLeft(RectTransform rt, float x0, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(w, h), new Vector2(x0, -y0));
        }

        private static void SetTopCenter(RectTransform rt, float y0, float w, float h)
        {
            SetAnchoredRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(w, h), new Vector2(0f, -y0));
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
