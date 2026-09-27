using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>Ombra stile CSS box-shadow (senza spread): offset x a destra / y in basso, sfumatura in unita' canvas.</summary>
    [Serializable]
    public struct UI51Shadow
    {
        public Vector2 offset;
        public float blur;
        public Color color;

        public UI51Shadow(float x, float y, float blur, Color color)
        {
            offset = new Vector2(x, y);
            this.blur = blur;
            this.color = color;
        }
    }

    /// <summary>
    /// Rettangolo arrotondato con gradiente lineare (angolo CSS), bordo interno e ombre morbide, disegnato via SDF
    /// da UI51/Shape (o UI51/Banner). Sostituisce 9-slice e sprite di pannelli/pulsanti/chip (SPEC §2-§3, §7).
    /// Dati per vertice documentati in UI51Shape.cginc.
    /// </summary>
    [AddComponentMenu("UI51/Shape")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UI51Shape : MaskableGraphic
    {
        const float Pad = 2f; // margine per l'antialias oltre il bordo
        const AdditionalCanvasShaderChannels Channels =
            AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;

        [SerializeField] Gradient m_Fill = new Gradient();
        [Tooltip("Angolo CSS: 180 = dall'alto in basso, 90 = da sinistra a destra.")]
        [SerializeField] float m_Angle = 180f;
        [Tooltip("Raggi in ordine CSS: alto-sx, alto-dx, basso-dx, basso-sx.")]
        [SerializeField] Vector4 m_Radii = new Vector4(16f, 16f, 16f, 16f);
        [SerializeField] float m_BorderWidth;
        [SerializeField] Color m_BorderColor = new Color(0.953f, 0.788f, 0.412f, 0.25f);
        [SerializeField] UI51Shadow[] m_Shadows = Array.Empty<UI51Shadow>();
        [Tooltip("Moltiplica il riempimento (feedback di pressione).")]
        [SerializeField] float m_Brightness = 1f;

        static Material s_DefaultMaterial;
        static readonly List<float> s_Breaks = new List<float>(20);
        static readonly List<Vector2> s_PolyA = new List<Vector2>(16);
        static readonly List<Vector2> s_PolyB = new List<Vector2>(16);

        public Gradient fill { get => m_Fill; set { m_Fill = value ?? new Gradient(); SetVerticesDirty(); } }
        public float angle { get => m_Angle; set { m_Angle = value; SetVerticesDirty(); } }
        public Vector4 radii { get => m_Radii; set { m_Radii = value; SetVerticesDirty(); } }
        public float radius { set => radii = new Vector4(value, value, value, value); }
        public float borderWidth { get => m_BorderWidth; set { m_BorderWidth = value; SetVerticesDirty(); } }
        public Color borderColor { get => m_BorderColor; set { m_BorderColor = value; SetVerticesDirty(); } }
        public UI51Shadow[] shadows { get => m_Shadows; set { m_Shadows = value ?? Array.Empty<UI51Shadow>(); SetVerticesDirty(); } }
        public float brightness { get => m_Brightness; set { if (m_Brightness == value) return; m_Brightness = value; SetVerticesDirty(); } }

        public override Material defaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
                    Shader shader = Resources.Load<Shader>("UI51/Shape");
                    if (shader == null) shader = Shader.Find("UI51/Shape");
                    if (shader == null) return base.defaultMaterial;
                    s_DefaultMaterial = new Material(shader) { name = "UI51 Shape (default)", hideFlags = HideFlags.DontSave };
                }
                return s_DefaultMaterial;
            }
        }

        /// <summary>Riempimento in tinta unita.</summary>
        public static Gradient Solid(Color c) => Linear((c, 0f), (c, 1f));

        /// <summary>Gradiente da coppie (colore, posizione 0-1); massimo 8 stop (limite di Unity).</summary>
        public static Gradient Linear(params (Color color, float time)[] stops)
        {
            int n = Mathf.Clamp(stops.Length, 1, 8);
            var colors = new GradientColorKey[n];
            var alphas = new GradientAlphaKey[n];
            for (int i = 0; i < n; i++)
            {
                (Color color, float time) s = i < stops.Length ? stops[i] : (Color.white, 0f);
                colors[i] = new GradientColorKey(s.color, s.time);
                alphas[i] = new GradientAlphaKey(s.color.a, s.time);
            }
            var g = new Gradient();
            g.SetKeys(colors, alphas);
            return g;
        }

        /// <summary>Imposta tutto in una volta (un solo rebuild).</summary>
        public void Set(Gradient fill, float angle, Vector4 radii, float borderWidth, Color borderColor, params UI51Shadow[] shadows)
        {
            m_Fill = fill ?? new Gradient();
            m_Angle = angle;
            m_Radii = radii;
            m_BorderWidth = borderWidth;
            m_BorderColor = borderColor;
            m_Shadows = shadows ?? Array.Empty<UI51Shadow>();
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureChannels();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            EnsureChannels();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            EnsureChannels();
        }

        void EnsureChannels()
        {
            Canvas c = canvas;
            if (c == null) return;
            if ((c.additionalShaderChannels & Channels) != Channels) c.additionalShaderChannels |= Channels;
            Canvas root = c.rootCanvas;
            if (root != null && (root.additionalShaderChannels & Channels) != Channels) root.additionalShaderChannels |= Channels;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            float hw = r.width * 0.5f, hh = r.height * 0.5f;
            if (hw <= 0f || hh <= 0f) return;
            Vector2 c = r.center;
            float maxR = Mathf.Min(hw, hh);
            var rad = new Vector4(
                Mathf.Clamp(m_Radii.y, 0f, maxR), Mathf.Clamp(m_Radii.z, 0f, maxR),
                Mathf.Clamp(m_Radii.x, 0f, maxR), Mathf.Clamp(m_Radii.w, 0f, maxR));
            Color tint = color;

            // Ombre sotto la forma: in CSS la prima e' in cima, quindi si disegnano dall'ultima.
            if (m_Shadows != null)
            {
                for (int i = m_Shadows.Length - 1; i >= 0; i--)
                {
                    UI51Shadow s = m_Shadows[i];
                    if (s.color.a <= 0f) continue;
                    Vector2 sc = c + new Vector2(s.offset.x, -s.offset.y);
                    float blur = Mathf.Max(s.blur, 0.5f);
                    float e = blur + Pad;
                    var col = new Color(s.color.r, s.color.g, s.color.b, tint.a);
                    var uv1 = new Vector4(hw, hh, blur, s.color.a);
                    var uv3 = new Vector4(c.x - sc.x, c.y - sc.y, 1f, 0f);
                    int start = vh.currentVertCount;
                    AddVert(vh, sc + new Vector2(-hw - e, -hh - e), sc, col, uv1, rad, uv3);
                    AddVert(vh, sc + new Vector2(-hw - e, hh + e), sc, col, uv1, rad, uv3);
                    AddVert(vh, sc + new Vector2(hw + e, hh + e), sc, col, uv1, rad, uv3);
                    AddVert(vh, sc + new Vector2(hw + e, -hh - e), sc, col, uv1, rad, uv3);
                    vh.AddTriangle(start, start + 1, start + 2);
                    vh.AddTriangle(start + 2, start + 3, start);
                }
            }

            // Riempimento: una fascia per ogni tratto lineare del gradiente, ritagliata sul rettangolo.
            Color32 bc = m_BorderColor;
            float packed = bc.r * 65536f + bc.g * 256f + bc.b;
            var fillUv3 = new Vector4(packed, m_BorderColor.a, 0f, 0f);
            float bw = Mathf.Clamp(m_BorderWidth, 0f, maxR);
            float theta = m_Angle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Sin(theta), Mathf.Cos(theta));
            float len = Mathf.Max(Mathf.Abs(r.width * dir.x) + Mathf.Abs(r.height * dir.y), 1e-3f);
            Gradient g = m_Fill ?? new Gradient();

            CollectBreaks(g);
            bool uniform = true;
            Color first = g.Evaluate(s_Breaks[0]);
            for (int i = 1; i < s_Breaks.Count && uniform; i++) uniform = g.Evaluate(s_Breaks[i]) == first;

            for (int band = 0; band <= s_Breaks.Count; band++)
            {
                s_PolyA.Clear();
                s_PolyA.Add(new Vector2(-hw - Pad, -hh - Pad));
                s_PolyA.Add(new Vector2(-hw - Pad, hh + Pad));
                s_PolyA.Add(new Vector2(hw + Pad, hh + Pad));
                s_PolyA.Add(new Vector2(hw + Pad, -hh - Pad));
                if (!uniform)
                {
                    if (band > 0) Clip(s_PolyA, s_PolyB, dir, (s_Breaks[band - 1] - 0.5f) * len);
                    else s_PolyB.Clear();
                    List<Vector2> poly = band > 0 ? s_PolyB : s_PolyA;
                    List<Vector2> other = band > 0 ? s_PolyA : s_PolyB;
                    if (band < s_Breaks.Count)
                    {
                        Clip(poly, other, -dir, -(s_Breaks[band] - 0.5f) * len);
                        poly = other;
                    }
                    EmitFill(vh, poly, c, dir, len, g, tint, hw, hh, bw, rad, fillUv3);
                }
                else
                {
                    EmitFill(vh, s_PolyA, c, dir, len, g, tint, hw, hh, bw, rad, fillUv3);
                    break;
                }
            }
        }

        static void CollectBreaks(Gradient g)
        {
            s_Breaks.Clear();
            s_Breaks.Add(0f);
            s_Breaks.Add(1f);
            foreach (GradientColorKey k in g.colorKeys) s_Breaks.Add(Mathf.Clamp01(k.time));
            foreach (GradientAlphaKey k in g.alphaKeys) s_Breaks.Add(Mathf.Clamp01(k.time));
            s_Breaks.Sort();
            for (int i = s_Breaks.Count - 1; i > 0; i--)
                if (s_Breaks[i] - s_Breaks[i - 1] < 1e-4f) s_Breaks.RemoveAt(i);
        }

        // Sutherland-Hodgman su un semipiano: tiene i punti con dot(p, n) >= offset.
        static void Clip(List<Vector2> input, List<Vector2> output, Vector2 n, float offset)
        {
            output.Clear();
            int count = input.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = input[i], b = input[(i + 1) % count];
                float da = Vector2.Dot(a, n) - offset, db = Vector2.Dot(b, n) - offset;
                if (da >= 0f) output.Add(a);
                if ((da >= 0f) != (db >= 0f)) output.Add(Vector2.Lerp(a, b, da / (da - db)));
            }
        }

        void EmitFill(VertexHelper vh, List<Vector2> poly, Vector2 c, Vector2 dir, float len, Gradient g, Color tint,
            float hw, float hh, float bw, Vector4 rad, Vector4 uv3)
        {
            if (poly.Count < 3) return;
            int start = vh.currentVertCount;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 q = poly[i];
                Color f = g.Evaluate(Mathf.Clamp01(Vector2.Dot(q, dir) / len + 0.5f));
                var col = new Color(
                    Mathf.Clamp01(f.r * tint.r * m_Brightness),
                    Mathf.Clamp01(f.g * tint.g * m_Brightness),
                    Mathf.Clamp01(f.b * tint.b * m_Brightness),
                    tint.a);
                AddVert(vh, c + q, c, col, new Vector4(hw, hh, bw, f.a), rad, uv3);
            }
            for (int i = 1; i < poly.Count - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }

        static void AddVert(VertexHelper vh, Vector2 pos, Vector2 centre, Color col, Vector4 uv1, Vector4 uv2, Vector4 uv3)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = new Vector3(pos.x, pos.y, 0f);
            v.color = col;
            v.uv0 = new Vector4(pos.x - centre.x, pos.y - centre.y, 0f, 0f);
            v.uv1 = uv1;
            v.uv2 = uv2;
            v.uv3 = uv3;
            vh.AddVert(v);
        }
    }
}
