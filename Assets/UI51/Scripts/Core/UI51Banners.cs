using System.Collections.Generic;
using Project51.Core;
using UnityEngine;

namespace Project51.UI51
{
    /// <summary>Banner del giocatore (SPEC §7). I primi tre sono gradienti fissi, gli ultimi tre animati via shader UI51/Banner.</summary>
    public enum BannerStyle { Notte, Smeraldo, Porpora, Aurora, Stellato, Oro }

    /// <summary>
    /// Applica un banner a un UI51Shape. Statici: solo gradiente sul colore dei vertici (nessun materiale).
    /// Animati: materiale UI51/Banner condiviso per stile, riempimento bianco (il colore dei vertici fa da tinta).
    /// Con grafica ridotta (GamePreferences.ReducedGraphics) il globale _UI51Still ferma le animazioni.
    /// </summary>
    public static class UI51Banners
    {
        public const string ShaderName = "UI51/Banner";
        static readonly int StillId = Shader.PropertyToID("_UI51Still");
        static readonly Dictionary<BannerStyle, Material> s_Shared = new Dictionary<BannerStyle, Material>();

        public static bool IsAnimated(BannerStyle style) => style >= BannerStyle.Aurora;

        public static Color BorderColor(BannerStyle style) =>
            style == BannerStyle.Oro ? UI51Tokens.Rgba(252, 226, 154, 0.8f) : UI51Tokens.GoldA(0.4f);

        /// <summary>Riempimento e angolo CSS dei banner statici (per gli animati: bianco, lo decide lo shader).</summary>
        public static Gradient StaticFill(BannerStyle style, out float angle)
        {
            switch (style)
            {
                case BannerStyle.Smeraldo:
                    angle = 135f;
                    return UI51Shape.Linear((UI51Tokens.Hex("#0E4F3A"), 0f), (UI51Tokens.Hex("#0A3A2B"), 0.55f), (UI51Tokens.Hex("#146A50"), 1f));
                case BannerStyle.Porpora:
                    angle = 135f;
                    return UI51Shape.Linear((UI51Tokens.Hex("#4A1420"), 0f), (UI51Tokens.Hex("#2E0B14"), 0.55f), (UI51Tokens.Hex("#5C1A2A"), 1f));
                case BannerStyle.Notte:
                    angle = 180f;
                    return UI51Shape.Linear((UI51Tokens.Rgba(12, 26, 50, 0.97f), 0f), (UI51Tokens.Rgba(6, 13, 27, 0.99f), 1f));
                default:
                    angle = 180f;
                    return UI51Shape.Solid(Color.white);
            }
        }

        /// <summary>Imposta riempimento, bordo 1px e materiale del banner. Raggio e ombre restano quelli del componente.</summary>
        public static void Apply(UI51Shape shape, BannerStyle style)
        {
            if (shape == null) return;
            var fill = StaticFill(style, out float angle);
            shape.Set(fill, angle, shape.radii, 1f, BorderColor(style), shape.shadows);
            shape.material = IsAnimated(style) ? SharedMaterial(style) : null;
        }

        /// <summary>Materiale condiviso (non salvato) per uno stile animato; null per gli statici.</summary>
        public static Material SharedMaterial(BannerStyle style)
        {
            if (!IsAnimated(style)) return null;
            if (s_Shared.TryGetValue(style, out var m) && m != null) return m;
            m = CreateMaterial(style);
            if (m != null) m.hideFlags = HideFlags.DontSave;
            s_Shared[style] = m;
            return m;
        }

        /// <summary>Nuovo materiale UI51/Banner configurato per lo stile (usato da SharedMaterial).</summary>
        public static Material CreateMaterial(BannerStyle style)
        {
            var shader = Resources.Load<Shader>("UI51/Banner");
            if (shader == null) shader = Shader.Find(ShaderName);
            if (shader == null) { Debug.LogWarning("[UI51] Shader UI51/Banner non trovato."); return null; }
            var m = new Material(shader) { name = "UI51 Banner " + style };
            Configure(m, style);
            return m;
        }

        /// <summary>Parametri dai mockup (SPEC §7, @keyframes bnAurora / bnStars / bnSheen).</summary>
        public static void Configure(Material m, BannerStyle style)
        {
            if (m == null) return;
            switch (style)
            {
                case BannerStyle.Aurora:
                    // linear-gradient(120deg, ...) background-size 320%, bnAurora 7s ease infinite.
                    SetColors(m, "#0B1D3A", "#1B6B8F", "#27B585", "#6B3FA0", "#1B6B8F", "#0B1D3A");
                    m.SetVector("_Stops0", new Vector4(0f, 0.2f, 0.4f, 0.6f));
                    m.SetVector("_Stops1", new Vector4(0.8f, 1f, 6f, 120f));
                    m.SetVector("_Anim", new Vector4(3.2f, 7f, 0f, 0f));
                    m.SetVector("_Sheen", new Vector4(3f, 0f, 0.45f, 20f));
                    m.SetVector("_Stars", Vector4.zero);
                    break;
                case BannerStyle.Stellato:
                    // Fondo 180deg #0A0F2A -> #1B1440 + 5 puntini (bnStars 5s ease-in-out alternate).
                    SetColors(m, "#0A0F2A", "#1B1440", "#1B1440", "#1B1440", "#1B1440", "#1B1440");
                    m.SetVector("_Stops0", new Vector4(0f, 1f, 1f, 1f));
                    m.SetVector("_Stops1", new Vector4(1f, 1f, 2f, 180f));
                    m.SetVector("_Anim", new Vector4(1f, 0f, 0f, 0f));
                    m.SetVector("_Sheen", new Vector4(3f, 0f, 0.45f, 20f));
                    m.SetVector("_Stars", new Vector4(5f, 5f, 0f, 0f));
                    // (x, y, raggio px, lato tile) | (spostamento x, y in px CSS, oro 0/1)
                    SetDot(m, 0, new Vector4(0.12f, 0.30f, 1.2f, 1.2f), new Vector4(6f, -3f, 0f, 0f));
                    SetDot(m, 1, new Vector4(0.34f, 0.70f, 1.6f, 1.3f), new Vector4(-8f, 4f, 1f, 0f));
                    SetDot(m, 2, new Vector4(0.58f, 0.25f, 1.0f, 1.1f), new Vector4(5f, 3f, 0f, 0f));
                    SetDot(m, 3, new Vector4(0.78f, 0.62f, 1.4f, 1.25f), new Vector4(-4f, -5f, 0f, 0f));
                    SetDot(m, 4, new Vector4(0.92f, 0.35f, 1.0f, 1.15f), new Vector4(7f, 2f, 1f, 0f));
                    break;
                case BannerStyle.Oro:
                    // 135deg #5A3A10 / #8A5A12 45% / #3A2508 + riflesso bnSheen 3s (larghezza 45%, skewX -20deg).
                    SetColors(m, "#5A3A10", "#8A5A12", "#3A2508", "#3A2508", "#3A2508", "#3A2508");
                    m.SetVector("_Stops0", new Vector4(0f, 0.45f, 1f, 1f));
                    m.SetVector("_Stops1", new Vector4(1f, 1f, 3f, 135f));
                    m.SetVector("_Anim", new Vector4(1f, 0f, 0f, 0f));
                    m.SetVector("_Sheen", new Vector4(3f, 0.5f, 0.45f, 20f));
                    m.SetColor("_SheenColor", UI51Tokens.Rgba(255, 236, 170, 1f));
                    m.SetVector("_Stars", Vector4.zero);
                    break;
            }
        }

        static void SetColors(Material m, params string[] hex)
        {
            for (int i = 0; i < 6; i++) m.SetColor("_C" + i, UI51Tokens.Hex(hex[Mathf.Min(i, hex.Length - 1)]));
        }

        static void SetDot(Material m, int i, Vector4 a, Vector4 b)
        {
            m.SetVector("_DotA" + i, a);
            m.SetVector("_DotB" + i, b);
        }

        /// <summary>Grafica ridotta = banner fermi (SPEC §6: niente loop decorativi).</summary>
        public static void SyncStill() => Shader.SetGlobalFloat(StillId, GamePreferences.ReducedGraphics ? 1f : 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            GamePreferences.Changed -= SyncStill;
            GamePreferences.Changed += SyncStill;
            SyncStill();
        }
    }
}
