using UnityEngine;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Sposta in basso il RectTransform su cui e' attaccato di quanto serve per non finire
    /// sotto il notch/status bar del dispositivo (Screen.safeArea), invece di un offset fisso
    /// tarato su un solo device (es. iPhone 12) che sarebbe sbagliato su tutti gli altri.
    /// Pensato per barre ancorate al bordo superiore del canvas (anchorMin/Max Y = 1).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaTopOffset : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Rect lastSafeArea;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            parentCanvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                Apply();
            }
        }

        private void Apply()
        {
            lastSafeArea = Screen.safeArea;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            float topInsetPixels = Mathf.Max(0f, Screen.height - Screen.safeArea.yMax);
            float scaleFactor = parentCanvas != null && parentCanvas.scaleFactor > 0f ? parentCanvas.scaleFactor : 1f;
            float topInsetCanvasUnits = topInsetPixels / scaleFactor;

            var pos = rectTransform.anchoredPosition;
            pos.y = -topInsetCanvasUnits;
            rectTransform.anchoredPosition = pos;
        }
    }
}
