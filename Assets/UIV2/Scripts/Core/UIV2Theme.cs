using UnityEngine;
using TMPro;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// K2 shared palette, typography and sprites, applied at runtime by UIV2DesignSystem;
    /// all consumers share this single theme.
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

        [Header("K2 Typography (UI design units)")]
        public TMP_FontAsset TitleFont;
        public TMP_FontAsset SubtitleFont;
        public TMP_FontAsset BodyFont;
        public Material GoldLabelMaterial;
        public Color GoldLabelColor = HexColor("#FFFCF2");
        public float TitleSize = 40;
        public float SubtitleSize = 32;
        public float BodySize = 24;
        public float CaptionSize = 20;

        [Header("K2 Content panel / icon button")]
        public Sprite ContentBackground;
        public Sprite ContentBorder;
        public Sprite IconButtonBackground;

        public enum TextRole { Title, Subtitle, Body, Caption }
        public enum PanelKind { Modal, Content }

        public float TextSize(TextRole role)
        {
            switch (role)
            {
                case TextRole.Title: return TitleSize;
                case TextRole.Subtitle: return SubtitleSize;
                case TextRole.Caption: return CaptionSize;
                default: return BodySize;
            }
        }

        public void ApplyGoldLabel(TMP_Text label)
        {
            if (label == null) return;
            if (TitleFont != null) label.font = TitleFont;
            if (GoldLabelMaterial != null) label.fontSharedMaterial = GoldLabelMaterial;
            label.fontStyle = FontStyles.Normal;
            label.color = GoldLabelColor;
            label.enableVertexGradient = false;
        }

        public void ApplyTypography(TMP_Text text, TextRole role, bool preserveSize = false)
        {
            if (text == null) return;
            var font = role == TextRole.Title ? TitleFont : role == TextRole.Subtitle ? SubtitleFont : BodyFont;
            // Decorative outlines must keep their matching atlas and calibrated size.
            bool outlined = text.fontSharedMaterial != null && text.fontSharedMaterial.IsKeywordEnabled(ShaderUtilities.Keyword_Outline);
            if (!outlined && font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
                text.fontStyle &= ~FontStyles.Bold;
            }
            if (preserveSize) return;
            float size = TextSize(role);
            if (text.enableAutoSizing)
            {
                float ratio = text.fontSizeMax > 0 ? text.fontSizeMin / text.fontSizeMax : .6f;
                text.fontSizeMax = size;
                text.fontSizeMin = size * Mathf.Clamp(ratio, .5f, 1f);
            }
            text.fontSize = size;
        }

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
