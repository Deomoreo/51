using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Valori del design system "Dragon's Hoard" (vedi
    /// Assets/UI/Sprites/DragonsHoard/51cirulla_design_system.xml, fonte di verita').
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "51 Cirulla/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Palette - Emerald")]
        public Color EmeraldHighlight = HexColor("#2E7A5A");
        public Color EmeraldMid = HexColor("#0D382A");
        public Color EmeraldDeep = HexColor("#051611");

        [Header("Palette - Gold")]
        public Color GoldLight = HexColor("#FAD682");
        public Color Gold = HexColor("#E8B24A");
        public Color GoldDark = HexColor("#966922");

        [Header("Palette - Testo & Bordi")]
        public Color Cream = HexColor("#FAF4E0");
        public Color TextMuted = HexColor("#C6D6C8");
        public Color GlassBorder = HexColor("#466E58");
        public Color GemAccent = HexColor("#46E2AA");

        [Header("Sprites - Assets/UI/Sprites/DragonsHoard")]
        public Sprite PanelGlass;
        public Sprite ButtonPrimary;
        public Sprite ButtonSecondary;
        public Sprite GlowSoft;
        public Sprite DropShadow;

        [Header("Corner Radius (px, riferimento 1x)")]
        public float CornerRadiusSmall = 10f;
        public float CornerRadiusMedium = 18f;
        public float CornerRadiusLarge = 28f;
        public float CornerRadiusPill = 9999f;

        [Header("Motion (durate in secondi)")]
        public float DurationMicro = 0.08f;
        public float DurationFast = 0.18f;
        public float DurationBase = 0.30f;
        public float DurationReflow = 0.25f;

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;
        }
    }
}
