using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Ridimensiona lo sfondo di partita (SpriteRenderer) in modo che copra sempre
    /// l'intera area visibile della camera, indipendentemente da risoluzione,
    /// aspect ratio o dispositivo (telefono, tablet, orientamento portrait/landscape).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [ExecuteAlways]
    public class GameBackgroundFitter : MonoBehaviour
    {
        public enum FitMode
        {
            /// Copre tutta l'area visibile, ritagliando l'eccesso (mai lascia bordi vuoti).
            Cover,
            /// Sta tutta dentro l'area visibile, senza ritagli (puo' lasciare bordi vuoti).
            Contain
        }

        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Settings")]
        [SerializeField] private FitMode fitMode = FitMode.Cover;
        [SerializeField] private Vector2 extraMargin = Vector2.zero; // margine extra in world units, per lato

        private SpriteRenderer cachedRenderer;
        private float lastAspect = -1f;
        private float lastOrthoSize = -1f;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            Apply();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                Apply();
                return;
            }

            if (targetCamera == null)
            {
                return;
            }

            float aspect = targetCamera.aspect;
            float orthoSize = targetCamera.orthographicSize;
            if (!Mathf.Approximately(lastAspect, aspect) || !Mathf.Approximately(lastOrthoSize, orthoSize))
            {
                Apply();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureReferences();
            Apply();
        }
#endif

        public void Apply()
        {
            EnsureReferences();
            if (cachedRenderer == null || cachedRenderer.sprite == null || targetCamera == null)
            {
                return;
            }

            if (!targetCamera.orthographic)
            {
                Debug.LogWarning("[GameBackgroundFitter] Richiede una camera ortografica per un fit corretto.");
                return;
            }

            float visibleHeight = targetCamera.orthographicSize * 2f + extraMargin.y * 2f;
            float visibleWidth = visibleHeight * targetCamera.aspect + extraMargin.x * 2f;

            Vector2 spriteSize = cachedRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            float scaleX = visibleWidth / spriteSize.x;
            float scaleY = visibleHeight / spriteSize.y;

            float uniformScale = fitMode == FitMode.Cover
                ? Mathf.Max(scaleX, scaleY)
                : Mathf.Min(scaleX, scaleY);

            transform.localScale = new Vector3(uniformScale, uniformScale, transform.localScale.z);

            lastAspect = targetCamera.aspect;
            lastOrthoSize = targetCamera.orthographicSize;
        }

        private void EnsureReferences()
        {
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponent<SpriteRenderer>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindObjectOfType<Camera>();
                }
            }
        }
    }
}
