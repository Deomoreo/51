using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// G1 - Il tavolo vive in un'area di design fissa: il mockup 1080x1920 = DesignWorldSize
    /// (5,625 x 10 unita' mondo, cioe' la vecchia camera a orthographicSize 5 su 9:16), centrata
    /// sullo schermo. La camera mostra sempre tutta l'area di design con la stessa scala dei Canvas
    /// del tavolo (PortraitCanvasMatch: larghezza sui telefoni piu' stretti del 9:16, altezza sui
    /// tablet), quindi un pixel di design e' sempre lo stesso pezzo di mondo e banner, pulsanti e
    /// carte restano allineati su ogni proporzione. Lo spazio in piu' resta attorno (sfondo).
    ///
    /// Prima la camera era ferma a orthographicSize 5: su un telefono 19,5:9 il tavolo era il 18% piu'
    /// grande dello schermo e la UI (match 0,5) scivolava rispetto alle carte.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class CameraResponsiveFit : MonoBehaviour
    {
        public const float DesignWidthPx = 1080f;
        public const float DesignHeightPx = 1920f;
        public const float DesignWorldHeight = 10f;
        public static readonly Vector2 DesignWorldSize =
            new Vector2(DesignWorldHeight * DesignWidthPx / DesignHeightPx, DesignWorldHeight);

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
            if (!Mathf.Approximately(lastAspect, GetAspect())) Apply();
        }

        public void Apply()
        {
            EnsureCamera();
            if (cachedCamera == null) return;

            float aspect = GetAspect();
            if (aspect <= 0f) return;

            cachedCamera.orthographic = true;
            float designAspect = DesignWidthPx / DesignHeightPx;
            // Piu' stretto del design: si blocca la larghezza e si vede piu' tavolo in altezza.
            // Piu' largo (tablet): si blocca l'altezza, come prima.
            cachedCamera.orthographicSize = aspect < designAspect
                ? DesignWorldHeight * 0.5f * designAspect / aspect
                : DesignWorldHeight * 0.5f;
            lastAspect = aspect;
        }

        private void EnsureCamera()
        {
            if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
        }

        private float GetAspect()
        {
            if (cachedCamera != null) return cachedCamera.aspect;
            return Screen.height > 0 ? (float)Screen.width / Screen.height : 0f;
        }
    }
}
