using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project51.EditorTools
{
    /// <summary>
    /// Cleanup una tantum: un run precedente di un tool ora eliminato (PanelPersonalizzaBuilder,
    /// prima che forzasse l'apertura di HomeScreen.unity) aveva costruito "PanelPersonalizzaOverlay"
    /// nella scena sbagliata (GameScene invece di HomeScreen). Quel pannello e' stato rifatto
    /// come pagina CARTE (DeckPage, vedi DeckPageBuilder) dentro HomeScreen: questo oggetto
    /// orfano in GameScene non serve a nulla e va rimosso. Idempotente: se non lo trova non fa nulla.
    /// </summary>
    public static class GameSceneStrayCleanup
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        [MenuItem("Tools/Dragons Hoard/Cleanup Stray PanelPersonalizzaOverlay in GameScene")]
        private static void Cleanup()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var roots = scene.GetRootGameObjects();
            GameObject found = null;
            foreach (var root in roots)
            {
                var t = root.transform;
                var match = FindDeepChild(t, "PanelPersonalizzaOverlay");
                if (match != null)
                {
                    found = match.gameObject;
                    break;
                }
            }

            if (found == null)
            {
                Debug.Log("[GameSceneStrayCleanup] Nessun PanelPersonalizzaOverlay orfano trovato in GameScene: nulla da fare.");
                return;
            }

            Object.DestroyImmediate(found);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameSceneStrayCleanup] PanelPersonalizzaOverlay orfano rimosso da GameScene. Salvato.");
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
