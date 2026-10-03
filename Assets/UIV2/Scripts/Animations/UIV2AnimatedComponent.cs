using System;
using DG.Tweening;
using UnityEngine;

namespace Project51.UIV2.Animations
{
    public abstract class UIV2AnimatedComponent : MonoBehaviour
    {
        [SerializeField] protected RectTransform visualRoot;
        [SerializeField] protected CanvasGroup visualCanvasGroup;
        protected RectTransform VisualRoot => visualRoot != null ? visualRoot : (RectTransform)transform;
        private Tween motion;
        private Vector3 originalScale;
        private float originalAlpha;
        private bool initialized;
        private Vector3 RestScale => originalScale;

        protected virtual void Reset() => visualRoot = transform as RectTransform;

        private void Initialize()
        {
            if (initialized) return;
            originalScale = VisualRoot.localScale;
            originalAlpha = visualCanvasGroup != null ? visualCanvasGroup.alpha : 1f;
            initialized = true;
        }

        private Sequence Begin()
        {
            Initialize();
            UIV2Motion.Cancel(ref motion);
            var sequence = DOTween.Sequence().SetUpdate(true);
            motion = sequence;
            return sequence;
        }

        public virtual void PlayShow(Action onComplete = null)
        {
            var sequence = Begin();
            VisualRoot.localScale = RestScale * UIV2Motion.PanelScale;
            if (visualCanvasGroup != null)
            {
                visualCanvasGroup.alpha = 0;
                sequence.Join(visualCanvasGroup.DOFade(originalAlpha, UIV2Motion.Enter));
            }
            sequence.Join(VisualRoot.DOScale(RestScale, UIV2Motion.Enter).SetEase(Ease.OutBack, 1.1f));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        public virtual void PlayPress()
        {
            // A scene installer can add feedback after this component registered its click listener.
            if (!isActiveAndEnabled || GetComponent<UIV2ButtonFeedback>() != null) return;
            var sequence = Begin();
            sequence.Append(VisualRoot.DOScale(RestScale * UIV2Motion.PressScale, UIV2Motion.Press).SetEase(Ease.OutQuad));
            sequence.Append(VisualRoot.DOScale(RestScale, UIV2Motion.Release).SetEase(Ease.OutBack, 1.1f));
        }

        public virtual void PlayReward(Action onComplete = null)
        {
            var sequence = Begin();
            sequence.Append(VisualRoot.DOScale(RestScale * 1.12f, UIV2Motion.Press).SetEase(Ease.OutQuad));
            sequence.Append(VisualRoot.DOScale(RestScale, UIV2Motion.Release).SetEase(Ease.OutBack, 1.1f));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        protected virtual void OnDisable()
        {
            UIV2Motion.Cancel(ref motion);
            if (!initialized) return;
            if (VisualRoot != null) VisualRoot.localScale = RestScale;
            if (visualCanvasGroup != null) visualCanvasGroup.alpha = originalAlpha;
        }

        protected virtual void OnDestroy() => UIV2Motion.Cancel(ref motion);
    }
}
