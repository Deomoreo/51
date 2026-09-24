using Project51.UIV2.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public static class UIV2ShaderKit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initial() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects()) Apply(root);
        }

        public static void Apply(GameObject root)
        {
            if (root == null) return;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var art = button.targetGraphic as Image;
                if (art == null || art.sprite == null || !art.sprite.name.StartsWith("btn_gold_")) continue;
                SetSurface(art, UIV2SurfaceEffect.Surface.Sweep);
            }
            // Preserve existing glow objects so selection/animation references remain valid.
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (!image.name.EndsWith("SoftGlow") || image.transform.parent == null) continue;
                string targetName = image.name.Substring(0, image.name.Length - "SoftGlow".Length);
                var target = image.transform.parent.Find(targetName);
                var source = target != null ? target.GetComponent<Image>() : null;
                if (source == null || source.sprite == null || source == image) continue;
                var shape = image.GetComponent<UIV2ShapeGlow>() ?? image.gameObject.AddComponent<UIV2ShapeGlow>();
                shape.Configure(source);
                SetSurface(image, UIV2SurfaceEffect.Surface.Silhouette);
            }
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.name == "BackgroundLayer" && image.GetComponentInParent<Canvas>() != null)
                    SetSurface(image, UIV2SurfaceEffect.Surface.Background);
        }

        public static UIV2SurfaceEffect SetSurface(Graphic graphic, UIV2SurfaceEffect.Surface mode)
        {
            var effect = graphic.GetComponent<UIV2SurfaceEffect>() ?? graphic.gameObject.AddComponent<UIV2SurfaceEffect>();
            effect.Configure(mode);
            return effect;
        }
    }
}
