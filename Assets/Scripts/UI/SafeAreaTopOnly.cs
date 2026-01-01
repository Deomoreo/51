using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Applica la Safe Area solo alla parte superiore dello schermo (notch/dynamic island).
    /// La parte inferiore rimane invariata, utile per bottom bar che devono stare al bordo.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaTopOnly : MonoBehaviour
    {
        [Tooltip("Se true, applica anche la safe area laterale (per telefoni con notch laterali in landscape)")]
        [SerializeField] private bool applySides = false;

        private RectTransform _rt;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_rt == null) return;

            Rect safe = Screen.safeArea;
            if (safe == _lastSafeArea) return;
            _lastSafeArea = safe;

            // Calcola i valori normalizzati
            float minX = applySides ? safe.xMin / Screen.width : 0f;
            float maxX = applySides ? safe.xMax / Screen.width : 1f;
            
            // Bottom resta a 0 (bordo inferiore dello schermo)
            float minY = 0f;
            
            // Top usa la safe area (per evitare notch/dynamic island)
            float maxY = safe.yMax / Screen.height;

            _rt.anchorMin = new Vector2(minX, minY);
            _rt.anchorMax = new Vector2(maxX, maxY);
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
