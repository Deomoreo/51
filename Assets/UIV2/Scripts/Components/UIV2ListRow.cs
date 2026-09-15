using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Riga generica riusabile per liste data-driven (Posta, Premi, Negozio, ecc.): icona +
    /// titolo + sottotitolo + uno slot "trailing" dove il chiamante puo' instanziare un
    /// bottone/pill specifico (Riscuoti, Prezzo, ...) senza che questo componente conosca
    /// quel contenuto.
    /// </summary>
    public class UIV2ListRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private RectTransform trailingSlot;

        public RectTransform TrailingSlot => trailingSlot;

        public void Bind(string title, string subtitle, Sprite iconSprite)
        {
            if (titleLabel != null) titleLabel.text = title;
            if (subtitleLabel != null)
            {
                subtitleLabel.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
                subtitleLabel.text = subtitle;
            }
            if (icon != null)
            {
                icon.gameObject.SetActive(iconSprite != null);
                icon.sprite = iconSprite;
            }
        }
    }
}
