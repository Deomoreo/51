using Project51.UIV2.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project51.UIV2.Animations
{
    /// <summary>One pass per loaded scene; prefab buttons are already configured by the builder.</summary>
    public static class UIV2MotionInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInitialScene() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects()) Apply(root);
        }

        public static bool ShouldAnimate(UnityEngine.UI.Button button)
        {
            if (button == null || button.transition == UnityEngine.UI.Selectable.Transition.Animation) return false;
            string name = button.name.ToLowerInvariant();
            if (name.Contains("dimmer") || name.Contains("dimbackground") || name == "dim" || name.Contains("backdrop") || name.Contains("overlay")) return false;
            if (button.GetComponent<DismissOnBackdrop>() != null || button.GetComponent("PrimaryButtonUI") != null) return false;
            return true;
        }

        public static int Apply(GameObject root)
        {
            int added = 0;
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                string lower = button.name.ToLowerInvariant();
                if (!lower.Contains("dimmer") && !lower.Contains("backdrop") && lower != "dim" &&
                    !lower.Contains("overlay") && !lower.Contains("dimbackground") &&
                    button.GetComponent<DismissOnBackdrop>() == null && button.GetComponent<UIV2HapticButton>() == null)
                    button.gameObject.AddComponent<UIV2HapticButton>();
                if (!ShouldAnimate(button) || button.GetComponent<UIV2ButtonFeedback>() != null) continue;
                button.gameObject.AddComponent<UIV2ButtonFeedback>();
                added++;
            }
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.parent == null) continue;
                string parent = rect.parent.name;
                bool auth = rect.name == "Design" && (parent == "LoginPanel" || parent == "RegisterPanel" || parent == "AccountPanel");
                bool page = (rect.name == "DesignArea" && parent == "StartScreenV2") || (rect.name == "Design" && parent == "NewsV2");
                if ((!auth && !page) || rect.GetComponent<UIV2PageEntrance>() != null) continue;
                rect.gameObject.AddComponent<UIV2PageEntrance>();
                added++;
            }
            return added;
        }
    }
}
