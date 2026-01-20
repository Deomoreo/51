using UnityEngine;

namespace Project51.Unity
{
    /// <summary>
    /// UI-friendly wrapper per ProceduralFoilNoiseTexture.
    /// Genera una texture di rumore e la assegna ad un Material (istanza o shared) su una proprietà.
    /// </summary>
    public sealed class UICardFoilNoiseTexture : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Material targetMaterial;
        [SerializeField] private string textureProperty = "_FoilTex";

        [Header("Texture")]
        [SerializeField] private int size = 512;
        [SerializeField] private int seed = 12345;
        [SerializeField] private float noiseScale = 6f;
        [SerializeField] private bool useColorNoise = true;
        [SerializeField] private bool regenerateOnEnable = true;

        [Header("Post")]
        [SerializeField] private float contrast = 1.35f;
        [SerializeField] private float brightness = 0.1f;

        private Texture2D _generated;

        private void OnEnable()
        {
            if (regenerateOnEnable)
                GenerateAndApply();
        }

        private void OnDisable()
        {
            if (_generated != null)
            {
                Destroy(_generated);
                _generated = null;
            }
        }

        [ContextMenu("Generate And Apply")]
        public void GenerateAndApply()
        {
            if (targetMaterial == null) return;
            if (size < 8) size = 8;

            if (_generated != null)
            {
                Destroy(_generated);
                _generated = null;
            }

            _generated = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            _generated.wrapMode = TextureWrapMode.Repeat;
            _generated.filterMode = FilterMode.Bilinear;

            var rng = new System.Random(seed);
            float ox = (float)rng.NextDouble() * 1000f;
            float oy = (float)rng.NextDouble() * 1000f;
            float orx = (float)rng.NextDouble() * 1000f;
            float ory = (float)rng.NextDouble() * 1000f;

            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;

                    float n1 = Mathf.PerlinNoise(u * noiseScale + ox, v * noiseScale + oy);
                    float n2 = Mathf.PerlinNoise(u * (noiseScale * 2.31f) + orx, v * (noiseScale * 2.31f) + ory);
                    float n = (n1 * 0.65f + n2 * 0.35f);

                    float lines = Mathf.Abs(Mathf.Sin((u + v) * 40f)) * 0.12f;
                    n = Mathf.Clamp01(n + lines);

                    n = Mathf.Clamp01((n - 0.5f) * contrast + 0.5f + brightness);

                    if (useColorNoise)
                    {
                        float r = Mathf.Clamp01(n + (Mathf.PerlinNoise(u * (noiseScale * 1.7f) + 13.1f, v * (noiseScale * 1.7f) + 7.7f) - 0.5f) * 0.25f);
                        float g = Mathf.Clamp01(n + (Mathf.PerlinNoise(u * (noiseScale * 1.9f) + 31.4f, v * (noiseScale * 1.9f) + 19.2f) - 0.5f) * 0.25f);
                        float b = Mathf.Clamp01(n + (Mathf.PerlinNoise(u * (noiseScale * 1.5f) + 5.3f, v * (noiseScale * 1.5f) + 41.9f) - 0.5f) * 0.25f);
                        _generated.SetPixel(x, y, new Color(r, g, b, 1f));
                    }
                    else
                    {
                        _generated.SetPixel(x, y, new Color(n, n, n, 1f));
                    }
                }
            }

            _generated.Apply(false, false);

            if (targetMaterial.HasProperty(textureProperty))
                targetMaterial.SetTexture(textureProperty, _generated);
        }

        private void OnValidate()
        {
            size = Mathf.Clamp(size, 8, 2048);
            noiseScale = Mathf.Max(0.1f, noiseScale);
            contrast = Mathf.Max(0f, contrast);
        }
    }
}
