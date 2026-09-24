using UnityEngine;
using Project51.Core;

namespace Project51.Unity
{
    /// <summary>Optional sprite decoration. No game state or rarity is inferred here.</summary>
    [DisallowMultipleComponent]
    public sealed class CardShaderEffect : MonoBehaviour
    {
        private SpriteRenderer target;
        private Material originalMaterial;
        private Material effectMaterial;
        private MaterialPropertyBlock originalProperties;
        private MaterialPropertyBlock properties;
        private float sweepRemaining;
        private float sweepDuration = SweepDuration;
        private float dissolve;
        private bool holographic;
        private const float SweepDuration = 0.65f;

        public void Bind(SpriteRenderer renderer)
        {
            if (target == renderer) return;
            ResetEffects();
            target = renderer;
        }

        public void PlaySweep(float duration = SweepDuration)
        {
            if (!isActiveAndEnabled || !Acquire()) return;
            sweepRemaining = sweepDuration = Mathf.Max(.05f, duration);
            Apply();
        }

        public void SetDissolve(float amount)
        {
            dissolve = Mathf.Clamp01(amount);
            if (!isActiveAndEnabled || !Acquire()) return;
            Apply();
        }

        public void SetHolographic(bool enabled)
        {
            holographic = enabled;
            if (!enabled && dissolve <= 0f && sweepRemaining <= 0f) { ResetEffects(); return; }
            if (isActiveAndEnabled && Acquire()) Apply();
        }

        private bool Acquire()
        {
            if (GamePreferences.ReducedGraphics) return false;
            if (target == null) return false;
            if (effectMaterial != null) return true;
            var shader = Resources.Load<Shader>("K4/CardSurface");
            if (shader == null || !shader.isSupported) return false;
            originalMaterial = target.sharedMaterial;
            originalProperties = new MaterialPropertyBlock();
            target.GetPropertyBlock(originalProperties);
            properties = new MaterialPropertyBlock();
            effectMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            if (originalMaterial != null)
            {
                effectMaterial.CopyPropertiesFromMaterial(originalMaterial);
                effectMaterial.shader = shader;
            }
            target.sharedMaterial = effectMaterial;
            return true;
        }

        private void Update()
        {
            if (effectMaterial == null) return;
            sweepRemaining = Mathf.Max(0f, sweepRemaining - Time.unscaledDeltaTime);
            if (sweepRemaining <= 0f && dissolve <= 0f && !holographic) ResetEffects();
            else Apply();
        }

        private void Apply()
        {
            // Merge with the current block instead of clobbering unrelated renderer overrides.
            target.GetPropertyBlock(properties);
            properties.SetFloat("_K4Sweep", sweepRemaining > 0f ? 1f - sweepRemaining / sweepDuration : -1f);
            properties.SetFloat("_K4Dissolve", dissolve);
            properties.SetFloat("_K4Holographic", holographic ? 1f : 0f);
            var bounds = target.sprite != null ? target.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            properties.SetVector("_K4Bounds", new Vector4(bounds.min.x, bounds.min.y,
                1f / Mathf.Max(bounds.size.x, 0.0001f), 1f / Mathf.Max(bounds.size.y, 0.0001f)));
            target.SetPropertyBlock(properties);
        }

        public void ResetEffects()
        {
            sweepRemaining = 0f;
            dissolve = 0f;
            holographic = false;
            ReleaseMaterial();
        }

        private void ReleaseMaterial()
        {
            if (effectMaterial == null) return;
            if (target != null)
            {
                if (target.sharedMaterial == effectMaterial) target.sharedMaterial = originalMaterial;
                target.SetPropertyBlock(originalProperties);
            }
            if (Application.isPlaying) Destroy(effectMaterial);
            else DestroyImmediate(effectMaterial);
            effectMaterial = null;
            originalMaterial = null;
            originalProperties = null;
        }

        private void OnEnable()
        {
            GamePreferences.Changed += ApplyPreference;
            ApplyPreference();
        }
        private void ApplyPreference()
        {
            if (GamePreferences.ReducedGraphics)
            {
                sweepRemaining = dissolve = 0f;
                ReleaseMaterial();
            }
            else if (holographic && Acquire()) Apply();
        }
        private void OnDisable()
        {
            GamePreferences.Changed -= ApplyPreference;
            ResetEffects();
        }
        private void OnDestroy() => ResetEffects();
    }
}
