using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Pannello generico che sale dal basso (stile Clash Royale).
    /// Usato come base per DeckSelector, ModalitySelector, ecc.
    /// </summary>
    public class SlideUpPanelUI : MonoBehaviour
    {
        [Header("Panel Structure")]
        [Tooltip("Root dell'intero sistema (overlay + container). Deve restare attivo.")]
        [SerializeField] protected GameObject panelRoot;
        [Tooltip("Container che si anima (slide up/down).")]
        [SerializeField] protected RectTransform panelContainer;

        [Header("Animation")]
        [SerializeField] protected float openDuration = 0.28f;
        [SerializeField] protected float closeDuration = 0.22f;
        [SerializeField] protected Ease ease = Ease.OutCubic;
        [SerializeField] protected float hiddenY = -1200f;
        [SerializeField] protected float shownY = 0f;

        [Header("Close on Tap Outside")]
        [Tooltip("Se true, un tap fuori dal pannello lo chiude.")]
        [SerializeField] protected bool closeOnTapOutside = true;
        [Tooltip("Canvas per calcolare i bounds (solo se non ScreenSpaceOverlay).")]
        [SerializeField] protected Canvas rootCanvas;

        [Header("Input Blocking")]
        [Tooltip("CanvasGroup per bloccare i click dietro quando aperto.")]
        [SerializeField] protected CanvasGroup inputBlocker;

        [Header("Background Dim")]
        [Tooltip("Overlay scuro uniforme da mettere tra 'pagina sotto' e il pannello. Deve avere un Image (anche nero) + CanvasGroup. Non deve bloccare raycast.")]
        [SerializeField] private CanvasGroup backgroundDimOverlay;
        [Tooltip("Opacità dell'overlay quando il pannello è aperto.")]
        [SerializeField, Range(0f, 1f)] private float backgroundDimAlpha = 0.55f;
        [Tooltip("Durata fade-in/out overlay.")]
        [SerializeField] private float backgroundDimFadeDuration = 0.18f;

        public event Action<bool> OnVisibilityChanged;

        protected bool _isOpen;
        protected Tweener _tween;

        public bool IsOpen => _isOpen;

        protected virtual void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (panelContainer != null)
            {
                panelContainer.gameObject.SetActive(false);
                panelContainer.anchoredPosition = new Vector2(panelContainer.anchoredPosition.x, hiddenY);
            }

            SetInputBlocking(false);

            if (backgroundDimOverlay != null)
            {
                backgroundDimOverlay.alpha = 0f;
                backgroundDimOverlay.interactable = false;
                backgroundDimOverlay.blocksRaycasts = false;
                backgroundDimOverlay.gameObject.SetActive(false);
            }
        }

        protected virtual void OnDestroy()
        {
            _tween?.Kill(false);
        }

        protected virtual void Update()
        {
            if (!closeOnTapOutside || !_isOpen || panelContainer == null)
                return;

            if (Input.GetMouseButtonDown(0))
                TryCloseIfTapOutside(Input.mousePosition);
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                TryCloseIfTapOutside(Input.GetTouch(0).position);
        }

        protected virtual void TryCloseIfTapOutside(Vector2 screenPos)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Verifica se il tap è su un elemento DENTRO il pannello
                var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, results);

                foreach (var result in results)
                {
                    if (result.gameObject.transform.IsChildOf(panelContainer))
                        return; // tap dentro il pannello, non chiudere
                }
            }

            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = rootCanvas.worldCamera;

            bool inside = RectTransformUtility.RectangleContainsScreenPoint(panelContainer, screenPos, cam);
            if (!inside)
                Close();
        }

        protected virtual void SetInputBlocking(bool value)
        {
            if (inputBlocker == null) return;

            inputBlocker.alpha = value ? 1f : 0f;
            inputBlocker.interactable = value;
            inputBlocker.blocksRaycasts = value;
        }

        protected virtual void SetBackgroundDim(bool value)
        {
            if (backgroundDimOverlay == null) return;

            backgroundDimOverlay.DOKill(false);
            backgroundDimOverlay.gameObject.SetActive(true);
            backgroundDimOverlay.interactable = false;
            backgroundDimOverlay.blocksRaycasts = false;

            float target = value ? backgroundDimAlpha : 0f;

            float a = backgroundDimOverlay.alpha;
            DOTween.To(() => a, v =>
                {
                    a = v;
                    backgroundDimOverlay.alpha = v;
                },
                target,
                backgroundDimFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(backgroundDimOverlay)
                .OnComplete(() =>
                {
                    if (!value)
                        backgroundDimOverlay.gameObject.SetActive(false);
                });
        }

        public virtual void Open()
        {
            if (_isOpen || panelContainer == null) return;

            _isOpen = true;
            panelContainer.gameObject.SetActive(true);
            _tween?.Kill(false);

            SetInputBlocking(true);
            SetBackgroundDim(true);
            OnOpening();

            panelContainer.anchoredPosition = new Vector2(panelContainer.anchoredPosition.x, hiddenY);
            _tween = DOTween.To(
                    () => panelContainer.anchoredPosition,
                    v => panelContainer.anchoredPosition = v,
                    new Vector2(panelContainer.anchoredPosition.x, shownY),
                    openDuration)
                .SetEase(ease)
                .SetTarget(panelContainer)
                .OnComplete(() =>
                {
                    OnOpened();
                    OnVisibilityChanged?.Invoke(true);
                });
        }

        public virtual void Close()
        {
            if (!_isOpen || panelContainer == null) return;

            _isOpen = false;
            _tween?.Kill(false);

            OnClosing();
            SetBackgroundDim(false);

            _tween = DOTween.To(
                    () => panelContainer.anchoredPosition,
                    v => panelContainer.anchoredPosition = v,
                    new Vector2(panelContainer.anchoredPosition.x, hiddenY),
                    closeDuration)
                .SetEase(ease)
                .SetTarget(panelContainer)
                .OnComplete(() =>
                {
                    SetInputBlocking(false);
                    panelContainer.gameObject.SetActive(false);
                    OnClosed();
                    OnVisibilityChanged?.Invoke(false);
                });
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        // Hook per sottoclassi
        protected virtual void OnOpening() { }
        protected virtual void OnOpened() { }
        protected virtual void OnClosing() { }
        protected virtual void OnClosed() { }
    }
}
