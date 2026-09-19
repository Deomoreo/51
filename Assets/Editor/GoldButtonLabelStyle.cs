using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// C9 - scritte dei pulsanti oro principali (GIOCA della Home, GIOCA COME OSPITE della schermata
    /// iniziale) come nei mockup home_B2 e 01: Poppins ExtraBold color panna con contorno bruno spesso.
    /// Prima erano Bold crema senza contorno (o con un filo blu) e sull'oro si leggevano poco.
    /// Corpo calcolato dall'altezza delle maiuscole misurata nei mockup (38 e 22 px). Rilanciabile.
    /// </summary>
    public static class GoldButtonLabelStyle
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Poppins-ExtraBold SDF.asset";
        private const string MaterialPath = "Assets/UIV2/Art/Fonts/Poppins-ExtraBold SDF Outline Gold Button.mat";

        private static readonly Color FaceColor = new Color32(255, 252, 242, 255);   // #FFFCF2 dal mockup
        private static readonly Color OutlineColor = new Color32(148, 84, 8, 255);   // #945408 dal mockup

        // (percorso della scritta, altezza delle maiuscole nel mockup in px)
        private static readonly (string path, float capHeight)[] Labels =
        {
            ("HomeScreenV2/BottomControlsGroup/PlayButtonSlot/UIV2_PrimaryGoldButton/Label", 38f),
            ("StartScreenV2/DesignArea/GuestButton/Label", 22f),
        };

        [MenuItem("Tools/UIV2/Style Gold Button Labels")]
        private static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = GoldButtonMaterial(font);
            float capPerPoint = font.faceInfo.capLine / font.faceInfo.pointSize;

            int styled = 0;
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (text.gameObject.scene != scene) continue;
                foreach (var (path, capHeight) in Labels)
                {
                    if (!PathOf(text.transform).EndsWith(path)) continue;
                    Undo.RecordObject(text, "Style Gold Button Labels");
                    text.font = font;
                    text.fontSharedMaterial = material;
                    text.fontStyle = FontStyles.Normal;
                    text.color = FaceColor;
                    text.enableVertexGradient = false; // il prefab aveva un gradiente che tingeva la faccia di crema
                    text.characterSpacing = 0f;
                    text.enableAutoSizing = false;
                    text.fontSize = capHeight / capPerPoint;
                    EditorUtility.SetDirty(text);
                    styled++;
                    Debug.Log($"[GoldButtonLabels] {path}: ExtraBold {text.fontSize:0.0} con contorno bruno");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GoldButtonLabels] {styled} scritte aggiornate.");
        }

        /// <summary>
        /// Contorno di circa 5 px a corpo 53 e 4 a corpo 31, come nei mockup. 0,6 + 0,3 e' il massimo gia'
        /// provato sui nastri (Calibrate Panel Titles): oltre si esce dal padding dell'atlas (8 su 80) e
        /// compaiono rettangoli attorno ai glifi.
        /// </summary>
        private static Material GoldButtonMaterial(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.6f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
