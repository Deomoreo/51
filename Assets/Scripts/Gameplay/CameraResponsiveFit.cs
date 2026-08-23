using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Adatta la camera ortografica al rapporto d'aspetto del dispositivo,
    /// mantenendo sempre visibile un'area di gioco di riferimento.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class CameraResponsiveFit : MonoBehaviour
    {
        [Header("Reference Play Area")]
        [SerializeField] private float referenceWidth = 12f;
        [SerializeField] private float referenceHeight = 8.5f;
        [SerializeField, Range(0f, 0.25f)] private float safeMarginPercent = 0.06f;

        private Camera cachedCamera;
        private float lastAspect = -1f;

        public float VisibleHeight => cachedCamera != null ? cachedCamera.orthographicSize * 2f : 0f;
        public float VisibleWidth => cachedCamera != null ? VisibleHeight * cachedCamera.aspect : 0f;

        private void Awake()
        {
            EnsureCamera();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                Apply();
                return;
            }

            float aspect = GetAspect();
            if (!Mathf.Approximately(lastAspect, aspect))
            {
                Apply();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            referenceWidth = Mathf.Max(1f, referenceWidth);
            referenceHeight = Mathf.Max(1f, referenceHeight);
            Apply();
        }
#endif

        public void Apply()
        {
            EnsureCamera();
            if (cachedCamera == null)
            {
                return;
            }

            cachedCamera.orthographic = true;

            float aspect = GetAspect();
            if (aspect <= 0f)
            {
                return;
            }

            float marginMultiplier = 1f + safeMarginPercent;
            float referenceAspect = referenceWidth / referenceHeight;

            float targetSize = aspect < referenceAspect
                ? (referenceWidth * marginMultiplier) / (2f * aspect)
                : (referenceHeight * marginMultiplier) * 0.5f;

            cachedCamera.orthographicSize = targetSize;
            lastAspect = aspect;
        }

        private void EnsureCamera()
        {
            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }
        }

        private float GetAspect()
        {
            if (cachedCamera != null)
            {
                return cachedCamera.aspect;
            }

            return Screen.height > 0 ? (float)Screen.width / Screen.height : 0f;
        }
    }
}
