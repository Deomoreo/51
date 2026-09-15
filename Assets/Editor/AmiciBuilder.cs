using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce l'overlay "Amici" (Assets/Mockup/07_amici.png) in HomeScreen.unity.
    /// Stessa architettura di ImpostazioniBuilder (fill+ring per le righe, controller su
    /// GameObject sempre attivo, niente ScrollRect - il roster mock ci sta per intero).
    /// Aggiunge il bottone di ingresso "FriendsButton" (ic_person) alla RightRail.
    /// Nessun sistema amici reale nel progetto (verificato): roster statico, azioni stub.
    /// </summary>
    public static class AmiciBuilder
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
        private static readonly Color RowFill = new Color(0.086f, 0.157f, 0.235f);   // #16283C
        private static readonly Color RowRingOffline = new Color(0.18f, 0.31f, 0.42f); // #2E4F6C
        private static readonly Color RowRingOnline = new Color(0.91f, 0.70f, 0.29f);  // #E8B24A-ish

        private struct FriendRow
        {
            public string Name;
            public string Status;
            public bool Online;
            public bool InPartita;
        }

        private static readonly FriendRow[] Online = new[]
        {
            new FriendRow { Name = "Marco_88", Status = "In partita", Online = true, InPartita = true },
            new FriendRow { Name = "Luca_02", Status = "Disponibile", Online = true, InPartita = false },
        };

        private static readonly FriendRow[] Offline = new[]
        {
            new FriendRow { Name = "Giulia_R", Status = "2 ore fa" },
            new FriendRow { Name = "Andrea99", Status = "Ieri" },
            new FriendRow { Name = "Paolo_C", Status = "3 giorni fa" },
        };

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Amici")]
        private static void Build()
        {
            if (!LoadContext()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[AmiciBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            DestroyIfExists(canvasRect, "AmiciOverlay");
            DestroyIfExists(canvasRect, "AmiciManager");

            var friendsButton = EnsureFriendsRailButton(canvasRect);

            var overlay = CreateUIObject("AmiciOverlay", canvasRect);
            StretchFill(overlay);
            var overlayCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            var dimmer = CreateDimBackground(overlay);
            var panelFrame = CreatePanelFrame(overlay);
            CreateTitleRibbon(panelFrame);
            var closeButton = CreateCloseButton(panelFrame);

            var addButton = CreateSearchRow(panelFrame, 390f);

            float y = 480f;
            CreateSectionLabel(panelFrame, $"ONLINE {Online.Length}", y);
            y += 46f;
            foreach (var friend in Online)
            {
                CreateFriendRow(panelFrame, friend, y);
                y += 126f;
            }

            y += 30f;
            CreateSectionLabel(panelFrame, $"OFFLINE {Offline.Length}", y);
            y += 46f;
            foreach (var friend in Offline)
            {
                CreateFriendRow(panelFrame, friend, y);
                y += 126f;
            }

            var codeText = CreateFooterCode(panelFrame, 1650f);
            var shareButton = CreateShareButton(panelFrame, 1700f);

            CreateController(canvasRect, overlay, overlayCanvasGroup, closeButton, dimmer, friendsButton,
                addButton, shareButton);

            overlay.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[AmiciBuilder] AmiciOverlay creato in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // RightRail entry point
        // ------------------------------------------------------------------

        private static Button EnsureFriendsRailButton(RectTransform canvasRect)
        {
            var rail = FindDeepChild(canvasRect, "RightRail");
            if (rail == null)
            {
                Debug.LogWarning("[AmiciBuilder] RightRail non trovata in Home: il bottone Amici non viene creato.");
                return null;
            }

            var existing = FindDeepChild(rail, "FriendsButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var button = CreateUIObject("FriendsButton", rail);
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
            var iconImage = AddSpriteImage(icon, "ic_person", preserveAspect: true);

            var labelRt = CreateUIObject("Label", button);
            SetAnchoredRect(labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(112f, 24f), new Vector2(0f, -16f));
            AddText(labelRt, "Amici", 17f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);

            var themedButton = button.gameObject.AddComponent<ThemedButton>();
            var so = new SerializedObject(themedButton);
            so.FindProperty("theme").objectReferenceValue = _theme;
            so.FindProperty("variant").enumValueIndex = (int)ThemedButton.Variant.IconTab;
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            themedButton.Apply();

            // Ordine rail: Premi, Classifica, Posta, Impostazioni (gia' esistenti), Amici per
            // ultima cosi' l'ordine visivo di quelle gia' approvate non cambia.
            button.SetAsLastSibling();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)rail);

            return btn;
        }

        // ------------------------------------------------------------------
        // Controller wiring
        // ------------------------------------------------------------------

        private static void CreateController(RectTransform canvasRect, RectTransform overlay, CanvasGroup overlayCanvasGroup,
            Button closeButton, Button dimmerButton, Button openButton, Button addButton, Button shareButton)
        {
            var managerGO = CreateUIObject("AmiciManager", canvasRect).gameObject;
            var controller = managerGO.AddComponent<PanelAmiciController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
            so.FindProperty("canvasGroup").objectReferenceValue = overlayCanvasGroup;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("dimmerButton").objectReferenceValue = dimmerButton;
            so.FindProperty("openButton").objectReferenceValue = openButton;
            so.FindProperty("addFriendButton").objectReferenceValue = addButton;
            so.FindProperty("shareCodeButton").objectReferenceValue = shareButton;
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
            var text = AddText(textRt, "AMICI", 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
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
        // Search + Aggiungi
        // ------------------------------------------------------------------

        private static Button CreateSearchRow(RectTransform panelFrame, float yAbs)
        {
            float addButtonWidth = 220f;
            float spacing = 16f;
            float searchWidth = (RowX1 - RowX0) - addButtonWidth - spacing;

            var searchRt = CreateUIObject("SearchField", panelFrame);
            SetTopLeft(searchRt, RowX0 - FrameX0, yAbs - FrameY0, searchWidth, 74f);
            CreateRowBackground(searchRt, RowRingOffline);

            var placeholder = CreateUIObject("Placeholder", searchRt);
            SetTopLeft(placeholder, 24f, 0f, searchWidth - 48f, 74f);
            AddText(placeholder, "Cerca per nome o codice...", 22f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            var addButton = CreateUIObject("AddFriendButton", panelFrame);
            SetTopLeft(addButton, RowX1 - FrameX0 - addButtonWidth, yAbs - FrameY0, addButtonWidth, 74f);
            var bg = AddSpriteImage(addButton, "btn_green_small", raycastTarget: true, sliced: true);
            var btn = addButton.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var textRt = CreateUIObject("Text", addButton);
            StretchFill(textRt);
            AddText(textRt, "+ AGGIUNGI", 22f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            return btn;
        }

        // ------------------------------------------------------------------
        // Sezioni / righe amico
        // ------------------------------------------------------------------

        private static void CreateSectionLabel(RectTransform panelFrame, string label, float yAbs)
        {
            var rt = CreateUIObject("Section_" + label, panelFrame);
            SetTopLeft(rt, RowX0 - FrameX0, yAbs - FrameY0, RowX1 - RowX0, 30f);
            AddText(rt, label, 22f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineLeft);
        }

        private static void CreateRowBackground(RectTransform row, Color ringColor)
        {
            var fill = row.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = RowFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", row);
            StretchFill(ringRt);
            var ring = AddSpriteImage(ringRt, "panel_ring_r24", sliced: true);
            ring.color = ringColor;
        }

        private static void CreateFriendRow(RectTransform panelFrame, FriendRow friend, float yAbs)
        {
            var row = CreateUIObject("Row_" + friend.Name, panelFrame);
            SetTopLeft(row, RowX0 - FrameX0, yAbs - FrameY0, RowX1 - RowX0, 110f);
            CreateRowBackground(row, friend.Online ? RowRingOnline : RowRingOffline);

            var avatarRt = CreateUIObject("Avatar", row);
            SetAnchoredRect(avatarRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(52f, 52f), new Vector2(28f, 0f));
            var avatarImg = AddSpriteImage(avatarRt, "ic_person", preserveAspect: true);
            avatarImg.color = _theme.TextMuted;

            var nameRt = CreateUIObject("Name", row);
            SetTopLeft(nameRt, 100f, 20f, RowX1 - RowX0 - 320f, 34f);
            AddText(nameRt, friend.Name, 27f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.MidlineLeft);

            var statusRt = CreateUIObject("Status", row);
            SetTopLeft(statusRt, 100f, 58f, RowX1 - RowX0 - 320f, 28f);
            AddText(statusRt, friend.Status, 20f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            if (friend.Online)
            {
                var inviteRt = CreateUIObject("InviteButton", row);
                SetAnchoredRect(inviteRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(150f, 56f), new Vector2(-58f, 0f));
                var inviteBg = AddSpriteImage(inviteRt, "btn_teal", raycastTarget: true, sliced: true);
                inviteRt.gameObject.AddComponent<Button>().targetGraphic = inviteBg;
                var inviteTextRt = CreateUIObject("Text", inviteRt);
                StretchFill(inviteTextRt);
                AddText(inviteTextRt, "INVITA", 20f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            }

            var dotRt = CreateUIObject("StatusDot", row);
            SetAnchoredRect(dotRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(22f, 22f), new Vector2(-28f, 0f));
            var dotImg = dotRt.gameObject.AddComponent<Image>();
            dotImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            dotImg.color = friend.Online ? Hex("#3CD16E") : Hex("#5A6B7A");
            dotImg.raycastTarget = false;
        }

        // ------------------------------------------------------------------
        // Footer: codice amico + condividi
        // ------------------------------------------------------------------

        private static TMP_Text CreateFooterCode(RectTransform panelFrame, float yAbs)
        {
            var rt = CreateUIObject("FriendCodeText", panelFrame);
            SetTopCenter(rt, yAbs - FrameY0, 700f, 36f);
            return AddText(rt, "Il tuo codice amico: DEO-7742", 24f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.Center);
        }

        private static Button CreateShareButton(RectTransform panelFrame, float yAbs)
        {
            var button = CreateUIObject("ShareCodeButton", panelFrame);
            SetTopCenter(button, yAbs - FrameY0, 700f, 72f);
            var bg = AddSpriteImage(button, "btn_blue_long", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var textRt = CreateUIObject("Text", button);
            StretchFill(textRt);
            AddText(textRt, "CONDIVIDI CODICE", 26f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

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
                Debug.LogError($"[AmiciBuilder] UITheme non trovato in {ThemePath}.");
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
