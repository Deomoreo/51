using System;
using DG.Tweening;
using UnityEngine;

namespace Project51.UIV2.Animations
{
    /// <summary>Attach to a decorative icon. Presentation only: never changes balances.</summary>
    [DisallowMultipleComponent]
    public sealed class UIV2RewardFlight : MonoBehaviour
    {
        private Tween motion;
        private RectTransform destination;
        private Vector3 origin;
        private Vector3 scale;
        private bool captured;

        public void Play(RectTransform target, Action arrived = null)
        {
            ResetFlight();
            if (target == null || !target.gameObject.activeInHierarchy || !isActiveAndEnabled) return;
            destination = target;
            origin = transform.localPosition;
            scale = transform.localScale;
            captured = true;
            var start = transform.position;
            var arc = transform.parent != null ? transform.parent.TransformVector(Vector3.up * 60) : Vector3.up * 60;
            motion = DOVirtual.Float(0, 1, UIV2Motion.Flight, t =>
            {
                if (destination == null || !destination.gameObject.activeInHierarchy) { Cancel(); return; }
                transform.position = Vector3.Lerp(start, destination.position, t) + arc * Mathf.Sin(t * Mathf.PI);
                transform.localScale = scale * Mathf.Lerp(1, .45f, t);
            }).SetEase(Ease.InOutQuad).SetUpdate(true).OnComplete(() =>
            {
                bool reached = destination != null && destination.gameObject.activeInHierarchy;
                motion = null;
                gameObject.SetActive(false);
                if (reached) arrived?.Invoke();
            });
        }

        public void Cancel()
        {
            ResetFlight();
            gameObject.SetActive(false);
        }

        private void ResetFlight()
        {
            UIV2Motion.Cancel(ref motion);
            destination = null;
            if (!captured) return;
            transform.localPosition = origin;
            transform.localScale = scale;
            captured = false;
        }

        private void OnDisable() => ResetFlight();
        private void OnDestroy() => UIV2Motion.Cancel(ref motion);
    }
}
