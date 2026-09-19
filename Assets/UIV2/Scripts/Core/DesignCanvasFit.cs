using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>Keeps the portrait design inside the device safe area without stretching its art.</summary>
    [ExecuteAlways]
    public sealed class DesignCanvasFit : MonoBehaviour
    {
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
            rect.sizeDelta = new Vector2(1080, 1920);
            rect.anchoredPosition = (min + max) * .5f - parent.rect.center;
            rect.localScale = Vector3.one * Mathf.Min((max.x - min.x) / 1080, (max.y - min.y) / 1920);
        }
    }
}
