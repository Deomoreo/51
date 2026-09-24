using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Project51.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

public sealed class K4BlurTests
{
    [TestCase(true, 1f, -1f)]
    [TestCase(false, 0f, 1f)]
    public void ScreenshotPresentationCorrectsApiOriginWithoutFlippingOrdinaryTextures(bool startsAtTop, float origin, float height)
    {
        var go = new GameObject("capture orientation", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        try
        {
            var blur = go.GetComponent<BackdropBlur>();
            var present = typeof(BackdropBlur).GetMethod("PresentCapture", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(present, Is.Not.Null);
            present.Invoke(blur, new object[] { Texture2D.whiteTexture, 16, 16, startsAtTop });
            var image = go.GetComponent<RawImage>();
            Assert.That(image.enabled, Is.True);
            Assert.That(image.uvRect, Is.EqualTo(new Rect(0f, origin, 1f, height)));
            var render = typeof(BackdropBlur).GetMethod("RenderBlur", BindingFlags.NonPublic | BindingFlags.Instance);
            render.Invoke(blur, new object[] { Texture2D.whiteTexture, 16, 16 });
            Assert.That(image.uvRect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
        }
        finally
        {
            typeof(BackdropBlur).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(go.GetComponent<BackdropBlur>(), null);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ShaderIsIncludedAndSupported()
    {
        var shader = Resources.Load<Shader>("K4/BackdropBlur");
        Assert.That(shader, Is.Not.Null);
        Assert.That(shader.isSupported, Is.True);
    }

    [Test]
    public void DestroyDuringPendingCaptureDoesNotResumeRendering()
    {
        var go = new GameObject("blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        var capture = go.GetComponent<BackdropBlur>().Capture();
        Assert.That(capture.MoveNext(), Is.True);
        Object.DestroyImmediate(go);
        Assert.That(capture.MoveNext(), Is.False);
    }

    [Test]
    public void DisableCancelsPendingCapture()
    {
        var go = new GameObject("blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        try
        {
            var capture = go.GetComponent<BackdropBlur>().Capture();
            Assert.That(capture.MoveNext(), Is.True);
            go.SetActive(false);
            // Ordinary MonoBehaviours do not receive lifecycle callbacks in EditMode.
            typeof(BackdropBlur).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(go.GetComponent<BackdropBlur>(), null);
            Assert.That(capture.MoveNext(), Is.False);
            Assert.That(go.GetComponent<RawImage>().enabled, Is.False);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void NewCaptureCancelsTheOlderPendingRequestEvenWhenInitiallyInactive()
    {
        var go = new GameObject("blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        try
        {
            go.SetActive(false);
            var blur = go.GetComponent<BackdropBlur>();
            var older = blur.Capture();
            var newer = blur.Capture();
            Assert.That(older.MoveNext(), Is.True);
            Assert.That(newer.MoveNext(), Is.True);
            Assert.That(older.MoveNext(), Is.False);
            ((System.IDisposable)newer).Dispose();
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void GpuBlurSpreadsImpulseAndReleasesOutputOnDisable()
    {
        var go = new GameObject("blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        var source = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
        pixels[16 * 32 + 16] = Color.white;
        source.SetPixels(pixels);
        source.Apply();
        var readback = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            var blur = go.GetComponent<BackdropBlur>();
            var render = typeof(BackdropBlur).GetMethod("RenderBlur", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(render, Is.Not.Null);
            Assert.That((bool)render.Invoke(blur, new object[] { source, 32, 32 }), Is.True);
            var output = go.GetComponent<RawImage>().texture as RenderTexture;
            Assert.That(output, Is.Not.Null);
            RenderTexture.active = output;
            readback.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
            readback.Apply();
            Assert.That(readback.GetPixel(16, 16).r, Is.InRange(0.001f, 0.5f));
            Assert.That(readback.GetPixel(15, 16).r, Is.GreaterThan(0.001f));
            Assert.That(readback.GetPixel(16, 15).r, Is.GreaterThan(0.001f));
            RenderTexture.active = previous;
            Assert.That((bool)render.Invoke(blur, new object[] { source, 32, 32 }), Is.True);
            Assert.That(go.GetComponent<RawImage>().texture, Is.SameAs(output), "Recapture reuses its output.");
            Assert.That((bool)render.Invoke(blur, new object[] { source, 16, 16 }), Is.True);
            Assert.That(output == null || !output.IsCreated(), Is.True, "Resize releases the old allocation.");
            output = go.GetComponent<RawImage>().texture as RenderTexture;
            Assert.That(output.width, Is.EqualTo(16));
            go.SetActive(false);
            typeof(BackdropBlur).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(go.GetComponent<BackdropBlur>(), null);
            Assert.That(go.GetComponent<RawImage>().texture, Is.Null);
            Assert.That(output == null || !output.IsCreated(), Is.True);
        }
        finally
        {
            RenderTexture.active = previous;
            typeof(BackdropBlur).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(go.GetComponent<BackdropBlur>(), null);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(readback);
        }
    }

    [UnityTest, Explicit]
    public IEnumerator RuntimeLifecycleReleasesInactiveCaptureOutputAndCancelsPendingCapture()
    {
        if (!Application.isPlaying) Assert.Ignore("Run explicitly in PlayMode");
        GameObject go = null;
        try
        {
            go = new GameObject("runtime blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
            var blur = go.GetComponent<BackdropBlur>();
            var rawImage = go.GetComponent<RawImage>();
            var render = typeof(BackdropBlur).GetMethod("RenderBlur", BindingFlags.NonPublic | BindingFlags.Instance);
            go.SetActive(false);
            Assert.That((bool)render.Invoke(blur, new object[] { Texture2D.whiteTexture, 16, 16 }), Is.True);
            var output = rawImage.texture as RenderTexture;
            Assert.That(output, Is.Not.Null, "An inactive panel can prepare its GPU output.");
            go.SetActive(true);
            Assert.That(rawImage.texture, Is.SameAs(output));
            var pending = blur.Capture();
            Assert.That(pending.MoveNext(), Is.True);
            go.SetActive(false);
            Assert.That(pending.MoveNext(), Is.False, "The real OnDisable callback cancels the suspended capture.");
            Assert.That(rawImage.texture, Is.Null);
            Assert.That(output == null || !output.IsCreated(), Is.True);

            go.SetActive(true);
            Assert.That((bool)render.Invoke(blur, new object[] { Texture2D.whiteTexture, 16, 16 }), Is.True);
            output = rawImage.texture as RenderTexture;
            Object.Destroy(go);
            yield return null;
            Assert.That(output == null, Is.True, "Destroy releases and destroys the owned GPU output.");
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
