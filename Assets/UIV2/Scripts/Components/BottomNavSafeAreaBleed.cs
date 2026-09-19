using UnityEngine;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// La barra in basso sta dentro la SafeArea: sui telefoni con la barra di sistema in basso
    /// (iPhone, molti Android a tutto schermo) sotto restava una fascia vuota di ~100 px e la barra
    /// sembrava "sollevata". Qui, dal vero Screen.safeArea:
    /// - lo sfondo (fill) scende fino al bordo dello schermo;
    /// - il contenuto scende di una parte dell'inset (sink), restando sopra l'indicatore di sistema.
    /// Senza inset (niente barra di sistema) non cambia nulla.
    /// </summary>
    public sealed class BottomNavSafeAreaBleed : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform fill;
        [SerializeField, Range(0f, 1f)] private float sink = 0.45f;

        private Rect lastSafeArea;
        private Vector2 lastScreen;
        private float lastScale;

        private void OnEnable() => Apply(force: true);

        private void LateUpdate() => Apply(force: false);

        private void Apply(bool force)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || content == null) return;
            var root = canvas.rootCanvas;
            float scale = root.scaleFactor;
            var safe = Screen.safeArea;
            var screen = new Vector2(Screen.width, Screen.height);
            if (!force && safe == lastSafeArea && screen == lastScreen && Mathf.Approximately(scale, lastScale)) return;
            lastSafeArea = safe;
            lastScreen = screen;
            lastScale = scale;

            // Inset in basso in unita' del canvas. In Editor il Game view puo' riportare un safeArea
            // piu' grande dello schermo: si limita a quanto ha senso (mai negativo, mai oltre il 10%).
            float insetPx = Mathf.Clamp(safe.yMin, 0f, Screen.height * 0.1f);
            float inset = scale > 0f ? insetPx / scale : 0f;

            float down = inset * sink;
            content.offsetMin = new Vector2(content.offsetMin.x, -down);
            content.offsetMax = new Vector2(content.offsetMax.x, -down);
            if (fill != null)
            {
                // Dal fondo del contenuto fino al bordo dello schermo, con un margine.
                fill.sizeDelta = new Vector2(fill.sizeDelta.x, inset - down + 40f);
            }
        }
    }
}
