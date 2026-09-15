using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Design tokens della nuova UI (mockup "definitivo"). I colori sono una stima da
    /// mockup usata come placeholder: gli sprite reali (9-slice, ribbon, ecc.) vanno
    /// assegnati qui quando l'export grafico e' pronto - i componenti che referenziano
    /// questo asset funzionano anche senza, con fallback su colore flat.
    /// </summary>
    [CreateAssetMenu(fileName = "UIV2Theme", menuName = "51 Cirulla/UIV2/Theme")]
    public class UIV2Theme : ScriptableObject
    {
        [Header("Panel")]
        public Color PanelBlue = HexColor("#16283C");
        public Color BorderGold = HexColor("#E8B24A");
        public Color RibbonGreen = HexColor("#2E7A5A");

        [Header("Buttons")]
        public Color ButtonPrimaryGold = HexColor("#E8B24A");
        public Color ButtonSecondaryBlue = HexColor("#2E4F6C");
        public Color ButtonTeal = HexColor("#1F7A72");
        public Color ButtonGreenSmall = HexColor("#3FA65C");

        [Header("Text")]
        public Color TextCream = HexColor("#FAF4E0");
        public Color TextMuted = HexColor("#8FA6BC");

        [Header("Sprites (opzionali - assegnare quando l'export dai mockup e' pronto)")]
        public Sprite PanelBackground;
        public Sprite PanelBorder;
        public Sprite RibbonSprite;
        public Sprite ButtonPrimarySprite;
        public Sprite ButtonSecondarySprite;
        public Sprite ButtonTealSprite;
        public Sprite ButtonGreenSmallSprite;
        public Sprite PillBackground;
        public Sprite AvatarFrame;
        public Sprite DefaultAvatarSilhouette;

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
