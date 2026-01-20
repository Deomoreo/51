using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Layout per 2 carte UI (Image) al centro, in stile "fan".
    /// Mantiene centratura e proporzioni su qualsiasi risoluzione.
    /// 
    /// Requisiti UI:
    /// - `leftCard` e `rightCard` devono essere Image (o RectTransform con Image)
    /// - gli oggetti devono essere sotto lo stesso parent (tipicamente un container centrato)
    /// </summary>
    [ExecuteAlways]
    public sealed class HomeCenterCardsLayout : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform container;
        [SerializeField] private RectTransform leftCard;
        [SerializeField] private RectTransform rightCard;

        [Header("Sizing")]
        [Tooltip("Altezza delle carte come percentuale dell'altezza del container.")]
        [SerializeField, Range(0.1f, 1f)] private float cardHeightPercent = 0.42f;
        [Tooltip("Aspetto carta (width/height). Se usi sprite standard poker: ~0.7")]
        [SerializeField] private float cardAspect = 0.7f;

        [Header("Fan")]
        [SerializeField] private float horizontalSeparationPercent = 0.14f;
        [SerializeField] private float verticalOffsetPercent = 0.02f;
        [SerializeField] private float rotationDegrees = 10f;

        [Header("Depth")]
        [SerializeField] private bool rightOnTop = true;

        private Vector2Int _lastScreen;

        private void Reset()
        {
            container = transform as RectTransform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void LateUpdate()
        {
            var s = new Vector2Int(Screen.width, Screen.height);
            if (_lastScreen == s) return;
            _lastScreen = s;
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled) return;
            Apply();
        }

        public void Apply()
        {
            if (container == null) container = transform as RectTransform;
            if (container == null || leftCard == null || rightCard == null) return;

            Canvas.ForceUpdateCanvases();

            CenterNoStretch(container);
            SetupCard(leftCard);
            SetupCard(rightCard);

            float h = container.rect.height;
            float w = container.rect.width;

            float cardH = Mathf.Max(1f, h * cardHeightPercent);
            float cardW = Mathf.Max(1f, cardH * cardAspect);

            leftCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardH);
            leftCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardW);
            rightCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardH);
            rightCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardW);

            float sep = w * horizontalSeparationPercent;
            float yOff = h * verticalOffsetPercent;

            leftCard.anchoredPosition = new Vector2(-sep, -yOff);
            rightCard.anchoredPosition = new Vector2(sep, yOff);

            leftCard.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            rightCard.localRotation = Quaternion.Euler(0f, 0f, -rotationDegrees);

            if (rightOnTop)
            {
                leftCard.SetAsFirstSibling();
                rightCard.SetAsLastSibling();
            }
            else
            {
                rightCard.SetAsFirstSibling();
                leftCard.SetAsLastSibling();
            }
        }

        private static void CenterNoStretch(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetupCard(RectTransform rt)
        {
            CenterNoStretch(rt);
            rt.anchoredPosition = Vector2.zero;

            // Evita che l'Image catturi click se è solo decorazione.
            var img = rt.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }
    }
}
