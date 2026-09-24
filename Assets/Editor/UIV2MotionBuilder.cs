using System;
using Project51.UIV2.Animations;
using UnityEditor;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>Repeatable prefab configuration. Scene completion is handled on scene load,
    /// avoiding serialization of ExecuteAlways camera/layout changes.</summary>
    public static class UIV2MotionBuilder
    {
        public static bool ShouldAnimate(UnityEngine.UI.Button button) => UIV2MotionInstaller.ShouldAnimate(button);
        public static int Apply(GameObject root) => UIV2MotionInstaller.Apply(root);

        [MenuItem("Tools/UIV2/Apply Motion Kit")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the motion kit.");
            int added = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UIV2" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int changed = Apply(root);
                    if (changed > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); added += changed; }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Debug.Log($"[UIV2 Motion] Added {added} prefab motion components. Scene completion runs once on scene load.");
        }
    }
}