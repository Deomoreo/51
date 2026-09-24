using DG.Tweening;
using UnityEngine;

namespace Project51.UIV2.Animations
{
    /// <summary>Local content fade. Routing, sorting and input remain owned by the page controller.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class UIV2PageEntrance : MonoBehaviour
    {
        private CanvasGroup group;
        private Tween motion;
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            group = GetComponent<CanvasGroup>();
            UIV2Motion.Cancel(ref motion);
            group.alpha = 0;
            motion = group.DOFade(1, UIV2Motion.Fade).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private void OnDisable()
        {
            UIV2Motion.Cancel(ref motion);
            if (group != null) group.alpha = 1;
        }

        private void OnDestroy() => UIV2Motion.Cancel(ref motion);
    }
}
