using UnityEngine;

namespace Project51.UI
{
    /// <summary>
    /// Fits only the BOTTOM of a RectTransform to the safe area (useful for bottom bars).
    /// Leaves top (and optionally sides) untouched.
    /// 
    /// USAGE:
    /// Put this on a "BottomArea" parent and keep your BottomBar child at anchoredPosition.y = 0.
    /// The child's bottom edge will automatically sit on the safe area line.
    /// 
    /// For "background full screen but icons above safe area" pattern:
    /// - Put SafeAreaBottomOnly on BottomArea (parent)
    /// - BottomNavBar is child with anchorMin.y=0, anchoredPosition.y=0
    /// - Put BottomInsetFitter on the internal content container (for padding)
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaBottomOnly : MonoBehaviour
    {
        [Tooltip("If true, also apply left/right safe area.")]
        [SerializeField] private bool applySides = false;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private RectTransform _rt;
        private Rect _lastSafe;
        private Vector2Int _lastScreen;
        private bool _applied;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _applied = false;
            ApplyIfNeeded();
        }

        private void LateUpdate()
        {
            // Check only in LateUpdate to avoid conflicts with layout systems
            ApplyIfNeeded();
        }

        private void ApplyIfNeeded()
        {
            if (_rt == null)
                _rt = GetComponent<RectTransform>();

            var currentScreen = new Vector2Int(Screen.width, Screen.height);
            Rect safe = SafeAreaUtil.GetSafeAreaRenderingPixels();


            // Skip if nothing changed (and we already applied at least once)
            if (_applied && currentScreen == _lastScreen && safe == _lastSafe)
                return;

            _lastScreen = currentScreen;
            _lastSafe = safe;
            _applied = true;

            ApplyAnchors(safe);
        }

        private void ApplyAnchors(Rect safe)
        {
            if (Screen.height <= 0) return;

            float bottomNorm = safe.yMin / Screen.height;

            Vector2 anchorMin = new Vector2(
                applySides ? (safe.xMin / Screen.width) : 0f,
                bottomNorm
            );
            Vector2 anchorMax = new Vector2(
                applySides ? (safe.xMax / Screen.width) : 1f,
                1f
            );

            // Only write if different (avoid triggering layout rebuild)
            if (_rt.anchorMin != anchorMin || _rt.anchorMax != anchorMax)
            {
                _rt.anchorMin = anchorMin;
                _rt.anchorMax = anchorMax;

                if (debugLog)
                    Debug.Log($"[SafeAreaBottomOnly] <b>{gameObject.name}</b> Applied: safeBottomPx={safe.yMin:F0} bottomNorm={bottomNorm:F4} screen={Screen.width}x{Screen.height}", this);
            }

            // Always zero offsets to prevent drift
            if (_rt.offsetMin != Vector2.zero || _rt.offsetMax != Vector2.zero)
            {
                _rt.offsetMin = Vector2.zero;
                _rt.offsetMax = Vector2.zero;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                _applied = false;
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) ApplyIfNeeded();
                };
            }
        }
#endif

        [ContextMenu("Force Apply")]
        public void ForceApply()
        {
            _applied = false;
            ApplyIfNeeded();
        }
    }
}
