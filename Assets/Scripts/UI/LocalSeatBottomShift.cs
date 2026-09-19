using UnityEngine;

namespace Project51.Unity.UI
{
    /// <summary>
    /// G1 - Sui telefoni piu' allungati del 9:16 l'area di design 1080x1920 resta centrata e avanza
    /// spazio sopra e sotto. Qui il posto del giocatore locale (banner, pulsanti Emoji/Accuso, bolla
    /// emoticon) scende di una parte dello spazio libero in basso, tolta la safe area: la mano e il
    /// mazzetto prese seguono il banner (CardViewManager), quindi scendono con lui.
    /// A 9:16 e sui tablet non c'e' spazio in piu' e non si muove niente.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class LocalSeatBottomShift : MonoBehaviour
    {
        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;

        [SerializeField] private RectTransform[] targets;
        [Tooltip("Quota dello spazio libero sotto l'area di design (safe area esclusa) usata per scendere.")]
        [SerializeField, Range(0f, 1f)] private float fractionOfFreeSpace = 0.5f;
        [SerializeField] private float maxShift = 120f;

        private Vector2[] basePositions;
        private Vector2Int lastScreen;
        private Rect lastSafeArea;

        public float CurrentShift { get; private set; }

        private void Awake()
        {
            if (targets == null) targets = new RectTransform[0];
            basePositions = new Vector2[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null) basePositions[i] = targets[i].anchoredPosition;
            }
            Apply();
        }

        private void Update()
        {
            if (lastScreen.x != Screen.width || lastScreen.y != Screen.height || lastSafeArea != Screen.safeArea) Apply();
        }

        private void Apply()
        {
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
            CurrentShift = ComputeShift(Screen.width, Screen.height, Screen.safeArea.yMin, fractionOfFreeSpace, maxShift);

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null) targets[i].anchoredPosition = basePositions[i] + new Vector2(0f, -CurrentShift);
            }
        }

        /// <summary>Discesa in pixel di design; stessa scala di PortraitCanvasMatch.</summary>
        public static float ComputeShift(float screenWidth, float screenHeight, float safeBottomPx, float fraction, float max)
        {
            if (screenWidth <= 0f || screenHeight <= 0f) return 0f;
            float aspect = screenWidth / screenHeight;
            if (aspect >= DesignWidth / DesignHeight) return 0f; // tablet o 9:16: si blocca l'altezza, niente spazio extra

            float designPerPixel = DesignWidth / screenWidth;
            float canvasHeight = screenHeight * designPerPixel;
            float freeBelow = (canvasHeight - DesignHeight) * 0.5f - safeBottomPx * designPerPixel;
            return Mathf.Clamp(freeBelow * fraction, 0f, max);
        }
    }
}
