using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// Anello di attesa (mockup ricerca partita / sala d'attesa): traccia scura completa e arco
    /// colorato che sfuma verso la coda. Disegnato alla dimensione reale, ruota da solo.
    /// Nel kit grafico non esiste uno sprite ad anello circolare.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RingArcGraphic : MaskableGraphic
    {
        public float Thickness = 10f;
        [Range(0f, 1f)] public float Arc = 0.72f;
        public Color TrackColor = new Color32(44, 62, 90, 255);
        public Color HeadColor = new Color32(232, 190, 70, 255);
        public Color TailColor = new Color32(120, 116, 70, 255);
        public float DegreesPerSecond = -160f;
        private const int Segments = 72;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            float inner = Mathf.Max(0f, outer - Thickness);
            AddArc(vh, rect.center, inner, outer, 0f, 1f, TrackColor, TrackColor);
            AddArc(vh, rect.center, inner, outer, 0f, Arc, TailColor, HeadColor);
        }

        private static void AddArc(VertexHelper vh, Vector2 center, float inner, float outer, float from, float to, Color start, Color end)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Segments * (to - from)));
            int first = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float t = Mathf.Lerp(from, to, (float)i / steps);
                float angle = Mathf.PI * 0.5f - t * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var color = Color.Lerp(start, end, steps == 0 ? 1f : (float)i / steps);
                vh.AddVert(center + direction * outer, color, Vector2.zero);
                vh.AddVert(center + direction * inner, color, Vector2.zero);
            }
            for (int i = 0; i < steps; i++)
            {
                int a = first + i * 2;
                vh.AddTriangle(a, a + 2, a + 1);
                vh.AddTriangle(a + 1, a + 2, a + 3);
            }
        }

        private void Update()
        {
            if (Application.isPlaying && DegreesPerSecond != 0f)
                rectTransform.Rotate(0f, 0f, DegreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
