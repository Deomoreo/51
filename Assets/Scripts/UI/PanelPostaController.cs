using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Apre/chiude PostaOverlay (stesso pattern fade-only degli altri pannelli). Nessuna
    /// casella di posta reale nel progetto: lista statica di mock, "RISCUOTI TUTTO" e' uno
    /// stub (Debug.Log) invece di accreditare ricompense che non esistono in nessun sistema
    /// economico reale ancora.
    /// </summary>
    public class PanelPostaController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private Button openButton;
        [SerializeField] private Button claimAllButton;
        [SerializeField] private float animDuration = 0.22f;

        private Sequence _sequence;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (claimAllButton != null) claimAllButton.onClick.AddListener(() =>
                Debug.Log("[PanelPostaController] Riscuoti tutto: nessuna casella di posta/sistema economico reale collegato ancora."));
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
