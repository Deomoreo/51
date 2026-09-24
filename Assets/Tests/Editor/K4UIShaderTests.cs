#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Project51.UIV2.Animations;
using Project51.UIV2.Core;

namespace Project51.Tests
{
    public class K4UIShaderTests
    {
        [Test]
        public void ReapplyingKitPreservesInputAndDoesNotSerializeRuntimeMaterials()
        {
            var root = new GameObject("Fixture", typeof(RectTransform));
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            sprite.name = "btn_gold_test";
            try
            {
                var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(root.transform, false);
                var image = go.GetComponent<Image>(); image.sprite = sprite;
                var button = go.GetComponent<Button>(); button.targetGraphic = image; button.interactable = false;
                var original = image.material;
                UIV2ShaderKit.Apply(root); UIV2ShaderKit.Apply(root);
                Assert.AreEqual(1, go.GetComponents<UIV2SurfaceEffect>().Length);
                Assert.AreSame(original, image.material);
                Assert.IsFalse(button.interactable);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void ShapeGlowCopiesSliceAndGeometryWithoutChangingTargetOrSelectionState()
        {
            var root = new GameObject("Fixture", typeof(RectTransform));
            var texture = new Texture2D(8, 8);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, Vector4.one);
            try
            {
                var go = new GameObject("Row", typeof(RectTransform), typeof(Image)); go.transform.SetParent(root.transform, false);
                var source = go.GetComponent<Image>(); source.sprite = sprite; source.type = Image.Type.Sliced;
                source.rectTransform.sizeDelta = new Vector2(420, 72);
                source.rectTransform.localScale = Vector3.one * 0.94f;
                var glow = new GameObject("RowSoftGlow", typeof(RectTransform), typeof(Image)); glow.transform.SetParent(root.transform, false);
                glow.SetActive(false);
                UIV2ShaderKit.Apply(root); UIV2ShaderKit.Apply(root);
                Assert.IsFalse(glow.activeSelf, "Selection controls the existing object.");
                Assert.AreSame(source, glow.GetComponent<UIV2ShapeGlow>().Source);
                Assert.AreSame(sprite, glow.GetComponent<Image>().sprite);
                Assert.AreEqual(source.rectTransform.sizeDelta, glow.GetComponent<RectTransform>().sizeDelta);
                Assert.AreEqual(source.rectTransform.localScale, glow.transform.localScale);
                Assert.IsFalse(glow.GetComponent<Image>().raycastTarget);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void SurfaceShaderShipsWithPlayerAndSupportsUIMasks()
        {
            var shader = Resources.Load<Shader>("K4/UISurface");
            Assert.IsNotNull(shader, "K4 shader must be included in the player.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
            var material = new Material(shader);
            try
            {
                foreach (var property in new[] { "_Stencil", "_StencilComp", "_ColorMask", "_UseUIAlphaClip" })
                    Assert.IsTrue(material.HasProperty(property), property);
            }
            finally { Object.DestroyImmediate(material); }
        }
    }
}
#endif
