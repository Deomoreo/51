using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Barra di progresso data-driven. Track e Fill sono ENTRAMBI Image.Type.Sliced (mai
    /// Filled: Filled ignora il 9-slice e ritaglia la texture intera, rovinando i tappi
    /// arrotondati man mano che la barra si riempie). Il progresso e' espresso
    /// ridimensionando il RectTransform del Fill (anchorMax.x, ancorato a sinistra, dentro
    /// FillArea che rappresenta lo spazio disponibile del track) invece che con fillAmount:
    /// niente localScale, nessun ricalcolo manuale su resize/LayoutGroup perche' gli anchor
    /// sono gia' frazioni della larghezza disponibile del genitore.
    /// </summary>
    public class UIV2ProgressBar : MonoBehaviour, IUIV2ProgressAnimatable
    {
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private TMP_Text valueLabel;
        // {0} = current, {1} = max. Energia Home: "{0}", XP Home: "{0} / {1}".
        [SerializeField] private string labelFormat = "{0}/{1}";

        private Tweener _fillTweener;

        public float NormalizedValue => fillRect != null ? fillRect.anchorMax.x : 0f;

        public void SetProgress(int current, int max, bool animate = true)
        {
            float normalized = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            SetProgress(normalized, animate);
            if (valueLabel != null) valueLabel.text = string.Format(labelFormat, current, max);
        }

        public void SetProgress(float normalizedValue, bool animate = true)
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            if (fillRect == null) return;

            float from = fillRect.anchorMax.x;
            if (!animate)
            {
                SetFillFraction(normalizedValue);
                return;
            }

            _fillTweener?.Kill();
            _fillTweener = DOTween.To(() => fillRect.anchorMax.x, SetFillFraction, normalizedValue, 0.25f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

            PlayProgressChanged(from, normalizedValue);
        }

        private void SetFillFraction(float x)
        {
            var anchorMax = fillRect.anchorMax;
            anchorMax.x = x;
            fillRect.anchorMax = anchorMax;
        }

        public void PlayProgressChanged(float fromNormalized, float toNormalized)
        {
            // Hook per feedback extra (flash/particellare) da estendere con l'arte definitiva.
        }
    }
}
