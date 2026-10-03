using System.Reflection;
using NUnit.Framework;
using Project51.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Project51.UIV2.Animations;
using Project51.UIV2.Core;
using DG.Tweening;

public class K6FeedbackTests
{
    private bool hadPreference;
    private int savedPreference;
    private const string Key = "Settings_Vibration";

    [SetUp]
    public void SavePreference()
    {
        hadPreference = PlayerPrefs.HasKey(Key);
        savedPreference = PlayerPrefs.GetInt(Key, 1);
    }

    [TearDown]
    public void RestorePreference()
    {
        GamePreferences.SetVibrationEnabled(savedPreference != 0);
        if (!hadPreference) PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    private static void ResetFeedback() =>
        typeof(GameFeedback).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);

    [Test]
    public void AcceptedClickStillPulsesAfterEarlierListenerClosesPanel()
    {
        ResetFeedback();
        GamePreferences.SetVibrationEnabled(true);
        var go = new GameObject("K6 click", typeof(RectTransform), typeof(Button));
        try
        {
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => go.SetActive(false));
            var haptic = go.AddComponent<UIV2HapticButton>();
            typeof(UIV2HapticButton).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(haptic, null);
            button.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            Assert.IsFalse(go.activeSelf);
            Assert.IsFalse(GameFeedback.TryHaptic(false), "Accepted click should already have consumed the light pulse.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void DisabledButtonDoesNotPulse()
    {
        ResetFeedback();
        GamePreferences.SetVibrationEnabled(true);
        var go = new GameObject("K6 disabled click", typeof(RectTransform), typeof(Button));
        try
        {
            var button = go.GetComponent<Button>();
            var haptic = go.AddComponent<UIV2HapticButton>();
            typeof(UIV2HapticButton).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(haptic, null);
            button.interactable = false;
            button.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
            Assert.IsTrue(GameFeedback.TryHaptic(false), "Rejected click must not consume the pulse.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void ConsecutiveAccusiKeepBothCallbacksAndSecondSlamDoesNotRepeatThem()
    {
        var go = new GameObject("K6 accusi", typeof(RectTransform));
        var caption = new GameObject("Caption", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        caption.transform.SetParent(go.transform);
        try
        {
            var impact = go.AddComponent<AccusoImpactV2>();
            impact.Caption = caption.GetComponent<TMPro.TMP_Text>();
            int local = 0, remote = 0;
            impact.Play("Local", null, () => local++);
            impact.Play("Remote", null, () => remote++);
            var slam = typeof(AccusoImpactV2).GetMethod("Impact", BindingFlags.NonPublic | BindingFlags.Instance);
            slam.Invoke(impact, null);
            slam.Invoke(impact, null);
            Assert.AreEqual(1, local);
            Assert.AreEqual(1, remote);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    public static System.Collections.IEnumerator RunRuntimeChecks()
    {
        var presenter = UnityEngine.Object.FindObjectOfType<UIV2FeedbackParticles>();
        Assert.IsNotNull(presenter);
        bool reduced = GamePreferences.ReducedGraphics;
        GamePreferences.SetReducedGraphics(false);
        var randomState = UnityEngine.Random.state;
        float expected = UnityEngine.Random.value;
        UnityEngine.Random.state = randomState;
        for (int i = 0; i < 20; i++) presenter.Burst(FeedbackKind.Victory, Vector2.one * .5f);
        Assert.AreEqual(expected, UnityEngine.Random.value, "Feedback must not alter gameplay RNG.");
        UnityEngine.Random.state = randomState;
        var systems = presenter.GetComponentsInChildren<ParticleSystem>(true);
        Assert.AreEqual(4, systems.Length, "Repeated bursts must reuse the bounded pool.");
        foreach (var ps in systems) Assert.LessOrEqual(ps.particleCount, 48);
        yield return new WaitForSecondsRealtime(1.2f);
        foreach (var ps in systems) { Assert.AreEqual(0, ps.particleCount); Assert.IsFalse(ps.gameObject.activeInHierarchy); }
        presenter.Burst(FeedbackKind.Accuso, Vector2.one * .5f);
        GamePreferences.SetReducedGraphics(true);
        foreach (var ps in systems) { Assert.AreEqual(0, ps.particleCount); Assert.IsFalse(ps.gameObject.activeInHierarchy); }
        GamePreferences.SetReducedGraphics(reduced);
        Debug.Log("K6_RUNTIME_CHECKS_PASSED: bounded pool, expiry, disable cleanup, gameplay RNG unchanged.");
    }

    [Test]
    public void DisabledPreferenceSuppressesLocalHapticsAndPersists()
    {
        ResetFeedback();
        GamePreferences.SetVibrationEnabled(false);
        Assert.AreEqual(0, PlayerPrefs.GetInt(Key, 1));
        Assert.IsFalse(GameFeedback.TryHaptic(false, true));
    }

    [Test]
    public void RemoteEventsNeverConsumeTheLocalHapticAndDuplicateTapsAreThrottled()
    {
        ResetFeedback();
        GamePreferences.SetVibrationEnabled(true);
        Assert.IsFalse(GameFeedback.TryHaptic(false, false));
        Assert.IsTrue(GameFeedback.TryHaptic(false, true));
        Assert.IsFalse(GameFeedback.TryHaptic(false, true));
        // A confirmed win/impact may supersede a light click in the same frame.
        Assert.IsTrue(GameFeedback.TryHaptic(true, true));
        Assert.IsFalse(GameFeedback.TryHaptic(true, true));
    }
}
