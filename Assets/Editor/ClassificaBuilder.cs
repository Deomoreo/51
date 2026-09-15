using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Project51.Unity;

namespace Project51.EditorTools
{
    /// <summary>
    /// Costruisce l'overlay "Classifica" (Assets/Mockup/18_classifica.png) in
    /// HomeScreen.unity. Stessa architettura di PostaBuilder/AmiciBuilder. Il bottone di
    /// ingresso "LeaderboardButton" esiste gia' nella RightRail - qui lo trovo e collego.
    /// Nessun sistema di classifica reale nel progetto: podio + lista sono dati statici,
    /// i 3 filtri Globale/Amici/Settimana sono decorativi (nessun dato diverso dietro,
    /// niente Button su quelli non selezionati - stesso principio di onesta' gia' usato
    /// per le righe non cliccabili altrove in questo progetto).
    /// </summary>
    public static class ClassificaBuilder
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

        private struct RankRow
        {
            public int Rank;
            public string Name;
            public string Score;
            public bool IsYou;
        }

        private static readonly RankRow[] ListRows =
        {
            new RankRow { Rank = 4, Name = "Andrea99", Score = "1.088" },
            new RankRow { Rank = 5, Name = "Paolo_C", Score = "1.002" },
            new RankRow { Rank = 6, Name = "Sara_M", Score = "954" },
            new RankRow { Rank = 7, Name = "Deomoreo", Score = "901", IsYou = true },
        };

        private static UITheme _theme;

        [MenuItem("Tools/Dragons Hoard/Build Classifica")]
        private static void Build()
        {
            if (!LoadContext()) return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ClassificaBuilder] Nessun Canvas trovato in HomeScreen.unity.");
                return;
            }

            var canvasRect = (RectTransform)canvas.transform;

            DestroyIfExists(canvasRect, "ClassificaOverlay");
            DestroyIfExists(canvasRect, "ClassificaManager");

            var leaderboardButton = FindDeepChild(canvasRect, "LeaderboardButton")?.GetComponent<Button>();
            if (leaderboardButton == null)
            {
                Debug.LogWarning("[ClassificaBuilder] 'LeaderboardButton' non trovato nella RightRail: il pannello resta apribile solo a mano.");
            }

            var overlay = CreateUIObject("ClassificaOverlay", canvasRect);
            StretchFill(overlay);
            var overlayCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            var dimmer = CreateDimBackground(overlay);
            var panelFrame = CreatePanelFrame(overlay);
            CreateTitleRibbon(panelFrame);
            var closeButton = CreateCloseButton(panelFrame);

            CreateFilterPills(panelFrame, 360f);
            CreatePodium(panelFrame, 470f);

            float y = 900f;
            foreach (var row in ListRows)
            {
                CreateListRow(panelFrame, row, y);
                y += 106f;
            }

            CreateFooterNote(panelFrame, y + 30f);

            CreateController(canvasRect, overlay, overlayCanvasGroup, closeButton, dimmer, leaderboardButton);

            overlay.gameObject.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ClassificaBuilder] ClassificaOverlay creato in HomeScreen.unity. Salvato.");
        }

        // ------------------------------------------------------------------
        // Controller wiring
        // ------------------------------------------------------------------

        private static void CreateController(RectTransform canvasRect, RectTransform overlay, CanvasGroup overlayCanvasGroup,
            Button closeButton, Button dimmerButton, Button openButton)
        {
            var managerGO = CreateUIObject("ClassificaManager", canvasRect).gameObject;
            var controller = managerGO.AddComponent<PanelClassificaController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
            so.FindProperty("canvasGroup").objectReferenceValue = overlayCanvasGroup;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("dimmerButton").objectReferenceValue = dimmerButton;
            so.FindProperty("openButton").objectReferenceValue = openButton;
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
            var text = AddText(textRt, "CLASSIFICA", 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
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
        // Filtri (decorativi)
        // ------------------------------------------------------------------

        private static void CreateFilterPills(RectTransform panelFrame, float yAbs)
        {
            string[] labels = { "Globale", "Amici", "Settimana" };
            float gap = 20f;
            float width = (RowX1 - RowX0 - gap * 2) / 3f;

            for (int i = 0; i < labels.Length; i++)
            {
                var pill = CreateUIObject("Filter_" + labels[i], panelFrame);
                SetTopLeft(pill, RowX0 - FrameX0 + i * (width + gap), yAbs - FrameY0, width, 64f);
                bool selected = i == 0;
                var bg = AddSpriteImage(pill, selected ? "btn_teal" : "btn_blue_mid", sliced: true);
                var textRt = CreateUIObject("Text", pill);
                StretchFill(textRt);
                AddText(textRt, labels[i], 22f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);
            }
        }

        // ------------------------------------------------------------------
        // Podio (1-2-3)
        // ------------------------------------------------------------------

        private static void CreatePodium(RectTransform panelFrame, float baselineYAbs)
        {
            // 3 colonne uguali affiancate da RowX0 (stessa tecnica di CreateRowOfCells in
            // PremiBuilder) invece di centrare ciascuna sul centro pannello: quella math
            // aveva fatto sovrapporre la colonna 1 (piu' larga/alta) con la 2 e la 3.
            const float gap = 20f;
            float colWidth = (RowX1 - RowX0 - gap * 2f) / 3f;

            float x2 = RowX0;
            float x1 = RowX0 + colWidth + gap;
            float x3 = RowX0 + (colWidth + gap) * 2f;

            CreatePodiumColumn(panelFrame, 2, "Giulia_R", "1.284", x2, baselineYAbs + 70f, 220f, colWidth);
            CreatePodiumColumn(panelFrame, 1, "Marco_88", "1.502", x1, baselineYAbs, 290f, colWidth);
            CreatePodiumColumn(panelFrame, 3, "Luca_02", "1.190", x3, baselineYAbs + 70f, 220f, colWidth);
        }

        private static void CreatePodiumColumn(RectTransform panelFrame, int rank, string name, string score,
            float xAbs, float yAbs, float height, float width)
        {
            var medalRt = CreateUIObject("Medal_" + rank, panelFrame);
            SetTopLeft(medalRt, xAbs - FrameX0 + width * 0.5f - 34f, yAbs - FrameY0 - 78f, 68f, 68f);
            var medalImg = AddSpriteImage(medalRt, rank == 1 ? "ic_trophy" : "ic_cup", preserveAspect: true);
            medalImg.color = rank == 1 ? _theme.Gold : (rank == 2 ? Hex("#C7D3DC") : Hex("#CC8A4B"));

            var box = CreateUIObject("PodiumBox_" + rank, panelFrame);
            SetTopLeft(box, xAbs - FrameX0, yAbs - FrameY0, width, height);

            var fill = box.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = RowFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", box);
            StretchFill(ringRt);
            var ring = AddSpriteImage(ringRt, "panel_ring_r24", sliced: true);
            ring.color = rank == 1 ? _theme.Gold : RingMuted;

            var rankRt = CreateUIObject("Rank", box);
            SetTopCenter(rankRt, 20f, width - 20f, 60f);
            AddText(rankRt, rank.ToString(), 40f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            var nameRt = CreateUIObject("Name", box);
            SetTopCenter(nameRt, 92f, width - 20f, 32f);
            AddText(nameRt, name, 22f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.Center);

            var scoreRt = CreateUIObject("Score", box);
            SetTopCenter(scoreRt, 128f, width - 20f, 30f);
            AddText(scoreRt, score, 22f, FontStyles.Normal, _theme.Gold, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // Lista posizioni 4+
        // ------------------------------------------------------------------

        private static void CreateListRow(RectTransform panelFrame, RankRow row, float yAbs)
        {
            var rowRt = CreateUIObject("Row_" + row.Rank, panelFrame);
            SetTopLeft(rowRt, RowX0 - FrameX0, yAbs - FrameY0, RowX1 - RowX0, 90f);

            var fill = rowRt.gameObject.AddComponent<Image>();
            fill.sprite = LoadSprite("panel_fill_r24");
            fill.type = Image.Type.Sliced;
            fill.color = RowFill;
            fill.raycastTarget = false;

            var ringRt = CreateUIObject("Ring", rowRt);
            StretchFill(ringRt);
            var ring = AddSpriteImage(ringRt, "panel_ring_r24", sliced: true);
            ring.color = row.IsYou ? RingHighlight : RingMuted;

            var rankRt = CreateUIObject("Rank", rowRt);
            SetAnchoredRect(rankRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(50f, 40f), new Vector2(26f, 0f));
            AddText(rankRt, row.Rank.ToString(), 24f, FontStyles.Bold, _theme.TextMuted, TextAlignmentOptions.MidlineLeft);

            var avatarRt = CreateUIObject("Avatar", rowRt);
            SetAnchoredRect(avatarRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(40f, 40f), new Vector2(86f, 0f));
            AddSpriteImage(avatarRt, "ic_person", preserveAspect: true);

            var nameRt = CreateUIObject("Name", rowRt);
            SetAnchoredRect(nameRt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(RowX1 - RowX0 - 320f, 0f), new Vector2(128f, 0f));
            AddText(nameRt, row.Name, 25f, FontStyles.Bold, _theme.Cream, TextAlignmentOptions.MidlineLeft);

            var scoreRt = CreateUIObject("Score", rowRt);
            SetAnchoredRect(scoreRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(160f, 40f), new Vector2(-28f, 0f));
            AddText(scoreRt, row.Score, 25f, FontStyles.Bold, _theme.Gold, TextAlignmentOptions.MidlineRight);
        }

        // ------------------------------------------------------------------
        // Footer
        // ------------------------------------------------------------------

        private static void CreateFooterNote(RectTransform panelFrame, float yAbs)
        {
            var rt = CreateUIObject("FooterNote", panelFrame);
            SetTopCenter(rt, yAbs - FrameY0, 800f, 30f);
            AddText(rt, "La classifica si aggiorna ogni ora", 19f, FontStyles.Normal, _theme.TextMuted, TextAlignmentOptions.Center);
        }

        // ------------------------------------------------------------------
        // Contesto / helper generici
        // ------------------------------------------------------------------

        private static bool LoadContext()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                Debug.LogError($"[ClassificaBuilder] UITheme non trovato in {ThemePath}.");
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
