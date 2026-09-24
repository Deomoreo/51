using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Core;
using Project51.UIV2.Animations;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Primary, secondary, flat and icon buttons. Teal/GreenSmall keep their serialized
    /// values for compatibility with existing selection controls.
    /// </summary>
    public class UIV2Button : UIV2AnimatedComponent
    {
        public enum Style
        {
            PrimaryGold = 0,
            SecondaryBlue = 1,
            Teal = 2,
            GreenSmall = 3,
            Flat = 4,
            Icon = 5
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
            if (button != null && GetComponent<UIV2ButtonFeedback>() == null) button.onClick.AddListener(PlayPress);
        }

        protected override void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(PlayPress);
            base.OnDisable();
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
                case Style.Flat:
                    sprite = null;
                    flatColor = Color.clear;
                    break;
                case Style.Icon:
                    sprite = theme.IconButtonBackground;
                    flatColor = theme.ButtonSecondaryBlue;
                    break;
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
                    textColor = theme.GoldLabelColor;
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
                    background.sprite = null;
                    background.color = flatColor;
                }
            }

            if (style == Style.PrimaryGold) theme.ApplyGoldLabel(label);
            else if (label != null && !keepLabelColor) label.color = textColor;
        }
    }
}
