using Project51.UIV2.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project51.EditorTools
{
    public static class UIV2FeedbackBuilder
    {
        [MenuItem("Tools/UIV2/Apply Feedback Kit")]
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
                    { home.EnsureMusicToggle(); home.EnsureVibrationToggle(); EditorUtility.SetDirty(home); }
                    // Anche le etichette spente: nella scena salvata non resta un numero vecchio.
                    foreach (var label in root.GetComponentsInChildren<Project51.UIV2.Components.VersionLabelV2>(true))
                    { label.GetComponent<TMPro.TMP_Text>().text = label.Prefix + Application.version; EditorUtility.SetDirty(label.gameObject); }
                    foreach (var table in root.GetComponentsInChildren<InGameSettingsV2>(true))
                    { table.EnsureVibrationToggle(); EditorUtility.SetDirty(table); }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            Debug.Log("[K6] Feedback settings applied; effects and input hooks install at runtime.");
        }
    }
}
