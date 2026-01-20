using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Pannello generico che sale dal basso (stile Clash Royale).
    /// Supporta: slide up/down animation, tap outside to close, swipe down to close.
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

        [Header("Close Animation Feel")]
        [SerializeField] private Ease closeEase = Ease.InCubic;

        [Header("Input Guard")]
        [Tooltip("Ignore mouse/touch for a short time right after closing to avoid re-open/close ping-pong when using overlay button.")]
        [SerializeField] private float inputIgnoreAfterClose = 0.12f;

        [Header("Close on Tap Outside")]
        [Tooltip("Se true, un tap fuori dal pannello lo chiude.")]
        [SerializeField] protected bool closeOnTapOutside = true;
        [Tooltip("Canvas per calcolare i bounds (solo se non ScreenSpaceOverlay).")]
        [SerializeField] protected Canvas rootCanvas;

        [Header("Swipe Down to Close")]
        [Tooltip("Se true, swipe verso il basso chiude il pannello.")]
        [SerializeField] protected bool swipeDownToClose = true;
        [Tooltip("Area dedicata su cui trascinare per muovere/chiudere il pannello (es. HeaderModePanel).")]
        [SerializeField] private RectTransform dragHandle;
        [SerializeField] protected float swipeThreshold = 100f;
        [SerializeField] protected float swipeVelocityThreshold = 500f;

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

        [Header("Overlay Close (Optional)")]
        [Tooltip("Optional button in the overlay/header area that closes this panel. If set, tap-outside logic is disabled.")]
        [SerializeField] private Button overlayCloseButton;

        [Header("Diagnostics")]
        [SerializeField] private bool logScrollRectIssues = true;
        [SerializeField] private bool logPointerRaycastOnDrag = true;
        [SerializeField] private bool heartbeatWhileOpen = false;
        [SerializeField] private float heartbeatInterval = 1.0f;

        [Header("Debug")]
        [SerializeField] protected bool enableDebugLogs = true;

        [Header("Scroll Debug (Optional)")]
        [SerializeField] private ScrollRect debugInnerScrollRect;
        [SerializeField] private Scrollbar debugScrollbar;

        [Header("Scroll Behavior")]
        [SerializeField] private bool preserveInnerScrollOnOpen = true;
        [Tooltip("If true, when opening the panel the inner ScrollRect is forced back to the top.")]
        [SerializeField] private bool resetInnerScrollToTopOnOpen = true;
        [Tooltip("If true, the scroll is animated back to top when opening (instead of snapping).")]
        [SerializeField] private bool animateResetScrollToTopOnOpen = true;
        [SerializeField] private float resetScrollToTopDuration = 0.18f;
        [SerializeField] private Ease resetScrollToTopEase = Ease.OutCubic;

        [Header("Swipe/Scroll Handoff")]
        [Tooltip("If true, when the inner ScrollRect is at the very top and the user drags down, the gesture will move the panel instead of the scroll content.")]
        [SerializeField] private bool allowPanelDragWhenInnerAtTop = true;
        [SerializeField] private float innerTopEpsilon = 0.001f;

        [Header("Swipe Down Feel")]
        [SerializeField] private float panelOvershootBelowHidden = 200f;

        private Vector2 _lastInnerNorm;
        private float _lastScrollbarValue;
        private float _nextHeartbeat;

        private float _ignoreInputUntil;

        public event Action<bool> OnVisibilityChanged;

        protected bool _isOpen;
        protected Tweener _tween;

        // Drag tracking (only from dragHandle)
        private bool _isDragging;
        private float _dragVelocity;
        private float _lastDragY;
        private float _lastDragTime;

        // Pointer tracking for swipe/scroll handoff
        private bool _pointerDown;
        private Vector2 _pointerDownPos;

        // Helper to log raycast target under a screen position
        private string GetTopRaycastName(Vector2 screenPos)
        {
            if (EventSystem.current == null)
                return "<no EventSystem>";

            var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            return results.Count > 0 && results[0].gameObject != null ? results[0].gameObject.name : "<no hit>";
        }

        private ScrollRect _panelScrollRect;
        private Vector2 _lastPanelScrollNorm;

        public bool IsOpen => _isOpen;

        // Nome identificativo per debug
        private string PanelName => panelContainer != null ? panelContainer.name : gameObject.name;

        protected virtual void Awake()
        {
            if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Awake - panelRoot: {panelRoot}, panelContainer: {panelContainer}");

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

            if (overlayCloseButton != null)
            {
                overlayCloseButton.onClick.RemoveListener(Close);
                overlayCloseButton.onClick.AddListener(Close);
            }

            _panelScrollRect = panelContainer != null ? panelContainer.GetComponent<ScrollRect>() : null;
            if (_panelScrollRect != null)
            {
                _lastPanelScrollNorm = _panelScrollRect.normalizedPosition;

                if (enableDebugLogs)
                    Debug.LogWarning($"[SlideUpPanelUI:{PanelName}] panelContainer has a ScrollRect. This usually causes snap-back when combined with panel sliding. Remove it; keep only the inner Scroll View ScrollRect.");
            }

            // Auto-wire debug references if not set
            if (debugInnerScrollRect == null && panelContainer != null)
                debugInnerScrollRect = panelContainer.GetComponentInChildren<ScrollRect>(true);

            if (debugScrollbar == null && debugInnerScrollRect != null)
                debugScrollbar = debugInnerScrollRect.verticalScrollbar;

            if (debugInnerScrollRect != null)
                _lastInnerNorm = debugInnerScrollRect.normalizedPosition;

            if (debugScrollbar != null)
                _lastScrollbarValue = debugScrollbar.value;
        }

        protected virtual void OnDestroy()
        {
            _tween?.Kill(false);
        }

        protected virtual void Update()
        {
            // Heartbeat to confirm Update runs
            if (heartbeatWhileOpen && enableDebugLogs && _isOpen && Time.unscaledTime >= _nextHeartbeat)
            {
                _nextHeartbeat = Time.unscaledTime + heartbeatInterval;
                Debug.Log($"[SlideUpPanelUI:{PanelName}] Heartbeat - isOpen={_isOpen}, panelY={(panelContainer != null ? panelContainer.anchoredPosition.y : 0f)}, inner={(debugInnerScrollRect != null ? debugInnerScrollRect.normalizedPosition.ToString() : "<null>")}, bar={(debugScrollbar != null ? debugScrollbar.value.ToString("F3") : "<null>")}");
            }

            // If we are in the ignore window, skip tap-outside checks to avoid immediate close after open.
            bool inputGuardActive = Time.unscaledTime < _ignoreInputUntil;

            // If an explicit overlay close button exists, don't run tap-outside detection.
            // But still allow swipe/drag handle.
            bool allowTapOutside = overlayCloseButton == null;

            if (!allowTapOutside)
            {
                // Do nothing here; still process drag below.
            }
            else
            {
                if (!inputGuardActive && closeOnTapOutside && _isOpen && panelContainer != null && !_isDragging)
                {
                    if (!panelContainer.gameObject.activeInHierarchy)
                    {
                        if (enableDebugLogs) Debug.LogWarning($"[SlideUpPanelUI:{PanelName}] _isOpen is true but panelContainer is not active! Fixing...");
                        _isOpen = false;
                        return;
                    }

                    if (Input.GetMouseButtonDown(0))
                        TryCloseIfTapOutside(Input.mousePosition);
                    else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                        TryCloseIfTapOutside(Input.GetTouch(0).position);
                }
            }

            // Drag handle processing (mouse)
            if (swipeDownToClose && _isOpen)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _pointerDown = true;
                    _pointerDownPos = Input.mousePosition;
                    TryBeginHandleDrag(Input.mousePosition);
                }
                else if (Input.GetMouseButton(0))
                {
                    // If we didn't start dragging yet, allow takeover from inner scroll when user is pulling down from top
                    if (!_isDragging && _pointerDown)
                        TryTakeOverDragFromInnerScroll(Input.mousePosition);

                    UpdateHandleDrag(Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _pointerDown = false;
                    EndHandleDrag();
                }

                // Touch
                if (Input.touchCount > 0)
                {
                    var t = Input.GetTouch(0);
                    if (t.phase == TouchPhase.Began)
                    {
                        _pointerDown = true;
                        _pointerDownPos = t.position;
                        TryBeginHandleDrag(t.position);
                    }
                    else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        if (!_isDragging && _pointerDown)
                            TryTakeOverDragFromInnerScroll(t.position);

                        UpdateHandleDrag(t.position);
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        _pointerDown = false;
                        EndHandleDrag();
                    }
                }
            }

            if (_panelScrollRect != null && logScrollRectIssues)
            {
                var n = _panelScrollRect.normalizedPosition;
                if ((n - _lastPanelScrollNorm).sqrMagnitude > 0.0001f)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[SlideUpPanelUI:{PanelName}] panelContainer ScrollRect normalizedPosition changed: {_lastPanelScrollNorm} -> {n} (velocity={_panelScrollRect.velocity})");
                    _lastPanelScrollNorm = n;
                }
            }

            if (enableDebugLogs)
            {
                if (debugInnerScrollRect != null)
                {
                    var n = debugInnerScrollRect.normalizedPosition;
                    if ((n - _lastInnerNorm).sqrMagnitude > 0.0001f)
                    {
                        Debug.Log($"[SlideUpPanelUI:{PanelName}] INNER ScrollRect normalizedPosition: {_lastInnerNorm} -> {n}, velocity={debugInnerScrollRect.velocity}");
                        _lastInnerNorm = n;
                    }
                }

                if (debugScrollbar != null)
                {
                    var v = debugScrollbar.value;
                    if (Mathf.Abs(v - _lastScrollbarValue) > 0.0001f)
                    {
                        Debug.Log($"[SlideUpPanelUI:{PanelName}] Scrollbar value: {_lastScrollbarValue} -> {v}");
                        _lastScrollbarValue = v;
                    }
                }
            }
        }

        private void TryBeginHandleDrag(Vector2 screenPos)
        {
            if (_panelScrollRect != null)
                return;

            if (_isDragging || !swipeDownToClose || panelContainer == null)
                return;

            if (dragHandle == null)
                return;

            // Find topmost UI hit under pointer
            Transform topHitTransform = null;
            string topHitName = null;

            if (EventSystem.current != null)
            {
                var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, results);
                if (results.Count > 0 && results[0].gameObject != null)
                {
                    topHitTransform = results[0].gameObject.transform;
                    topHitName = results[0].gameObject.name;
                }
            }

            // If pointer is over the inner scroll view or its content, don't start dragging the panel.
            if (debugInnerScrollRect != null && topHitTransform != null && topHitTransform.IsChildOf(debugInnerScrollRect.transform))
                return;

            // Start dragging only if the pointer hit is actually within dragHandle hierarchy.
            if (topHitTransform == null || !topHitTransform.IsChildOf(dragHandle))
                return;

            if (logPointerRaycastOnDrag && enableDebugLogs)
                Debug.Log($"[SlideUpPanelUI:{PanelName}] Begin panel drag (handle) at {screenPos}, topHit={(topHitName ?? "<none>")}");

            _isDragging = true;
            _lastDragY = screenPos.y;
            _lastDragTime = Time.unscaledTime;
            _dragVelocity = 0f;

            _tween?.Kill(false);
        }

        private bool IsInnerAtTop()
        {
            if (debugInnerScrollRect == null && debugScrollbar == null)
                return false;

            // Prefer ScrollRect normalized position
            if (debugInnerScrollRect != null)
            {
                // In Unity, verticalNormalizedPosition is usually 1=top, 0=bottom.
                // If the linked scrollbar is inverted, we correct the interpretation.
                if (debugScrollbar != null)
                {
                    bool scrollbarInverted = debugScrollbar.direction == Scrollbar.Direction.BottomToTop;
                    if (scrollbarInverted)
                        return debugInnerScrollRect.verticalNormalizedPosition <= innerTopEpsilon;
                }

                return debugInnerScrollRect.verticalNormalizedPosition >= (1f - innerTopEpsilon);
            }

            // Fallback to scrollbar value
            if (debugScrollbar != null)
            {
                bool scrollbarInverted = debugScrollbar.direction == Scrollbar.Direction.BottomToTop;
                float v = debugScrollbar.value;
                return scrollbarInverted ? v <= innerTopEpsilon : v >= (1f - innerTopEpsilon);
            }

            return false;
        }

        private void TryTakeOverDragFromInnerScroll(Vector2 currentPos)
        {
            if (!allowPanelDragWhenInnerAtTop)
                return;

            if (!IsInnerAtTop())
                return;

            float deltaY = currentPos.y - _pointerDownPos.y;
            if (deltaY <= 0f)
                return;

            // start panel drag even if pointer isn't on dragHandle, but only if the gesture began on the panel area
            if (panelContainer == null)
                return;

            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = rootCanvas.worldCamera;

            if (!RectTransformUtility.RectangleContainsScreenPoint(panelContainer, _pointerDownPos, cam))
                return;

            _isDragging = true;
            _lastDragY = currentPos.y;
            _lastDragTime = Time.unscaledTime;
            _dragVelocity = 0f;
            _tween?.Kill(false);

            // ensure inner stops moving so it doesn't fight the panel
            debugInnerScrollRect.StopMovement();
        }

        private void UpdateHandleDrag(Vector2 screenPos)
        {
            if (!_isDragging || panelContainer == null)
                return;

            if (logPointerRaycastOnDrag && enableDebugLogs)
                Debug.Log($"[SlideUpPanelUI:{PanelName}] UpdateHandleDrag at {screenPos}, topHit={GetTopRaycastName(screenPos)}");

            float deltaY = screenPos.y - _lastDragY;
            float deltaTime = Time.unscaledTime - _lastDragTime;

            if (deltaTime > 0.001f)
                _dragVelocity = deltaY / deltaTime;

            _lastDragY = screenPos.y;
            _lastDragTime = Time.unscaledTime;

            float newY = panelContainer.anchoredPosition.y + deltaY;
            newY = Mathf.Min(newY, shownY);

            // Allow a bit of overshoot below hidden for better feel
            float minY = hiddenY - Mathf.Max(0f, panelOvershootBelowHidden);
            newY = Mathf.Max(newY, minY);

            panelContainer.anchoredPosition = new Vector2(panelContainer.anchoredPosition.x, newY);

            if (backgroundDimOverlay != null)
            {
                float progress = Mathf.InverseLerp(hiddenY, shownY, Mathf.Clamp(newY, hiddenY, shownY));
                backgroundDimOverlay.alpha = backgroundDimAlpha * progress;
            }
        }

        private void EndHandleDrag()
        {
            if (enableDebugLogs)
                Debug.Log($"[SlideUpPanelUI:{PanelName}] EndHandleDrag - isDragging={_isDragging}, panelY={(panelContainer != null ? panelContainer.anchoredPosition.y : 0f)}");

            if (!_isDragging || panelContainer == null)
                return;

            _isDragging = false;

            float currentY = panelContainer.anchoredPosition.y;
            float dragDistance = shownY - currentY;
            bool shouldClose = dragDistance > swipeThreshold || _dragVelocity < -swipeVelocityThreshold;

            _tween?.Kill(false);

            if (shouldClose)
            {
                // Smooth close from current position
                OnClosing();
                SetBackgroundDim(false);

                // guard against immediate re-open/close ping-pong
                MarkIgnoreInputAfterClose();
                ForceClearPointerState();

                _isOpen = false;
                _tween = DOTween.To(
                        () => panelContainer.anchoredPosition,
                        v => panelContainer.anchoredPosition = v,
                        new Vector2(panelContainer.anchoredPosition.x, hiddenY),
                        closeDuration)
                    .SetEase(closeEase)
                    .SetUpdate(true)
                    .SetTarget(panelContainer)
                    .OnComplete(CompleteClose);
            }
            else
            {
                // Smooth snap back to open
                _tween = DOTween.To(
                        () => panelContainer.anchoredPosition,
                        v => panelContainer.anchoredPosition = v,
                        new Vector2(panelContainer.anchoredPosition.x, shownY),
                        openDuration * 0.5f)
                    .SetEase(Ease.OutCubic)
                    .SetTarget(panelContainer);

                if (backgroundDimOverlay != null)
                    backgroundDimOverlay.alpha = backgroundDimAlpha;
            }
        }

        private void TryCloseIfTapOutside(Vector2 screenPos)
        {
            if (EventSystem.current != null)
            {
                var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, results);

                if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Raycast hit {results.Count} objects");

                foreach (var result in results)
                {
                    if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Hit: {result.gameObject.name}");
                    
                    if (panelContainer != null && result.gameObject.transform.IsChildOf(panelContainer))
                    {
                        if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Tap inside panel, NOT closing");
                        return;
                    }
                }
            }

            Camera cam = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = rootCanvas.worldCamera;

            bool inside = RectTransformUtility.RectangleContainsScreenPoint(panelContainer, screenPos, cam);
            if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Tap inside bounds: {inside}");
            
            if (!inside)
            {
                if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Closing because tap outside");
                Close();
            }
        }

        #region Swipe Down to Close (IDragHandler)

        // NOTE: We no longer implement Unity drag interfaces globally because it conflicts with ScrollRect.
        // Dragging the panel is handled via dragHandle in Update().

        #endregion

        protected virtual void SetInputBlocking(bool value)
        {
            if (inputBlocker == null) return;

            if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] SetInputBlocking: {value}");

            inputBlocker.alpha = value ? 1f : 0f;
            inputBlocker.interactable = false;
            inputBlocker.blocksRaycasts = value;

            if (value && panelContainer != null && inputBlocker.transform.parent == panelContainer.parent)
            {
                inputBlocker.transform.SetAsFirstSibling();
                panelContainer.SetAsLastSibling();
            }
        }

        protected virtual void SetBackgroundDim(bool value)
        {
            if (backgroundDimOverlay == null) return;

            backgroundDimOverlay.DOKill(false);
            backgroundDimOverlay.gameObject.SetActive(true);
            backgroundDimOverlay.interactable = false;
            backgroundDimOverlay.blocksRaycasts = false;

            float target = value ? backgroundDimAlpha : 0f;

            DOTween.To(() => backgroundDimOverlay.alpha, v => backgroundDimOverlay.alpha = v, target, backgroundDimFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(backgroundDimOverlay)
                .OnComplete(() =>
                {
                    if (!value)
                        backgroundDimOverlay.gameObject.SetActive(false);
                });
        }

        private void StabilizeInnerScrollNextFrame()
        {
            if (debugInnerScrollRect == null)
                return;

            // If we want to always reopen from the top, force it and skip preservation.
            if (resetInnerScrollToTopOnOpen)
            {
                StartCoroutine(ResetInnerScrollToTopCoroutine());
                return;
            }

            if (!preserveInnerScrollOnOpen)
                return;

            StartCoroutine(StabilizeInnerScrollCoroutine());
        }

        private System.Collections.IEnumerator StabilizeInnerScrollCoroutine()
        {
            // wait 1 frame so layout/tween have applied
            yield return null;

            if (debugInnerScrollRect == null)
                yield break;

            var n = debugInnerScrollRect.normalizedPosition;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(debugInnerScrollRect.content);

            // restore previous normalized position after rebuild
            debugInnerScrollRect.normalizedPosition = n;

            if (enableDebugLogs)
                Debug.Log($"[SlideUpPanelUI:{PanelName}] StabilizeInnerScroll applied (normalized={n})");
        }

        private System.Collections.IEnumerator ResetInnerScrollToTopCoroutine()
        {
            // Wait 1 frame so layout has applied.
            yield return null;

            if (debugInnerScrollRect == null)
                yield break;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(debugInnerScrollRect.content);

            debugInnerScrollRect.StopMovement();

            if (!animateResetScrollToTopOnOpen)
            {
                debugInnerScrollRect.verticalNormalizedPosition = 1f;
                debugInnerScrollRect.normalizedPosition = new Vector2(debugInnerScrollRect.normalizedPosition.x, 1f);
            }
            else
            {
                float start = debugInnerScrollRect.verticalNormalizedPosition;
                float t = 0f;
                while (t < resetScrollToTopDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, resetScrollToTopDuration));
                    float eased = DOVirtual.EasedValue(0f, 1f, p, resetScrollToTopEase);
                    float v = Mathf.Lerp(start, 1f, eased);
                    debugInnerScrollRect.verticalNormalizedPosition = v;
                    yield return null;
                }
                debugInnerScrollRect.verticalNormalizedPosition = 1f;
            }

            if (debugScrollbar != null)
            {
                bool scrollbarInverted = debugScrollbar.direction == Scrollbar.Direction.BottomToTop;
                debugScrollbar.value = scrollbarInverted ? 0f : 1f;
            }

            debugInnerScrollRect.StopMovement();
        }

        public virtual void Open()
        {
            if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Open() called - _isOpen: {_isOpen}, panelContainer: {panelContainer}");

            // Guard: if we just closed due to a click, ignore immediate re-open triggered by the same click chain.
            if (Time.unscaledTime < _ignoreInputUntil)
            {
                if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Open() ignored due to input guard window");
                return;
            }

            if (_isOpen)
            {
                if (enableDebugLogs) Debug.LogWarning($"[SlideUpPanelUI:{PanelName}] Already open, ignoring Open()");
                return;
            }
            
            if (panelContainer == null)
            {
                if (enableDebugLogs) Debug.LogError($"[SlideUpPanelUI:{PanelName}] panelContainer is null!");
                return;
            }

            // Reset inner scroll BEFORE showing/open tween, to avoid visible snap at the end of the open.
            if (resetInnerScrollToTopOnOpen && debugInnerScrollRect != null)
            {
                debugInnerScrollRect.StopMovement();
                debugInnerScrollRect.verticalNormalizedPosition = 1f;
                debugInnerScrollRect.normalizedPosition = new Vector2(debugInnerScrollRect.normalizedPosition.x, 1f);
                if (debugScrollbar != null)
                {
                    bool scrollbarInverted = debugScrollbar.direction == Scrollbar.Direction.BottomToTop;
                    debugScrollbar.value = scrollbarInverted ? 0f : 1f;
                }
            }

            _nextHeartbeat = Time.unscaledTime + heartbeatInterval;

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
                    if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Panel opened successfully");
                    OnOpened();
                    OnVisibilityChanged?.Invoke(true);
                    StabilizeInnerScrollNextFrame();
                });
        }

        private void MarkIgnoreInputAfterClose()
        {
            _ignoreInputUntil = Mathf.Max(_ignoreInputUntil, Time.unscaledTime + Mathf.Max(0f, inputIgnoreAfterClose));
        }

        private void ForceClearPointerState()
        {
            _pointerDown = false;
            _isDragging = false;
            _dragVelocity = 0f;
        }

        private void CompleteClose()
        {
            SetInputBlocking(false);
            if (panelContainer != null)
                panelContainer.gameObject.SetActive(false);
            OnClosed();
            OnVisibilityChanged?.Invoke(false);
            if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Panel closed successfully");
        }

        public virtual void Close()
        {
            if (enableDebugLogs) Debug.Log($"[SlideUpPanelUI:{PanelName}] Close() called - _isOpen: {_isOpen}");

            if (!_isOpen || panelContainer == null)
                return;

            // prevent the same click/touch that closed from also triggering a close after the next open
            MarkIgnoreInputAfterClose();
            ForceClearPointerState();

            _isOpen = false;
            _tween?.Kill(false);

            OnClosing();
            SetBackgroundDim(false);

            _tween = DOTween.To(
                    () => panelContainer.anchoredPosition,
                    v => panelContainer.anchoredPosition = v,
                    new Vector2(panelContainer.anchoredPosition.x, hiddenY),
                    closeDuration)
                .SetEase(closeEase)
                .SetUpdate(true)
                .SetTarget(panelContainer)
                .OnComplete(CompleteClose);
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
