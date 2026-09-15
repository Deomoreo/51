using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce l'overlay "Posta" (Assets/Mockup/06_posta.png) in HomeScreen.unity.
    /// Stessa architettura di AmiciBuilder/PremiBuilder. Il bottone di ingresso "MailButton"
    /// esiste gia' nella RightRail (HomeScreenBuilder, mai wired) - qui lo trovo e collego.
    /// Nessuna casella di posta reale nel progetto: lista statica, "RISCUOTI TUTTO" stub.
    /// </summary>
    public static class PostaBuilder
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
        private static readonly Color RowFill = new Color(0.086f, 0.157f, 0.235f);
        private static readonly Color RingMuted = new Color(0.18f, 0.31f, 0.42f);
        private static readonly Color RingHighlight = new Color(0.91f, 0.70f, 0.29f);

        private struct MailRow
        {
            public string Icon;
            public string Title;
            public string Subtitle;
            public string Badge;
        }

        private static readonly MailRow[] Mails =
        {
            new MailRow { Icon = "ic_gem_green", Title = "Ricompensa livello 2", Subtitle = "Hai ricevuto 500 monete", Badge = "1" },
            new MailRow { Icon = "ic_trophy", Title = "Torneo settimanale", Subtitle = "Sei arrivato 12° su 340" },
            new MailRow { Icon = "ic_person", Title = "Marco_88 ti ha aggiunto", Subtitle = "2 giorni fa", Badge = "1" },
            new MailRow { Icon = "ic_mail", Title = "Benvenuto in 51 Cirulla!", Subtitle = "Ecco un premio di benvenuto" },
            new MailRow { Icon = "ic_calendar", Title = "Evento del weekend", Subtitle = "Doppi punti fino a domenica" },
        };

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Posta")]
        private static void Build()
        {
            if (!LoadContext()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PostaBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            DestroyIfExists(canvasRect, "PostaOverlay");
            DestroyIfExists(canvasRect, "PostaManager");

            var mailButton = FindDeepChild(canvasRect, "MailButton")?.GetComponent<Button>();
            if (mailButton == null)
            {
                Debug.LogWarning("[PostaBuilder] 'MailButton' non trovato nella RightRail: il pannello resta apribile solo a mano.");
            }

            var overlay = CreateUIObject("PostaOverlay", canvasRect);
            StretchFill(overlay);
            var overlayCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            var dimmer = CreateDimBackground(overlay);
            var panelFrame = CreatePanelFrame(overlay);
            CreateTitleRibbon(panelFrame);
            var closeButton = CreateCloseButton(panelFrame);

            float y = 380f;
            foreach (var mail in Mails)
            {
                CreateMailRow(panelFrame, mail, y);
                y += 158f;
            }

            CreateFooterNote(panelFrame, y + 20f);
            var claimAllButton = CreateClaimAllButton(panelFrame, y + 80f);

            CreateController(canvasRect, overlay, overlayCanvasGroup, closeButton, dimmer, mailButton, claimAllButton);

            overlay.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[PostaBuilder] PostaOverlay creato in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // Controller wiring
        // ------------------------------------------------------------------

        private static void CreateController(RectTransform canvasRect, RectTransform overlay, CanvasGroup overlayCanvasGroup,
            Button closeButton, Button dimmerButton, Button openButton, Button claimAllButton)
        {
            var managerGO = CreateUIObject("PostaManager", canvasRect).gameObject;
            var controller = managerGO.AddComponent<PanelPostaController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
            so.FindProperty("canvasGroup").objectReferenceValue = overlayCanvasGroup;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("dimmerButton").objectReferenceValue = dimmerButton;
            so.FindProperty("openButton").objectReferenceValue = openButton;
            so.FindProperty("claimAllButton").objectReferenceValue = claimAllButton;
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
        // Chrome
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
            var text = AddText(textRt, "POSTA", 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
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
        // Righe messaggio
        // ------------------------------------------------------------------

        private static void CreateMailRow(RectTransform panelFrame, MailRow mail, float yAbs)
        {
            var row = CreateUIObject("Row_" + mail.Title, panelFrame);
            SetTopLeft(row, RowX0 - FrameX0, yAbs - FrameY0, RowX1 - RowX0, 134f);

            var fill = row.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = RowFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", row);
            StretchFill(ringRt);
            var ring = AddSpriteImage(ringRt, "panel_ring_r24", sliced: true);
            ring.color = string.IsNullOrEmpty(mail.Badge) ? RingMuted : RingHighlight;

            var iconRt = CreateUIObject("Icon", row);
            SetAnchoredRect(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(54f, 54f), new Vector2(30f, 0f));
            AddSpriteImage(iconRt, mail.Icon, preserveAspect: true);

            var titleRt = CreateUIObject("Title", row);
            SetTopLeft(titleRt, 104f, 26f, RowX1 - RowX0 - 220f, 36f);
            AddText(titleRt, mail.Title, 26f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.MidlineLeft);

            var subtitleRt = CreateUIObject("Subtitle", row);
            SetTopLeft(subtitleRt, 104f, 68f, RowX1 - RowX0 - 220f, 32f);
            AddText(subtitleRt, mail.Subtitle, 21f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            if (!string.IsNullOrEmpty(mail.Badge))
            {
                var badgeRt = CreateUIObject("Badge", row);
                SetAnchoredRect(badgeRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(44f, 44f), new Vector2(-30f, 0f));
                var badgeImg = badgeRt.gameObject.AddComponent<Image>();
                badgeImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                badgeImg.color = Hex("#D6473F");
                badgeImg.raycastTarget = false;

                var badgeTextRt = CreateUIObject("Text", badgeRt);
                StretchFill(badgeTextRt);
                AddText(badgeTextRt, mail.Badge, 20f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            }
        }

        // ------------------------------------------------------------------
        // Footer: nota + Riscuoti tutto
        // ------------------------------------------------------------------

        private static void CreateFooterNote(RectTransform panelFrame, float yAbs)
        {
            var rt = CreateUIObject("FooterNote", panelFrame);
            SetTopCenter(rt, yAbs - FrameY0, 800f, 30f);
            AddText(rt, "I messaggi scadono dopo 30 giorni", 19f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);
        }

        private static Button CreateClaimAllButton(RectTransform panelFrame, float yAbs)
        {
            var button = CreateUIObject("ClaimAllButton", panelFrame);
            SetTopCenter(button, yAbs - FrameY0, 700f, 84f);
            var bg = AddSpriteImage(button, "btn_gold_long", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var textRt = CreateUIObject("Text", button);
            StretchFill(textRt);
            var text = AddText(textRt, "RISCUOTI TUTTO", 28f, FontStyles.Bold, Hex("#3A2208"), TextAlignmentOptions.Center);
            text.outlineWidth = 0.15f;
            text.outlineColor = Hex("#241608");

            return btn;
        }

        // ------------------------------------------------------------------
        // Contesto / helper generici
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[PostaBuilder] UITheme non trovato in {ThemePath}.");
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
