using UnityEngine;

namespace Project51.Unity.UI
{
    /// <summary>
    /// G1 - Sui telefoni piu' allungati del 9:16 l'area di design 1080x1920 resta centrata e avanza
    /// spazio sopra e sotto. Qui il posto del giocatore locale (banner, pulsanti Emoji/Accuso, bolla
    /// emoticon) scende di una parte dello spazio libero in basso, tolta la safe area: la mano e il
    /// mazzetto prese seguono il banner (CardViewManager), quindi scendono con lui.
    /// A 9:16 e sui tablet non c'e' spazio in piu' e non si muove niente.
    /// UI51 Fase 5: i topTargets (banner avversario) seguono invece il bordo alto della safe area, come la barra in alto
    /// (SafeAreaTopOffset): la distanza dalla pillola del punteggio resta quella del mockup su ogni telefono.
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
        [SerializeField] private RectTransform[] topTargets;

        private Vector2[] basePositions;
        private Vector2[] topBasePositions;
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
            if (topTargets == null) topTargets = new RectTransform[0];
            topBasePositions = new Vector2[topTargets.Length];
            for (int i = 0; i < topTargets.Length; i++)
            {
                if (topTargets[i] != null) topBasePositions[i] = topTargets[i].anchoredPosition;
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

            // Stessa lettura di SafeAreaTopOffset (limitata a 0: la Game view dell'Editor da' safe area fuori schermo).
            float topShift = ComputeTopShift(Screen.width, Screen.height, Mathf.Max(0f, Screen.height - Screen.safeArea.yMax));
            for (int i = 0; i < topTargets.Length; i++)
            {
                if (topTargets[i] != null) topTargets[i].anchoredPosition = topBasePositions[i] + new Vector2(0f, topShift);
            }
        }

        /// <summary>
        /// Salita in pixel di design che porta il bordo alto dell'area 1080x1920 sotto la safe area: positiva sui telefoni
        /// lunghi, negativa (scende) dove l'area riempie lo schermo e c'e' una barra di stato. Senza limiti: i banner in alto
        /// restano alla stessa distanza dalla barra in alto.
        /// </summary>
        public static float ComputeTopShift(float screenWidth, float screenHeight, float safeTopInsetPx)
        {
            if (screenWidth <= 0f || screenHeight <= 0f) return 0f;
            float designPerPixel = Mathf.Max(DesignWidth / screenWidth, DesignHeight / screenHeight); // come PortraitCanvasMatch
            return (screenHeight * designPerPixel - DesignHeight) * 0.5f - safeTopInsetPx * designPerPixel;
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
