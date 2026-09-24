using NUnit.Framework;
using UnityEngine;
using Project51.Unity;
using System.Collections;
using UnityEngine.TestTools;

public class K4CardShaderTests
{
    [Test]
    public void ResetRestoresMaterialAndExistingPropertiesAcrossRepeatedUse()
    {
        var go = new GameObject("card");
        try
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            var original = renderer.sharedMaterial;
            var block = new MaterialPropertyBlock();
            int other = Shader.PropertyToID("_UnrelatedProperty");
            block.SetFloat(other, 0.37f);
            renderer.SetPropertyBlock(block);
            var effect = go.AddComponent<CardShaderEffect>();
            effect.Bind(renderer);
            for (int i = 0; i < 3; i++)
            {
                effect.SetDissolve(0.5f);
                Assert.AreNotSame(original, renderer.sharedMaterial);
                effect.ResetEffects();
                Assert.AreSame(original, renderer.sharedMaterial);
                renderer.GetPropertyBlock(block);
                Assert.AreEqual(0.37f, block.GetFloat(other));
                Assert.AreEqual(0f, block.GetFloat("_K4Dissolve"));
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    [UnityTest, Explicit]
    public IEnumerator DisableRestoresRendererAndDoesNotAffectOtherCard()
    {
        if (!Application.isPlaying) Assert.Ignore("Run explicitly in PlayMode");
        yield return null;
        var a = new GameObject("a");
        var b = new GameObject("b");
        try
        {
            var ar = a.AddComponent<SpriteRenderer>();
            var br = b.AddComponent<SpriteRenderer>();
            var original = ar.sharedMaterial;
            var ae = a.AddComponent<CardShaderEffect>();
            var be = b.AddComponent<CardShaderEffect>();
            ae.Bind(ar); be.Bind(br);
            ae.SetDissolve(0.6f); be.SetDissolve(0.2f);
            a.SetActive(false);
            Assert.AreSame(original, ar.sharedMaterial);
            var block = new MaterialPropertyBlock();
            br.GetPropertyBlock(block);
            Assert.AreEqual(0.2f, block.GetFloat("_K4Dissolve"));
            Object.DestroyImmediate(be);
            Assert.AreSame(original, br.sharedMaterial);
        }
        finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
    }

    [UnityTest, Explicit]
    public IEnumerator ClearAndPoolResetOptInDecoration()
    {
        if (!Application.isPlaying) Assert.Ignore("Run explicitly in PlayMode");
        yield return null;
        var go = new GameObject("view");
        try
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            var original = renderer.sharedMaterial;
            var view = go.AddComponent<CardView>();
            view.SetHolographic(true);
            Assert.AreNotSame(original, renderer.sharedMaterial);
            view.ClearMattaTransform();
            Assert.AreSame(original, renderer.sharedMaterial);
            view.SetHolographic(true);
            go.SetActive(false);
            Assert.AreSame(original, renderer.sharedMaterial);
            go.SetActive(true);
            Assert.AreSame(original, renderer.sharedMaterial);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ResourceShaderExistsWithoutCompilerErrors()
    {
        var shader = Resources.Load<Shader>("K4/CardSurface");
        Assert.IsNotNull(shader);
        Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(shader));
    }
}
