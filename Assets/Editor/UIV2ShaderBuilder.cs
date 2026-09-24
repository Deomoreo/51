using Project51.UIV2.Core;
using UnityEditor;
using UnityEngine;

namespace Project51.EditorTools
{
    public static class UIV2ShaderBuilder
    {
        [MenuItem("Tools/UIV2/Apply Shader Kit")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode before applying Shader Kit.");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UIV2" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    UIV2ShaderKit.Apply(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[K4] Shader Kit applied. Scene objects are completed at runtime.");
        }
    }
}
