using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Titoli sui nastri teal di tutti i pannelli (Modalita', Mazzo, Impostazioni, stanze, risultati,
    /// roulette, emoticon...) nello stile dei mockup (panel_modalita_v2, panel_mazzo_v2, 05):
    /// Poppins ExtraBold bianco con contorno verde scuro spesso, centrato sulla fascia del nastro.
    /// Prima erano quattro stili diversi (Bold crema senza contorno visibile, contorni navy...) e
    /// i titoli "fratelli" del nastro erano posizionati a mano.
    ///
    /// Misure dai mockup (nastro alto 188): maiuscole di 28 px = corpo 40 ExtraBold; testo largo al
    /// massimo ~260 (IMPOSTAZIONI scende a ~36); centro del testo ~4 px sotto il centro del nastro.
    /// Tutto in proporzione all'altezza del nastro, cosi' vale anche per quelli piu' grandi.
    /// Il titolo prende lo stesso rettangolo del nastro: resta centrato se il nastro si sposta.
    /// Rilanciabile.
    /// </summary>
    public static class PanelTitlesCalibration
    {
        private const string FontDir = "Assets/TextMesh Pro/Resources/Fonts & Materials";
        private const string SourceMaterialPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Navy.mat";
        private const string RibbonMaterialPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Ribbon.mat";

        private const float ReferenceHeight = 188f;
        private const float ReferenceSize = 40f;
        private const float MaxWidthRatio = 0.52f;     // 270 su un nastro largo 520
        private const float CenterLiftRatio = -0.02f;  // 3,8 px sotto il centro su 188 (misurato a schermo contro il mockup)

        private static readonly Color TitleColor = new Color32(255, 250, 235, 255);
        private static readonly Color OutlineColor = new Color32(6, 48, 42, 255);

        private static readonly string[] ScenePaths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameScene.unity" };

        [MenuItem("Tools/UIV2/Calibrate Panel Titles")]
        private static void Calibrate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Poppins-ExtraBold SDF.asset");
            var material = RibbonMaterial();
            if (font == null || material == null)
            {
                Debug.LogError("[PanelTitles] Font o materiale mancanti.");
                return;
            }

            foreach (var path in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int count = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null || image.sprite.name != "ribbon_teal") continue;
                        var title = FindTitle(image.transform);
                        if (title == null) continue;
                        Apply(title, (RectTransform)image.transform, font, material);
                        count++;
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[PanelTitles] {path}: {count} titoli");
            }
        }

        /// <summary>Il titolo e' figlio del nastro (TitleRibbon/Text) o suo fratello "Title".</summary>
        private static TMP_Text FindTitle(Transform ribbon)
        {
            var child = ribbon.GetComponentInChildren<TMP_Text>(true);
            if (child != null) return child;
            var sibling = ribbon.parent != null ? ribbon.parent.Find("Title") : null;
            return sibling != null ? sibling.GetComponent<TMP_Text>() : null;
        }

        private static void Apply(TMP_Text title, RectTransform ribbon, TMP_FontAsset font, Material material)
        {
            Undo.RecordObject(title, "Calibrate Panel Titles");
            Undo.RecordObject(title.rectTransform, "Calibrate Panel Titles");

            float height = ribbon.rect.height;
            float width = ribbon.rect.width;
            float size = ReferenceSize * height / ReferenceHeight;
            float lift = CenterLiftRatio * height;
            float sideMargin = width * (1f - MaxWidthRatio) * 0.5f;

            var rt = title.rectTransform;
            if (rt.parent == ribbon)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(0f, lift);
                rt.offsetMax = new Vector2(0f, lift);
            }
            else
            {
                // Fratello: stesso rettangolo del nastro (il nastro sta prima, quindi il testo e' sopra).
                rt.anchorMin = ribbon.anchorMin;
                rt.anchorMax = ribbon.anchorMax;
                rt.pivot = ribbon.pivot;
                rt.sizeDelta = ribbon.sizeDelta;
                rt.anchoredPosition = ribbon.anchoredPosition + new Vector2(0f, lift);
                rt.localScale = ribbon.localScale;
                rt.localRotation = ribbon.localRotation;
                if (rt.GetSiblingIndex() < ribbon.GetSiblingIndex()) rt.SetSiblingIndex(ribbon.GetSiblingIndex());
            }

            title.font = font;
            title.fontSharedMaterial = material;
            title.fontStyle = FontStyles.Normal;
            title.color = TitleColor;
            title.alignment = TextAlignmentOptions.Center;
            title.enableWordWrapping = false;
            title.overflowMode = TextOverflowModes.Overflow;
            title.characterSpacing = 0f;
            title.margin = new Vector4(sideMargin, 0f, sideMargin, 0f);
            title.enableAutoSizing = true;
            title.fontSizeMax = size;
            title.fontSizeMin = size * 0.6f;
            title.fontSize = size;
            EditorUtility.SetDirty(title);
            EditorUtility.SetDirty(rt);
        }

        /// <summary>
        /// Contorno verde scuro del mockup (#06302A), circa 4-5 px a corpo 40. Oltre 0.6 + 0.3 si esce
        /// dal padding dell'atlas (8 su 80) e compaiono rettangoli attorno ai glifi.
        /// </summary>
        private static Material RibbonMaterial()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
            if (source == null) return null;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(RibbonMaterialPath);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, RibbonMaterialPath);
            }
            mat.CopyPropertiesFromMaterial(source);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.6f);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
