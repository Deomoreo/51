using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// Creates a simple procedural "shine stripe" texture (transparent with a soft diagonal white band)
    /// at runtime, so you don't need to import an external PNG.
    /// Assign the generated texture to a target material property.
    /// </summary>
    public sealed class ProceduralShineStripeTexture : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Material targetMaterial;
        [SerializeField] private string textureProperty = "_MainTex";

        [Header("Texture")]
        [SerializeField] private int size = 256;
        [SerializeField] private float bandWidth = 0.12f;
        [SerializeField] private float softness = 0.08f;
        [SerializeField] private float intensity = 1f;
        [SerializeField] private bool regenerateOnEnable = true;

        private Texture2D generated;

        private void OnEnable()
        {
            if (regenerateOnEnable)
                GenerateAndApply();
        }

        private void OnDisable()
        {
            if (generated != null)
            {
                Destroy(generated);
                generated = null;
            }
        }

        [ContextMenu("Generate And Apply")]
        public void GenerateAndApply()
        {
            if (size < 8) size = 8;
            if (targetMaterial == null) return;

            if (generated != null)
            {
                Destroy(generated);
                generated = null;
            }

            generated = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            generated.wrapMode = TextureWrapMode.Clamp;
            generated.filterMode = FilterMode.Bilinear;

            // Define diagonal stripe by distance to line x - y = 0 (45 degrees).
            // Normalize to [-1..1] range for consistent band parameters.
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;

                    float d = Mathf.Abs(u - v); // 0 on diagonal

                    // bandWidth is the solid-ish core, softness fades out.
                    float a;
                    if (d <= bandWidth)
                    {
                        a = 1f;
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(bandWidth, bandWidth + softness, d);
                        a = 1f - t;
                    }

                    a = Mathf.Clamp01(a) * intensity;

                    // Add very slight falloff towards corners so it looks less like a flat rectangle.
                    float corner = 1f - Mathf.Clamp01(Mathf.Abs((u - 0.5f) * 2f) * Mathf.Abs((v - 0.5f) * 2f));
                    a *= Mathf.Lerp(0.75f, 1f, corner);

                    generated.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            generated.Apply(false, false);

            if (targetMaterial.HasProperty(textureProperty))
                targetMaterial.SetTexture(textureProperty, generated);
        }

        private void OnValidate()
        {
            size = Mathf.Clamp(size, 8, 2048);
            bandWidth = Mathf.Max(0.001f, bandWidth);
            softness = Mathf.Max(0f, softness);
            intensity = Mathf.Max(0f, intensity);
        }
    }
}
