using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Fa sparire gradualmente un indicatore "c'e' altro sotto" (es. FadeBottom di un
    /// ScrollRect) quando lo scroll arriva in fondo, invece di restare fisso a coprire
    /// gli ultimi elementi (bug: copriva i bottoni Crea/Entra in fondo al Panel Modalita
    /// perche' la striscia era sempre alla stessa opacita' indipendentemente dallo scroll).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScrollFadeIndicator : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [Tooltip("Se true, sfuma vicino alla CIMA dello scroll invece che al fondo (per un indicatore in alto).")]
        [SerializeField] private bool fadeNearTop;
        [Tooltip("Frazione (0..1) dello scroll verticale, a partire dall'estremo scelto, in cui l'indicatore sfuma da 0 a piena opacita'.")]
        [SerializeField, Range(0.01f, 1f)] private float fadeRange = 0.2f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollChanged);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            }
        }

        private void OnScrollChanged(Vector2 _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (scrollRect == null || _canvasGroup == null) return;

            // verticalNormalizedPosition: 1 = in cima, 0 = in fondo.
            float v = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            float distanceFromEdge = fadeNearTop ? (1f - v) : v;
            _canvasGroup.alpha = Mathf.Clamp01(distanceFromEdge / fadeRange);
        }
    }
}
