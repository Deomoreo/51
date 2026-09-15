using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Apre/chiude ClassificaOverlay (stesso pattern fade-only degli altri pannelli).
    /// Nessun sistema di classifica reale nel progetto: 3 filtri Globale/Amici/Settimana
    /// sono un SelectableToggleGroup puramente visivo (nessun dato diverso dietro, gia'
    /// dichiarato onesto per lo stesso motivo di altri filtri stub in questo progetto).
    /// </summary>
    public class PanelClassificaController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button openButton;
        [SerializeField] private float animDuration = 0.22f;

        private Sequence _sequence;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
            if (openButton != null) openButton.onClick.AddListener(Open);
        }

        public void Open()
        {
            if (panelRoot == null) return;

            panelRoot.SetActive(true);

            _sequence?.Kill();
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            _sequence = DOTween.Sequence();
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(1f, animDuration));
            _sequence.SetUpdate(true);
        }

        public void Close()
        {
            if (panelRoot == null) return;

            _sequence?.Kill();

            _sequence = DOTween.Sequence();
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(0f, animDuration));
            _sequence.SetUpdate(true).OnComplete(() => panelRoot.SetActive(false));
        }
    }
}
