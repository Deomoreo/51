using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Applica lo stile "Dragon's Hoard" a un pulsante. Non tutti i pulsanti dell'app
    /// hanno la stessa forma, quindi la variante determina sprite/testo/icona da usare
    /// (vedi Components/Button nel design system per Primary/Secondary).
    /// </summary>
    public class ThemedButton : MonoBehaviour
    {
        public enum Variant
        {
            Primary,
            Secondary,
            ListRow,
            IconTab
        }

        [Header("Theme")]
        [SerializeField] private UITheme theme;
        [SerializeField] private Variant variant = Variant.Primary;

        [Header("References")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;

        [Header("Icon Tab")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image glowImage;
        public bool IsActive;

        private void OnEnable()
        {
            Apply();
        }

        [ContextMenu("Apply Theme")]
        public void Apply()
        {
            if (theme == null)
            {
                Debug.LogWarning("[ThemedButton] Nessun UITheme assegnato.", this);
                return;
            }

            if (background == null)
            {
                background = GetComponent<Image>();
            }
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }

            switch (variant)
            {
                case Variant.Primary:
                case Variant.Secondary:
                    ApplyPill();
                    break;
                case Variant.ListRow:
                    ApplyListRow();
                    break;
                case Variant.IconTab:
                    ApplyIconTab();
                    break;
            }
        }

        private void ApplyPill()
        {
            Sprite sprite = variant == Variant.Primary ? theme.ButtonPrimary : theme.ButtonSecondary;
            Color textColor = variant == Variant.Primary ? theme.EmeraldDeep : theme.Cream;

            if (background != null && sprite != null)
            {
                background.sprite = sprite;
                background.type = Image.Type.Simple;
            }

            if (label != null)
            {
                label.color = textColor;
                label.alignment = TextAlignmentOptions.Midline;
            }
        }

        private void ApplyListRow()
        {
            if (background != null && theme.PanelGlass != null)
            {
                background.sprite = theme.PanelGlass;
                background.type = Image.Type.Simple;
            }

            if (label != null)
            {
                label.color = theme.Cream;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        private void ApplyIconTab()
        {
            if (iconImage == null)
            {
                iconImage = FindIconImage();
            }

            if (iconImage != null)
            {
                iconImage.color = IsActive ? theme.Gold : theme.TextMuted;
            }

            ApplyIconGlow();
        }

        private void ApplyIconGlow()
        {
            if (!IsActive)
            {
                if (glowImage != null)
                {
                    glowImage.gameObject.SetActive(false);
                }
                return;
            }

            if (theme.GlowSoft == null)
            {
                return;
            }

            if (glowImage == null)
            {
                var go = new GameObject("IconGlow", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(transform, false);
                rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                glowImage = go.GetComponent<Image>();
                glowImage.raycastTarget = false;
            }

            glowImage.gameObject.SetActive(true);
            glowImage.sprite = theme.GlowSoft;
            Color glowColor = theme.Gold;
            glowColor.a = 0.35f;
            glowImage.color = glowColor;
        }

        /// <summary>
        /// L'icona non ha un nome/percorso fisso nei prefab esistenti: cerchiamo prima
        /// per tag "icon" (se registrato in Project Settings), poi il primo Image figlio
        /// diverso dallo sfondo.
        /// </summary>
        private Image FindIconImage()
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (HasTag(img.gameObject, "icon"))
                {
                    return img;
                }
            }

            foreach (var img in images)
            {
                if (img != background)
                {
                    return img;
                }
            }

            return null;
        }

        private static bool HasTag(GameObject go, string tag)
        {
            try
            {
                return go.CompareTag(tag);
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
