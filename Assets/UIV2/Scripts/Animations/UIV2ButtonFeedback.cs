using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project51.UIV2.Animations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class UIV2ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, ISubmitHandler, ICancelHandler
    {
        private UnityEngine.UI.Button button;
        private RectTransform visual;
        private Vector3 restScale;
        private Tween motion;
        private bool initialized;
        private int? pointer;

        private void Initialize()
        {
            if (initialized) return;
            button = GetComponent<UnityEngine.UI.Button>();
            visual = (RectTransform)transform;
            restScale = visual.localScale;
            initialized = true;
        }

        public void OnPointerDown(PointerEventData e)
        {
            Initialize();
            if (e.button != PointerEventData.InputButton.Left || pointer.HasValue ||
                !isActiveAndEnabled || !button.IsInteractable()) return;
            pointer = e.pointerId;
            UIV2Motion.Cancel(ref motion);
            motion = visual.DOScale(restScale * UIV2Motion.PressScale, UIV2Motion.Press)
                .SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (pointer != e.pointerId) return;
            Release();
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (pointer == e.pointerId) ResetPresentation();
        }

        public void OnCancel(BaseEventData e) => ResetPresentation();

        public void OnSubmit(BaseEventData e)
        {
            Initialize();
            if (!isActiveAndEnabled || !button.IsInteractable()) return;
            UIV2Motion.Cancel(ref motion);
            motion = DOTween.Sequence().SetUpdate(true)
                .Append(visual.DOScale(restScale * UIV2Motion.PressScale, UIV2Motion.Press))
                .Append(visual.DOScale(restScale, UIV2Motion.Release).SetEase(Ease.OutBack, 1.1f));
        }

        private void Release()
        {
            pointer = null;
            UIV2Motion.Cancel(ref motion);
            if (!isActiveAndEnabled) { ResetPresentation(); return; }
            motion = visual.DOScale(restScale, UIV2Motion.Release).SetEase(Ease.OutBack, 1.1f).SetUpdate(true);
        }

        private void Update()
        {
            if (initialized && pointer.HasValue && !button.IsInteractable()) ResetPresentation();
        }

        private void ResetPresentation()
        {
            pointer = null;
            UIV2Motion.Cancel(ref motion);
            if (initialized && visual != null) visual.localScale = restScale;
        }

        private void OnDisable() => ResetPresentation();
        private void OnDestroy() => UIV2Motion.Cancel(ref motion);
    }
}