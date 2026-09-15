using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Chip riusabile per selettori tipo "Modalita'"/"Mazzo" sopra il CTA GIOCA: icona +
    /// small label fisso + valore corrente, con click esposto per aprire in futuro un
    /// picker. Small label/icona/colore di sfondo sono strutturali (una prefab per
    /// posizione, come i due colori del mockup) - solo il valore corrente e' dati.
    /// </summary>
    public class UIV2SelectorChip : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text smallLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private Button button;

        public Button Button => button;

        public void SetSmallLabel(string text)
        {
            if (smallLabel != null) smallLabel.text = text;
        }

        public void SetValue(string value, Sprite iconSprite = null)
        {
            if (valueLabel != null) valueLabel.text = value;
            if (icon != null && iconSprite != null) icon.sprite = iconSprite;
        }
    }
}
