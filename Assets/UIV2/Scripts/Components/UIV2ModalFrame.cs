using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Project51.UIV2.Core;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Frame condiviso da tutti i modal globali (Ranking/Friends/Mail/Premi/...): pannello
    /// blu, bordo oro, ribbon verde in alto col titolo, close button, area contenuto vuota
    /// che ogni modal specifico riempie. Implementa IUIV2Modal cosi' UIV2ModalHost puo'
    /// gestirlo senza conoscerne il contenuto.
    /// </summary>
    public class UIV2ModalFrame : MonoBehaviour, IUIV2Modal
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private TMP_Text ribbonTitle;
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private float animDuration = 0.2f;

        public RectTransform ContentArea => contentArea;
        public bool IsOpen { get; private set; }

        private Sequence _sequence;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
        }

        public void SetTitle(string title)
        {
            if (ribbonTitle != null) ribbonTitle.text = title;
        }

        public void Open()
        {
            if (root == null) return;
            root.SetActive(true);
            IsOpen = true;

            _sequence?.Kill();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            _sequence = DOTween.Sequence().SetUpdate(true);
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(1f, animDuration));
        }

        public void Close()
        {
            if (root == null) return;
            IsOpen = false;

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);
            if (canvasGroup != null) _sequence.Join(canvasGroup.DOFade(0f, animDuration));
            _sequence.OnComplete(() => root.SetActive(false));
        }
    }
}
