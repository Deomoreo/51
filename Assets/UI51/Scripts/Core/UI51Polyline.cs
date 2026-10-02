using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Tratto SVG (stroke-linecap round): spezzata di spessore costante con estremi tondi, giunti a spigolo (niente sovrapposizioni,
    /// quindi anche semitrasparente resta uniforme). Punti nello spazio del viewBox, origine in alto a sinistra e y verso il basso
    /// come nell'SVG del mockup; il viewBox si adatta al rect. Le curve si campionano nel builder (Quad, Arc).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UI51Polyline : MaskableGraphic
    {
        [SerializeField] private Vector2[] m_Points = new Vector2[0];
        [SerializeField] private Vector2 m_ViewBox = new Vector2(100f, 100f);
        [SerializeField] private float m_Thickness = 2f;

        const int CapSegments = 8;

        public void Set(Vector2 viewBox, float thickness, params Vector2[] points)
        {
            m_ViewBox = viewBox;
            m_Thickness = thickness;
            m_Points = points ?? new Vector2[0];
            SetVerticesDirty();
        }

        /// <summary>Curva quadratica SVG "M p0 Q c p1" in n segmenti.</summary>
        public static Vector2[] Quad(Vector2 p0, Vector2 c, Vector2 p1, int n = 16)
        {
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, u = 1f - t;
                pts[i] = u * u * p0 + 2f * u * t * c + t * t * p1;
            }
            return pts;
        }

        /// <summary>Arco di cerchio (gradi SVG: 0 = destra, 90 = in basso, senso orario) in n segmenti.</summary>
        public static Vector2[] Arc(Vector2 center, float radius, float fromDeg, float toDeg, int n = 24)
        {
            var pts = new Vector2[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)n) * Mathf.Deg2Rad;
                pts[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            return pts;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = m_Points != null ? m_Points.Length : 0;
            if (n < 2 || m_ViewBox.x <= 0f || m_ViewBox.y <= 0f) return;
            var r = rectTransform.rect;
            var scale = new Vector2(r.width / m_ViewBox.x, r.height / m_ViewBox.y);
            float half = m_Thickness * 0.5f * Mathf.Min(scale.x, scale.y);
            var p = new Vector2[n];
            for (int i = 0; i < n; i++) p[i] = new Vector2(r.xMin + m_Points[i].x * scale.x, r.yMax - m_Points[i].y * scale.y);

            var c = (Color32)color;
            for (int i = 0; i < n; i++)
            {
                var into = i > 0 ? (p[i] - p[i - 1]).normalized : (p[1] - p[0]).normalized;
                var outOf = i < n - 1 ? (p[i + 1] - p[i]).normalized : into;
                var dir = (into + outOf).normalized;
                if (dir == Vector2.zero) dir = into;
                var normal = new Vector2(-dir.y, dir.x);
                // Giunto a spigolo: allunga la normale perche' i lati restino a distanza half dai due segmenti.
                float d = Vector2.Dot(normal, new Vector2(-into.y, into.x));
                float len = d > 0.25f ? half / d : half;
                vh.AddVert(p[i] + normal * len, c, Vector2.zero);
                vh.AddVert(p[i] - normal * len, c, Vector2.zero);
                if (i > 0)
                {
                    int b = i * 2;
                    vh.AddTriangle(b - 2, b, b + 1);
                    vh.AddTriangle(b - 2, b + 1, b - 1);
                }
            }
            Cap(vh, p[0], (p[0] - p[1]).normalized, half, c);
            Cap(vh, p[n - 1], (p[n - 1] - p[n - 2]).normalized, half, c);
        }

        /// <summary>Mezzo disco verso l'esterno (outward) centrato sull'estremo.</summary>
        static void Cap(VertexHelper vh, Vector2 at, Vector2 outward, float half, Color32 c)
        {
            int center = vh.currentVertCount;
            vh.AddVert(at, c, Vector2.zero);
            var normal = new Vector2(-outward.y, outward.x);
            for (int i = 0; i <= CapSegments; i++)
            {
                float a = Mathf.PI * i / CapSegments;
                vh.AddVert(at + (normal * Mathf.Cos(a) + outward * Mathf.Sin(a)) * half, c, Vector2.zero);
                if (i > 0) vh.AddTriangle(center, center + i, center + i + 1);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }
    }
}
