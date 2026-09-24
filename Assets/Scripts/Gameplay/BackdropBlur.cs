using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>
    /// Sfocatura vera dietro ai pannelli (mockup 26-28). Fotografa lo schermo prima che il pannello
    /// compaia, la rimpicciolisce e la sfoca, poi la mostra a tutto schermo. Sopra va il velo
    /// "Sfocatura sfondo" (immagine), che scurisce e fa la vignettatura.
    /// Capture() e' un IEnumerator semplice: si puo' attendere anche da un pannello ancora spento.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class BackdropBlur : MonoBehaviour
    {
        [Tooltip("Di quanto si rimpicciolisce la foto dello schermo prima di sfocarla.")]
        [SerializeField, Range(2, 16)] private int downsample = 8;
        [Tooltip("Raggio della sfocatura in pixel della foto rimpicciolita.")]
        [SerializeField, Range(1, 6)] private int radius = 2;
        [SerializeField, Range(1, 4)] private int passes = 3;

        private RawImage image;
        private RenderTexture blurred;
        private Material blurMaterial;
        private int captureVersion;
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        public bool HasSnapshot => blurred != null && blurred.IsCreated();

        /// <summary>Captures before the panel is visible; failure leaves only its veil.</summary>
        public IEnumerator Capture()
        {
            if (this == null) yield break;
            if (image == null) image = GetComponent<RawImage>();
            if (GamePreferences.ReducedGraphics) { ReleaseResources(); yield break; }
            int version = ++captureVersion;
            image.enabled = false;
            yield return new WaitForEndOfFrame();
            // Do not check isActiveAndEnabled: callers capture while the panel is inactive.
            if (this == null || version != captureVersion) yield break;
            if (GamePreferences.ReducedGraphics) { ReleaseResources(); yield break; }

            RenderTexture shot = null;
            var previous = RenderTexture.active;
            try
            {
                if (!EnsureMaterial() || Screen.width < 1 || Screen.height < 1)
                {
                    ReleaseResources();
                    yield break;
                }
                shot = Temporary(Screen.width, Screen.height);
                ScreenCapture.CaptureScreenshotIntoRenderTexture(shot);
                int divisor = Mathf.Clamp(downsample, 2, 16);
                PresentCapture(shot, Mathf.Max(16, shot.width / divisor), Mathf.Max(16, shot.height / divisor),
                    SystemInfo.graphicsUVStartsAtTop);
            }
            catch (System.Exception e)
            {
                ReleaseResources();
                Debug.LogWarning("[BackdropBlur] Foto dello schermo non riuscita: " + e.Message);
            }
            finally
            {
                RenderTexture.active = previous;
                if (shot != null) RenderTexture.ReleaseTemporary(shot);
            }
        }

        private void PresentCapture(Texture shot, int width, int height, bool startsAtTop)
        {
            if (!RenderBlur(shot, width, height)) return;
            // Screenshot RTs retain the backbuffer origin on D3D/Metal-like APIs.
            // Correct only their presentation; ordinary textures already use Unity UVs.
            image.uvRect = startsAtTop ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
        }

        private bool EnsureMaterial()
        {
            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32) ||
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return false;
            if (blurMaterial != null) return true;
            var shader = Resources.Load<Shader>("K4/BackdropBlur");
            if (shader == null || !shader.isSupported) return false;
            blurMaterial = new Material(shader) { name = "BackdropBlur GPU", hideFlags = HideFlags.HideAndDontSave };
            return true;
        }

        private bool RenderBlur(Texture source, int width, int height)
        {
            if (image == null) image = GetComponent<RawImage>();
            if (GamePreferences.ReducedGraphics) { ReleaseResources(); return false; }
            RenderTexture small = null, scratch = null, reduced = null;
            var previous = RenderTexture.active;
            try
            {
                if (source == null || width < 1 || height < 1 || !EnsureMaterial())
                {
                    ReleaseResources();
                    return false;
                }
                // Progressive bilinear reduction avoids aliasing thin table details.
                Texture current = source;
                int w = source.width, h = source.height;
                while (w / 2 >= width && h / 2 >= height && (w > width || h > height))
                {
                    w /= 2;
                    h /= 2;
                    var next = Temporary(w, h);
                    try { Graphics.Blit(current, next); }
                    catch { RenderTexture.ReleaseTemporary(next); throw; }
                    if (reduced != null) RenderTexture.ReleaseTemporary(reduced);
                    reduced = next;
                    current = next;
                }
                small = Temporary(width, height);
                scratch = Temporary(width, height);
                Graphics.Blit(current, small);
                if (blurred == null || blurred.width != width || blurred.height != height || !blurred.IsCreated())
                {
                    ReleaseOutput();
                    blurred = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                    {
                        name = "BackdropBlur GPU Output", filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
                    };
                    if (!blurred.Create()) throw new System.InvalidOperationException("RenderTexture allocation failed.");
                }
                float spread = Mathf.Clamp(radius, 1, 6) * 0.5f;
                for (int i = 0; i < Mathf.Clamp(passes, 1, 4); i++)
                {
                    blurMaterial.SetVector(DirectionId, new Vector4(spread, 0, 0, 0));
                    Graphics.Blit(small, scratch, blurMaterial);
                    blurMaterial.SetVector(DirectionId, new Vector4(0, spread, 0, 0));
                    Graphics.Blit(scratch, small, blurMaterial);
                }
                Graphics.Blit(small, blurred);
                image.texture = blurred;
                image.uvRect = new Rect(0, 0, 1, 1);
                image.enabled = true;
                return true;
            }
            catch (System.Exception e)
            {
                ReleaseResources();
                Debug.LogWarning("[BackdropBlur] Sfocatura GPU non riuscita: " + e.Message);
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                if (reduced != null) RenderTexture.ReleaseTemporary(reduced);
                if (small != null) RenderTexture.ReleaseTemporary(small);
                if (scratch != null) RenderTexture.ReleaseTemporary(scratch);
            }
        }

        /// <summary>Legacy CPU utility kept for callers/tests; screen capture uses only the GPU.</summary>
        public static void BoxBlur(Color32[] pixels, int width, int height, int radius, int passes)
        {
            if (pixels == null || radius < 1 || passes < 1 || pixels.Length != width * height) return;
            var buffer = new Color32[pixels.Length];
            for (int pass = 0; pass < passes; pass++)
            {
                Blur1D(pixels, buffer, width, height, radius, horizontal: true);
                Blur1D(buffer, pixels, width, height, radius, horizontal: false);
            }
        }

        private static void Blur1D(Color32[] source, Color32[] target, int width, int height, int radius, bool horizontal)
        {
            int lines = horizontal ? height : width;
            int length = horizontal ? width : height;
            int span = radius * 2 + 1;
            for (int line = 0; line < lines; line++)
            {
                int r = 0, g = 0, b = 0, a = 0;
                for (int i = -radius; i <= radius; i++)
                {
                    var c = source[Index(line, Mathf.Clamp(i, 0, length - 1), width, horizontal)];
                    r += c.r; g += c.g; b += c.b; a += c.a;
                }
                for (int i = 0; i < length; i++)
                {
                    target[Index(line, i, width, horizontal)] = new Color32((byte)(r / span), (byte)(g / span), (byte)(b / span), (byte)(a / span));
                    var enter = source[Index(line, Mathf.Min(i + radius + 1, length - 1), width, horizontal)];
                    var leave = source[Index(line, Mathf.Max(i - radius, 0), width, horizontal)];
                    r += enter.r - leave.r;
                    g += enter.g - leave.g;
                    b += enter.b - leave.b;
                    a += enter.a - leave.a;
                }
            }
        }

        private static int Index(int line, int position, int width, bool horizontal) =>
            horizontal ? line * width + position : position * width + line;

        private static RenderTexture Temporary(int width, int height)
        {
            var texture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private void ReleaseOutput()
        {
            if (image != null)
            {
                image.texture = null;
                image.enabled = false;
            }
            if (blurred == null) return;
            blurred.Release();
            DestroyOwned(blurred);
            blurred = null;
        }

        private void ReleaseResources()
        {
            ReleaseOutput();
            if (blurMaterial != null) DestroyOwned(blurMaterial);
            blurMaterial = null;
        }

        private static void DestroyOwned(Object resource)
        {
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }

        private void OnEnable()
        {
            GamePreferences.Changed += ApplyPreference;
            ApplyPreference();
        }

        private void ApplyPreference()
        {
            if (image == null) image = GetComponent<RawImage>();
            if (GamePreferences.ReducedGraphics)
            {
                ++captureVersion;
                image.enabled = false;
                image.texture = null;
                if (blurMaterial != null) DestroyOwned(blurMaterial);
                blurMaterial = null;
                // Retain only this open panel's small, already-rendered snapshot. Restoring
                // it cannot accidentally photograph the modal; OnDisable releases it.
            }
            else if (HasSnapshot)
            {
                image.texture = blurred;
                image.enabled = true;
            }
        }

        private void OnDisable()
        {
            GamePreferences.Changed -= ApplyPreference;
            ++captureVersion;
            ReleaseResources();
        }

        private void OnDestroy()
        {
            ++captureVersion;
            ReleaseResources();
        }
    }
}
