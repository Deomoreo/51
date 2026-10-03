using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Cornice tavolo (feltro verde + bordo oro + cornice legno) generata via codice,
    /// nessun asset esterno - Assets/UI_SPEC_Tavolo.md, sezione 1. Vive in world space,
    /// DIETRO le CardView (SpriteRenderer), sopra GameBackground (vedi sortingOrder).
    /// Con i banner in scena (UI51 Fase 5, S4) il bordo lo decide CardViewManager.TryGetTableRim
    /// (mockup Partita: angoli ellittici, legno a tre toni, emblema del sole); senza, dimensioni e
    /// posizione vengono dai rapporti sulla viewport reale della camera ortografica.
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
        [SerializeField, Range(0.05f, 0.5f)] private float cornerRadiusRatio = 0.22f; // relativo alla larghezza, come in spec (220/1000)
        [Tooltip("Raggio verticale degli angoli, sempre relativo alla larghezza. 0 = come cornerRadiusRatio (angoli rotondi).")]
        [SerializeField, Range(0f, 0.5f)] private float cornerRadiusYRatio;
        [SerializeField, Range(0f, 0.03f)] private float goldThicknessRatio = 0.005f;  // 5/1000
        [SerializeField, Range(0f, 0.05f)] private float woodThicknessRatio = 0.016f;  // 16/1000
        [Tooltip("Tetto della texture: col bordo dai banner si ferma ai pixel veri del telefono.")]
        [SerializeField, Range(64, 1024)] private int textureResolution = 512;

        [Header("Colori (Assets/UI_SPEC_Tavolo.md, sezione 1)")]
        [SerializeField] private Color feltColor = new Color32(0x12, 0x40, 0x32, 0xFF);
        [Tooltip("Centro del feltro. Alfa 0 = come prima (feltro schiarito del 35%).")]
        [SerializeField] private Color feltCenterColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color goldColor = new Color32(0xE8, 0xB2, 0x4A, 0xFF);
        [Tooltip("Legno a meta' altezza (e ovunque se i due sotto hanno alfa 0).")]
        [SerializeField] private Color woodColor = new Color32(0x3A, 0x26, 0x10, 0xFF);
        [SerializeField] private Color woodTopColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color woodBottomColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField, Range(0f, 1f)] private float centerGlowStrength = 0.35f;
        [SerializeField, Range(0f, 0.15f)] private float weaveStrength = 0.055f;
        [Tooltip("Ombra interna del feltro lungo il bordo (mockup: inset 0 0 50px nero). 0 = nessuna.")]
        [SerializeField, Range(0f, 1f)] private float feltEdgeShade;

        [Header("Emblema al centro del feltro (mockup: sole, opacita' 0,08)")]
        [SerializeField] private Sprite emblemSprite;
        [SerializeField, Range(0f, 1f)] private float emblemAlpha = 0.08f;

        [Header("Kit tavolo 2.22 (sorgenti in Design/sorgenti/Art/Table): se c'e' la cornice sostituisce la texture procedurale")]
        [SerializeField] private Sprite feltSprite;
        [SerializeField] private Sprite vignetteSprite;
        [Tooltip("Sprite 9-slice: lo spessore del legno lo decidono bordi e pixelsPerUnit dell'import.")]
        [SerializeField] private Sprite frameSprite;
        [Tooltip("Rientro del feltro sotto il legno, in pixel dello sprite cornice (angoli arrotondati coperti, buco pieno).")]
        [SerializeField] private float feltInsetPx = 50f;

        [SerializeField] private int sortingOrder = -1;

        private SpriteRenderer spriteRenderer;
        private SpriteRenderer vignetteRenderer;
        private SpriteRenderer frameRenderer;
        private SpriteRenderer emblemRenderer;
        private CardViewManager layout;
        private Texture2D texture;
        private Sprite generatedSprite;
        private bool rebuildRequested;
        private float builtAspect = -1f;
        private Rect builtRim;

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

        // Niente Start: il primo LateUpdate costruisce dopo LocalSeatBottomShift (banner gia' al loro posto),
        // senza generare la texture due volte.

        private float builtOrthoSize = -1f;
        private float builtCameraAspect = -1f;

        private void LateUpdate()
        {
            // CameraResponsiveFit cambia la camera dopo Start (e il Game view dell'Editor cambia
            // risoluzione all'avvio del Play): senza questo il feltro restava con proporzioni vecchie,
            // piu' alto del previsto.
            if (targetCamera == null || spriteRenderer == null) return;
            bool rimResized = false;
            if (frameSprite == null && TryGetRim(out var rim))
            {
                rimResized = Mathf.Abs(rim.width - builtRim.width) > 0.001f || Mathf.Abs(rim.height - builtRim.height) > 0.001f;
                if (!rimResized && (rim.center - builtRim.center).sqrMagnitude > 1e-6f)
                {
                    // Solo spostato (banner rimessi a posto): niente texture nuova.
                    builtRim = rim;
                    var t = spriteRenderer.transform;
                    t.position = new Vector3(rim.center.x, rim.center.y, t.position.z);
                }
            }
            if (rebuildRequested || rimResized || Mathf.Abs(targetCamera.orthographicSize - builtOrthoSize) > 0.001f || Mathf.Abs(targetCamera.aspect - builtCameraAspect) > 0.001f)
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

        /// <summary>Bordo esterno dai banner (CardViewManager), in mondo. False senza banner (test, scene vecchie).</summary>
        private bool TryGetRim(out Rect rim)
        {
            rim = default;
            if (layout == null && tableCenterReference != null) layout = tableCenterReference.GetComponentInParent<CardViewManager>();
            return layout != null && layout.TryGetTableRim(out rim, out _);
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
            Vector3 center = tableCenterReference != null ? tableCenterReference.position : transform.parent != null ? transform.parent.position : Vector3.zero;
            center += Vector3.up * (visibleHeight * verticalOffsetRatio);
            int texWidth = textureResolution;
            Rect rim = default;
            bool fromRim = frameSprite == null && TryGetRim(out rim);
            if (fromRim)
            {
                builtRim = rim;
                feltWidth = rim.width;
                feltHeight = rim.height;
                center = new Vector3(rim.center.x, rim.center.y, center.z);
                // Disegnata circa 1:1: oltre i pixel veri del telefono (iPhone SE ~711) e' solo memoria e tempo.
                float worldPerScreenPixel = targetCamera.orthographicSize * 2f / Mathf.Max(1, targetCamera.pixelHeight);
                texWidth = Mathf.Clamp(Mathf.CeilToInt(rim.width / worldPerScreenPixel), 64, textureResolution);
            }
            float aspect = feltWidth / Mathf.Max(0.01f, feltHeight);

            if (frameSprite != null)
            {
                // Stesso ordine di disegno (-1): i livelli si separano in z, piu' vicini alla camera = sopra.
                float inset = feltInsetPx / frameSprite.pixelsPerUnit;
                var feltSize = new Vector2(feltWidth - 2f * inset, feltHeight - 2f * inset);
                if (vignetteRenderer == null) vignetteRenderer = AddKitLayer("TableVignette", -.01f);
                if (frameRenderer == null) frameRenderer = AddKitLayer("TableFrame", -.02f);
                SetKitLayer(spriteRenderer, feltSprite, feltSize);
                SetKitLayer(vignetteRenderer, vignetteSprite, feltSize);
                SetKitLayer(frameRenderer, frameSprite, new Vector2(feltWidth, feltHeight));
            }
            else
            {
                // Dal kit alla texture procedurale (builder della Fase 5 con la scena aperta): via i livelli del kit.
                spriteRenderer.drawMode = SpriteDrawMode.Simple;
                RemoveLayer(ref vignetteRenderer);
                RemoveLayer(ref frameRenderer);
                if (texture == null || texture.width != texWidth || Mathf.Abs(aspect - builtAspect) > (fromRim ? 0.002f : 0.02f))
                {
                    ReleaseGeneratedAssets();
                    int texHeight = Mathf.Max(8, Mathf.RoundToInt(texWidth / aspect));
                    float ry = (cornerRadiusYRatio > 0f ? cornerRadiusYRatio : cornerRadiusRatio) * texWidth;
                    texture = GenerateFrameTexture(texWidth, texHeight, cornerRadiusRatio * texWidth, ry,
                        woodThicknessRatio * texWidth, goldThicknessRatio * texWidth);
                    builtAspect = aspect;
                }
                else
                {
                    // Stessa proporzione: basta ridimensionare lo sprite esistente invece di rigenerare la texture.
                    Release(generatedSprite);
                }
                spriteRenderer.transform.localScale = Vector3.one;
                generatedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), texture.width / feltWidth, 0, SpriteMeshType.FullRect);
                spriteRenderer.sprite = generatedSprite;
            }

            spriteRenderer.transform.position = center;
            UpdateEmblem(feltWidth, feltHeight);
        }

        /// <summary>Sole al centro (mockup: 220 su 370, 39% dell'altezza del feltro, 2% sopra il centro), dietro alle carte.</summary>
        private void UpdateEmblem(float width, float height)
        {
            if (emblemSprite == null || frameSprite != null)
            {
                RemoveLayer(ref emblemRenderer);
                return;
            }
            if (emblemRenderer == null) emblemRenderer = AddKitLayer("TableEmblem", -.01f);
            emblemRenderer.sprite = emblemSprite;
            emblemRenderer.drawMode = SpriteDrawMode.Simple;
            emblemRenderer.color = new Color(1f, 1f, 1f, emblemAlpha);
            float felt = height - 2f * woodThicknessRatio * width;
            float size = Mathf.Min(width * 220f / 370f, felt * 0.39f);
            float s = size / Mathf.Max(0.0001f, emblemSprite.bounds.size.y);
            emblemRenderer.transform.localScale = new Vector3(s, s, 1f);
            emblemRenderer.transform.localPosition = new Vector3(0f, height * 0.02f, -.01f);
        }

        private SpriteRenderer AddKitLayer(string layerName, float z)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(spriteRenderer.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private static void RemoveLayer(ref SpriteRenderer layer)
        {
            if (layer != null) Release(layer.gameObject);
            layer = null;
        }

        private static void SetKitLayer(SpriteRenderer sr, Sprite sprite, Vector2 size)
        {
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
        }

        private Texture2D GenerateFrameTexture(int width, int height, float radiusX, float radiusY, float woodThicknessPx, float goldThicknessPx)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "K5 woven table felt", hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[width * height];
            Vector2 halfSize = new Vector2(width / 2f, height / 2f);
            float inner = woodThicknessPx + goldThicknessPx;
            Color feltCenter = feltCenterColor.a > 0f ? feltCenterColor : Color.Lerp(feltColor, Color.white, 0.35f);
            Color woodTop = woodTopColor.a > 0f ? woodTopColor : woodColor;
            Color woodBottom = woodBottomColor.a > 0f ? woodBottomColor : woodColor;
            float shadeFalloff = width * 20f / 370f; // mockup: l'ombra interna sfuma in ~20 su 370

            for (int y = 0; y < height; y++)
            {
                // Riga 0 = in basso: il legno va dal tono in basso, a meta' woodColor, al tono in alto.
                float t = height > 1 ? y / (height - 1f) : 0.5f;
                Color wood = t < 0.5f ? Color.Lerp(woodBottom, woodColor, t * 2f) : Color.Lerp(woodColor, woodTop, (t - 0.5f) * 2f);
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - halfSize;

                    float dOuter = EllipseBoxSdf(p, halfSize, radiusX, radiusY);
                    float dGold = EllipseBoxSdf(p, halfSize - new Vector2(woodThicknessPx, woodThicknessPx),
                        radiusX - woodThicknessPx, radiusY - woodThicknessPx);
                    float dFelt = EllipseBoxSdf(p, halfSize - new Vector2(inner, inner), radiusX - inner, radiusY - inner);

                    // Legno, poi oro, poi feltro, con mezzo pixel sfumato su ogni confine (niente scalini agli angoli).
                    Color color = wood;
                    float gold = Mathf.Clamp01(0.5f - dGold);
                    if (gold > 0f) color = Color.Lerp(color, goldColor, gold);
                    float felt = Mathf.Clamp01(0.5f - dFelt);
                    if (felt > 0f)
                    {
                        // Broad elliptical light, normalized so phone/tablet proportions do not
                        // move the highlight. Grain is deterministic and never consumes game RNG.
                        var normalized = new Vector2(p.x / halfSize.x, p.y / halfSize.y - .12f);
                        float glow = Mathf.Exp(-normalized.sqrMagnitude * 1.7f) * centerGlowStrength;
                        Color cloth = Color.Lerp(feltColor, feltCenter, glow);
                        uint hash = unchecked((uint)(x * 374761393 + y * 668265263));
                        hash = unchecked((hash ^ (hash >> 13)) * 1274126177u);
                        float grain = (hash & 1023) / 1023f - .5f;
                        float weave = Mathf.Sin(x * 2.1f + (y % 2) * .7f) *
                            Mathf.Sin(y * 1.7f) * .35f + grain * .65f;
                        float shade = 1f + weave * weaveStrength;
                        if (feltEdgeShade > 0f) shade *= 1f - feltEdgeShade * Mathf.Exp(Mathf.Min(0f, dFelt) / shadeFalloff);
                        cloth = new Color(cloth.r * shade, cloth.g * shade, cloth.b * shade, 1f);
                        color = Color.Lerp(color, cloth, felt);
                    }

                    // Antialiasing morbido sul bordo esterno (dove incontra la trasparenza).
                    color.a = Mathf.Clamp01(0.5f - dOuter / 1.5f);
                    pixels[y * width + x] = color;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true); // niente copia in RAM: la texture si rigenera, non si rilegge
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
        /// SDF di un rettangolo con angoli ellittici (raggi rx, ry, come border-radius rx/ry in CSS): il confine e' esatto,
        /// la distanza e' in pixel lungo x e un po' stirata lungo y quando rx != ry.
        /// </summary>
        public static float EllipseBoxSdf(Vector2 p, Vector2 halfSize, float rx, float ry)
        {
            rx = Mathf.Max(0f, rx);
            ry = Mathf.Max(0f, ry);
            if (rx <= 0f || ry <= 0f) return RoundedBoxSdf(p, halfSize, 0f);
            float s = rx / ry;
            return RoundedBoxSdf(new Vector2(p.x, p.y * s), new Vector2(halfSize.x, halfSize.y * s), rx);
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
