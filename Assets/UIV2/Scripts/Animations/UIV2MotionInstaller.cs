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
            foreach (var root in scene.GetRootGameObjects())
            {
                Apply(root);
                foreach (var events in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                    events.pixelDragThreshold = DragThreshold(Screen.dpi);
            }
        }

        /// <summary>
        /// Soglia di trascinamento in pixel per circa 1,6 mm di dito (10 dp, come le liste di Android e iOS). I 10 pixel fissi di
        /// Unity sui telefoni veri sono mezzo millimetro: un tocco che rotola appena diventava un trascinamento e il pulsante
        /// sotto non partiva (visto con il vassoio delle prese, S8). DPI sconosciuti (0) o bassi: i 10 pixel di sempre.
        /// </summary>
        public static int DragThreshold(float dpi) => Mathf.Max(10, Mathf.RoundToInt(dpi / 160f * 10f));

        public static bool ShouldAnimate(UnityEngine.UI.Button button)
        {
            if (button == null || button.transition == UnityEngine.UI.Selectable.Transition.Animation) return false;
            if (button.GetComponent<Project51.UI51.UI51Press>() != null) return false; // UI51 ha la sua pressione
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
