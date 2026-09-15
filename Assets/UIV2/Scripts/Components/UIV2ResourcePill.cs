using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    public class UIV2ResourcePill : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private Button addButton;

        public Button AddButton => addButton;

        private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");

        public void SetResource(Sprite iconSprite, long amount)
        {
            // Icona opzionale: la moneta oro e' gia' cotta in bar_coin, quindi l'Image resta spenta
            // finche' un chiamante non passa un'icona per una valuta diversa.
            if (icon != null && iconSprite != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = true;
            }
            if (amountLabel != null) amountLabel.text = amount.ToString("N0", ItalianCulture);
        }
    }
}
