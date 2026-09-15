using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity
{
    /// <summary>
    /// Un elemento selezionabile (riga modalita' o pillola difficolta') dentro un
    /// SelectableToggleGroup: sa solo come mostrare il proprio stato selezionato/non
    /// selezionato (sprite di sfondo + eventuale check icon), non gestisce la mutua
    /// esclusione - quella la fa il group.
    /// </summary>
    public class SelectableToggleItem : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite unselectedSprite;
        [SerializeField] private GameObject checkIcon;
        [Tooltip("Opzionale: oggetto mostrato solo se selezionato, per un bordo/glow dietro l'elemento invece di (o in aggiunta a) uno swap di sprite - usato da DeckCell nel Panel Mazzo (feedback: un riempimento pieno 'sembra una macchia', meglio un bordo luminoso).")]
        [SerializeField] private GameObject glowObject;
        [Tooltip("Opzionale: oggetto mostrato SOLO se selezionato (es. testo 'In uso'), in aggiunta a checkIcon/glowObject - usato dalla pagina CARTE (DeckPageBuilder).")]
        [SerializeField] private GameObject equippedOnlyObject;
        [Tooltip("Opzionale: oggetto mostrato SOLO se NON selezionato (es. pulsante 'USA') - usato dalla pagina CARTE (DeckPageBuilder).")]
        [SerializeField] private GameObject availableOnlyObject;

        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                var sprite = selected ? selectedSprite : unselectedSprite;
                if (sprite != null)
                {
                    background.sprite = sprite;
                }
            }

            if (checkIcon != null)
            {
                checkIcon.SetActive(selected);
            }

            if (glowObject != null)
            {
                glowObject.SetActive(selected);
            }

            if (equippedOnlyObject != null)
            {
                equippedOnlyObject.SetActive(selected);
            }

            if (availableOnlyObject != null)
            {
                availableOnlyObject.SetActive(!selected);
            }
        }
    }
}
