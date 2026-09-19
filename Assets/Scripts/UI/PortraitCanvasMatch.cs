using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// G1 - Canvas del tavolo con la stessa scala della camera (CameraResponsiveFit): sui telefoni
    /// piu' stretti del 9:16 si blocca la larghezza (il canvas e' sempre largo 1080 e cresce in altezza),
    /// sui tablet si blocca l'altezza (1920, cresce in larghezza). A 9:16 le due regole coincidono.
    /// Prima era match 0,5: su un 19,5:9 il canvas era largo solo ~980 e banner e pulsante Accuso
    /// uscivano a destra dello schermo.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class PortraitCanvasMatch : MonoBehaviour
    {
        private CanvasScaler scaler;
        private float lastAspect = -1f;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (!Mathf.Approximately(lastAspect, Aspect())) Apply();
        }

        private void Apply()
        {
            float aspect = Aspect();
            if (scaler == null || aspect <= 0f) return;
            var reference = scaler.referenceResolution;
            float designAspect = reference.x / Mathf.Max(1f, reference.y);
            scaler.matchWidthOrHeight = aspect < designAspect ? 0f : 1f;
            lastAspect = aspect;
        }

        private static float Aspect() => Screen.height > 0 ? (float)Screen.width / Screen.height : 0f;
    }
}
