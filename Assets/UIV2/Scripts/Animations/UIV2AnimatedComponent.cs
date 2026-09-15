using System;
using DG.Tweening;
using UnityEngine;

namespace Project51.UIV2.Animations
{
    /// <summary>
    /// Base per componenti UIV2 animabili. Le animazioni agiscono sempre su "visualRoot"
    /// (un child dedicato, di norma lo stesso GameObject se non diversamente assegnato),
    /// mai sul RectTransform controllato da un LayoutGroup esterno - cosi' Show/Hide/Press/
    /// ecc. non alterano mai la struttura del layout che le contiene.
    /// </summary>
    public abstract class UIV2AnimatedComponent : MonoBehaviour,
        IUIV2Showable, IUIV2Selectable, IUIV2Pressable, IUIV2Unlockable, IUIV2Rewardable
    {
        [SerializeField] protected RectTransform visualRoot;
        [SerializeField] protected CanvasGroup visualCanvasGroup;

        protected RectTransform VisualRoot => visualRoot != null ? visualRoot : (RectTransform)transform;

        protected virtual void Reset()
        {
            visualRoot = transform as RectTransform;
        }

        public virtual void PlayShow(Action onComplete = null)
        {
            var t = VisualRoot;
            t.DOKill();
            t.localScale = Vector3.one * 0.92f;
            if (visualCanvasGroup != null)
            {
                visualCanvasGroup.DOKill();
                visualCanvasGroup.alpha = 0f;
                visualCanvasGroup.DOFade(1f, 0.18f).SetUpdate(true);
            }
            t.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public virtual void PlayHide(Action onComplete = null)
        {
            var t = VisualRoot;
            t.DOKill();
            if (visualCanvasGroup != null)
            {
                visualCanvasGroup.DOKill();
                visualCanvasGroup.DOFade(0f, 0.15f).SetUpdate(true);
            }
            t.DOScale(0.92f, 0.15f).SetEase(Ease.InCubic).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public virtual void PlayPress()
        {
            var t = VisualRoot;
            t.DOKill();
            t.localScale = Vector3.one;
            t.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.18f, 6, 0.7f).SetUpdate(true);
        }

        public virtual void PlaySelected(bool selected)
        {
            var t = VisualRoot;
            t.DOKill();
            t.DOScale(selected ? 1.05f : 1f, 0.15f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public virtual void PlayUnlock(Action onComplete = null)
        {
            var t = VisualRoot;
            t.DOKill();
            t.localScale = Vector3.zero;
            t.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public virtual void PlayReward(Action onComplete = null)
        {
            var t = VisualRoot;
            t.DOKill();
            var seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(t.DOScale(1.15f, 0.12f).SetEase(Ease.OutQuad));
            seq.Append(t.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
            seq.OnComplete(() => onComplete?.Invoke());
        }
    }
}
