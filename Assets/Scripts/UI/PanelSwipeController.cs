using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Project51.Unity
{
    public class PanelSwipeController : MonoBehaviour
    {
        [SerializeField] private RectTransform pagesViewport;
        [SerializeField] private List<RectTransform> pages;

        [Header("Bottom Bar / Safe Area")]
        [Tooltip("Se assegnato, il viewport verrà ridimensionato per non andare sotto questa bottom bar.")]
        [SerializeField] private RectTransform bottomBar;
        [Tooltip("Se true, usa anche la SafeArea del device (notch/home indicator) per calcolare l'altezza disponibile.")]
        [SerializeField] private bool respectSafeArea = true;
        [Tooltip("Altezza fissa della bottom bar in pixel schermo. Se > 0, il layout parte sempre da qui (più safe area se abilitata).")]
        [SerializeField] private float bottomBarFixedHeightPx = 190f;
        [Tooltip("Offset aggiuntivo (in pixel schermo) sopra la bottom bar/safe area. Utile per correggere layout particolari.")]
        [SerializeField] private float extraBottomPaddingPx = 0f;

        [Header("Animation")]
        [SerializeField] private float snapDuration = 0.33f;
        [SerializeField] private Ease snapEase = Ease.OutQuart;

        [Header("Return to center")]
        [SerializeField] private float settleDuration = 0.45f;
        [SerializeField] private Ease settleEase = Ease.OutCubic;

        [Header("Drag feel")]
        [SerializeField, Range(0f, 0.5f)] private float edgeResistance = 0.25f;
        [SerializeField, Range(0f, 0.5f)] private float dragSmoothing = 0.18f;

        [Header("Debug")]
        [Tooltip("Se true, forza CanvasGroup.blocksRaycasts=true su tutte le pagine (solo per capire se i raycast stanno venendo bloccati)")]
        [SerializeField] private bool debugForceBlocksRaycastsOnPages;

        [Header("Page Sizing (Clash Royale style)")]
        [Tooltip("Se > 0, ogni pagina sarà più larga dello schermo di questo valore (px per lato). Effetto 'pagine che sbucano dai bordi'.")]
        [SerializeField] private float pageHorizontalOverflow = 60f;
        [Tooltip("Se true, maschera le pagine laterali così solo la pagina corrente è visibile (niente overlap tra pagine).")]
        [SerializeField] private bool maskSidePages = true;

        private int currentIndex;
        private float screenWidth;
        private float pageSpacing; // Distanza effettiva tra i centri delle pagine (screenWidth + overflow compensation)
        private bool isAnimating;
        private bool isDragging;
        private Tweener snapTweener;
        private float smoothedOffsetX;

        private Vector2Int _lastScreen;
        private Rect _lastSafeArea;
        private float _lastViewportBottomOffset;

        private bool _overflowApplied;

        // Cache dell'overflow già applicato per rendere l'operazione idempotente
        // (evita che offsetMin/offsetMax vengano sommate più volte a ogni relayout).
        private readonly Dictionary<RectTransform, float> _appliedOverflowByPage = new Dictionary<RectTransform, float>();

        public bool IsAnimating => isAnimating;
        public bool IsDragging => isDragging;
        public int CurrentIndex => currentIndex;
        public float ScreenWidth => screenWidth;
        public int PageCount => pages != null ? pages.Count : 0;

        public event Action<int> OnPageChanged;
        public event Action<float> OnPageProgress;


        private void LateUpdate()
        {
            var s = new Vector2Int(Screen.width, Screen.height);
            var sa = Screen.safeArea;
            if (_lastScreen == s && _lastSafeArea == sa) return;
            _lastScreen = s;
            _lastSafeArea = sa;

            RecalculateAndRelayout();
        }
        private void RecalculateAndRelayout()
        {
            if (pagesViewport == null) return;

            Canvas.ForceUpdateCanvases(); // assicura rect aggiornati [web:291]

            // Ridimensiona il viewport in verticale per non sovrapporsi a bottom bar/safe area.
            ApplyViewportVerticalConstraints();

            screenWidth = pagesViewport.rect.width;
            pageSpacing = screenWidth + (pageHorizontalOverflow * 2f);

            // In caso di cambio device/orientamento, riallinea dimensioni e overflow.
            ForcePagesToFillViewport();
            ForcePagesToFillViewportY();
            ApplyPageOverflow();
            EnsureCanvasGroupsForInteraction();

            ApplyLayout(currentIndex);
        }

        private void OnRectTransformDimensionsChange()
        {
            // Chiamato da Unity quando cambiano le dimensioni del RectTransform (es. Canvas scaler/orientamento).
            if (!isActiveAndEnabled) return;
            RecalculateAndRelayout();
        }
        public void SetCurrentIndexImmediate(int newIndex)
        {
            if (pages == null || pages.Count == 0) return;
            newIndex = Mathf.Clamp(newIndex, 0, pages.Count - 1);

            snapTweener?.Kill();
            isAnimating = false;
            isDragging = false;

            currentIndex = newIndex;
            ApplyLayout(currentIndex);
        }

        private IEnumerator Start()
        {
            if (pagesViewport == null)
                yield break;

            ForcePagesActive();

            // Applica overflow orizzontale alle pagine
            ApplyPageOverflow();

            // Applica mask al viewport per nascondere pagine laterali
            if (maskSidePages)
                ApplyViewportMask();

            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();

            ForcePagesToFillViewport();
            ForcePagesToFillViewportY();
            screenWidth = pagesViewport.rect.width;
            
            // Calcola lo spacing tra pagine: screenWidth + overflow*2 (per evitare sovrapposizioni)
            pageSpacing = screenWidth + (pageHorizontalOverflow * 2f);

            ApplyLayout(currentIndex);
        }

        /// <summary>
        /// Rende ogni pagina più larga dello schermo (Clash Royale style).
        /// </summary>
        private void ApplyPageOverflow()
        {
            if (pages == null) return;

            // Se l'overflow è stato cambiato (o se si passa a 0), ripristina prima lo stato precedente.
            foreach (var page in pages)
            {
                if (page == null) continue;
                if (_appliedOverflowByPage.TryGetValue(page, out var alreadyApplied) && alreadyApplied != 0f)
                {
                    page.offsetMin = new Vector2(page.offsetMin.x + alreadyApplied, page.offsetMin.y);
                    page.offsetMax = new Vector2(page.offsetMax.x - alreadyApplied, page.offsetMax.y);
                    _appliedOverflowByPage[page] = 0f;
                }
            }

            if (pageHorizontalOverflow <= 0f) return;

            foreach (var page in pages)
            {
                if (page == null) continue;

                // Applica una sola volta per pagina (idempotente rispetto a relayout).
                if (_appliedOverflowByPage.TryGetValue(page, out var applied) && Mathf.Approximately(applied, pageHorizontalOverflow))
                    continue;

                // Estendi la pagina ai lati
                page.offsetMin = new Vector2(page.offsetMin.x - pageHorizontalOverflow, page.offsetMin.y);
                page.offsetMax = new Vector2(page.offsetMax.x + pageHorizontalOverflow, page.offsetMax.y);

                _appliedOverflowByPage[page] = pageHorizontalOverflow;

                // Opzionale: aggiungi padding al ContentSizeFitter se presente
                ContentSizeFitter csf = page.GetComponent<ContentSizeFitter>();
                if (csf != null && csf.horizontalFit == ContentSizeFitter.FitMode.PreferredSize)
                {
                    // Se usi ContentSizeFitter, potrebbe essere necessario aggiungere un LayoutGroup con padding
                    HorizontalLayoutGroup hlg = page.GetComponent<HorizontalLayoutGroup>();
                    if (hlg == null)
                    {
                        hlg = page.gameObject.AddComponent<HorizontalLayoutGroup>();
                        hlg.childControlWidth = false;
                        hlg.childControlHeight = false;
                        hlg.childForceExpandWidth = false;
                        hlg.childForceExpandHeight = false;
                    }
                    hlg.padding.left += (int)pageHorizontalOverflow;
                    hlg.padding.right += (int)pageHorizontalOverflow;
                }
            }

            _overflowApplied = true;
        }

        private void EnsureCanvasGroupsForInteraction()
        {
            if (pages == null) return;

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                if (page == null) continue;

                var canvasGroup = GetOrAddCanvasGroup(page);
                if (canvasGroup == null) continue;

                bool isCurrent = (i == currentIndex);
                canvasGroup.interactable = isCurrent;
                canvasGroup.blocksRaycasts = debugForceBlocksRaycastsOnPages ? true : isCurrent;
            }
        }

        private void ApplyViewportVerticalConstraints()
        {
            if (pagesViewport == null) return;

            var viewportParent = pagesViewport.parent as RectTransform;
            if (viewportParent == null) return;

            var canvas = pagesViewport.GetComponentInParent<Canvas>();
            var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            // Calcolo "a filo": lavora in coordinate locali del parent per evitare mismatch di pixel
            // (Device Simulator scale / CanvasScaler / render mode).
            // bottomOffsetLocal = distanza dal bordo basso del parent fino al TOP della bottom bar.
            float bottomOffsetLocal = 0f;

            // 1) Bottom bar (preferita): usa il suo TOP in local del parent del viewport.
            if (bottomBar != null)
            {
                var barCorners = new Vector3[4];
                bottomBar.GetWorldCorners(barCorners);
                float topLocalY = float.NegativeInfinity;
                for (int i = 0; i < 4; i++)
                {
                    // Converti corner world -> local del parent viewport
                    Vector2 local = viewportParent.InverseTransformPoint(barCorners[i]);
                    topLocalY = Mathf.Max(topLocalY, local.y);
                }

                // offsetMin.y è la distanza dal basso del parent: in coordinate locali con pivot 0.5,
                // il bordo basso è -rect.height * pivot.y
                float parentBottomLocalY = -viewportParent.rect.height * viewportParent.pivot.y;
                bottomOffsetLocal = Mathf.Max(0f, topLocalY - parentBottomLocalY);
            }
            else
            {
                // Fallback: altezza fissa espressa in pixel schermo -> convertita in unità locali.
                float px = Mathf.Max(0f, bottomBarFixedHeightPx);
                var parentCorners = new Vector3[4];
                viewportParent.GetWorldCorners(parentCorners);
                float parentBottomScreenY = float.PositiveInfinity;
                for (int i = 0; i < 4; i++)
                {
                    var sp = RectTransformUtility.WorldToScreenPoint(cam, parentCorners[i]);
                    parentBottomScreenY = Mathf.Min(parentBottomScreenY, sp.y);
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportParent, new Vector2(0f, parentBottomScreenY), cam, out var p0);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportParent, new Vector2(0f, parentBottomScreenY + px), cam, out var p1);
                bottomOffsetLocal = Mathf.Max(0f, p1.y - p0.y);
            }

            // 2) Safe area: applicala come minimo (se più grande della bottom bar) perché è una zona non utilizzabile.
            if (respectSafeArea)
            {
                // yMin in px dal basso schermo -> in local offset del parent
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportParent, new Vector2(0f, 0f), cam, out var s0);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportParent, new Vector2(0f, Screen.safeArea.yMin), cam, out var s1);
                float safeLocal = Mathf.Max(0f, s1.y - s0.y);
                bottomOffsetLocal = Mathf.Max(bottomOffsetLocal, safeLocal);
            }

            if (!Mathf.Approximately(extraBottomPaddingPx, 0f))
            {
                // extraBottomPaddingPx è in pixel schermo: converti in local
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportParent, new Vector2(0f, 0f), cam, out var e0);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportParent, new Vector2(0f, extraBottomPaddingPx), cam, out var e1);
                bottomOffsetLocal += Mathf.Max(0f, e1.y - e0.y);
            }

            // Evita di rieseguire se non è cambiato.
            if (Mathf.Approximately(_lastViewportBottomOffset, bottomOffsetLocal))
                return;
            _lastViewportBottomOffset = bottomOffsetLocal;

            // Applica: riduci l'altezza disponibile del viewport dal basso.
            // Presupposto: viewport ancorato stretch verticale nel canvas.
            pagesViewport.anchorMin = new Vector2(0f, 0f);
            pagesViewport.anchorMax = new Vector2(1f, 1f);
            pagesViewport.pivot = new Vector2(0.5f, 0.5f);

            pagesViewport.offsetMin = new Vector2(pagesViewport.offsetMin.x, bottomOffsetLocal);
        }


        public void ForcePagesActive()
        {
            if (pages == null) return;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] == null) continue;
                pages[i].gameObject.SetActive(true);
            }
        }
        private void ForcePagesToFillViewport()
        {
            if (pages == null) return;

            foreach (var page in pages)
            {
                if (page == null) continue;

                page.anchorMin = Vector2.zero;
                page.anchorMax = Vector2.one;
                page.pivot = new Vector2(0.5f, 0.5f);

                // riempi verticalmente il viewport
                page.offsetMin = new Vector2(page.offsetMin.x, 0f);
                page.offsetMax = new Vector2(page.offsetMax.x, 0f);
            }
        }
        private void ForcePagesToFillViewportY()
        {
            if (pages == null) return;

            foreach (var page in pages)
            {
                if (page == null) continue;

                page.anchorMin = new Vector2(0f, 0f);
                page.anchorMax = new Vector2(1f, 1f);

                // Non toccare X (perché lo usi per overflow), ma blocca Y:
                page.offsetMin = new Vector2(page.offsetMin.x, 0f);
                page.offsetMax = new Vector2(page.offsetMax.x, 0f);
            }
        }



        public void BeginDrag()
        {
            isDragging = true;
            isAnimating = false;
            snapTweener?.Kill();

            smoothedOffsetX = (currentIndex >= 0 && pages != null && currentIndex < pages.Count && pages[currentIndex] != null)
                ? pages[currentIndex].anchoredPosition.x
                : 0f;

            ApplyCanvasGroupsForDrag();
        }

        public void DragToOffset(float deltaX)
        {
            if (!isDragging) return;
            if (pageSpacing <= 0f) return;
            if (pages == null || pages.Count == 0) return;

            float adjusted = deltaX;
            if (currentIndex <= 0 && adjusted > 0f)
                adjusted *= (1f - edgeResistance);
            else if (currentIndex >= pages.Count - 1 && adjusted < 0f)
                adjusted *= (1f - edgeResistance);

            smoothedOffsetX = Mathf.Lerp(smoothedOffsetX, adjusted, 1f - Mathf.Exp(-dragSmoothing * 60f * Time.unscaledDeltaTime));

            for (int i = 0; i < pages.Count; i++)
            {
                RectTransform page = pages[i];
                if (page == null) continue;

                // Usa pageSpacing invece di screenWidth per evitare overlap
                float x = (i - currentIndex) * pageSpacing + smoothedOffsetX;
                page.anchoredPosition = new Vector2(x, 0f);
            }

            float pageFloat = CalculatePageFloat();
            OnPageProgress?.Invoke(pageFloat);
        }

        public float CalculatePageFloat()
        {
            if (pageSpacing <= 0f) return currentIndex;
            if (pages == null || pages.Count == 0) return currentIndex;

            // Usa pageSpacing per calcolo coerente
            float normalizedOffset = -smoothedOffsetX / pageSpacing;
            float pageFloat = currentIndex + normalizedOffset;

            return Mathf.Clamp(pageFloat, 0f, pages.Count - 1);
        }

        public float GetPageFloatFromDelta(float deltaX)
        {
            if (pageSpacing <= 0f) return currentIndex;
            if (pages == null || pages.Count == 0) return currentIndex;

            float normalizedOffset = -deltaX / pageSpacing;
            float pageFloat = currentIndex + normalizedOffset;

            return Mathf.Clamp(pageFloat, 0f, pages.Count - 1);
        }

        public void EndDragAndSnapTo(int newIndex)
        {
            isDragging = false;

            if (pages == null || pages.Count == 0) return;

            if (newIndex == currentIndex)
            {
                SettleToCenter();
                return;
            }

            int oldIndex = currentIndex;
            currentIndex = Mathf.Clamp(newIndex, 0, pages.Count - 1);
            SnapToCurrentIndex(snapDuration, snapEase, () =>
            {
                if (oldIndex != currentIndex)
                    OnPageChanged?.Invoke(currentIndex);
            });
        }

        private void SettleToCenter()
        {
            snapTweener?.Kill();
            isAnimating = true;

            float fromOffset = 0f;
            if (pages != null && currentIndex >= 0 && currentIndex < pages.Count && pages[currentIndex] != null)
                fromOffset = pages[currentIndex].anchoredPosition.x;

            SnapInternal(fromOffset, 0f, settleDuration, settleEase, () =>
            {
                isAnimating = false;
                ApplyLayout(currentIndex);
                OnPageProgress?.Invoke(currentIndex);
            });
        }

        private void SnapToCurrentIndex(float duration, Ease ease, Action onComplete = null)
        {
            snapTweener?.Kill();
            isAnimating = true;

            float fromOffset = 0f;
            if (pages != null && currentIndex >= 0 && currentIndex < pages.Count && pages[currentIndex] != null)
                fromOffset = pages[currentIndex].anchoredPosition.x;

            SnapInternal(fromOffset, 0f, duration, ease, () =>
            {
                isAnimating = false;
                ApplyLayout(currentIndex);
                OnPageProgress?.Invoke(currentIndex);
                onComplete?.Invoke();
            });
        }

        private void SnapInternal(float from, float to, float duration, Ease ease, TweenCallback onComplete)
        {
            float offset = from;

            snapTweener = DOTween.To(
                    () => offset,
                    v =>
                    {
                        offset = v;
                        smoothedOffsetX = offset;

                        if (pages == null) return;
                        for (int i = 0; i < pages.Count; i++)
                        {
                            RectTransform page = pages[i];
                            if (page == null) continue;

                            // Usa pageSpacing invece di screenWidth
                            float baseX = (i - currentIndex) * pageSpacing;
                            page.anchoredPosition = new Vector2(baseX + offset, 0f);
                        }

                        float pageFloat = CalculatePageFloat();
                        OnPageProgress?.Invoke(pageFloat);
                    },
                    to,
                    duration)
                .SetUpdate(true)
                .SetEase(ease)
                .OnComplete(onComplete);
        }

        private CanvasGroup GetOrAddCanvasGroup(RectTransform page)
        {
            if (page == null) return null;
            var cg = page.GetComponent<CanvasGroup>();
            if (cg != null) return cg;
            return page.gameObject.AddComponent<CanvasGroup>();
        }

        private void EnsureInitializedForProgrammaticNavigation()
        {
            if (pagesViewport == null) return;

            if (screenWidth <= 0f)
                screenWidth = pagesViewport.rect.width;

            if (pageSpacing <= 0f)
            {
                // Spacing tra pagine: width del viewport + overflow laterale (per non sovrapporre)
                pageSpacing = screenWidth + (pageHorizontalOverflow * 2f);
            }
        }

        private void ApplyLayout(int index)
        {
            if (pagesViewport == null) return;
            if (pages == null) return;

            EnsureInitializedForProgrammaticNavigation();

            smoothedOffsetX = 0f;

            for (int i = 0; i < pages.Count; i++)
            {
                RectTransform page = pages[i];
                if (page == null) continue;

                page.gameObject.SetActive(true);

                float spacing = pageSpacing > 0f ? pageSpacing : pagesViewport.rect.width;
                page.anchoredPosition = new Vector2((i - index) * spacing, 0f);

                var canvasGroup = GetOrAddCanvasGroup(page);
                if (canvasGroup != null)
                {
                    bool isCurrent = (i == index);
                    canvasGroup.interactable = isCurrent;

                    // Cruciale: le pagine NON correnti non devono mai bloccare i raycast,
                    // altrimenti possono "coprire" la bottom bar (specialmente con Canvas multipli).
                    canvasGroup.blocksRaycasts = debugForceBlocksRaycastsOnPages ? true : isCurrent;
                    canvasGroup.alpha = 1f;
                }
            }
        }

        private void ApplyCanvasGroupsForDrag()
        {
            if (pages == null) return;
            for (int i = 0; i < pages.Count; i++)
            {
                RectTransform page = pages[i];
                if (page == null) continue;

                page.gameObject.SetActive(true);

                var canvasGroup = GetOrAddCanvasGroup(page);
                if (canvasGroup == null) continue;

                // Durante il drag vogliamo evitare click accidentali sulle pagine non correnti,
                // ma NON vogliamo "uccidere" l'input della pagina corrente (es. bottom bar / bottoni visibili).
                bool isCurrent = (i == currentIndex);
                canvasGroup.interactable = isCurrent;
                canvasGroup.blocksRaycasts = debugForceBlocksRaycastsOnPages ? true : isCurrent;
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Applica una Mask al PagesViewport per nascondere le pagine laterali.
        /// Solo la pagina corrente (quella centrata nel viewport) sarà visibile.
        /// </summary>
        private void ApplyViewportMask()
        {
            if (pagesViewport == null) return;

            // Aggiungi Mask component se non c'è già
            Mask mask = pagesViewport.GetComponent<Mask>();
            if (mask == null)
            {
                mask = pagesViewport.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false; // Non mostrare il grafico della mask
            }

            // Aggiungi Image component (richiesto dalla Mask) se non c'è
            Image img = pagesViewport.GetComponent<Image>();
            if (img == null)
            {
                img = pagesViewport.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f); // Trasparente
                img.raycastTarget = false;
            }
        }

        public void SwitchPage(int newIndex)
        {
            EnsureInitializedForProgrammaticNavigation();

            if (pages == null || pages.Count == 0)
            {
                return;
            }

            newIndex = Mathf.Clamp(newIndex, 0, pages.Count - 1);

            ForcePagesActive();

            // Se le misure non sono ancora pronte (Start coroutine non ha ancora settato rect),
            // posizioniamo immediatamente senza tween.
            if (pageSpacing <= 0f)
            {
                currentIndex = newIndex;
                ApplyLayout(currentIndex);
                OnPageProgress?.Invoke(currentIndex);
                OnPageChanged?.Invoke(currentIndex);
                return;
            }

            if (!isDragging && currentIndex == newIndex && !isAnimating)
            {
                ApplyLayout(currentIndex);
                return;
            }

            int oldIndex = currentIndex;
            currentIndex = newIndex;

            SnapToCurrentIndex(snapDuration, snapEase, () =>
            {
                if (oldIndex != newIndex)
                    OnPageChanged?.Invoke(currentIndex);
            });
        }
    }
}
