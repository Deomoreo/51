using UnityEngine;
using Project51.Unity;

namespace Project51.Unity
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopBarSafeAreaFitter : MonoBehaviour
    {
        [Tooltip("If assigned, this content rect will be inset below the safe area (notch/dynamic island). If null, uses this RectTransform.")]
        [SerializeField] private RectTransform content;

        [Tooltip("Fixed bar height in UI units (reference resolution units).")]
        [SerializeField] private float barHeight = 140f;

        [Tooltip("Optional extra padding below the safe area, in UI units.")]
        [SerializeField] private float extraTopPadding = 0f;

        private RectTransform _rt;
        private Vector2Int _lastScreen;
        private Rect _lastSafe;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (content == null)
                content = _rt;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (_rt == null)
                _rt = GetComponent<RectTransform>();
            if (_rt == null)
                return;

            if (content == null)
                content = _rt;

            var screen = new Vector2Int(Screen.width, Screen.height);
            Rect safe = SafeAreaUtil.GetSafeAreaRenderingPixels();

            if (screen == _lastScreen && safe == _lastSafe)
                return;

            _lastScreen = screen;
            _lastSafe = safe;

            // Ensure the bar is pinned to the very top of its parent.
            _rt.anchorMin = new Vector2(0f, 1f);
            _rt.anchorMax = new Vector2(1f, 1f);
            _rt.pivot = new Vector2(0.5f, 1f);
            _rt.anchoredPosition = Vector2.zero;

            // Keep a fixed height.
            _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, barHeight));

            // Apply safe-area inset to content only (so background can still fill the bar).
            float safeTopPx = Mathf.Max(0f, Screen.height - safe.yMax);

            float canvasScale = 1f;
            var canvas = _rt.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.scaleFactor > 0f)
                canvasScale = canvas.scaleFactor;

            float insetUi = (safeTopPx / canvasScale) + extraTopPadding;

            // Shift content down by inset and reduce its height accordingly.
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 0.5f);

            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, -insetUi);
        }
    }
}
