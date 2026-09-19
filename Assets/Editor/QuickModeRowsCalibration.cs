using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Pannello Modalita' della Home (QuickModeV2) tarato sui pixel di panel_modalita_v2.png
    /// (confronto 1:1 nel Game view 1080x1920):
    /// - righe: titolo 28 ExtraBold con contorno navy spesso, sottotitolo 19 (la copia di FrontendFlowBuilder
    ///   li aveva a 42/28), blocco centrato nel pulsante, icona 54, sottotitoli descrittivi del mockup;
    /// - titoli di sezione 20 px piu' su, corpo 23, con la linea sottile fino al bordo destro;
    /// - "Difficolta'" grigio chiaro e non in grassetto; etichette delle pillole ExtraBold con contorno;
    /// - barra di scorrimento oro a destra del viewport (maniglia vera, collegata allo ScrollRect).
    /// Rilanciabile: riscrive valori assoluti e ricrea solo i propri oggetti (Line, Scrollbar).
    /// </summary>
    public static class QuickModeRowsCalibration
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontDir = "Assets/TextMesh Pro/Resources/Fonts & Materials";
        private const string OutlineSourcePath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Navy.mat";
        private const string OutlinePath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Navy Thick.mat";

        private static readonly (string row, string subtitle)[] ModeRows =
        {
            ("Row_1v1", "Testa a testa online"),
            ("Row_2v2", "A squadre, online"),
            ("Row_1v3", "Tutti contro tutti"),
            ("Row_1v1Bot", "Offline contro il computer"),
            ("Row_2v2Bot", "Tu + 1 bot contro 2 bot"),
            ("Row_1v3Bot", "Tutti contro tutti, offline"),
        };
        private static readonly string[] ActionRows = { "Row_Crea", "Row_Entra" };
        private static readonly (string name, float top)[] Headers =
        {
            ("Header_PartitaVeloce", -4f), ("Header_Allenamento", -500f), ("Header_StanzaPrivata", -1144f),
        };
        private static readonly string[] Pills = { "Pill_Facile", "Pill_Medio", "Pill_Difficile" };

        private const float TitleSize = 28f;
        private const float SubSize = 19f;
        private const float TitleTop = -31f;
        private const float SubTop = -68f;
        private const float TextLeft = 105f;
        private const float IconSize = 54f;
        private const float HeaderSize = 23f;
        private const float HeaderLeft = 20f;
        private const float LineGap = 24f;
        private const float LineRight = 886f;   // x 964 nel mockup, contenuto a x 78
        private const float DifficultyTop = -986f;

        private static readonly Color SubColor = new Color32(186, 205, 228, 255);
        private static readonly Color LineColor = new Color32(70, 102, 142, 255);
        private static readonly Color TrackColor = new Color32(16, 28, 46, 255);
        private static readonly Color HandleColor = new Color32(241, 185, 77, 255);   // lo sprite arrotondato scurisce del 4%: a schermo 232,178,74 come il mockup

        [MenuItem("Tools/UIV2/Calibrate Quick Mode Panel")]
        private static void Calibrate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            Transform frame = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.transform.Find("ModalHost/QuickModeV2/PanelFrame");
                if (found != null) frame = found;
            }
            var scroll = frame != null ? frame.Find("ScrollView")?.GetComponent<ScrollRect>() : null;
            if (scroll == null)
            {
                Debug.LogError("[QuickMode] QuickModeV2/PanelFrame/ScrollView non trovato in MainMenu.");
                return;
            }

            var extraBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Poppins-ExtraBold SDF.asset");
            var medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/poppins-medium SDF.asset");
            var outline = ThickOutline();
            if (extraBold == null || medium == null || outline == null)
            {
                Debug.LogError("[QuickMode] Font o materiale di contorno mancanti.");
                return;
            }

            var content = scroll.content;
            foreach (var (rowName, subtitle) in ModeRows)
            {
                var row = content.Find(rowName);
                if (row == null) { Debug.LogWarning($"[QuickMode] {rowName} mancante"); continue; }
                CalibrateRow(row, extraBold, outline);
                var sub = Text(row, "SubText");
                if (sub != null) sub.text = subtitle;
            }
            foreach (var rowName in ActionRows)
            {
                var row = content.Find(rowName);
                if (row != null) CalibrateRow(row, extraBold, outline);
            }

            foreach (var (name, top) in Headers)
            {
                var header = content.Find(name) as RectTransform;
                if (header == null) continue;
                var text = header.GetComponent<TMP_Text>();
                text.fontSize = HeaderSize;
                header.anchoredPosition = new Vector2(HeaderLeft, top);
                BuildHeaderLine(header, text);
                EditorUtility.SetDirty(text);
            }

            var difficulty = content.Find("DifficoltaLabel") as RectTransform;
            if (difficulty != null)
            {
                var text = difficulty.GetComponent<TMP_Text>();
                text.font = medium;
                text.fontSharedMaterial = medium.material;
                text.fontStyle = FontStyles.Normal;
                text.fontSize = 22f;
                text.color = SubColor;
                difficulty.anchoredPosition = new Vector2(HeaderLeft, DifficultyTop);
                EditorUtility.SetDirty(text);
            }

            foreach (var pillName in Pills)
            {
                var label = content.Find(pillName)?.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;
                label.font = extraBold;
                label.fontSharedMaterial = outline;
                label.fontStyle = FontStyles.Normal;
                label.color = Color.white;
                EditorUtility.SetDirty(label);
            }

            BuildScrollbar(frame as RectTransform, scroll);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[QuickMode] Pannello Modalita' tarato sul mockup.");
        }

        private static void CalibrateRow(Transform row, TMP_FontAsset titleFont, Material outline)
        {
            var title = Text(row, "TitleText");
            if (title != null)
            {
                title.font = titleFont;
                title.fontSharedMaterial = outline;
                title.fontStyle = FontStyles.Normal;
                title.color = new Color32(255, 250, 238, 255);
                Place(title, TitleSize, TitleTop);
            }
            var sub = Text(row, "SubText");
            if (sub != null)
            {
                sub.color = SubColor;
                Place(sub, SubSize, SubTop);
            }
            var icon = row.Find("Icon") as RectTransform;
            if (icon != null) icon.sizeDelta = new Vector2(IconSize, IconSize);
        }

        /// <summary>
        /// Contorno navy di circa 3 px a corpo 28 come nel mockup. Il contorno TMP cresce per meta'
        /// dentro la lettera, quindi il carattere si ingrossa (dilate) per restare pieno; oltre
        /// 0.55 + 0.35 si esce dal padding dell'atlas (8 su 80) e compaiono rettangoli attorno ai glifi.
        /// </summary>
        private static Material ThickOutline()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(OutlineSourcePath);
            if (source == null) return null;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OutlinePath);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, OutlinePath);
            }
            mat.CopyPropertiesFromMaterial(source);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.55f);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.35f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static TMP_Text Text(Transform row, string child)
        {
            var t = row.Find(child);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private static void Place(TMP_Text text, float size, float top)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            var rt = text.rectTransform;
            float right = rt.anchoredPosition.x + rt.sizeDelta.x;
            rt.anchoredPosition = new Vector2(TextLeft, top);
            rt.sizeDelta = new Vector2(right - TextLeft, rt.sizeDelta.y);
            EditorUtility.SetDirty(text);
        }

        /// <summary>Linea del mockup: parte dopo il testo e arriva a x 964, centrata sul testo.</summary>
        private static void BuildHeaderLine(RectTransform header, TMP_Text text)
        {
            var old = header.Find("Line");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            float textWidth = text.GetPreferredValues(text.text).x;
            float left = textWidth + LineGap;
            float width = LineRight - header.anchoredPosition.x - left;
            if (width <= 0f) return;

            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(header, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(left, 0f);
            rt.sizeDelta = new Vector2(width, 2f);
            var image = go.GetComponent<Image>();
            image.color = LineColor;
            image.raycastTarget = false;
        }

        /// <summary>
        /// Barra del mockup: 10 px a x 1001-1010, alta quanto il viewport; binario scuro e maniglia oro
        /// con le estremita' arrotondate. Collegata allo ScrollRect, quindi segue lo scorrimento.
        /// </summary>
        private static void BuildScrollbar(RectTransform frame, ScrollRect scroll)
        {
            var old = frame.Find("Scrollbar");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var view = (RectTransform)scroll.transform;

            var bar = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var barRt = (RectTransform)bar.transform;
            barRt.SetParent(frame, false);
            barRt.anchorMin = barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 1f);
            barRt.anchoredPosition = new Vector2(955f, view.anchoredPosition.y);
            barRt.sizeDelta = new Vector2(12f, view.sizeDelta.y);   // bordo antialias compreso: pieno di 10 px
            var track = bar.GetComponent<Image>();
            track.sprite = rounded;
            track.type = Image.Type.Sliced;
            track.color = TrackColor;

            var area = new GameObject("SlidingArea", typeof(RectTransform));
            var areaRt = (RectTransform)area.transform;
            areaRt.SetParent(barRt, false);
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = areaRt.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRt = (RectTransform)handle.transform;
            handleRt.SetParent(areaRt, false);
            handleRt.offsetMin = handleRt.offsetMax = Vector2.zero;
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = rounded;
            handleImage.type = Image.Type.Sliced;
            handleImage.color = HandleColor;

            var scrollbar = bar.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var colors = scrollbar.colors;
            colors.highlightedColor = colors.pressedColor = colors.selectedColor = Color.white;
            scrollbar.colors = colors;

            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 0f;
            EditorUtility.SetDirty(scroll);
        }
    }
}
