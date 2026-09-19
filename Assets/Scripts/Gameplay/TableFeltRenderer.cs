using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Cornice tavolo (feltro verde + bordo oro + cornice legno) generata via codice,
    /// nessun asset esterno - Assets/UI_SPEC_Tavolo.md, sezione 1. Vive in world space,
    /// DIETRO le CardView (SpriteRenderer), sopra GameBackground (vedi sortingOrder).
    /// Dimensioni/posizione responsive: rapporti applicati alla viewport reale della
    /// camera ortografica, stesso approccio di CardViewManager.GetVisibleWidth/Height,
    /// non pixel fissi del mockup 1080x1920.
    /// </summary>
    [ExecuteAlways]
    public class TableFeltRenderer : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [Tooltip("Transform usato come centro tavolo, es. TableCardsContainer del prefab CardViewManager.")]
        [SerializeField] private Transform tableCenterReference;

        [Header("Dimensioni (rapporti sulla viewport camera, da tarare a occhio)")]
        [SerializeField, Range(0.5f, 1f)] private float widthRatio = 0.985f; // prima 0.95 poi 0.97 - ancora piu' vicino ai bordi laterali, su richiesta ripetuta
        [SerializeField, Range(0.2f, 0.7f)] private float heightRatio = 0.42f;
        [Tooltip("Offset verticale del centro tavolo rispetto a tableCenterReference, in frazione di altezza visibile. Positivo = verso l'alto.")]
        [SerializeField, Range(-0.2f, 0.2f)] private float verticalOffsetRatio = -0.04f; // prima 0.05 (troppo in alto, non centrato) - abbassato su richiesta
        [SerializeField, Range(0.05f, 0.35f)] private float cornerRadiusRatio = 0.22f; // relativo alla larghezza, come in spec (220/1000)
        [SerializeField, Range(0f, 0.03f)] private float goldThicknessRatio = 0.005f;  // 5/1000
        [SerializeField, Range(0f, 0.05f)] private float woodThicknessRatio = 0.016f;  // 16/1000
        [SerializeField, Range(64, 1024)] private int textureResolution = 512;

        [Header("Colori (Assets/UI_SPEC_Tavolo.md, sezione 1)")]
        [SerializeField] private Color feltColor = new Color32(0x12, 0x40, 0x32, 0xFF);
        [SerializeField] private Color goldColor = new Color32(0xE8, 0xB2, 0x4A, 0xFF);
        [SerializeField] private Color woodColor = new Color32(0x3A, 0x26, 0x10, 0xFF);
        [SerializeField, Range(0f, 1f)] private float centerGlowStrength = 0.35f;

        [SerializeField] private int sortingOrder = -1;

        private SpriteRenderer spriteRenderer;
        private Texture2D texture;
        private float builtAspect = -1f;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            // [ExecuteAlways] fa scattare di nuovo Awake ad ogni ricompilazione script mentre la
            // scena e' aperta in Editor (non solo alla creazione vera dell'oggetto). Prima questo
            // creava un NUOVO figlio "TableFeltSprite" ogni volta senza pulire quello precedente:
            // piu' ricompilazioni durante la sessione = piu' feltri sovrapposti, ognuno con la
            // dimensione calcolata in un momento diverso (bug segnalato: "uno piccolo uno grande").
            // Puliamo SEMPRE tutti i figli prima di ricrearne esattamente uno, cosi' Awake e'
            // idempotente indipendentemente da quante volte scatta.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var spriteGO = new GameObject("TableFeltSprite");
            spriteGO.transform.SetParent(transform, false);
            spriteRenderer = spriteGO.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = sortingOrder;
        }

        private void Start()
        {
            Rebuild();
        }

        private float builtOrthoSize = -1f;
        private float builtCameraAspect = -1f;

        private void LateUpdate()
        {
            // CameraResponsiveFit cambia la camera dopo Start (e il Game view dell'Editor cambia
            // risoluzione all'avvio del Play): senza questo il feltro restava con proporzioni vecchie,
            // piu' alto del previsto.
            if (targetCamera == null || spriteRenderer == null) return;
            if (Mathf.Abs(targetCamera.orthographicSize - builtOrthoSize) > 0.001f || Mathf.Abs(targetCamera.aspect - builtCameraAspect) > 0.001f)
            {
                texture = null;
                Rebuild();
            }
        }

        private void OnValidate()
        {
            // Aggiorna live in Editor quando si tarano gli slider in Inspector (grazie a [ExecuteAlways]).
            if (spriteRenderer != null)
            {
                texture = null; // forza la rigenerazione anche se l'aspect ratio non e' cambiato
                Rebuild();
            }
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || !targetCamera.orthographic || spriteRenderer == null) return;

            builtOrthoSize = targetCamera.orthographicSize;
            builtCameraAspect = targetCamera.aspect;
            // Misure dall'area di design (CameraResponsiveFit), non da tutto il visibile: su un telefono
            // piu' alto del 9:16 il feltro deve restare dov'e' rispetto ai banner, non allungarsi.
            float visibleHeight = Mathf.Min(targetCamera.orthographicSize * 2f, CameraResponsiveFit.DesignWorldSize.y);
            float visibleWidth = Mathf.Min(targetCamera.orthographicSize * 2f * targetCamera.aspect, CameraResponsiveFit.DesignWorldSize.x);

            float feltWidth = visibleWidth * widthRatio;
            float feltHeight = visibleHeight * heightRatio;
            float aspect = feltWidth / Mathf.Max(0.01f, feltHeight);

            if (texture == null || Mathf.Abs(aspect - builtAspect) > 0.02f)
            {
                int texWidth = textureResolution;
                int texHeight = Mathf.Max(8, Mathf.RoundToInt(texWidth / aspect));
                texture = GenerateFrameTexture(texWidth, texHeight, cornerRadiusRatio * texWidth,
                    woodThicknessRatio * texWidth, goldThicknessRatio * texWidth);
                builtAspect = aspect;

                float pixelsPerUnit = texWidth / feltWidth;
                spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texWidth, texHeight),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }
            else
            {
                // Stessa proporzione: basta ridimensionare lo sprite esistente invece di rigenerare la texture.
                spriteRenderer.transform.localScale = Vector3.one;
                float pixelsPerUnit = texture.width / feltWidth;
                spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }

            Vector3 center = tableCenterReference != null ? tableCenterReference.position : transform.parent != null ? transform.parent.position : Vector3.zero;
            spriteRenderer.transform.position = center + Vector3.up * (visibleHeight * verticalOffsetRatio);
        }

        private Texture2D GenerateFrameTexture(int width, int height, float cornerRadiusPx, float woodThicknessPx, float goldThicknessPx)
        {
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[width * height];
            Vector2 halfSize = new Vector2(width / 2f, height / 2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - halfSize;

                    float dOuter = RoundedBoxSdf(p, halfSize, cornerRadiusPx);
                    float dGold = RoundedBoxSdf(p, halfSize - new Vector2(woodThicknessPx, woodThicknessPx),
                        Mathf.Max(0f, cornerRadiusPx - woodThicknessPx));
                    float dFelt = RoundedBoxSdf(p, halfSize - new Vector2(woodThicknessPx + goldThicknessPx, woodThicknessPx + goldThicknessPx),
                        Mathf.Max(0f, cornerRadiusPx - woodThicknessPx - goldThicknessPx));

                    Color color;
                    if (dFelt <= 0f)
                    {
                        // Luce calda al centro-alto del feltro, cosmetica (sezione 1 della spec).
                        Vector2 glowFocus = new Vector2(0f, halfSize.y * 0.4f);
                        float glowDist = Vector2.Distance(p, glowFocus) / (halfSize.magnitude * 0.85f);
                        float glow = Mathf.Clamp01(1f - glowDist) * centerGlowStrength;
                        color = Color.Lerp(feltColor, Color.Lerp(feltColor, Color.white, 0.35f), glow);
                    }
                    else if (dGold <= 0f)
                    {
                        color = goldColor;
                    }
                    else if (dOuter <= 0f)
                    {
                        color = woodColor;
                    }
                    else
                    {
                        color = new Color(0f, 0f, 0f, 0f);
                    }

                    // Antialiasing morbido sul solo bordo esterno (dove incontra la trasparenza).
                    if (dOuter > -1.5f && dOuter < 1.5f)
                    {
                        float edgeAlpha = Mathf.Clamp01(0.5f - dOuter / 1.5f);
                        color = new Color(color.r, color.g, color.b, color.a * edgeAlpha + (1f - edgeAlpha) * 0f);
                    }

                    pixels[y * width + x] = color;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// SDF di un rettangolo arrotondato (formula standard di Inigo Quilez): negativo dentro, positivo fuori.
        /// </summary>
        private static float RoundedBoxSdf(Vector2 p, Vector2 halfSize, float radius)
        {
            radius = Mathf.Max(0f, radius);
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (halfSize - new Vector2(radius, radius));
            Vector2 qMax = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return qMax.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }
    }
}
