using Project51.UIV2.Components;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// 2.20 - icone rapide a destra nella Home (Premio, Classifica, Posta, Opzioni): niente piu'
    /// quadrato blu (sq_blue resta come area di tocco, alfa 0), una striscia scura morbida unica
    /// dietro tutta la colonna (2.23, prima un alone per icona), scritte ExtraBold con contorno e ombra come la
    /// barra in basso (stesso materiale Nav Label). Rilanciabile; va rilanciato dopo un rebuild
    /// completo della Home (il prefab HomeScreenV2 ha ancora lo stile vecchio).
    /// </summary>
    public static partial class UIV2FoundationBuilder
    {
        private const string QuickActionLabelFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Poppins-ExtraBold SDF.asset";
        private const string QuickActionLabelMaterialPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Nav Label.mat";
        private const string QuickActionHaloName = "Halo";
        private const string QuickActionStripName = "ShadowStrip";

        private static readonly Color QuickActionHalo = new Color(0.02f, 0.05f, 0.11f, 0.45f);
        // Margini della striscia oltre la colonna (130 x 586.5): le scritte sono larghe 170, l'ultima scende ~2 sotto.
        private const float QuickActionStripSide = 30f;
        private const float QuickActionStripTop = 40f;
        private const float QuickActionStripBottom = 60f;
        private static readonly Vector2 QuickActionIconBoxV2 = new Vector2(66f, 60f);
        private static readonly Color QuickActionLabelFace = new Color32(255, 250, 238, 255);
        private const float QuickActionLabelSize = 24f; // misura "body": il restyle runtime non la cambia

        [MenuItem("Tools/UIV2/Build Home Quick Actions")]
        private static void BuildHomeQuickActions()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var column = FindInScene(scene, "HomeScreenV2/QuickActionsColumn");

            var halo = EnsurePillGlowSprite();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(QuickActionLabelFontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(QuickActionLabelMaterialPath);
            int count = 0;
            foreach (Transform action in column)
            {
                if (action.GetComponent<UIV2QuickActionButton>() == null) continue;
                var visual = action.Find("Visual");
                var icon = visual != null ? visual.Find("Icon") as RectTransform : null;
                if (icon == null) { Debug.LogError("[HomeQuickActions] " + action.name + ": Visual/Icon mancante"); continue; }

                // Nascosto ma non spento: resta bersaglio del Button e area di tocco.
                var box = visual.GetComponent<Image>();
                box.color = new Color(1f, 1f, 1f, 0f);
                box.canvasRenderer.cullTransparentMesh = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);

                // 2.23: niente piu' alone per icona, c'e' la striscia unica sotto.
                var old = visual.Find(QuickActionHaloName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                icon.sizeDelta = QuickActionIconBoxV2;
                PrefabUtility.RecordPrefabInstancePropertyModifications(icon);

                var labelRect = action.Find("Label") as RectTransform;
                if (labelRect != null)
                {
                    var label = labelRect.GetComponent<TextMeshProUGUI>();
                    label.font = font;
                    label.fontSharedMaterial = material;
                    label.fontStyle = FontStyles.Normal;
                    label.fontSize = QuickActionLabelSize;
                    label.enableAutoSizing = false;
                    label.color = QuickActionLabelFace;
                    label.enableWordWrapping = false;
                    label.overflowMode = TextOverflowModes.Overflow;
                    label.alignment = TextAlignmentOptions.Center;
                    labelRect.sizeDelta = new Vector2(170f, 34f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(labelRect);
                }
                count++;
            }

            // Una sola pillola morbida dietro tutta la colonna (icone + scritte), fuori dal layout.
            var oldStrip = column.Find(QuickActionStripName);
            if (oldStrip != null) Object.DestroyImmediate(oldStrip.gameObject);
            var strip = CreateUIObject(QuickActionStripName, column);
            strip.SetSiblingIndex(0);
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            strip.anchorMin = Vector2.zero;
            strip.anchorMax = Vector2.one;
            strip.offsetMin = new Vector2(-QuickActionStripSide, -QuickActionStripBottom);
            strip.offsetMax = new Vector2(QuickActionStripSide, QuickActionStripTop);
            var stripImage = strip.gameObject.AddComponent<Image>();
            stripImage.sprite = halo;
            stripImage.type = Image.Type.Sliced;
            // Sfumatura lunga quanto mezza larghezza: in orizzontale e' il profilo del vecchio alone.
            stripImage.pixelsPerUnitMultiplier = GlowPillBorder / (strip.rect.width * 0.5f);
            stripImage.color = QuickActionHalo;
            stripImage.raycastTarget = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HomeQuickActions] " + count + " icone rapide: senza quadrato blu, striscia d'ombra unica, scritte con contorno.");
        }
    }
}
