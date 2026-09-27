using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>Keeps the portrait design inside the device safe area without stretching its art.</summary>
    [ExecuteAlways]
    public sealed class DesignCanvasFit : MonoBehaviour
    {
        /// <summary>Design size in reference units (UIV2 1080x1920, UI51 390x844).</summary>
        public Vector2 Reference = new Vector2(1080, 1920);
        /// <summary>When true the rect grows to fill the whole safe area on its long axis instead of letterboxing.</summary>
        public bool Fill;

        private void LateUpdate()
        {
            var rect = (RectTransform)transform;
            var parent = (RectTransform)rect.parent;
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            // Device Simulator can report safe-area coordinates at native resolution.
            var safeArea = Project51.Unity.SafeAreaUtil.GetSafeAreaRenderingPixels();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safeArea.min, camera, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safeArea.max, camera, out var max);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            var size = max - min;
            var scale = Mathf.Min(size.x / Reference.x, size.y / Reference.y);
            rect.sizeDelta = Fill && scale > 0f ? size / scale : Reference;
            rect.anchoredPosition = (min + max) * .5f - parent.rect.center;
            rect.localScale = Vector3.one * scale;
        }
    }
}
