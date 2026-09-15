using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce l'overlay "Premi Giornalieri" (Assets/Mockup/08_premi.png) in
    /// HomeScreen.unity. Stessa architettura di AmiciBuilder/ImpostazioniBuilder. Il
    /// bottone di ingresso "RewardButton" esiste gia' nella RightRail (HomeScreenBuilder,
    /// mai wired ad un onClick) - qui lo trovo e lo collego, non lo ricreo.
    /// Nessun sistema di ricompense reale nel progetto: griglia statica, "RISCUOTI" stub.
    /// </summary>
    public static class PremiBuilder
    {
        private const string ScenePath = "Assets/Scenes/HomeScreen.unity";
        private const string ThemePath = "Assets/Resources/DragonsHoardTheme.asset";

        private const float FrameX0 = 44f;
        private const float FrameY0 = 210f;
        private const float FrameX1 = 1036f;
        private const float FrameY1 = 1500f;
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        private const float RowX0 = 74f;
        private const float RowX1 = 1006f;

        private struct DayReward
        {
            public string Amount;
            public string Icon;
            public bool Claimed;
            public bool Current;
        }

        private static readonly DayReward[] Days =
        {
            new DayReward { Amount = "100", Icon = "ic_coin_clover", Claimed = true },
            new DayReward { Amount = "200", Icon = "ic_coin_clover", Claimed = true },
            new DayReward { Amount = "300", Icon = "ic_coin_clover", Current = true },
            new DayReward { Amount = "500", Icon = "ic_coin_clover" },
            new DayReward { Amount = "800", Icon = "ic_coin_clover" },
            new DayReward { Amount = "1.2k", Icon = "ic_coin_clover" },
            new DayReward { Amount = "GEMME", Icon = "ic_gem_green" },
        };

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Premi")]
        private static void Build()
        {
            if (!LoadContext()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[PremiBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            DestroyIfExists(canvasRect, "PremiOverlay");
            DestroyIfExists(canvasRect, "PremiManager");

            var rewardButton = FindDeepChild(canvasRect, "RewardButton")?.GetComponent<Button>();
            if (rewardButton == null)
            {
                Debug.LogWarning("[PremiBuilder] 'RewardButton' non trovato nella RightRail: il pannello resta apribile solo a mano.");
            }

            var overlay = CreateUIObject("PremiOverlay", canvasRect);
            StretchFill(overlay);
            var overlayCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            var dimmer = CreateDimBackground(overlay);
            var panelFrame = CreatePanelFrame(overlay);
            CreateTitleRibbon(panelFrame);
            CreateSubtitle(panelFrame, 350f);
            var closeButton = CreateCloseButton(panelFrame);

            CreateDayGrid(panelFrame, 420f);

            CreateCountdownText(panelFrame, 1200f);
            var claimButton = CreateClaimButton(panelFrame, 1250f);
            CreateFooterNote(panelFrame, 1340f);

            CreateController(canvasRect, overlay, overlayCanvasGroup, closeButton, dimmer, rewardButton, claimButton);

            overlay.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[PremiBuilder] PremiOverlay creato in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // Controller wiring
        // ------------------------------------------------------------------

        private static void CreateController(RectTransform canvasRect, RectTransform overlay, CanvasGroup overlayCanvasGroup,
            Button closeButton, Button dimmerButton, Button openButton, Button claimButton)
        {
            var managerGO = CreateUIObject("PremiManager", canvasRect).gameObject;
            var controller = managerGO.AddComponent<PanelPremiController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
            so.FindProperty("canvasGroup").objectReferenceValue = overlayCanvasGroup;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("dimmerButton").objectReferenceValue = dimmerButton;
            so.FindProperty("openButton").objectReferenceValue = openButton;
            so.FindProperty("claimButton").objectReferenceValue = claimButton;
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
            SetTopCenter(ribbon, 154f - FrameY0, 560f, 188f);
            AddSpriteImage(ribbon, "ribbon_teal", raycastTarget: false, sliced: true);

            var textRt = CreateUIObject("Text", ribbon);
            StretchFill(textRt);
            var text = AddText(textRt, "PREMI GIORNALIERI", 34f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            text.outlineWidth = 0.2f;
            text.outlineColor = Hex("#241608");
        }

        private static void CreateSubtitle(RectTransform panelFrame, float yAbs)
        {
            var rt = CreateUIObject("Subtitle", panelFrame);
            SetTopCenter(rt, yAbs - FrameY0, 800f, 32f);
            AddText(rt, "Torna ogni giorno per premi migliori", 21f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);
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
        // Griglia 7 giorni (4 + 3, entrambe le righe centrate)
        // ------------------------------------------------------------------

        private const float CellSize = 204f;
        private const float CellGap = 30f;

        private static void CreateDayGrid(RectTransform panelFrame, float rowsTopYAbs)
        {
            float contentWidth = RowX1 - RowX0;

            CreateRowOfCells(panelFrame, 0, 4, rowsTopYAbs, contentWidth);
            CreateRowOfCells(panelFrame, 4, 3, rowsTopYAbs + CellSize + 76f, contentWidth);
        }

        private static void CreateRowOfCells(RectTransform panelFrame, int startIndex, int count, float yAbs, float contentWidth)
        {
            float rowWidth = count * CellSize + (count - 1) * CellGap;
            float startX = RowX0 + (contentWidth - rowWidth) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int dayIndex = startIndex + i;
                float x = startX + i * (CellSize + CellGap);
                CreateDayCell(panelFrame, Days[dayIndex], dayIndex + 1, x, yAbs);
            }
        }

        private static void CreateDayCell(RectTransform panelFrame, DayReward day, int dayNumber, float xAbs, float yAbs)
        {
            var cell = CreateUIObject("DayCell_" + dayNumber, panelFrame);
            SetTopLeft(cell, xAbs - FrameX0, yAbs - FrameY0, CellSize, CellSize);

            var fill = cell.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = day.Claimed ? new Color(0.06f, 0.16f, 0.10f) : Hex("#16283C");
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", cell);
            StretchFill(ringRt);
            var ring = AddSpriteImage(ringRt, "panel_ring_r24", sliced: true);
            ring.color = day.Current ? _theme.Gold : new Color(0.18f, 0.31f, 0.42f);

            var iconRt = CreateUIObject("Icon", cell);
            SetAnchoredRect(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(74f, 74f), new Vector2(0f, -28f));
            AddSpriteImage(iconRt, day.Icon, preserveAspect: true);

            var amountRt = CreateUIObject("Amount", cell);
            SetAnchoredRect(amountRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(CellSize - 16f, 34f), new Vector2(0f, -110f));
            AddText(amountRt, day.Amount, day.Amount == "GEMME" ? 22f : 26f, FontStyles.Bold,
                day.Claimed ? _theme.TextMuted : _theme.Cream, TextAlignmentOptions.Center);

            var labelRt = CreateUIObject("DayLabel", panelFrame);
            SetTopLeft(labelRt, xAbs - FrameX0, yAbs - FrameY0 + CellSize + 10f, CellSize, 30f);
            AddText(labelRt, "Giorno " + dayNumber, 20f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);

            if (day.Claimed)
            {
                var checkRt = CreateUIObject("CheckIcon", cell);
                SetAnchoredRect(checkRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(40f, 40f), new Vector2(-8f, -8f));
                AddSpriteImage(checkRt, "ic_check", preserveAspect: true);
            }
        }

        // ------------------------------------------------------------------
        // Countdown / Riscuoti / nota
        // ------------------------------------------------------------------

        private static void CreateCountdownText(RectTransform panelFrame, float yAbs)
        {
            var rt = CreateUIObject("CountdownText", panelFrame);
            SetTopCenter(rt, yAbs - FrameY0, 700f, 34f);
            AddText(rt, "Prossimo premio tra 14h 22m", 23f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
        }

        private static Button CreateClaimButton(RectTransform panelFrame, float yAbs)
        {
            var button = CreateUIObject("ClaimButton", panelFrame);
            SetTopCenter(button, yAbs - FrameY0, 700f, 84f);
            var bg = AddSpriteImage(button, "btn_gold_long", raycastTarget: true, sliced: true);
            var btn = button.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var textRt = CreateUIObject("Text", button);
            StretchFill(textRt);
            var text = AddText(textRt, "RISCUOTI · 300", 30f, FontStyles.Bold, Hex("#3A2208"), TextAlignmentOptions.Center);
            text.outlineWidth = 0.15f;
            text.outlineColor = Hex("#241608");

            return btn;
        }

        private static void CreateFooterNote(RectTransform panelFrame, float yAbs)
        {
            var rt = CreateUIObject("FooterNote", panelFrame);
            SetTopCenter(rt, yAbs - FrameY0, 800f, 30f);
            AddText(rt, "Se salti un giorno riparti dal Giorno 1", 19f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // Contesto / helper generici
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[PremiBuilder] UITheme non trovato in {ThemePath}.");
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
