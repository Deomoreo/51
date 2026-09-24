using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Project51.UIV2.Animations
{
    [DisallowMultipleComponent]
    public sealed class UIV2NumberCounter : MonoBehaviour
    {
        public TMP_Text Label;
        public string Prefix = "";
        public long DisplayedValue { get; private set; }
        public long TargetValue { get; private set; }
        private Tween motion;
        private bool initialized;
        private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");

        public void SetValue(long value, bool animate = true)
        {
            UIV2Motion.Cancel(ref motion);
            TargetValue = value;
            if (!initialized || !animate || !isActiveAndEnabled)
            {
                initialized = true;
                Render(value);
                return;
            }
            long from = DisplayedValue;
            if (from == value) { Render(value); return; }
            motion = DOVirtual.Float(0, 1, UIV2Motion.Count, t =>
                Render((long)decimal.Round(from + ((decimal)value - from) * (decimal)Mathf.Clamp01(t))))
                .SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() => Render(value));
        }

        public void AnimateFrom(long from, long to)
        {
            SetValue(from, false);
            SetValue(to);
        }

        private void Render(long value)
        {
            DisplayedValue = value;
            if (Label == null) Label = GetComponent<TMP_Text>();
            if (Label != null) Label.text = Prefix + value.ToString("N0", Italian);
        }

        private void OnDisable()
        {
            UIV2Motion.Cancel(ref motion);
            if (initialized) Render(TargetValue);
        }

        private void OnDestroy() => UIV2Motion.Cancel(ref motion);
    }
}