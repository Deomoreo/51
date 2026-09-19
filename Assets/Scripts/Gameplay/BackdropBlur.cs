using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
        private Texture2D blurred;

        /// <summary>
        /// Da chiamare con il pannello ancora invisibile: la foto viene scattata a fine frame.
        /// Se la foto non riesce resta solo il velo.
        /// </summary>
        public IEnumerator Capture()
        {
            if (image == null) image = GetComponent<RawImage>();
            image.enabled = false;
            yield return new WaitForEndOfFrame();

            Texture2D shot = null;
            try
            {
                shot = ScreenCapture.CaptureScreenshotAsTexture();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[BackdropBlur] Foto dello schermo non riuscita: " + e.Message);
            }
            if (shot == null) yield break;

            int width = Mathf.Max(16, shot.width / downsample);
            int height = Mathf.Max(16, shot.height / downsample);

            // Dimezza piu' volte con il filtro bilineare: gia' questo ammorbidisce senza sfarfallii.
            Texture current = shot;
            RenderTexture halved = null;
            int w = shot.width, h = shot.height;
            while (w / 2 >= width && h / 2 >= height)
            {
                w /= 2;
                h /= 2;
                var next = Temporary(w, h);
                Graphics.Blit(current, next);
                if (halved != null) RenderTexture.ReleaseTemporary(halved);
                halved = next;
                current = next;
            }

            var small = Temporary(width, height);
            Graphics.Blit(current, small);
            if (halved != null) RenderTexture.ReleaseTemporary(halved);
            Destroy(shot);

            if (blurred == null || blurred.width != width || blurred.height != height)
            {
                if (blurred != null) Destroy(blurred);
                blurred = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "BackdropBlur",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            var previous = RenderTexture.active;
            RenderTexture.active = small;
            blurred.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(small);

            var pixels = blurred.GetPixels32();
            BoxBlur(pixels, width, height, radius, passes);
            blurred.SetPixels32(pixels);
            blurred.Apply(false);

            image.texture = blurred;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            image.enabled = true;
        }

        /// <summary>Sfocatura a scatola separabile (orizzontale poi verticale), ripetuta passes volte.</summary>
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
            return texture;
        }

        private void OnDestroy()
        {
            if (blurred != null) Destroy(blurred);
        }
    }
}
