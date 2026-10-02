using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>Font del design (SPEC §2): Cinzel per titoli/numeri/CTA, Nunito per il resto.</summary>
    public enum FontFace { CinzelSemiBold, CinzelBold, NunitoRegular, NunitoSemiBold, NunitoBold, NunitoExtraBold }

    /// <summary>Token di stile della SPEC §1-2: colori, raggi, gradienti, ombre, font. Spazio colore Gamma: i valori CSS valgono tali e quali.</summary>
    public static class UI51Tokens
    {
        // --- Canvas (SPEC §1)
        public static readonly Vector2 ReferenceResolution = new Vector2(390f, 844f);
        public const float MatchWidthOrHeight = 0.5f;

        // --- Colori (SPEC §2)
        public static readonly Color Gold = Hex("#F3C969");
        public static readonly Color GoldLight = Hex("#FCE29A");
        public static readonly Color GoldDark = Hex("#C4922F");
        public static readonly Color OnGold = Hex("#25160A");
        public static readonly Color Cream = Hex("#F5E9D0");
        public static readonly Color CreamMuted = Rgba(245, 233, 208, 0.55f);
        public static readonly Color Danger = Hex("#E5484D");
        public static readonly Color DangerText = Hex("#F08A8D");
        public static readonly Color Success = Hex("#27B585");
        public static readonly Color SuccessText = Hex("#7FE0B8");
        public static readonly Color TeamBlue = Hex("#4F80E8");
        public static readonly Color TeamBlueDark = Hex("#1B3A7A");
        public static readonly Color TeamBlueText = Hex("#9DB8F5");
        public static readonly Color Navy = Hex("#0B1D3A");
        public static readonly Color Wood = Hex("#7A4B29");
        public static readonly Color WoodDark = Hex("#4E2D16");
        public static readonly Color WoodMid = Hex("#6B4122");
        public static readonly Color Scrim = Rgba(3, 7, 16, 0.5f);
        public static readonly Color NavBar = Rgba(5, 11, 23, 0.96f);
        public static readonly Color BadgeRing = Hex("#0B1626"); // bordo scuro di pallini e "+N"
        public static readonly Color[] Felt = { Hex("#1F8460"), Hex("#146346"), Hex("#0C4532"), Hex("#093826") };

        public static Color GoldA(float a) => WithAlpha(Gold, a);
        public static Color CreamA(float a) => WithAlpha(Cream, a);
        public static Color WhiteA(float a) => new Color(1f, 1f, 1f, a);
        public static Color BlackA(float a) => new Color(0f, 0f, 0f, a);

        // Bordi oro ricorrenti (0.25 pannelli, 0.35 tondi piccoli, 0.45 bottoni).
        public static readonly Color BorderGoldSoft = GoldA(0.25f);
        public static readonly Color BorderGold = GoldA(0.35f);
        public static readonly Color BorderGoldStrong = GoldA(0.45f);

        // --- Raggi (SPEC §2)
        public const float RadiusPill = 25f;
        public const float RadiusPanel = 16f;
        public const float RadiusCard = 16f;
        public const float RadiusButton = 16f;
        public const float RadiusSheet = 24f;
        public const float RadiusChip = 8f;
        public const float RadiusTabs = 12f;
        public const float RadiusDialog = 22f;
        public const float RoundButtonSize = 40f;

        /// <summary>Raggi in ordine CSS (alto-sx, alto-dx, basso-dx, basso-sx).</summary>
        public static Vector4 Radii(float r) => new Vector4(r, r, r, r);
        public static Vector4 RadiiTop(float r) => new Vector4(r, r, 0f, 0f);

        // --- Gradienti (angoli in gradi CSS: 180 = dall'alto al basso)
        public static Gradient PanelFill() => UI51Shape.Linear((Rgba(10, 22, 44, 0.82f), 0f), (Rgba(6, 13, 27, 0.90f), 1f));
        public static Gradient SheetFill() => UI51Shape.Linear((Rgba(12, 26, 50, 0.97f), 0f), (Rgba(6, 13, 27, 0.99f), 1f));
        public static Gradient DialogFill() => UI51Shape.Linear((Rgba(14, 28, 52, 0.98f), 0f), (Rgba(7, 14, 28, 0.99f), 1f));
        public static Gradient GoldButtonFill() => UI51Shape.Linear((GoldLight, 0f), (Gold, 0.55f), (GoldDark, 1f));
        public static Gradient TeamBlueFill() => UI51Shape.Linear((TeamBlue, 0f), (TeamBlueDark, 1f));
        public static Gradient RoundButtonFill() => UI51Shape.Solid(Rgba(11, 29, 58, 0.6f));
        public static readonly Color GoldButtonBorder = WhiteA(0.5f);

        // --- Ombre (SPEC §2: 0 6-10 14-30 rgba(0,0,0,.35-.5))
        public static readonly UI51Shadow ShadowSm = new UI51Shadow(0f, 6f, 14f, BlackA(0.35f));
        public static readonly UI51Shadow ShadowMd = new UI51Shadow(0f, 8f, 22f, BlackA(0.4f));
        public static readonly UI51Shadow ShadowLg = new UI51Shadow(0f, 10f, 30f, BlackA(0.5f));
        public static readonly UI51Shadow ShadowDialog = new UI51Shadow(0f, 20f, 50f, BlackA(0.6f));

        // --- Font
        static readonly Dictionary<FontFace, TMP_FontAsset> s_Fonts = new Dictionary<FontFace, TMP_FontAsset>();

        /// <summary>Nome dell'asset (senza " SDF") = nome del .ttf in Assets/UI51/Fonts.</summary>
        public static string FontName(FontFace face)
        {
            switch (face)
            {
                case FontFace.CinzelSemiBold: return "Cinzel-SemiBold";
                case FontFace.CinzelBold: return "Cinzel-Bold";
                case FontFace.NunitoRegular: return "Nunito-Regular";
                case FontFace.NunitoSemiBold: return "Nunito-SemiBold";
                case FontFace.NunitoBold: return "Nunito-Bold";
                default: return "Nunito-ExtraBold";
            }
        }

        public static TMP_FontAsset Font(FontFace face)
        {
            if (s_Fonts.TryGetValue(face, out var f) && f != null) return f;
            f = Resources.Load<TMP_FontAsset>("UI51/Fonts/" + FontName(face) + " SDF");
            if (f == null) Debug.LogWarning($"[UI51] Font {FontName(face)} SDF mancante: esegui Tools/UI51/Font Assets.");
            s_Fonts[face] = f;
            return f;
        }

        /// <summary>Applica font, corpo (px CSS), colore e letter-spacing (px CSS) a un testo TMP.</summary>
        public static TMP_Text Style(TMP_Text text, FontFace face, float size, Color color, float letterSpacingPx = 0f)
        {
            if (text == null) return null;
            var font = Font(face);
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.characterSpacing = LetterSpacing(letterSpacingPx, size);
            text.enableWordWrapping = false;
            text.richText = true;
            return text;
        }

        /// <summary>TMP misura la spaziatura in centesimi di em: px CSS / corpo * 100.</summary>
        public static float LetterSpacing(float px, float fontSize) => fontSize > 0f ? px / fontSize * 100f : 0f;

        // --- Helper colore
        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = alpha;
            return c;
        }

        public static Color Rgba(int r, int g, int b, float a) => new Color(r / 255f, g / 255f, b / 255f, a);
        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
    }
}
