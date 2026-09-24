using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project51.EditorTools
{
    public static class UIV2ReducedGraphicsBuilder
    {
        [MenuItem("Tools/UIV2/Apply Reduced Graphics Settings")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            var original = SceneManager.GetActiveScene();
            foreach (string path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameScene.unity" })
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var home in root.GetComponentsInChildren<SettingsV2Integration>(true))
                    { home.EnsureReducedGraphicsToggle(); EditorUtility.SetDirty(home); }
                    foreach (var table in root.GetComponentsInChildren<InGameSettingsV2>(true))
                    { table.EnsureReducedGraphicsToggle(); EditorUtility.SetDirty(table); }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            Debug.Log("[I5] Shared reduced graphics settings applied to Home and table.");
        }
    }
}
