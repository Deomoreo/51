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

        public bool IsAnimating => isAnimating;
        public bool IsDragging => isDragging;
        public int CurrentIndex => currentIndex;
        public float ScreenWidth => screenWidth;
        public int PageCount => pages != null ? pages.Count : 0;

        public event Action<int> OnPageChanged;
        public event Action<float> OnPageProgress;

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
            if (pageHorizontalOverflow <= 0f) return;

            foreach (var page in pages)
            {
                if (page == null) continue;

                // Estendi la pagina ai lati
                page.offsetMin = new Vector2(page.offsetMin.x - pageHorizontalOverflow, page.offsetMin.y);
                page.offsetMax = new Vector2(page.offsetMax.x + pageHorizontalOverflow, page.offsetMax.y);

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
