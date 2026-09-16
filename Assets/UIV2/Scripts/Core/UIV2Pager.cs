using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project51.UIV2.Core
{
    public sealed class UIV2Pager : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform[] pages;
        public Func<bool> CanNavigate;
        public event Action<int> OnPageChanged;
        public int CurrentIndex { get; private set; }
        public bool IsMoving => tween != null && tween.IsActive() || dragging;
        private Tween tween;
        private float position;
        private float startPosition;
        private Vector2 startPointer;
        private bool dragging;
        private float lastWidth;

        private void Start() { Layout(); SetInteraction(true); }
        public void Select(int index)
        {
            if (CanNavigate != null && !CanNavigate()) return;
            Snap(Mathf.Clamp(index, 0, pages.Length - 1));
        }
        private void Snap(int index)
        {
            tween?.Kill(); dragging = false;
            CurrentIndex = index;
            SetInteraction(false);
            OnPageChanged?.Invoke(index);
            tween = DOTween.To(() => position, x => { position = x; Layout(); }, index, .3f)
                .SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => { tween = null; SetInteraction(true); });
        }
        public bool BeginSwipe(PointerEventData e)
        {
            if (CanNavigate != null && !CanNavigate()) return false;
            tween?.Kill(); tween = null;
            startPointer = e.pressPosition; startPosition = position; dragging = true;
            SetInteraction(false);
            return true;
        }
        public void DragSwipe(PointerEventData e)
        {
            if (!dragging) return;
            var canvas = viewport.GetComponentInParent<Canvas>();
            float pixels = viewport.rect.width * canvas.scaleFactor;
            float candidate = startPosition - (e.position.x - startPointer.x) / Mathf.Max(1, pixels);
            position = Mathf.Clamp(candidate, -.12f, pages.Length - .88f);
            Layout();
        }
        public void EndSwipe(PointerEventData e)
        {
            if (!dragging) return;
            float delta = position - startPosition;
            int target = Mathf.RoundToInt(startPosition);
            if (Mathf.Abs(delta) > .16f) target += delta > 0 ? 1 : -1;
            Snap(Mathf.Clamp(target, 0, pages.Length - 1));
        }
        private void Layout()
        {
            if (viewport == null || pages == null) return;
            lastWidth = viewport.rect.width;
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null) pages[i].anchoredPosition = new Vector2((i - position) * lastWidth, 0);
        }
        private void SetInteraction(bool enabled)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                var group = pages[i].GetComponent<CanvasGroup>();
                if (group == null) group = pages[i].gameObject.AddComponent<CanvasGroup>();
                group.interactable = enabled && i == CurrentIndex;
                group.blocksRaycasts = i == CurrentIndex;
            }
        }
        private void LateUpdate() { if (viewport != null && !Mathf.Approximately(lastWidth, viewport.rect.width)) Layout(); }
        private void OnDisable() { tween?.Kill(); tween = null; dragging = false; position = CurrentIndex; Layout(); }
    }
}
