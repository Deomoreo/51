using System;
using System.Collections;
using System.Reflection;
using DG.Tweening;
using NUnit.Framework;
using Project51.UIV2.Animations;
using Project51.UIV2.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Project51.Tests
{
    // MCP jobs can lose their in-memory callback during Enter/ExitPlayMode. Keep a small local
    // result for this explicit runtime regression, registered again after each domain reload.
    [UnityEditor.InitializeOnLoad]
    internal sealed class MotionTestResultRecorder : UnityEditor.TestTools.TestRunner.Api.ICallbacks
    {
        static MotionTestResultRecorder()
        {
            var api = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
            api.RegisterCallbacks(new MotionTestResultRecorder());
        }

        public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }
        public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result) { }
        public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }
        public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            if (!result.Test.FullName.EndsWith("RuntimeMotionPreservesStateThroughInterruptions")) return;
            if (result.ResultState.StartsWith("Skipped") || result.ResultState == "Explicit") return;
            System.IO.File.WriteAllText("Temp/K1-motion-runtime.txt", DateTime.UtcNow.ToString("O") + "\n" + result.ResultState + "\n" + result.Message + "\n" + result.StackTrace);
        }
    }

    public class UIV2MotionTests
    {
        private GameObject root;
        private float timeScale;

        private static Tween Owned(Component owner)
        {
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("motion", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return (Tween)field.GetValue(owner);
            }
            return null;
        }

        private static GameObject Child(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            foreach (var type in components) go.AddComponent(type);
            return go;
        }

        [UnityTest, Explicit("Run separately: entering PlayMode reloads the MCP test-job tracker.")]
        public IEnumerator RuntimeMotionPreservesStateThroughInterruptions()
        {
            // DOTween.Init deliberately does nothing in EditMode; cancellation needs its real runtime.
            yield return new EnterPlayMode();
            // Create captured callback locals after domain reload, not in the serialized outer iterator.
            yield return RuntimeScenarios();
            yield return new ExitPlayMode();
        }

        private IEnumerator RuntimeScenarios()
        {
            timeScale = Time.timeScale;
            root = new GameObject("K1 runtime tests", typeof(RectTransform));
            System.IO.File.WriteAllText("Temp/K1-motion-runtime.txt", DateTime.UtcNow.ToString("O") + "\nRunning");
            var visual = Child("Visual", root.transform);
            visual.transform.localScale = new Vector3(2, 3, 1);
            var motion = visual.AddComponent<Project51.UIV2.Components.UIV2Button>();
            motion.PlayShow();
            Owned(motion).Complete(true);
            Assert.AreEqual(new Vector3(2, 3, 1), visual.transform.localScale, "Show preserves authored scale");
            motion.PlayPress();
            Owned(motion).Complete(true);
            Assert.AreEqual(new Vector3(2, 3, 1), visual.transform.localScale, "Press preserves authored scale");

            var external = ((RectTransform)visual.transform).DOAnchorPos(Vector2.one * 100, 10);
            motion.PlayPress();
            Assert.IsTrue(external.IsActive(), "Feedback must not cancel another owner's tween");
            external.Kill();
            bool rewarded = false;
            motion.PlayReward(() => rewarded = true);
            var oldReward = Owned(motion);
            visual.SetActive(false);
            Assert.IsFalse(oldReward.IsActive(), "Disabled reward is cancelled");
            Assert.IsFalse(rewarded);
            Assert.AreEqual(new Vector3(2, 3, 1), visual.transform.localScale);

            var modalRoot = Child("Modal", root.transform, typeof(CanvasGroup));
            modalRoot.SetActive(false);
            var modal = modalRoot.AddComponent<AnimatedModalV2>();
            modal.Group = modalRoot.GetComponent<CanvasGroup>();
            modal.Frame = (RectTransform)Child("Frame", modalRoot.transform).transform;
            modal.Frame.anchoredPosition = new Vector2(15, 22);
            modal.Frame.localScale = new Vector3(.8f, 1.2f, 1);
            modal.Open(); modal.Close(); modal.Open();
            Owned(modal).Complete(true);
            Assert.IsTrue(modal.IsOpen && modal.Group.interactable && modal.Group.blocksRaycasts, "Reopened modal is interactive");
            Assert.AreEqual(new Vector2(15, 22), modal.Frame.anchoredPosition);
            Assert.AreEqual(new Vector3(.8f, 1.2f, 1), modal.Frame.localScale);
            modal.Close();
            var oldClose = Owned(modal);
            modalRoot.SetActive(false);
            modalRoot.SetActive(true);
            modal.Open();
            Assert.IsFalse(oldClose.IsActive(), "External disable cancels old close");
            Owned(modal).Complete(true);
            Assert.IsTrue(modalRoot.activeSelf);
            modal.CloseImmediate();
            Assert.IsFalse(modal.Group.blocksRaycasts);

            var buttonGo = Child("Button", root.transform, typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UIV2ButtonFeedback));
            var button = buttonGo.GetComponent<UnityEngine.UI.Button>();
            var feedback = buttonGo.GetComponent<UIV2ButtonFeedback>();
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 7, button = PointerEventData.InputButton.Left };
            button.interactable = false;
            feedback.OnPointerDown(pointer);
            Assert.IsNull(Owned(feedback), "Disabled button has no press animation");
            button.interactable = true;
            feedback.OnPointerDown(pointer);
            Owned(feedback).Complete(true);
            Assert.That(buttonGo.transform.localScale.x, Is.EqualTo(UIV2Motion.PressScale).Within(.001f));
            feedback.OnPointerExit(pointer);
            Assert.AreEqual(Vector3.one, buttonGo.transform.localScale, "Dragging away restores button");
            feedback.OnPointerDown(pointer);
            feedback.OnPointerUp(pointer);
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(buttonGo.transform.localScale.x, Is.EqualTo(1).Within(.001f), "UI release runs while game is paused");

            var counter = Child("Counter", root.transform, typeof(UIV2NumberCounter)).GetComponent<UIV2NumberCounter>();
            counter.SetValue(100);
            counter.SetValue(500);
            Owned(counter).Goto(.1f);
            long displayed = counter.DisplayedValue;
            counter.SetValue(25);
            Assert.AreEqual(displayed, counter.DisplayedValue, "Counter retargets without jumping");
            Owned(counter).Complete(true);
            Assert.AreEqual(25, counter.DisplayedValue);
            counter.SetValue(long.MaxValue);
            Owned(counter).Complete(true);
            Assert.AreEqual(long.MaxValue, counter.DisplayedValue, "Large value stays exact");
            counter.SetValue(long.MinValue);
            counter.gameObject.SetActive(false);
            Assert.AreEqual(long.MinValue, counter.DisplayedValue, "Disabled counter commits last authoritative value");

            var icon = Child("Reward icon", root.transform, typeof(UIV2RewardFlight));
            var target = Child("Destination", root.transform);
            target.transform.localPosition = Vector3.one * 80;
            var flight = icon.GetComponent<UIV2RewardFlight>();
            bool arrived = false;
            flight.Play((RectTransform)target.transform, () => arrived = true);
            target.SetActive(false);
            Owned(flight).Goto(.1f);
            Assert.IsFalse(arrived, "Missing destination cancels, never awards anything");
            Assert.AreEqual(Vector3.zero, icon.transform.localPosition);
            Assert.IsFalse(icon.activeSelf, "Cancelled decorative flight is hidden");
            target.SetActive(true);
            icon.SetActive(true);
            flight.Play((RectTransform)target.transform, () => arrived = true);
            Owned(flight).Complete(true);
            Assert.IsTrue(arrived);
            Assert.IsFalse(icon.activeSelf, "Decorative icon is hidden on arrival");
            Assert.AreEqual(Vector3.zero, icon.transform.localPosition);

            Time.timeScale = timeScale;
            UnityEngine.Object.DestroyImmediate(root);
            root = null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Application.isPlaying)
            {
                Time.timeScale = timeScale;
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                yield return new ExitPlayMode();
            }
        }

        [Test]
        public void BuilderIsRepeatableAndPreservesClickListeners()
        {
            var panel = new GameObject("Builder test", typeof(RectTransform));
            try
            {
                var button = Child("Confirm", panel.transform, typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
                var dim = Child("Dim", panel.transform, typeof(UnityEngine.UI.Button));
                bool clicked = false;
                button.onClick.AddListener(() => clicked = true);
                Assert.AreEqual(1, UIV2MotionInstaller.Apply(panel));
                Assert.AreEqual(0, UIV2MotionInstaller.Apply(panel));
                Assert.IsNull(dim.GetComponent<UIV2ButtonFeedback>());
                button.onClick.Invoke();
                Assert.IsTrue(clicked);
            }
            finally { UnityEngine.Object.DestroyImmediate(panel); }
        }
    }
}
