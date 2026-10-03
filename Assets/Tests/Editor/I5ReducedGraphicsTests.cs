using System.Reflection;
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using Project51.UIV2.Animations;
using UnityEngine;
using UnityEngine.UI;
using Project51.UIV2.Components;
using Project51.UIV2.Core;
using DG.Tweening;

public class I5ReducedGraphicsTests
{
    private readonly string[] keys = { "Settings_ReducedGraphics", "Settings_FastAnimations", "Settings_AnimazioniVeloci", "Settings_GraphicsQuality" };
    private readonly bool[] existed = new bool[4];
    private readonly int[] values = new int[4];

    [SetUp]
    public void SavePreferences()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            existed[i] = PlayerPrefs.HasKey(keys[i]);
            values[i] = PlayerPrefs.GetInt(keys[i]);
            PlayerPrefs.DeleteKey(keys[i]);
        }
        ClearCache();
    }

    [TearDown]
    public void RestorePreferences()
    {
        bool previous = existed[0] ? values[0] != 0 : existed[1] ? values[1] != 0 : values[2] != 0;
        GamePreferences.SetReducedGraphics(previous);
        for (int i = 0; i < keys.Length; i++)
        {
            if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]);
            else PlayerPrefs.DeleteKey(keys[i]);
        }
        ClearCache();
        PlayerPrefs.Save();
    }

    private static void ClearCache()
    {
        foreach (string field in new[] { "fastAnimations", "graphicsQuality" })
            typeof(GamePreferences).GetField(field, BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, -1);
    }

    private static bool Reduced()
    {
        var property = typeof(GamePreferences).GetProperty("ReducedGraphics");
        Assert.IsNotNull(property, "I5 needs the shared reduced graphics preference.");
        return (bool)property.GetValue(null);
    }

    private static void SetReduced(bool value)
    {
        var setter = typeof(GamePreferences).GetMethod("SetReducedGraphics");
        Assert.IsNotNull(setter, "I5 needs the shared reduced graphics preference.");
        setter.Invoke(null, new object[] { value });
    }

    [TestCase("Settings_FastAnimations")]
    [TestCase("Settings_AnimazioniVeloci")]
    public void ExistingFastChoiceMigratesOnceAndKeepsAnimationsShort(string legacy)
    {
        PlayerPrefs.SetInt(legacy, 1);
        Assert.IsTrue(Reduced());
        Assert.AreEqual(GamePreferences.QualityLow, PlayerPrefs.GetInt(GamePreferences.GraphicsQualityKey, -1));
        Assert.Less(GamePreferences.Scaled(1f), 1f);
        SetReduced(false);
        PlayerPrefs.SetInt(legacy, 1);
        ClearCache();
        Assert.IsFalse(Reduced(), "An explicit new choice must win over old settings on restart.");
    }

    [Test]
    public void GraphicsQualityAndFastAnimationsAreIndependent()
    {
        GamePreferences.SetGraphicsQuality(GamePreferences.QualityLow);
        GamePreferences.SetFastAnimations(false);
        Assert.IsTrue(GamePreferences.ReducedGraphics);
        Assert.AreEqual(1f, GamePreferences.Scaled(1f));

        GamePreferences.SetGraphicsQuality(GamePreferences.QualityMedium);
        GamePreferences.SetFastAnimations(true);
        ClearCache();
        Assert.AreEqual(GamePreferences.QualityMedium, GamePreferences.GraphicsQuality);
        Assert.IsFalse(GamePreferences.ReducedGraphics);
        Assert.Less(GamePreferences.Scaled(1f), 1f);
    }

    [Test]
    public void TableChoiceTakesPrecedenceOverOlderHomeChoice()
    {
        PlayerPrefs.SetInt(keys[1], 0);
        PlayerPrefs.SetInt(keys[2], 1);
        Assert.IsFalse(Reduced());
    }

    [Test]
    public void ReducedGraphicsSuppressesNewFeedbackWithoutChangingVibration()
    {
        bool vibration = GamePreferences.VibrationEnabled;
        int presented = 0;
        System.Action<FeedbackKind, Vector2> listener = (kind, position) => presented++;
        GameFeedback.SetParticlesEnabled(true);
        GameFeedback.Presented += listener;
        try
        {
            SetReduced(true);
            GameFeedback.Present(FeedbackKind.Scopa, false, Vector2.zero);
            Assert.AreEqual(0, presented);
            Assert.IsFalse(GameFeedback.ParticlesEnabled);
            Assert.AreEqual(vibration, GamePreferences.VibrationEnabled);
            SetReduced(false);
            GameFeedback.Present(FeedbackKind.Scopa, false, Vector2.zero);
            Assert.AreEqual(1, presented);
        }
        finally { GameFeedback.Presented -= listener; }
    }

    [Test]
    public void ReducedBlurExitsBeforeWaitingForScreenshotEvenOnInactivePanel()
    {
        var go = new GameObject("I5 blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        try
        {
            go.SetActive(false);
            SetReduced(true);
            Assert.IsFalse(go.GetComponent<BackdropBlur>().Capture().MoveNext());
            Assert.IsFalse(go.GetComponent<RawImage>().enabled);
            Assert.IsNull(go.GetComponent<RawImage>().texture);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void ExistingCardEffectRestoresMaterialAndNewSweepIsBlocked()
    {
        var go = new GameObject("I5 card", typeof(SpriteRenderer), typeof(CardShaderEffect));
        try
        {
            SetReduced(false);
            var renderer = go.GetComponent<SpriteRenderer>();
            var effect = go.GetComponent<CardShaderEffect>();
            // Plain MonoBehaviours do not receive OnEnable in EditMode fixtures.
            Invoke(effect, "OnEnable");
            effect.Bind(renderer);
            var original = renderer.sharedMaterial;
            effect.PlaySweep();
            Assert.AreNotSame(original, renderer.sharedMaterial);
            SetReduced(true);
            Assert.AreSame(original, renderer.sharedMaterial);
            effect.PlaySweep();
            Assert.AreSame(original, renderer.sharedMaterial);
            SetReduced(false);
            effect.PlaySweep();
            Assert.AreNotSame(original, renderer.sharedMaterial);
        }
        finally { Invoke(go.GetComponent<CardShaderEffect>(), "OnDisable"); Object.DestroyImmediate(go); }
    }

    [Test]
    public void ReducedGlowHidesOnlyDecorationAndRestoresOnPreferenceChange()
    {
        var source = new GameObject("I5 source", typeof(RectTransform), typeof(Image));
        var glow = new GameObject("I5 glow", typeof(RectTransform), typeof(Image), typeof(UIV2ShapeGlow));
        try
        {
            SetReduced(false);
            glow.GetComponent<UIV2ShapeGlow>().Configure(source.GetComponent<Image>());
            Assert.IsTrue(glow.GetComponent<Image>().enabled);
            SetReduced(true);
            Assert.IsFalse(glow.GetComponent<Image>().enabled);
            Assert.IsTrue(source.GetComponent<Image>().enabled);
            SetReduced(false);
            Assert.IsTrue(glow.GetComponent<Image>().enabled);
        }
        finally { Object.DestroyImmediate(glow); Object.DestroyImmediate(source); }
    }

    private static void Invoke(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    [Test]
    public void ExistingBackdropRestoresWithoutCapturingTheOpenModal()
    {
        var go = new GameObject("I5 restored blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        var source = new Texture2D(16, 16);
        var blur = go.GetComponent<BackdropBlur>();
        try
        {
            SetReduced(false);
            Invoke(blur, "OnEnable");
            bool rendered = (bool)typeof(BackdropBlur).GetMethod("RenderBlur", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(blur, new object[] { source, 16, 16 });
            Assert.IsTrue(rendered);
            var image = go.GetComponent<RawImage>();
            var snapshot = image.texture;
            SetReduced(true);
            Assert.IsFalse(image.enabled);
            Assert.IsNull(image.texture);
            SetReduced(false);
            Assert.IsTrue(image.enabled);
            Assert.AreSame(snapshot, image.texture);
            Invoke(blur, "OnDisable");
            Assert.IsNull(image.texture, "Closing the modal releases its snapshot.");
        }
        finally { Invoke(blur, "OnDisable"); Object.DestroyImmediate(go); Object.DestroyImmediate(source); }
    }

    [Test]
    public void ReducingResultsStopsLoopingConfettiAndRestoresOnlyWhileVisible()
    {
        var go = new GameObject("I5 results");
        var confetti = new GameObject("Confetti", typeof(RectTransform));
        confetti.transform.SetParent(go.transform);
        var piece = new GameObject("Piece", typeof(RectTransform));
        piece.transform.SetParent(confetti.transform);
        var results = go.AddComponent<MatchResultsV2>();
        results.ConfettiRoot = (RectTransform)confetti.transform;
        results.MatchPanel = go;
        try
        {
            SetReduced(false);
            typeof(MatchResultsV2).GetField("finished", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(results, true);
            Invoke(results, "OnEnable");
            Invoke(results, "PlayConfetti");
            Assert.IsTrue(DOTween.IsTweening(piece.transform));
            SetReduced(true);
            Assert.IsFalse(confetti.activeSelf);
            Assert.IsFalse(DOTween.IsTweening(piece.transform));
            SetReduced(false);
            Assert.IsTrue(confetti.activeSelf);
            Assert.IsTrue(DOTween.IsTweening(piece.transform));
            go.SetActive(false);
            SetReduced(true);
            SetReduced(false);
            Assert.IsFalse(confetti.activeSelf);
        }
        finally { Invoke(results, "OnDisable"); Object.DestroyImmediate(go); }
    }

    public static System.Collections.IEnumerator RunRuntimeChecks()
    {
        bool previous = GamePreferences.ReducedGraphics;
        var card = new GameObject("I5 runtime card", typeof(SpriteRenderer), typeof(CardShaderEffect));
        var surface = new GameObject("I5 runtime surface", typeof(RectTransform), typeof(Image));
        var blurObject = new GameObject("I5 runtime blur", typeof(RectTransform), typeof(RawImage), typeof(BackdropBlur));
        var motes = new GameObject("I5 runtime motes", typeof(RectTransform), typeof(UIV2MoteField));
        try
        {
            GamePreferences.SetReducedGraphics(false);
            var effect = card.GetComponent<CardShaderEffect>();
            var renderer = card.GetComponent<SpriteRenderer>();
            effect.Bind(renderer);
            var originalCard = renderer.sharedMaterial;
            effect.SetHolographic(true);
            Assert.AreNotSame(originalCard, renderer.sharedMaterial);
            var feedback = Object.FindObjectOfType<UIV2FeedbackParticles>();
            Assert.IsNotNull(feedback);
            feedback.Burst(FeedbackKind.Victory, new Vector2(.5f, .5f));
            Assert.Greater(feedback.GetComponentsInChildren<ParticleSystem>().Length, 0);

            var blur = blurObject.GetComponent<BackdropBlur>();
            var pending = blur.Capture();
            Assert.IsTrue(pending.MoveNext(), "Full graphics starts the end-of-frame capture wait.");
            GamePreferences.SetReducedGraphics(true);
            Assert.IsFalse(pending.MoveNext(), "Reduction cancels the pending screenshot.");
            Assert.IsNull(blurObject.GetComponent<RawImage>().texture);
            Assert.IsFalse(blur.Capture().MoveNext());
            Assert.AreSame(originalCard, renderer.sharedMaterial);
            Assert.AreEqual(0, feedback.GetComponentsInChildren<ParticleSystem>().Length, "Active bursts cleared immediately.");
            feedback.Burst(FeedbackKind.Scopa, Vector2.zero);
            Assert.AreEqual(0, feedback.GetComponentsInChildren<ParticleSystem>().Length);

            var image = surface.GetComponent<Image>();
            var originalSurface = image.material;
            var decoration = surface.AddComponent<UIV2SurfaceEffect>();
            decoration.Configure(UIV2SurfaceEffect.Surface.Background);
            Assert.AreSame(originalSurface, image.material, "New effects honor the saved policy.");
            motes.GetComponent<UIV2MoteField>().Burst();
            using (var mesh = new VertexHelper())
            {
                typeof(UIV2MoteField).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(motes.GetComponent<UIV2MoteField>(), new object[] { mesh });
                Assert.AreEqual(0, mesh.currentVertCount);
            }
            yield return null;
            foreach (var shader in Object.FindObjectsOfType<UIV2SurfaceEffect>())
                Assert.AreNotEqual("Project51/UI/Surface", shader.GetComponent<Graphic>().material.shader.name);
            foreach (var glow in Object.FindObjectsOfType<UIV2ShapeGlow>())
                Assert.IsFalse(glow.GetComponent<Image>().enabled);

            GamePreferences.SetReducedGraphics(false);
            Assert.AreNotSame(originalCard, renderer.sharedMaterial, "Persistent holographic intent restores.");
            Assert.AreNotSame(originalSurface, image.material);
            surface.SetActive(false);
            GamePreferences.SetReducedGraphics(true);
            surface.SetActive(true);
            Assert.AreSame(originalSurface, image.material, "Reopened effects honor reduction.");
            GamePreferences.SetReducedGraphics(false);
            Assert.AreNotSame(originalSurface, image.material);
            Debug.Log("I5_RUNTIME_PASS: active bursts, new/reopened shaders, card material restoration, glow, motes and pending blur.");
        }
        finally
        {
            Object.Destroy(card); Object.Destroy(surface); Object.Destroy(blurObject); Object.Destroy(motes);
            GamePreferences.SetReducedGraphics(previous);
        }
    }

    public static System.Collections.IEnumerator RunTableRuntimeChecks()
    {
        var table = Object.FindObjectOfType<InGameSettingsV2>(true);
        Assert.IsNotNull(table);
        GamePreferences.SetReducedGraphics(true);
        if (table.IsOpen) table.Hide();
        yield return new WaitForSecondsRealtime(.3f);
        table.Open();
        yield return new WaitForSecondsRealtime(.3f);
        Assert.IsNotNull(table.GraphicsSwitch);
        System.Action flip = () => table.GraphicsSwitch.Toggle();
        Assert.IsTrue(table.GraphicsSwitch.isOn);
        Assert.IsFalse(table.Blur.HasSnapshot);
        Assert.IsFalse(table.Blur.GetComponent<RawImage>().enabled);
        flip();
        yield return new WaitForEndOfFrame();
        yield return null;
        Assert.IsFalse(GamePreferences.ReducedGraphics);
        Assert.IsTrue(table.Blur.HasSnapshot, "Disabling reduction in a panel opened reduced captures a clean backdrop.");
        Assert.IsTrue(table.Blur.GetComponent<RawImage>().enabled);
        Assert.AreEqual(1f, table.GetComponent<CanvasGroup>().alpha);
        var snapshot = table.Blur.GetComponent<RawImage>().texture;
        flip();
        Assert.IsFalse(table.Blur.GetComponent<RawImage>().enabled);
        flip();
        Assert.AreSame(snapshot, table.Blur.GetComponent<RawImage>().texture);
        Assert.IsTrue(table.Blur.GetComponent<RawImage>().enabled);
        Debug.Log("I5_TABLE_PASS: shared toggle, initial reduced backdrop, clean capture and immediate snapshot restore.");
    }
}

// Existing shader/blur tests require full graphics, regardless of the user's saved choice.
[SetUpFixture]
public class GraphicsPreferencesTestScope
{
    private readonly string[] keys = { "Settings_ReducedGraphics", "Settings_FastAnimations", "Settings_AnimazioniVeloci", "Settings_GraphicsQuality" };
    private readonly bool[] existed = new bool[4];
    private readonly int[] values = new int[4];

    [OneTimeSetUp]
    public void EnterFullGraphics()
    {
        for (int i = 0; i < keys.Length; i++) { existed[i] = PlayerPrefs.HasKey(keys[i]); values[i] = PlayerPrefs.GetInt(keys[i]); }
        GamePreferences.SetReducedGraphics(false);
    }

    [OneTimeTearDown]
    public void RestoreUserChoice()
    {
        GamePreferences.SetReducedGraphics(existed[0] ? values[0] != 0 : existed[1] ? values[1] != 0 : values[2] != 0);
        for (int i = 0; i < keys.Length; i++)
        {
            if (existed[i]) PlayerPrefs.SetInt(keys[i], values[i]);
            else PlayerPrefs.DeleteKey(keys[i]);
        }
        typeof(GamePreferences).GetField("graphicsQuality", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, -1);
        typeof(GamePreferences).GetField("fastAnimations", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, -1);
        PlayerPrefs.Save();
    }
}
