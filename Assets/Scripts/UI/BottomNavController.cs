using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace Project51.Unity
{
    public class BottomNavController : MonoBehaviour
    {
        [System.Serializable]
        public class NavTab
        {
            public Button button;
            public LayoutElement layoutElement;
            public int tabIndex;

            [Header("Overlay Target")]
            [Tooltip("RectTransform usato come target del selettore (pill/underline). Se vuoto, usa il RectTransform del Button.")]
            public RectTransform overlayTarget;

            [Header("Animation Refs")]
            public CanvasGroup selectedContent;
            public Transform iconTransform;
            public RectTransform arrowLeft;
            public RectTransform arrowRight;
        }

        [Header("Tab Configuration")]
        [SerializeField] private List<NavTab> tabs = new List<NavTab>();
        [SerializeField] private float expandedWidth = 300f;
        [SerializeField] private float collapsedWidth = 230f;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        [Header("Overlay (Clash Royale style)")]
        [SerializeField] private RectTransform selectionOverlay;
        [Tooltip("Larghezza fissa dell'overlay (non cambia durante swipe).")]
        [SerializeField] private float overlayFixedWidth = 300f;
        [SerializeField] private float overlayTweenDuration = 0.22f;
        [SerializeField] private Ease overlayTweenEase = Ease.OutCubic;
        [Tooltip("Se true, l'overlay usa coordinate (world->local) e quindi può stare sotto layout group / avere anchors diversi dai target.")]
        [SerializeField] private bool overlayUseWorldSpaceConversion = true;

        [Header("Swipe behaviour")]
        [Tooltip("Se true, durante swipe NON modifichiamo larghezze/alpha dei bottoni (evita spostamenti del LayoutGroup).")]
        [SerializeField] private bool freezeTabsDuranteSwipe = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogging;
        [SerializeField, Range(1, 30)] private int debugLogEveryNFrames = 6;
        [SerializeField] private bool debugVerboseSwipe;

        [Header("Starting Tab")]
        [SerializeField] private int defaultSelectedIndex = 0;

        [SerializeField] private PanelSwipeController swipeController;

        [Header("Integration")]
        [Tooltip("Se true, SelectTab cambia anche pagina tramite PanelSwipeController.\nDisattiva (false) se la navigazione pagine è gestita altrove (es. MainHudController + BottomNavBarUI).")]
        [SerializeField] private bool drivePages = false;

        [Header("UI Only")]
        [Tooltip("Se true, disabilita qualsiasi movimento/animazione dell'overlay da parte di questo controller.\nUsalo quando l'indicator durante swipe è gestito da BottomNavBarUI.")]
        [SerializeField] private bool disableOverlayMovement = true;

        private int currentSelectedIndex = -1;
        public int CurrentSelectedIndex => currentSelectedIndex;
        public int TabCount => tabs.Count;

        private bool swipePreviewActive;
        private Tweener overlayTweener;

        private RectTransform TabsRootRect => transform as RectTransform;
        private int debugFrameCounter;
        private int swipeSessionId;
        private int swipeSetProgressCalls;
        private int lastSwipeFromIndex;
        private int lastSwipeToIndex;
        private float lastSwipeProgress;

        // Cache delle posizioni X dei tab (calcolate una volta dopo il layout)
        private float[] cachedTabCentersX;

        private Vector2 _lastTabsRootSize;

        private void OnEnable()
        {
            // Questo controller non deve intercettare i click se la navigazione è gestita da BottomNavBarUI.
            // Lasciamo i bottoni così come sono già configurati altrove.
            if (!drivePages)
                return;

            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].button == null) continue;

                tabs[i].button.onClick.RemoveAllListeners();
                int index = i;
                tabs[i].button.onClick.AddListener(() => SelectTab(index, animate: true));
                tabs[i].button.interactable = true;
            }
        }

        private IEnumerator Start()
        {
            yield return null;

            currentSelectedIndex = Mathf.Clamp(defaultSelectedIndex, 0, tabs.Count - 1);

            // Sincronizza subito il controller pagine col tab selezionato
            if (swipeController != null)
            {
                swipeController.ForcePagesActive();
                swipeController.SetCurrentIndexImmediate(currentSelectedIndex);
            }

            // Imposta subito lo stato larghezze/alpha per la tab di default
            ApplyImmediateSelectionState(currentSelectedIndex);

            // Forza il layout DOPO aver impostato le larghezze
            ForceRebuildLayout();

            // Applica dimensione fissa overlay
            ApplyOverlayFixedSize();

            // Cache le posizioni dei tab
            CacheTabPositions();

            // Posiziona overlay correttamente dopo layout
            if (selectionOverlay != null && cachedTabCentersX != null && currentSelectedIndex < cachedTabCentersX.Length)
            {
                selectionOverlay.anchoredPosition = new Vector2(cachedTabCentersX[currentSelectedIndex], selectionOverlay.anchoredPosition.y);
            }

            if (debugLogging)
                LogState("Start/Initialized", currentSelectedIndex, currentSelectedIndex, 0f);
        }

        private void CacheTabPositions()
        {
            if (cachedTabCentersX == null || cachedTabCentersX.Length != tabs.Count)
                cachedTabCentersX = new float[tabs.Count];

            for (int i = 0; i < tabs.Count; i++)
            {
                var target = GetTabOverlayTarget(tabs[i]);
                if (target == null)
                {
                    cachedTabCentersX[i] = 0f;
                    continue;
                }

                // IMPORTANT: usa sempre la stessa conversione usata da SetOverlay/Swipe
                var info = GetOverlayLocalTargetInfo(target);
                cachedTabCentersX[i] = info.anchoredPos.x;
            }

            if (debugLogging)
            {
                string positions = string.Join(", ", cachedTabCentersX);
                Debug.Log($"[BottomNavController] Cached tab positions: [{positions}]");
            }
        }

        public void BeginSwipePreview()
        {
            if (disableOverlayMovement) return;

            swipePreviewActive = true;
            swipeSessionId++;
            swipeSetProgressCalls = 0;
            lastSwipeFromIndex = -1;
            lastSwipeToIndex = -1;
            lastSwipeProgress = 0f;

            overlayTweener?.Kill(false);
            ApplyOverlayFixedSize();

            ForceRebuildLayout();
            CacheTabPositions();

            if (freezeTabsDuranteSwipe)
                ApplyImmediateSelectionState(currentSelectedIndex);

            if (selectionOverlay != null && cachedTabCentersX != null && currentSelectedIndex < cachedTabCentersX.Length)
            {
                float desiredX = cachedTabCentersX[currentSelectedIndex];
                if (Mathf.Abs(selectionOverlay.anchoredPosition.x - desiredX) > 0.5f)
                    selectionOverlay.anchoredPosition = new Vector2(desiredX, selectionOverlay.anchoredPosition.y);
            }

            if (debugLogging)
            {
                Debug.Log($"[BottomNavController] BeginSwipePreview sid={swipeSessionId} frame={Time.frameCount} time={Time.unscaledTime:F3} currentSelectedIndex={currentSelectedIndex} overlayX={(selectionOverlay != null ? selectionOverlay.anchoredPosition.x : 0f):F2} cache={(cachedTabCentersX != null ? string.Join(", ", cachedTabCentersX) : "null")}");
            }
        }

        /// <summary>
        /// Termina lo swipe preview.
        /// </summary>
        /// <param name="pendingTargetIndex">Se >= 0, indica che stiamo per fare SelectTab a questo indice, 
        /// quindi non animare l'overlay verso currentSelectedIndex.</param>
        public void EndSwipePreview(int pendingTargetIndex = -1)
        {
            if (disableOverlayMovement) return;

            if (!swipePreviewActive) return;
            swipePreviewActive = false;

            // Se stiamo per cambiare pagina, non animare l'overlay qui
            // SelectTab si occuperà dell'animazione
            bool willChangePage = pendingTargetIndex >= 0 && pendingTargetIndex != currentSelectedIndex;

            if (!willChangePage)
            {
                // Torniamo al tab corrente, anima l'overlay
                if (selectionOverlay != null && cachedTabCentersX != null && currentSelectedIndex < cachedTabCentersX.Length)
                {
                    overlayTweener?.Kill(false);
                    float targetX = cachedTabCentersX[currentSelectedIndex];
                    overlayTweener = selectionOverlay.DOAnchorPosX(targetX, overlayTweenDuration)
                        .SetEase(overlayTweenEase)
                        .SetTarget(selectionOverlay);
                }
            }

            // Ripristina lo stato dei tab (larghezze/alpha)
            ApplyImmediateSelectionState(currentSelectedIndex);

            if (debugLogging)
            {
                Debug.Log($"[BottomNavController] EndSwipePreview sid={swipeSessionId} frame={Time.frameCount} time={Time.unscaledTime:F3} currentSelectedIndex={currentSelectedIndex} pendingTargetIndex={pendingTargetIndex} willChangePage={willChangePage} setProgressCalls={swipeSetProgressCalls} last(from={lastSwipeFromIndex},to={lastSwipeToIndex},p={lastSwipeProgress:F3}) overlayX={(selectionOverlay != null ? selectionOverlay.anchoredPosition.x : 0f):F2}");
            }
        }

        /// <summary>
        /// Aggiorna l'overlay durante lo swipe usando il progress normalizzato.
        /// progress > 0 = swipe verso destra (indice diminuisce)
        /// progress < 0 = swipe verso sinistra (indice aumenta)
        /// </summary>
        public void SetSwipeProgress(int fromIndex, float progress)
        {
            if (disableOverlayMovement) return;

            if (!swipePreviewActive)
            {
                if (debugLogging && debugVerboseSwipe)
                    Debug.Log($"[BottomNavController] SetSwipeProgress IGNORED (not in preview) frame={Time.frameCount} fromIndex={fromIndex} progress={progress:F3} currentSelectedIndex={currentSelectedIndex}");
                return;
            }
            if (tabs == null || tabs.Count == 0) return;
            if (cachedTabCentersX == null) return;

            swipeSetProgressCalls++;

            int actualFromIndex = Mathf.Clamp(fromIndex, 0, tabs.Count - 1);
            int desiredToIndex = progress < 0f ? actualFromIndex + 1 : actualFromIndex - 1;

            if (desiredToIndex < 0 || desiredToIndex >= tabs.Count)
            {
                if (debugLogging && debugVerboseSwipe)
                {
                    Debug.Log($"[BottomNavController] SetSwipeProgress EDGE sid={swipeSessionId} frame={Time.frameCount} from={actualFromIndex} desiredTo={desiredToIndex} p={progress:F3} (no adjacent tab) overlayX={(selectionOverlay != null ? selectionOverlay.anchoredPosition.x : 0f):F2}");
                }
                return;
            }

            int toIndex = desiredToIndex;
            float t = Mathf.Clamp01(Mathf.Abs(progress));

            float fromX = cachedTabCentersX[actualFromIndex];
            float toX = cachedTabCentersX[toIndex];
            float lerpedX = Mathf.Lerp(fromX, toX, t);

            if (selectionOverlay != null)
                selectionOverlay.anchoredPosition = new Vector2(lerpedX, selectionOverlay.anchoredPosition.y);

            lastSwipeFromIndex = actualFromIndex;
            lastSwipeToIndex = toIndex;
            lastSwipeProgress = progress;

            if (debugLogging)
            {
                debugFrameCounter++;
                if (debugVerboseSwipe || debugFrameCounter % debugLogEveryNFrames == 0)
                {
                    Debug.Log($"[BottomNavController] SetSwipeProgress sid={swipeSessionId} frame={Time.frameCount} from={actualFromIndex} to={toIndex} p={progress:F3} t={t:F3} fromX={fromX:F2} toX={toX:F2} lerpX={lerpedX:F2} overlayX={(selectionOverlay != null ? selectionOverlay.anchoredPosition.x : 0f):F2}");
                }
            }
        }

        public void SelectTab(int index, bool animate = true)
        {
            if (index < 0 || index >= tabs.Count) return;

            swipePreviewActive = false;
            overlayTweener?.Kill(false);

            ApplyOverlayFixedSize();

            if (debugLogging)
            {
                Debug.Log($"[BottomNavController] SelectTab called frame={Time.frameCount} time={Time.unscaledTime:F3} index={index} animate={animate} prevCurrent={currentSelectedIndex} overlayX={(selectionOverlay != null ? selectionOverlay.anchoredPosition.x : 0f):F2}");
            }

            if (index == currentSelectedIndex)
            {
                if (animate && tabs[index].iconTransform != null)
                {
                    Transform icon = tabs[index].iconTransform;
                    icon.DOKill(complete: true);
                    icon.localScale = Vector3.one;
                    icon.DOPunchScale(new Vector3(-0.15f, -0.15f, 0), 0.25f, 8, 0.8f);
                }

                if (debugLogging)
                    LogState("SelectTab (same)", index, index, 0f);

                return;
            }

            int previousIndex = currentSelectedIndex;

            KillTabTweens(previousIndex);
            KillTabTweens(index);

            currentSelectedIndex = index;

            // Avvia animazioni width
            if (previousIndex >= 0 && previousIndex < tabs.Count)
                AnimateTabWidth(tabs[previousIndex], collapsedWidth, isExpanding: false, animate);

            AnimateTabWidth(tabs[index], expandedWidth, isExpanding: true, animate);

            // Overlay: comportamento stabile come prima
            if (!disableOverlayMovement)
            {
                if (selectionOverlay != null && animate)
                {
                    StartCoroutine(AnimateOverlayAfterLayoutUpdate(index));
                }
                else if (selectionOverlay != null)
                {
                    ForceRebuildLayout();
                    CacheTabPositions();
                    if (cachedTabCentersX != null && index < cachedTabCentersX.Length)
                        selectionOverlay.anchoredPosition = new Vector2(cachedTabCentersX[index], selectionOverlay.anchoredPosition.y);
                }
            }

            // IMPORTANT: opzionale. Se la navigazione pagine è gestita altrove, non pilotare lo swipeController.
            if (drivePages && swipeController != null)
                swipeController.SwitchPage(index);

            if (debugLogging)
                LogState("SelectTab", index, index, 0f);
        }

        /// <summary>
        /// Selezione tab "solo UI" (non cambia pagina). Utile quando il controller pagine è gestito da altri script.
        /// </summary>
        public void SelectTabUIOnly(int index, bool animate = true)
        {
            bool prev = drivePages;
            drivePages = false;
            SelectTab(index, animate);
            drivePages = prev;
        }

        private IEnumerator AnimateOverlayAfterLayoutUpdate(int targetIndex)
        {
            if (disableOverlayMovement) yield break;

            int startFrame = Time.frameCount;
            float startTime = Time.unscaledTime;

            yield return null;

            ForceRebuildLayout();
            CacheTabPositions();

            if (selectionOverlay == null || cachedTabCentersX == null) yield break;
            if (targetIndex < 0 || targetIndex >= cachedTabCentersX.Length) yield break;

            float targetX = cachedTabCentersX[targetIndex];

            if (debugLogging)
            {
                Debug.Log($"[BottomNavController] AnimateOverlayAfterLayoutUpdate frame={Time.frameCount} (+{Time.frameCount - startFrame}) time={Time.unscaledTime:F3} (+{Time.unscaledTime - startTime:F3}) targetIndex={targetIndex} targetX={targetX:F2} overlayX(before)={selectionOverlay.anchoredPosition.x:F2} cache={(cachedTabCentersX != null ? string.Join(", ", cachedTabCentersX) : "null")}");
            }

            overlayTweener?.Kill(false);
            overlayTweener = selectionOverlay.DOAnchorPosX(targetX, overlayTweenDuration)
                .SetEase(overlayTweenEase)
                .SetTarget(selectionOverlay);
        }

        private void ForceRebuildLayout()
        {
            var rt = TabsRootRect;
            if (rt == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            Canvas.ForceUpdateCanvases();
        }

        private void ApplyImmediateSelectionState(int selectedIndex)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                bool isSelected = (i == selectedIndex);
                ApplyImmediateWidth(i, isSelected ? expandedWidth : collapsedWidth);
                ApplyImmediateAlpha(i, isSelected ? 1f : 0f);
            }
        }

        private void LateUpdate()
        {
            // Hard lock: evita che il layout o altri script cambino la width dell'overlay.
            ApplyOverlayFixedSize();

            // Fix "primo swipe offset": se il layout cambia (safe area / risoluzione / fitting),
            // ricache le posizioni e riallinea l'overlay.
            var rt = TabsRootRect;
            if (rt != null)
            {
                var size = rt.rect.size;
                if (size != _lastTabsRootSize)
                {
                    _lastTabsRootSize = size;

                    ForceRebuildLayout();
                    CacheTabPositions();

                    if (!swipePreviewActive && selectionOverlay != null && cachedTabCentersX != null &&
                        currentSelectedIndex >= 0 && currentSelectedIndex < cachedTabCentersX.Length)
                    {
                        selectionOverlay.anchoredPosition = new Vector2(cachedTabCentersX[currentSelectedIndex], selectionOverlay.anchoredPosition.y);
                    }
                }
            }
        }

        private void ApplyOverlayFixedSize()
        {
            if (selectionOverlay == null) return;
            if (overlayFixedWidth <= 0f) return;

            // Forza solo X, lasciando l'altezza invariata.
            var sd = selectionOverlay.sizeDelta;
            if (Mathf.Abs(sd.x - overlayFixedWidth) > 0.01f)
                selectionOverlay.sizeDelta = new Vector2(overlayFixedWidth, sd.y);
        }

        private (Vector2 anchoredPos, Vector2 size) GetOverlayLocalTargetInfo(RectTransform target)
        {
            if (selectionOverlay == null || selectionOverlay.parent == null)
                return (Vector2.zero, Vector2.zero);

            Vector2 size = target.rect.size;

            if (!overlayUseWorldSpaceConversion)
                return (target.anchoredPosition, size);

            RectTransform overlayParent = selectionOverlay.parent as RectTransform;
            if (overlayParent == null)
                return (target.anchoredPosition, size);

            // Ottieni il centro del target in world space
            Vector3[] targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);
            Vector3 targetWorldCenter = (targetCorners[0] + targetCorners[2]) * 0.5f;
            
            // Converti in local space del parent dell'overlay
            Vector3 localPos = overlayParent.InverseTransformPoint(targetWorldCenter);

            return (new Vector2(localPos.x, localPos.y), size);
        }

        private RectTransform GetTabOverlayTarget(NavTab tab)
        {
            if (tab == null) return null;
            if (tab.overlayTarget != null) return tab.overlayTarget;
            if (tab.button == null) return null;
            return tab.button.transform as RectTransform;
        }

        private void KillTabTweens(int index)
        {
            if (index < 0 || index >= tabs.Count) return;

            var tab = tabs[index];
            if (tab.layoutElement != null)
                DOTween.Kill(tab.layoutElement, complete: false);
            if (tab.selectedContent != null)
                tab.selectedContent.DOKill(complete: false);
            if (tab.iconTransform != null)
                tab.iconTransform.DOKill(complete: false);
            if (tab.arrowLeft != null)
                tab.arrowLeft.DOKill(complete: false);
            if (tab.arrowRight != null)
                tab.arrowRight.DOKill(complete: false);
        }

        private void ApplyImmediateWidth(int index, float width)
        {
            if (index < 0 || index >= tabs.Count) return;
            var tab = tabs[index];
            if (tab.layoutElement == null) return;

            // Con HorizontalLayoutGroup, la width effettiva è guidata soprattutto da preferredWidth.
            tab.layoutElement.preferredWidth = width;

            // Manteniamo minWidth coerente (soprattutto per evitare shrink sotto il minimo).
            tab.layoutElement.minWidth = width;

            // Evita che Flex schiacci/espanda in modo non voluto.
            tab.layoutElement.flexibleWidth = 0f;
        }

        private void AnimateTabWidth(NavTab tab, float targetWidth, bool isExpanding, bool animate)
        {
            // Anima larghezza del LayoutElement
            if (tab.layoutElement != null)
            {
                if (animate)
                {
                    DOTween.Kill(tab.layoutElement);

                    DOTween.To(() => tab.layoutElement.preferredWidth,
                               x =>
                               {
                                   tab.layoutElement.preferredWidth = x;
                                   // Forza aggiornamento layout mentre tweeniamo
                                   LayoutRebuilder.MarkLayoutForRebuild(TabsRootRect);
                               },
                               targetWidth,
                               animationDuration)
                           .SetEase(animationEase)
                           .SetTarget(tab.layoutElement)
                           .OnUpdate(() =>
                           {
                               // Alcuni layout group aggiornano solo a fine frame: aiutiamoli.
                               Canvas.ForceUpdateCanvases();
                           });

                    tab.layoutElement.minWidth = targetWidth;
                    tab.layoutElement.flexibleWidth = 0f;
                }
                else
                {
                    tab.layoutElement.preferredWidth = targetWidth;
                    tab.layoutElement.minWidth = targetWidth;
                    tab.layoutElement.flexibleWidth = 0f;
                }

                // Applica subito il layout (utile quando la UI sembra non aggiornarsi finché non si fa un altro input)
                ForceRebuildLayout();
            }

            // Anima alpha del contenuto selezionato
            if (tab.selectedContent != null)
            {
                float targetAlpha = isExpanding ? 1f : 0f;

                if (animate)
                {
                    tab.selectedContent.DOKill(complete: false);
                    float delay = isExpanding ? 0.1f : 0f;
                    tab.selectedContent.DOFade(targetAlpha, animationDuration * 0.8f)
                        .SetDelay(delay);
                }
                else
                {
                    tab.selectedContent.alpha = targetAlpha;
                }
            }

            // Animazioni frecce solo quando si espande
            if (isExpanding && animate)
            {
                if (tab.arrowLeft != null)
                {
                    tab.arrowLeft.DOKill(complete: true);
                    tab.arrowLeft.DOPunchAnchorPos(new Vector2(-20f, 0), 0.4f, 5, 0.5f).SetDelay(0.1f);
                }

                if (tab.arrowRight != null)
                {
                    tab.arrowRight.DOKill(complete: true);
                    tab.arrowRight.DOPunchAnchorPos(new Vector2(20f, 0), 0.4f, 5, 0.5f).SetDelay(0.1f);
                }
            }
        }

        private void ApplyImmediateAlpha(int index, float alpha)
        {
            if (index < 0 || index >= tabs.Count) return;
            var tab = tabs[index];
            if (tab.selectedContent == null) return;
            tab.selectedContent.alpha = alpha;
        }

        private void LogState(string tag, int fromIndex, int toIndex, float progress)
        {
            if (!debugLogging) return;
            if (selectionOverlay == null)
            {
                Debug.Log($"[BottomNavController] {tag} overlay=NULL");
                return;
            }

            string overlayInfo = $"overlay pos={selectionOverlay.anchoredPosition} size={selectionOverlay.sizeDelta}";
            string selInfo = $"currentSelectedIndex={currentSelectedIndex} swipePreviewActive={swipePreviewActive}";
            string swipeInfo = $"from={fromIndex} to={toIndex} progress={progress:F3}";
            string cacheInfo = cachedTabCentersX != null ? $"cache=[{string.Join(", ", cachedTabCentersX)}]" : "cache=null";

            Debug.Log($"[BottomNavController] {tag} | {selInfo} | {swipeInfo} | {overlayInfo} | {cacheInfo}");
        }
    }
}
