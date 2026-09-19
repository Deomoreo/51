using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Components
{
    /// <summary>
    /// C6 - particelle UI senza ParticleSystem (che non si disegna nei Canvas overlay).
    /// Una sola Graphic, una sola draw call: disegna puntini di luce morbidi (lo sprite
    /// "Bagliore morbido cerchio" in piccolo) in due modi:
    /// - pulviscolo continuo (ambient): puntini che salgono lenti, ondeggiano e pulsano,
    ///   nati in punti casuali del rettangolo e spariti in dissolvenza;
    /// - scoppio (Burst): puntini lanciati dal centro verso l'esterno che rallentano, cadono
    ///   un poco e si spengono - vittoria, e le ricompense quando ci saranno.
    /// Tempo non scalato: gira anche con il gioco in pausa o accelerato.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIV2MoteField : MaskableGraphic
    {
        [SerializeField] private Sprite moteSprite;

        [Header("Pulviscolo continuo")]
        [SerializeField, Min(0)] private int ambientCount = 20;
        [SerializeField] private Vector2 ambientSize = new Vector2(22f, 56f);   // il nucleo luminoso dello sprite e' circa meta' del quadrato
        [SerializeField] private Vector2 riseSpeed = new Vector2(14f, 38f);
        [SerializeField] private float sway = 18f;
        [SerializeField] private Vector2 lifetime = new Vector2(5f, 9f);
        [SerializeField, Range(0f, 1f)] private float twinkle = 0.35f;

        [Header("Scoppio")]
        [SerializeField, Min(0)] private int burstCount = 36;
        [SerializeField] private Vector2 burstSize = new Vector2(28f, 66f);
        [SerializeField] private Vector2 burstSpeed = new Vector2(380f, 820f);
        [SerializeField] private float burstGravity = 240f;
        [SerializeField] private float burstDrag = 2.2f;
        [SerializeField] private Vector2 burstLifetime = new Vector2(0.9f, 1.6f);
        [SerializeField] private bool burstOnEnable;

        private struct Mote
        {
            public Vector2 Position, Velocity;
            public float Size, Age, Life, Phase, SwaySpeed;
            public bool Burst;
        }

        private Mote[] ambient = new Mote[0];
        private Mote[] bursts = new Mote[0];
        private int burstAlive;
        private float lastTime;

        public override Texture mainTexture => moteSprite != null ? moteSprite.texture : s_WhiteTexture;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            lastTime = Time.unscaledTime;
            SeedAmbient();
            if (burstOnEnable && Application.isPlaying) Burst();
        }

        /// <summary>Scoppio dal centro del rettangolo.</summary>
        public void Burst() => Burst(rectTransform.rect.center, burstCount);

        /// <summary>Scoppio da un punto locale del rettangolo.</summary>
        public void Burst(Vector2 localCenter, int count)
        {
            if (count <= 0) return;
            if (bursts.Length < burstAlive + count) System.Array.Resize(ref bursts, burstAlive + count);
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(burstSpeed.x, burstSpeed.y);
                bursts[burstAlive++] = new Mote
                {
                    Position = localCenter,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    Size = Random.Range(burstSize.x, burstSize.y),
                    Life = Random.Range(burstLifetime.x, burstLifetime.y),
                    Phase = Random.value * 10f,
                    Burst = true,
                };
            }
            SetVerticesDirty();
        }

        private void SeedAmbient()
        {
            if (ambient.Length != ambientCount) ambient = new Mote[ambientCount];
            for (int i = 0; i < ambient.Length; i++)
            {
                ambient[i] = NewAmbient();
                ambient[i].Age = Random.Range(0f, ambient[i].Life); // gia' sparsi al primo frame
            }
        }

        private Mote NewAmbient()
        {
            var r = rectTransform.rect;
            return new Mote
            {
                Position = new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax)),
                Velocity = new Vector2(0f, Random.Range(riseSpeed.x, riseSpeed.y)),
                Size = Random.Range(ambientSize.x, ambientSize.y),
                Life = Random.Range(lifetime.x, lifetime.y),
                Phase = Random.value * 10f,
                SwaySpeed = Random.Range(0.4f, 1.1f),
            };
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            float dt = Mathf.Min(now - lastTime, 0.1f);
            lastTime = now;
            if (dt <= 0f) return;
            if (ambient.Length != ambientCount) SeedAmbient();

            for (int i = 0; i < ambient.Length; i++)
            {
                ambient[i].Age += dt;
                if (ambient[i].Age >= ambient[i].Life) ambient[i] = NewAmbient();
                else ambient[i].Position.y += ambient[i].Velocity.y * dt;
            }

            float damping = Mathf.Exp(-burstDrag * dt);
            for (int i = burstAlive - 1; i >= 0; i--)
            {
                bursts[i].Age += dt;
                if (bursts[i].Age >= bursts[i].Life)
                {
                    bursts[i] = bursts[--burstAlive];
                    continue;
                }
                bursts[i].Velocity *= damping;
                bursts[i].Velocity.y -= burstGravity * dt;
                bursts[i].Position += bursts[i].Velocity * dt;
            }

            if (ambient.Length > 0 || burstAlive > 0) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var uv = moteSprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(moteSprite) : new Vector4(0f, 0f, 1f, 1f);
            float t = Application.isPlaying ? Time.unscaledTime : 0f;

            for (int i = 0; i < ambient.Length; i++)
            {
                var m = ambient[i];
                float life01 = m.Life > 0f ? m.Age / m.Life : 0f;
                // entra e esce in dissolvenza, pulsa piano
                float fade = Mathf.Clamp01(life01 * 5f) * Mathf.Clamp01((1f - life01) * 3f);
                float pulse = 1f - twinkle * (0.5f + 0.5f * Mathf.Sin(t * 2.3f + m.Phase));
                var pos = m.Position + new Vector2(Mathf.Sin(t * m.SwaySpeed + m.Phase) * sway, 0f);
                AddQuad(vh, pos, m.Size, fade * pulse, uv);
            }
            for (int i = 0; i < burstAlive; i++)
            {
                var m = bursts[i];
                float life01 = m.Age / m.Life;
                float fade = Mathf.Clamp01((1f - life01) * 2.5f);
                AddQuad(vh, m.Position, m.Size * (1f - 0.4f * life01), fade, uv);
            }
        }

        private void AddQuad(VertexHelper vh, Vector2 center, float size, float alpha, Vector4 uv)
        {
            if (alpha <= 0.001f) return;
            var c = color;
            c.a *= alpha;
            float h = size * 0.5f;
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(center.x - h, center.y - h), c, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(center.x - h, center.y + h), c, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(center.x + h, center.y + h), c, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(center.x + h, center.y - h), c, new Vector2(uv.z, uv.y));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
