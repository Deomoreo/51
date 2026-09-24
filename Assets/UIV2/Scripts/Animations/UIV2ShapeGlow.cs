using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Project51.Core;

namespace Project51.UIV2.Animations
{
    /// <summary>Soft silhouette copies of the actual sliced sprite mesh, never a stretched pill.</summary>
    [RequireComponent(typeof(Image)), DisallowMultipleComponent]
    public sealed class UIV2ShapeGlow : BaseMeshEffect
    {
        [SerializeField] private Image source;
        private readonly List<UIVertex> vertices = new List<UIVertex>();
        public Image Source => source;
        public void Configure(Image target) { source = target; Sync(); }
        protected override void OnEnable()
        {
            base.OnEnable(); Sync();
            GamePreferences.Changed += Sync;
            Canvas.willRenderCanvases += PreserveExpandedGeometry;
        }
        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= PreserveExpandedGeometry;
            GamePreferences.Changed -= Sync;
            base.OnDisable();
        }
        private void PreserveExpandedGeometry()
        {
            // RectMask2D culls by the unexpanded rect. Keep halo triangles available at its
            // edges; the normal canvas clip rectangle still clips their pixels on the GPU.
            if (IsActive() && graphic != null && graphic.enabled && source != null)
                graphic.canvasRenderer.cull = false;
        }
        private void LateUpdate() { Sync(); }
        private void Sync()
        {
            if (source == null || graphic == null) return;
            var image = (Image)graphic;
            if (GamePreferences.ReducedGraphics) { image.enabled = false; return; }
            image.raycastTarget = false;
            image.sprite = source.overrideSprite;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
            image.fillCenter = source.fillCenter;
            image.fillAmount = source.fillAmount;
            image.fillMethod = source.fillMethod;
            image.fillOrigin = source.fillOrigin;
            image.fillClockwise = source.fillClockwise;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.enabled = source.enabled && source.gameObject.activeInHierarchy;
            var target = source.rectTransform;
            var rect = image.rectTransform;
            rect.anchorMin = target.anchorMin; rect.anchorMax = target.anchorMax;
            rect.pivot = target.pivot; rect.sizeDelta = target.sizeDelta;
            rect.anchoredPosition3D = target.anchoredPosition3D;
            rect.localRotation = target.localRotation; rect.localScale = target.localScale;
        }
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || source == null) return;
            vertices.Clear(); vh.GetUIVertexStream(vertices); vh.Clear();
            // Two radii approximate a soft convolution of alpha without texture readback.
            for (int ring = 0; ring < 2; ring++)
                for (int step = 0; step < 12; step++)
                {
                    float angle = step * Mathf.PI / 6;
                    var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * (ring == 0 ? 5f : 12f);
                    int first = vh.currentVertCount;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        var vertex = vertices[i]; vertex.position += offset;
                        var color = vertex.color; color.a = (byte)(color.a * (ring == 0 ? 0.095f : 0.038f));
                        vertex.color = color; vh.AddVert(vertex);
                    }
                    for (int i = 0; i < vertices.Count; i += 3) vh.AddTriangle(first+i, first+i+1, first+i+2);
                }
        }
    }
}
