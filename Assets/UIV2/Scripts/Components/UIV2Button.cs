using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Core;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Un solo componente per le 4 varianti bottone dei mockup (Primary Gold / Secondary
    /// Blue / Teal / Green Small) - stesso schema del ThemedButton legacy: lo stile decide
    /// solo sprite/colore, non la struttura. UIV2_PrimaryGoldButton ecc. sono prefab
    /// distinti che impostano "style" in modo diverso, non 4 script separati.
    /// </summary>
    public class UIV2Button : UIV2AnimatedComponent
    {
        public enum Style
        {
            PrimaryGold,
            SecondaryBlue,
            Teal,
            GreenSmall
        }

        [Header("Theme")]
        [SerializeField] private UIV2Theme theme;
        [SerializeField] private Style style = Style.PrimaryGold;

        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        // true = il colore/materiale della label e' impostato per istanza (es. GIOCA crema con
        // outline marrone) e Apply() non deve sovrascriverlo col colore dello stile.
        [SerializeField] private bool keepLabelColor;

        public Button Button => button;

        private void OnEnable()
        {
            Apply();
            if (button != null) button.onClick.AddListener(PlayPress);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(PlayPress);
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        [ContextMenu("Apply Theme")]
        public void Apply()
        {
            if (theme == null) return;

            Sprite sprite;
            Color flatColor;
            Color textColor = theme.TextCream;

            switch (style)
            {
                case Style.SecondaryBlue:
                    sprite = theme.ButtonSecondarySprite;
                    flatColor = theme.ButtonSecondaryBlue;
                    break;
                case Style.Teal:
                    sprite = theme.ButtonTealSprite;
                    flatColor = theme.ButtonTeal;
                    break;
                case Style.GreenSmall:
                    sprite = theme.ButtonGreenSmallSprite;
                    flatColor = theme.ButtonGreenSmall;
                    break;
                default:
                    sprite = theme.ButtonPrimarySprite;
                    flatColor = theme.ButtonPrimaryGold;
                    textColor = theme.PanelBlue;
                    break;
            }

            if (background != null)
            {
                if (sprite != null)
                {
                    background.sprite = sprite;
                    background.type = Image.Type.Sliced;
                    background.color = Color.white;
                }
                else
                {
                    background.color = flatColor;
                }
            }

            if (label != null && !keepLabelColor) label.color = textColor;
        }
    }
}
