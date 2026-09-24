using UnityEngine;
using UnityEngine.UI;
using Project51.Core;

namespace Project51.UIV2.Animations
{
    /// <summary>Atlas-safe local coordinates; source UVs, tint and UI masking stay intact.</summary>
    [DisallowMultipleComponent]
    public sealed class UIV2SurfaceEffect : BaseMeshEffect
    {
        public enum Surface { Sweep, Silhouette, Background, Holographic }
        [SerializeField] private Surface surface;
        private Material owned;
        private Material original;
        public Surface Mode => surface;

        public void Configure(Surface mode)
        {
            surface = mode;
            if (isActiveAndEnabled) Install();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GamePreferences.Changed += Install;
            Install();
        }
        private void Install()
        {
            if (GamePreferences.ReducedGraphics) { ReleaseMaterial(); return; }
            // Keep transient runtime materials out of saved prefabs and scenes.
            if (graphic == null || !Application.isPlaying) return;
            if (owned == null)
            {
                var shader = Resources.Load<Shader>("K4/UISurface");
                if (shader == null || !shader.isSupported) return;
                original = graphic.material;
                owned = new Material(shader) { name = "K4 " + surface, hideFlags = HideFlags.HideAndDontSave };
            }
            owned.SetFloat("_Mode", (float)surface);
            graphic.material = owned;
            if (graphic.canvas != null) graphic.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            graphic.SetVerticesDirty();
        }

        protected override void OnCanvasHierarchyChanged() { base.OnCanvasHierarchyChanged(); if (isActiveAndEnabled) Install(); }
        protected override void OnDisable()
        {
            GamePreferences.Changed -= Install;
            ReleaseMaterial();
            base.OnDisable();
        }

        private void ReleaseMaterial()
        {
            if (graphic != null && graphic.material == owned) graphic.material = original;
            if (owned != null)
            {
                if (Application.isPlaying) Destroy(owned); else DestroyImmediate(owned);
                owned = null;
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || graphic == null || GamePreferences.ReducedGraphics) return;
            var rect = graphic.rectTransform.rect;
            var vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                vertex.uv1 = new Vector2((vertex.position.x - rect.xMin) / Mathf.Max(1, rect.width),
                    (vertex.position.y - rect.yMin) / Mathf.Max(1, rect.height));
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
