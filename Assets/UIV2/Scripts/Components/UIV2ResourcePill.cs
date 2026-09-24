using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    public class UIV2ResourcePill : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private Button addButton;

        public Button AddButton => addButton;

        private UIV2NumberCounter counter;

        public void SetResource(Sprite iconSprite, long amount)
        {
            // Icona opzionale: la moneta oro e' gia' cotta in bar_coin, quindi l'Image resta spenta
            // finche' un chiamante non passa un'icona per una valuta diversa.
            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }
            if (amountLabel == null) return;
            if (counter == null)
            {
                counter = amountLabel.GetComponent<UIV2NumberCounter>();
                if (counter == null) counter = amountLabel.gameObject.AddComponent<UIV2NumberCounter>();
                counter.Label = amountLabel;
            }
            counter.SetValue(amount);
        }
    }
}
