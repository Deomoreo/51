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
        [SerializeField, Range(0f, 0.15f)] private float weaveStrength = 0.055f;

        [SerializeField] private int sortingOrder = -1;

        private SpriteRenderer spriteRenderer;
        private Texture2D texture;
        private Sprite generatedSprite;
        private bool rebuildRequested;
        private float builtAspect = -1f;

        private void Awake()
        {
            ReleaseGeneratedAssets();
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
            if (rebuildRequested || Mathf.Abs(targetCamera.orthographicSize - builtOrthoSize) > 0.001f || Mathf.Abs(targetCamera.aspect - builtCameraAspect) > 0.001f)
            {
                Rebuild();
            }
        }

        private void OnValidate()
        {
            // Validation can run while Unity imports/deserializes: release assets only
            // on the next normal editor update, never inside OnValidate.
            rebuildRequested = true;
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || !targetCamera.orthographic || spriteRenderer == null) return;
            if (rebuildRequested) builtAspect = -1f;
            rebuildRequested = false;

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
                ReleaseGeneratedAssets();
                int texWidth = textureResolution;
                int texHeight = Mathf.Max(8, Mathf.RoundToInt(texWidth / aspect));
                texture = GenerateFrameTexture(texWidth, texHeight, cornerRadiusRatio * texWidth,
                    woodThicknessRatio * texWidth, goldThicknessRatio * texWidth);
                builtAspect = aspect;

                float pixelsPerUnit = texWidth / feltWidth;
                generatedSprite = Sprite.Create(texture, new Rect(0, 0, texWidth, texHeight),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
                spriteRenderer.sprite = generatedSprite;
            }
            else
            {
                // Stessa proporzione: basta ridimensionare lo sprite esistente invece di rigenerare la texture.
                spriteRenderer.transform.localScale = Vector3.one;
                float pixelsPerUnit = texture.width / feltWidth;
                Release(generatedSprite);
                generatedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
                spriteRenderer.sprite = generatedSprite;
            }

            Vector3 center = tableCenterReference != null ? tableCenterReference.position : transform.parent != null ? transform.parent.position : Vector3.zero;
            spriteRenderer.transform.position = center + Vector3.up * (visibleHeight * verticalOffsetRatio);
        }

        private Texture2D GenerateFrameTexture(int width, int height, float cornerRadiusPx, float woodThicknessPx, float goldThicknessPx)
        {
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, true)
            {
                name = "K5 woven table felt", hideFlags = HideFlags.DontSave,
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
                        // Broad elliptical light, normalized so phone/tablet proportions do not
                        // move the highlight. Grain is deterministic and never consumes game RNG.
                        var normalized = new Vector2(p.x / halfSize.x, p.y / halfSize.y - .12f);
                        float glow = Mathf.Exp(-normalized.sqrMagnitude * 1.7f) * centerGlowStrength;
                        color = Color.Lerp(feltColor, Color.Lerp(feltColor, Color.white, 0.35f), glow);
                        uint hash = unchecked((uint)(x * 374761393 + y * 668265263));
                        hash = unchecked((hash ^ (hash >> 13)) * 1274126177u);
                        float grain = (hash & 1023) / 1023f - .5f;
                        float weave = Mathf.Sin(x * 2.1f + (y % 2) * .7f) *
                            Mathf.Sin(y * 1.7f) * .35f + grain * .65f;
                        float shade = 1f + weave * weaveStrength;
                        color = new Color(color.r * shade, color.g * shade, color.b * shade, color.a);
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

        private void OnDestroy() => ReleaseGeneratedAssets();

        private void ReleaseGeneratedAssets()
        {
            if (spriteRenderer != null) spriteRenderer.sprite = null;
            Release(generatedSprite);
            Release(texture);
            generatedSprite = null;
            texture = null;
        }

        private static void Release(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
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
