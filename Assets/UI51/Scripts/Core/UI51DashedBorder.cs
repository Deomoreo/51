using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51
{
    /// <summary>
    /// Bordo tratteggiato (border dashed del CSS, che UI51Shape non fa) di un rettangolo arrotondato, alla misura vera del rect:
    /// regge le card larghe "flex 1". Tratto dentro il rect come il bordo CSS; trattini e spazi si allungano un poco perche' il
    /// giro si chiuda pari. Raggio = meta' del lato corto = cerchio.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UI51DashedBorder : MaskableGraphic
    {
        [SerializeField] private float m_Radius = 16f;
        [SerializeField] private float m_Thickness = 1.5f;
        [SerializeField] private float m_Dash = 4.5f;
        [SerializeField] private float m_Gap = 3f;

        /// <summary>Come Chrome per "1.5px dashed": trattino 3x lo spessore, spazio 2x.</summary>
        public void Set(float radius, float thickness)
        {
            m_Radius = radius;
            m_Thickness = thickness;
            m_Dash = thickness * 3f;
            m_Gap = thickness * 2f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float half = m_Thickness * 0.5f;
            float w = r.width - m_Thickness, h = r.height - m_Thickness;
            if (w <= 0f || h <= 0f || m_Dash <= 0f) return;
            float rad = Mathf.Clamp(m_Radius - half, 0f, Mathf.Min(w, h) * 0.5f);
            float sx = w - 2f * rad, sy = h - 2f * rad, arc = Mathf.PI * 0.5f * rad;
            float total = 2f * sx + 2f * sy + 4f * arc;
            float period = m_Dash + m_Gap;
            int n = Mathf.Max(1, Mathf.RoundToInt(total / period));
            float k = total / (n * period);
            var path = new Path { origin = new Vector2(r.xMin + half, r.yMin + half), rad = rad, sx = sx, sy = sy, arc = arc };
            var c = (Color32)color;
            for (int i = 0; i < n; i++)
            {
                float a = i * period * k, b = a + m_Dash * k;
                int steps = Mathf.Max(1, Mathf.CeilToInt((b - a) / 1.5f));
                int first = vh.currentVertCount;
                for (int s = 0; s <= steps; s++)
                {
                    var p = path.At(Mathf.Lerp(a, b, s / (float)steps) % total, out var normal);
                    vh.AddVert(p + normal * half, c, Vector2.zero);
                    vh.AddVert(p - normal * half, c, Vector2.zero);
                    if (s == 0) continue;
                    int v = first + s * 2;
                    vh.AddTriangle(v - 2, v, v + 1);
                    vh.AddTriangle(v - 2, v + 1, v - 1);
                }
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        /// <summary>Giro orario dall'inizio del lato alto (dopo l'angolo in alto a sinistra): lati e quarti di cerchio.</summary>
        private struct Path
        {
            public Vector2 origin;
            public float rad, sx, sy, arc;

            public Vector2 At(float t, out Vector2 normal)
            {
                // Lati: alto, destra, basso, sinistra; ognuno seguito dal suo angolo (alto-destra, basso-destra, basso-sinistra, alto-sinistra).
                for (int side = 0; side < 4; side++)
                {
                    float len = side % 2 == 0 ? sx : sy;
                    if (t <= len) return origin + Edge(side, t, out normal);
                    t -= len;
                    if (t <= arc || side == 3) return origin + Corner(side, Mathf.Min(t, arc), out normal);
                    t -= arc;
                }
                normal = Vector2.up;
                return origin;
            }

            private Vector2 Edge(int side, float t, out Vector2 normal)
            {
                float w = sx + 2f * rad, h = sy + 2f * rad;
                switch (side)
                {
                    case 0: normal = Vector2.up; return new Vector2(rad + t, h);
                    case 1: normal = Vector2.right; return new Vector2(w, h - rad - t);
                    case 2: normal = Vector2.down; return new Vector2(w - rad - t, 0f);
                    default: normal = Vector2.left; return new Vector2(0f, rad + t);
                }
            }

            private Vector2 Corner(int side, float t, out Vector2 normal)
            {
                // Centri e angolo di partenza (gradi, antiorario da destra) degli angoli dopo ogni lato; si gira in senso orario.
                Vector2 center;
                float start;
                switch (side)
                {
                    case 0: center = new Vector2(rad + sx, rad + sy); start = 90f; break;
                    case 1: center = new Vector2(rad + sx, rad); start = 0f; break;
                    case 2: center = new Vector2(rad, rad); start = -90f; break;
                    default: center = new Vector2(rad, rad + sy); start = 180f; break;
                }
                float a = (start - (rad > 0f ? t / rad : 0f) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                normal = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                return center + normal * rad;
            }
        }
    }
}
