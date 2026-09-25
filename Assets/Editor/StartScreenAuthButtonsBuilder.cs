using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Schermata iniziale (MainMenu, StartScreenV2): ACCEDI e REGISTRATI come il mockup
    /// 01_schermata_iniziale.png. Scritta ExtraBold crema con contorno scuro spesso, allineata a
    /// sinistra dopo l'icona (persona blu / spunta verde, dal foglio Icons), REGISTRATI resta ciano
    /// (UIV2DesignSystem non lo passa piu' al blu). Suggerimento sotto in grigio-azzurro tenue.
    /// Misure dal mockup (stesse coordinate della DesignArea 1080x1920). Rilanciabile.
    /// </summary>
    public static class StartScreenAuthButtonsBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string IconsPath = "Assets/UI/Sprites/DragonsHoard/sprites_unity/sprites_unity/Icons.png";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Poppins-ExtraBold SDF.asset";
        // Il nome deve finire con " Button": UIV2DesignSystem allora non tocca la misura.
        private const string MaterialPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Dark Button.mat";

        private const float CapHeight = 22f;   // come GIOCA COME OSPITE (a 21 le maiuscole misuravano 18,5)
        private const float IconX = 44f;      // centro icona dal bordo sinistro del pulsante
        private const float IconBox = 44f;
        private const float LabelLeft = 82f;  // inizio scritta dal bordo sinistro del pulsante (mockup: ACCEDI a x 234)

        private static readonly Color Face = new Color32(255, 250, 238, 255);
        private static readonly Color Outline = new Color32(8, 18, 34, 255);
        private static readonly Color HintColor = new Color32(150, 180, 205, 255);

        [MenuItem("Tools/UIV2/Build Start Screen Auth Buttons")]
        private static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            Transform design = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "DesignArea" && t.parent != null && t.parent.name == "StartScreenV2") design = t;
            if (design == null)
            {
                Debug.LogError("[StartScreen] StartScreenV2/DesignArea non trovato.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = LabelMaterial(font);
            // Scarto verticale: il centro visibile dello sprite non e' il centro del rettangolo.
            Style(design.Find("LoginButton"), "ic_person", -3f, font, material);
            Style(design.Find("RegisterButton"), "ic_check", -1f, font, material);

            var hint = design.Find("Hint");
            if (hint != null)
            {
                hint.GetComponent<TMP_Text>().color = HintColor;
                PrefabUtility.RecordPrefabInstancePropertyModifications(hint.GetComponent<TMP_Text>());
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[StartScreen] ACCEDI/REGISTRATI: icone, scritte ExtraBold con contorno, REGISTRATI ciano.");
        }

        private static void Style(Transform button, string iconName, float offsetY, TMP_FontAsset font, Material material)
        {
            if (button == null) { Debug.LogError("[StartScreen] pulsante mancante"); return; }

            var label = button.Find("Label").GetComponent<TextMeshProUGUI>();
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(LabelLeft, offsetY);
            rect.offsetMax = new Vector2(-20f, offsetY);
            label.font = font;
            label.fontSharedMaterial = material;
            label.fontStyle = FontStyles.Normal;
            label.color = Face;
            label.enableVertexGradient = false;
            label.characterSpacing = 0f;
            label.enableAutoSizing = false;
            label.fontSize = CapHeight / (font.faceInfo.capLine / font.faceInfo.pointSize);
            label.alignment = TextAlignmentOptions.MidlineLeft; // centra le maiuscole, non la riga
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);

            var iconTransform = button.Find("Icon");
            if (iconTransform == null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconTransform = go.transform;
                iconTransform.SetParent(button, false);
            }
            var icon = iconTransform.GetComponent<Image>();
            icon.sprite = IconSprite(iconName);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconBox, IconBox);
            iconRect.anchoredPosition = new Vector2(IconX, offsetY);
        }

        private static Sprite IconSprite(string name)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(IconsPath))
                if (asset is Sprite sprite && sprite.name == name) return sprite;
            Debug.LogError("[StartScreen] sprite " + name + " non trovato in Icons.png");
            return null;
        }

        // Come GoldButtonLabelStyle (0,6 + 0,3), contorno quasi nero come il mockup.
        private static Material LabelMaterial(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.6f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
