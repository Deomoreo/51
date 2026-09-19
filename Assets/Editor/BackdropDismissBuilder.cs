using Project51.UIV2.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.EditorTools
{
    /// <summary>
    /// Toccare fuori da un pannello lo chiude, come gia' facevano Privacy/Termini, Impostazioni e
    /// Elimina account e Impostazioni in partita (InGameSettingsV2.Backdrop). Mette DismissOnBackdrop sul velo di ogni pannello che si chiude con la X.
    /// Esclusi di proposito: ricerca partita e sale d'attesa (un tocco accidentale farebbe uscire
    /// dalla stanza o annullerebbe la ricerca), risultati e roulette (chiedono una scelta).
    /// Rilanciabile.
    /// </summary>
    public static class BackdropDismissBuilder
    {
        private static readonly (string scene, string backdrop, string close)[] Targets =
        {
            ("Assets/Scenes/MainMenu.unity", "ModalHost/QuickModeV2/DimBackground", "ModalHost/QuickModeV2/PanelFrame/CloseButton"),
            ("Assets/Scenes/MainMenu.unity", "ModalHost/QuickDeckV2/DimBackground", "ModalHost/QuickDeckV2/PanelFrame/CloseButton"),
            ("Assets/Scenes/MainMenu.unity", "OnlineFlowV2/CreateRoom/Dim", "OnlineFlowV2/CreateRoom/Design/Close"),
            ("Assets/Scenes/MainMenu.unity", "OnlineFlowV2/JoinRoom/Dim", "OnlineFlowV2/JoinRoom/Design/Close"),
            ("Assets/Scenes/GameScene.unity", "GamePresentationV2/Emoticons/Dim", "GamePresentationV2/Emoticons/Design/Frame/Close"),
            ("Assets/Scenes/GameScene.unity", "GameCanvas/PanelPersonalizzaOverlay/DimBackground", "GameCanvas/PanelPersonalizzaOverlay/PanelFrame/CloseButton"),
        };

        [MenuItem("Tools/UIV2/Build Backdrop Dismiss")]
        private static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string open = null;
            UnityEngine.SceneManagement.Scene scene = default;
            foreach (var (scenePath, backdropPath, closePath) in Targets)
            {
                if (open != scenePath)
                {
                    if (open != null) Save(scene);
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    open = scenePath;
                }
                var backdrop = Find(scene, backdropPath);
                var close = Find(scene, closePath);
                if (backdrop == null || close == null || close.GetComponent<Button>() == null)
                {
                    Debug.LogWarning($"[BackdropDismiss] {backdropPath} o {closePath} non trovato");
                    continue;
                }
                var dismiss = backdrop.GetComponent<DismissOnBackdrop>();
                if (dismiss == null) dismiss = backdrop.gameObject.AddComponent<DismissOnBackdrop>();
                dismiss.CloseButton = close.GetComponent<Button>();
                backdrop.GetComponent<Graphic>().raycastTarget = true;
                EditorUtility.SetDirty(dismiss);
            }
            if (open != null) Save(scene);
            Debug.Log("[BackdropDismiss] Chiusura toccando fuori collegata.");
        }

        private static void Save(UnityEngine.SceneManagement.Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Transform Find(UnityEngine.SceneManagement.Scene scene, string path)
        {
            int slash = path.IndexOf('/');
            string rootName = path.Substring(0, slash);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName) return root.transform.Find(path.Substring(slash + 1));
                var nested = root.transform.Find(path);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
