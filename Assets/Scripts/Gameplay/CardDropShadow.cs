using UnityEngine;

namespace Project51.Unity
{
    /// <summary>Shared soft contact shadow for cards and disposable animation copies.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class CardDropShadow : MonoBehaviour
    {
        private SpriteRenderer source;
        private SpriteRenderer shadow;
        private MaterialPropertyBlock properties;
        private float elevation;
        private bool ownsAssets;
        private static Sprite sharedSprite;
        private static Material sharedMaterial;
        private static int users;
        private static readonly int Dissolve = Shader.PropertyToID("_K4Dissolve");

        public void Bind(SpriteRenderer renderer)
        {
            source = renderer;
            if (source == null) return;
            if (shadow == null)
            {
                AcquireAssets();
                var child = new GameObject("Card contact shadow") { hideFlags = HideFlags.DontSave };
                child.layer = source.gameObject.layer;
                child.transform.SetParent(source.transform, false);
                shadow = child.AddComponent<SpriteRenderer>();
                shadow.sprite = sharedSprite;
                shadow.sharedMaterial = sharedMaterial;
                properties = new MaterialPropertyBlock();
                users++;
                ownsAssets = true;
            }
            else if (shadow.transform.parent != source.transform)
                shadow.transform.SetParent(source.transform, false);
            LateUpdate();
        }

        public void SetElevation(float amount) => elevation = Mathf.Clamp01(amount);

        private void LateUpdate()
        {
            if (shadow == null) return;
            shadow.enabled = isActiveAndEnabled && source != null && source.enabled &&
                !source.forceRenderingOff && source.gameObject.activeInHierarchy && source.sprite != null;
            if (!shadow.enabled) return;

            var bounds = source.sprite.bounds;
            var center = bounds.center;
            if (source.flipX) center.x = -center.x;
            if (source.flipY) center.y = -center.y;
            // Keep the light direction fixed in table space even when a hand card is rotated.
            var offset = new Vector3(.025f, -.04f, .015f) * Mathf.Lerp(1f, 2.6f, elevation);
            shadow.transform.position = source.transform.TransformPoint(center) + offset;
            shadow.transform.localRotation = Quaternion.identity;
            float spread = Mathf.Lerp(1.16f, 1.3f, elevation);
            shadow.transform.localScale = new Vector3(bounds.size.x * spread / sharedSprite.bounds.size.x,
                bounds.size.y * spread / sharedSprite.bounds.size.y, 1f);
            shadow.sortingLayerID = source.sortingLayerID;
            // Same order keeps the lowest card above the felt (-1). The shared material's
            // earlier transparent queue places the shadow behind its face without changing
            // the existing consecutive card orders or Matta halo ordering.
            shadow.sortingOrder = source.sortingOrder;
            shadow.maskInteraction = source.maskInteraction;
            shadow.gameObject.layer = source.gameObject.layer;
            source.GetPropertyBlock(properties);
            float dissolve = Mathf.Clamp01(properties.GetFloat(Dissolve));
            shadow.color = new Color(0f, 0f, 0f,
                source.color.a * (1f - dissolve) * Mathf.Lerp(.32f, .2f, elevation));
        }

        private void OnDisable()
        {
            elevation = 0f;
            if (shadow != null) shadow.enabled = false;
        }

        private void OnEnable() => LateUpdate();

        private void OnDestroy()
        {
            if (!ownsAssets) return;
            ownsAssets = false;
            if (shadow != null) Release(shadow.gameObject);
            shadow = null;
            if (--users != 0) return;
            if (sharedSprite != null) Release(sharedSprite.texture);
            Release(sharedSprite);
            Release(sharedMaterial);
            sharedSprite = null;
            sharedMaterial = null;
        }

        private static void Release(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }

        private static void AcquireAssets()
        {
            if (sharedSprite != null) return;
            const int width = 64, height = 96;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "K5 shared soft shadow", hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var q = new Vector2(Mathf.Abs(x + .5f - width * .5f) - 22f,
                    Mathf.Abs(y + .5f - height * .5f) - 38f);
                float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude +
                    Mathf.Min(Mathf.Max(q.x, q.y), 0) - 3f;
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-3f, 7f, distance));
                pixels[y * width + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            sharedSprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f, width,
                0, SpriteMeshType.FullRect);
            sharedSprite.hideFlags = HideFlags.HideAndDontSave;
            sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "K5 shared shadow", hideFlags = HideFlags.HideAndDontSave, renderQueue = 2999
            };
        }
    }
}
