using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// C4 - Poppins su tutta la UI. Ogni testo TMP ancora in LiberationSans passa a Poppins:
    /// - stile Bold -> Poppins-Bold (tolto il grassetto finto, che su Poppins ingrosserebbe due volte);
    /// - tutto il resto -> Poppins Medium, il font predefinito.
    /// I materiali preset (contorni, ombra) vengono ricreati sull'atlas del nuovo font con le stesse
    /// proprieta', cosi' un testo con contorno navy resta con contorno navy.
    /// Tocca MainMenu, GameScene e i prefab di Prefabs/UIV2/Resources. Rilanciabile: senza testi
    /// in LiberationSans non cambia niente.
    /// </summary>
    public static class PoppinsMigration
    {
        private const string FontDir = "Assets/TextMesh Pro/Resources/Fonts & Materials";
        private const string MaterialDir = "Assets/UIV2/Art/Fonts";
        private const string OldFontName = "LiberationSans SDF";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GameScene.unity",
        };

        private static readonly string[] PrefabRoots = { "Assets/Prefabs", "Assets/UIV2", "Assets/Resources" };

        private static TMP_FontAsset oldFont, medium, bold;
        private static readonly Dictionary<(Material, TMP_FontAsset), Material> materialCache =
            new Dictionary<(Material, TMP_FontAsset), Material>();

        [MenuItem("Tools/UIV2/Apply Poppins Everywhere")]
        private static void ApplyPoppinsEverywhere()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            oldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/{OldFontName}.asset");
            medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/poppins-medium SDF.asset");
            bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Poppins-Bold SDF.asset");
            if (oldFont == null || medium == null || bold == null)
            {
                Debug.LogError("[Poppins] Font asset non trovati: niente da fare.");
                return;
            }
            materialCache.Clear();

            int total = 0;
            var prefabPaths = AssetDatabase.FindAssets("t:Prefab", PrefabRoots)
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToList();

            // Piu' passate: un testo dentro un prefab annidato si sistema nel suo prefab sorgente,
            // la passata successiva raccoglie eventuali override rimasti in LiberationSans.
            for (int pass = 0; pass < 4; pass++)
            {
                int changedThisPass = 0;
                foreach (var path in prefabPaths)
                {
                    var root = PrefabUtility.LoadPrefabContents(path);
                    int n = MigrateTexts(root.GetComponentsInChildren<TMP_Text>(true));
                    if (n > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        Debug.Log($"[Poppins] {path}: {n} testi");
                    }
                    PrefabUtility.UnloadPrefabContents(root);
                    changedThisPass += n;
                }
                total += changedThisPass;
                if (changedThisPass == 0) break;
            }

            foreach (var scenePath in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var texts = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)).ToArray();
                int n = MigrateTexts(texts);
                if (n > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                Debug.Log($"[Poppins] {scenePath}: {n} testi");
                total += n;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Poppins] Fatto: {total} testi passati a Poppins.");
        }

        private static int MigrateTexts(IEnumerable<TMP_Text> texts)
        {
            int changed = 0;
            foreach (var text in texts)
            {
                if (text.font != oldFont) continue;

                // Testo di un prefab annidato ancora da migrare nel suo prefab: lo sistema la sua passata.
                var source = PrefabUtility.GetCorrespondingObjectFromSource(text);
                if (source != null && source.font == oldFont && !IsFontOverridden(text)) continue;

                bool isBold = (text.fontStyle & FontStyles.Bold) != 0;
                var newFont = isBold ? bold : medium;
                var oldMaterial = text.fontSharedMaterial;

                Undo.RecordObject(text, "Apply Poppins");
                text.font = newFont;
                if (isBold) text.fontStyle &= ~FontStyles.Bold;
                text.fontSharedMaterial = MapMaterial(oldMaterial, newFont);
                EditorUtility.SetDirty(text);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                changed++;
            }
            return changed;
        }

        private static bool IsFontOverridden(TMP_Text text)
        {
            var mods = PrefabUtility.GetPropertyModifications(text);
            return mods != null && mods.Any(m => m.target != null && m.propertyPath == "m_fontAsset");
        }

        /// <summary>
        /// Stesso preset sul nuovo atlas: "LiberationSans SDF Outline Navy" -> "poppins-medium SDF Outline Navy".
        /// Se il preset esiste gia' (creato dai builder UIV2) si riusa quello.
        /// </summary>
        private static Material MapMaterial(Material oldMaterial, TMP_FontAsset newFont)
        {
            if (oldMaterial == null || oldMaterial == oldFont.material) return newFont.material;
            if (materialCache.TryGetValue((oldMaterial, newFont), out var cached)) return cached;

            // Un materiale incorporato nella scena (istanza creata dal codice) non ha un nome utile:
            // si chiamano tutti "... Material (Instance)" pur avendo contorni diversi. Il nome
            // del preset si ricava allora dal contorno, altrimenti istanze diverse finirebbero fuse.
            string suffix;
            if (!AssetDatabase.Contains(oldMaterial))
                suffix = oldMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth)
                    ? $" Outline {ColorUtility.ToHtmlStringRGB(oldMaterial.GetColor(ShaderUtilities.ID_OutlineColor))} {oldMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}"
                    : " Instance";
            else if (oldMaterial.name.StartsWith(OldFontName))
                suffix = oldMaterial.name.Substring(OldFontName.Length);
            else
                suffix = " " + oldMaterial.name;
            string path = $"{MaterialDir}/{newFont.name}{suffix}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(oldMaterial);
                var fontMat = newFont.material;
                mat.SetTexture(ShaderUtilities.ID_MainTex, fontMat.GetTexture(ShaderUtilities.ID_MainTex));
                mat.SetFloat(ShaderUtilities.ID_TextureWidth, fontMat.GetFloat(ShaderUtilities.ID_TextureWidth));
                mat.SetFloat(ShaderUtilities.ID_TextureHeight, fontMat.GetFloat(ShaderUtilities.ID_TextureHeight));
                mat.SetFloat(ShaderUtilities.ID_GradientScale, fontMat.GetFloat(ShaderUtilities.ID_GradientScale));
                mat.SetFloat(ShaderUtilities.ID_WeightNormal, fontMat.GetFloat(ShaderUtilities.ID_WeightNormal));
                mat.SetFloat(ShaderUtilities.ID_WeightBold, fontMat.GetFloat(ShaderUtilities.ID_WeightBold));
                AssetDatabase.CreateAsset(mat, path);
                Debug.Log($"[Poppins] Nuovo materiale {path}");
            }
            materialCache[(oldMaterial, newFont)] = mat;
            return mat;
        }
    }
}
