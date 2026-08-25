using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Applica lo stile "Dragon's Hoard" (panel_glass) a un pannello,
    /// con ombra opzionale (drop_shadow) come figlio dietro al pannello.
    /// </summary>
    public class ThemedPanel : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private UITheme theme;

        [Header("References")]
        [SerializeField] private Image background;

        [Header("Drop Shadow (opzionale)")]
        [SerializeField] private bool useDropShadow = false;
        [SerializeField] private RectTransform shadowInstance;
        [SerializeField] private Vector2 shadowOffset = new Vector2(0f, -8f);

        private void OnEnable()
        {
            Apply();
        }

        [ContextMenu("Apply Theme")]
        public void Apply()
        {
            if (theme == null)
            {
                Debug.LogWarning("[ThemedPanel] Nessun UITheme assegnato.", this);
                return;
            }

            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (background != null && theme.PanelGlass != null)
            {
                background.sprite = theme.PanelGlass;
                background.type = Image.Type.Sliced;
            }

            if (useDropShadow)
            {
                ApplyDropShadow();
            }
            else if (shadowInstance != null)
            {
                shadowInstance.gameObject.SetActive(false);
            }
        }

        private void ApplyDropShadow()
        {
            if (theme.DropShadow == null)
            {
                return;
            }

            if (shadowInstance == null)
            {
                var go = new GameObject("DropShadow", typeof(RectTransform), typeof(Image));
                shadowInstance = go.GetComponent<RectTransform>();
                shadowInstance.SetParent(transform, false);
            }

            shadowInstance.gameObject.SetActive(true);
            shadowInstance.SetAsFirstSibling(); // dietro al pannello nella gerarchia UI

            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                shadowInstance.anchorMin = rect.anchorMin;
                shadowInstance.anchorMax = rect.anchorMax;
                shadowInstance.pivot = rect.pivot;
                shadowInstance.sizeDelta = rect.sizeDelta;
            }
            shadowInstance.anchoredPosition = shadowOffset;

            var shadowImage = shadowInstance.GetComponent<Image>();
            shadowImage.sprite = theme.DropShadow;
            shadowImage.type = Image.Type.Sliced;
            shadowImage.raycastTarget = false;
        }
    }
}
