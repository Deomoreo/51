using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEngine.TestTools;

public class K5TableTests
{
    private GameObject cardObject, controllerObject;
    private Texture2D texture;
    private Sprite sprite;

    [SetUp]
    public void SetUp()
    {
        cardObject = new GameObject("K5 card");
        controllerObject = new GameObject("K5 animation");
        texture = new Texture2D(32, 48);
        sprite = Sprite.Create(texture, new Rect(0, 0, 32, 48), Vector2.one * .5f, 32);
    }

    [TearDown]
    public void TearDown()
    {
        cardObject.transform.DOKill();
        DestroyCard(cardObject);
        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }

    private SpriteRenderer InitializeCard()
    {
        var renderer = cardObject.AddComponent<SpriteRenderer>();
        cardObject.AddComponent<CardView>().Initialize(new Card(Suit.Coppe, 4), sprite);
        return renderer;
    }

    private static SpriteRenderer ShadowOf(GameObject go, SpriteRenderer face)
    {
        var shadow = go.GetComponentsInChildren<SpriteRenderer>(true)
            .SingleOrDefault(r => r != face && r.gameObject.name == "Card contact shadow");
        Assert.IsNotNull(shadow, "Each visible card needs one soft shadow, including flight copies.");
        return shadow;
    }

    private static void InvokeLifecycle(MonoBehaviour component, string method) =>
        component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);

    private static void DestroyCard(GameObject go)
    {
        // Ordinary MonoBehaviours do not receive OnDestroy in EditMode tests.
        if (!Application.isPlaying)
            foreach (var shadow in go.GetComponentsInChildren<CardDropShadow>(true))
                InvokeLifecycle(shadow, "OnDestroy");
        Object.DestroyImmediate(go);
    }

    // B26: la matta trasformata ha bordo e scritta finche' dura, il suggerimento ha il suo bordo; nessuno resta dopo.
    [Test]
    public void MattaMarkerAndHintBorderComeAndGo()
    {
        InitializeCard();
        var view = cardObject.GetComponent<CardView>();
        cardObject.SetActive(false); // niente coroutine del giro in EditMode
        bool On(string name) => cardObject.GetComponentsInChildren<Transform>(true).Any(t => t.name == name && t.gameObject.activeSelf);
        view.ShowMattaTransform(sprite, null);
        Assert.IsTrue(On("MattaTag") && On("MattaOutline"));
        view.ClearMattaTransform();
        Assert.IsFalse(On("MattaTag") || On("MattaOutline"));
        view.SetMoveHint(true, null);
        Assert.IsTrue(On("HintOutline"));
        view.SetGlow(true, null, Color.yellow); // alone del turno: il bordo azzurro sparisce
        Assert.IsFalse(On("HintOutline"));
    }

    [Test]
    public void HiddenOriginalDoesNotLeaveShadowDuringFlight()
    {
        var face = InitializeCard();
        var shadow = ShadowOf(cardObject, face);
        face.enabled = false;
        InvokeLifecycle(cardObject.GetComponent<CardDropShadow>(), "LateUpdate");
        Assert.IsFalse(shadow.enabled, "The original's shadow must disappear with its renderer.");
        face.enabled = true;
        face.color = new Color(1, 1, 1, .4f);
        face.sortingOrder = 17;
        InvokeLifecycle(cardObject.GetComponent<CardDropShadow>(), "LateUpdate");
        Assert.IsTrue(shadow.enabled);
        Assert.LessOrEqual(shadow.sortingOrder, face.sortingOrder);
        Assert.Less(shadow.color.a, .4f);
    }

    [Test]
    public void VisualCopyHasIndependentShadowAndRepeatedBindDoesNotDuplicateIt()
    {
        var face = InitializeCard();
        cardObject.GetComponent<CardView>().Initialize(new Card(Suit.Denari, 2), sprite);
        var sourceShadow = ShadowOf(cardObject, face);
        var controller = controllerObject.AddComponent<CardAnimationController>();
        Assert.IsTrue(controller.TryCreateVisualCopy(cardObject.transform, face, "K5 copy", out var copy, out var copyFace));
        try
        {
            var copyShadow = ShadowOf(copy.gameObject, copyFace);
            Assert.AreNotSame(sourceShadow, copyShadow);
            face.enabled = false;
            InvokeLifecycle(copy.GetComponent<CardDropShadow>(), "LateUpdate");
            Assert.IsTrue(copyShadow.enabled);
        }
        finally { DestroyCard(copy.gameObject); }
    }

    [UnityTest, Explicit]
    public IEnumerator InterruptedPlayRestoresPoseAndDoesNotCompleteMove()
    {
        if (!Application.isPlaying) Assert.Ignore("DOTween kill callbacks require Play Mode.");
        yield return null;
        var face = cardObject.AddComponent<SpriteRenderer>();
        face.sprite = sprite;
        face.sortingOrder = 13;
        cardObject.transform.position = new Vector3(1, -2, 0);
        cardObject.transform.localScale = new Vector3(.6f, .6f, 1);
        cardObject.transform.rotation = Quaternion.Euler(0, 0, 14);
        var startPosition = cardObject.transform.position;
        var startScale = cardObject.transform.localScale;
        var startRotation = cardObject.transform.rotation;
        var controller = controllerObject.AddComponent<CardAnimationController>();
        int completed = 0;
        var sequence = controller.PlayCardToTable(cardObject.transform, face, Vector3.zero, 0, .4f, () => completed++);
        sequence.Pause();
        sequence.Goto(sequence.Duration() * .5f);
        sequence.Kill();
        Assert.AreEqual(startPosition, cardObject.transform.position);
        Assert.AreEqual(startScale, cardObject.transform.localScale);
        Assert.Less(Quaternion.Angle(startRotation, cardObject.transform.rotation), .01f);
        Assert.AreEqual(13, face.sortingOrder);
        Assert.AreEqual(0, completed);
    }

    [Test]
    public void DisabledSelectedCardRestoresRestPoseBeforeReuse()
    {
        var face = InitializeCard();
        var view = cardObject.GetComponent<CardView>();
        view.SetPosition(new Vector3(1, 2, 0));
        view.SetDisplayScale(.7f);
        view.SetBaseSortingOrder(8);
        view.SetSelected(true);
        cardObject.transform.position += Vector3.up * .2f;
        InvokeLifecycle(view, "OnDisable");
        Assert.AreEqual(new Vector3(1, 2, 0), cardObject.transform.position);
        Assert.AreEqual(new Vector3(.7f, .7f, 1f), cardObject.transform.localScale);
        Assert.AreEqual(8, face.sortingOrder);
    }

    [Test]
    public void FeltRebuildReleasesPreviousSpriteWithoutConsumingGameRandom()
    {
        var camera = controllerObject.AddComponent<Camera>();
        camera.orthographic = true;
        var felt = cardObject.AddComponent<TableFeltRenderer>();
        typeof(TableFeltRenderer).GetField("targetCamera", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(felt, camera);
        var random = Random.state;
        try
        {
            float expected = Random.value;
            Random.state = random;
            felt.Rebuild();
            var oldSprite = cardObject.GetComponentInChildren<SpriteRenderer>().sprite;
            felt.Rebuild();
            Assert.IsTrue(oldSprite == null, "Rebuilding must release the previous generated Sprite.");
            Assert.AreEqual(expected, Random.value);
        }
        finally { Random.state = random; }
    }

    [UnityTest, Explicit]
    public IEnumerator HoverThenSelectionAndMattaKeepTheirFinalPose()
    {
        if (!Application.isPlaying) Assert.Ignore("Run explicitly in PlayMode.");
        var face = InitializeCard();
        var view = cardObject.GetComponent<CardView>();
        view.SetPosition(new Vector3(1, 2, 0));
        view.SetDisplayScale(.7f);
        view.EnableHover = true;
        InvokeLifecycle(view, "OnMouseEnter");
        view.SetSelected(true);
        view.ShowMattaTransform(sprite, null);
        float deadline = Time.realtimeSinceStartup + 6f;
        var flip = typeof(CardView).GetField("mattaFlip", BindingFlags.Instance | BindingFlags.NonPublic);
        while (flip.GetValue(view) != null && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsNull(flip.GetValue(view), "Matta must complete even at low frame rates.");
        Assert.Less(Vector3.Distance(new Vector3(1, 2.35f, 0), cardObject.transform.position), .001f);
        Assert.AreEqual(.784f, cardObject.transform.localScale.x, .001f);
        Assert.AreEqual(.784f, cardObject.transform.localScale.y, .001f);
        cardObject.SetActive(false);
        cardObject.SetActive(true);
        Assert.AreEqual(new Vector3(1, 2, 0), cardObject.transform.position);
        Assert.AreEqual(new Vector3(.7f, .7f, 1), cardObject.transform.localScale);
        yield return null;
        Assert.IsTrue(ShadowOf(cardObject, face).enabled);
    }

    [UnityTest, Explicit]
    public IEnumerator SelectionNearEndOfMattaNeverReintroducesFlattenedWidth()
    {
        if (!Application.isPlaying) Assert.Ignore("Run explicitly in PlayMode.");
        InitializeCard();
        var view = cardObject.GetComponent<CardView>();
        view.SetDisplayScale(.7f);
        view.ShowMattaTransform(sprite, null);
        yield return new WaitForSeconds(.27f);
        view.SetSelected(true);
        var flip = typeof(CardView).GetField("mattaFlip", BindingFlags.Instance | BindingFlags.NonPublic);
        float deadline = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            if (flip.GetValue(view) == null)
                Assert.AreEqual(cardObject.transform.localScale.y, cardObject.transform.localScale.x, .001f);
        }
    }

    public static IEnumerator RunRuntimeChecks()
    {
        for (int i = 0; i < 3; i++)
        {
            var test = new K5TableTests();
            test.SetUp();
            try
            {
                IEnumerator check = i == 0 ? test.InterruptedPlayRestoresPoseAndDoesNotCompleteMove() :
                    i == 1 ? test.HoverThenSelectionAndMattaKeepTheirFinalPose() :
                    test.SelectionNearEndOfMattaNeverReintroducesFlattenedWidth();
                while (check.MoveNext()) yield return check.Current;
            }
            finally { test.TearDown(); }
        }
        Debug.Log("K5_RUNTIME_CHECKS_PASSED: 3 tests; interruption, hover-selection, Matta, pooling.");
    }

    [Test]
    public void PlayedCardKeepsMovingThroughMidFlightAndUntilLanding()
    {
        var face = cardObject.AddComponent<SpriteRenderer>();
        face.sprite = sprite;
        cardObject.transform.position = new Vector3(0, -3, 0);
        var controller = controllerObject.AddComponent<CardAnimationController>();
        var sequence = controller.PlayCardToTable(cardObject.transform, face, Vector3.zero, 0, .5f);
        sequence.Pause();
        float duration = sequence.Duration();
        sequence.Goto(duration * .40f);
        Vector3 beforeMidpoint = cardObject.transform.position;
        sequence.Goto(duration * .44f);
        Assert.Greater(Vector3.Distance(beforeMidpoint, cardObject.transform.position), .03f,
            "The card must not brake to a stop halfway through a three-unit flight.");
        sequence.Goto(duration * .9f);
        Assert.Greater(cardObject.transform.position.magnitude, .0003f,
            "Landing must not cut off movement before the end of the animation.");
        sequence.Complete(true);
    }

    [Test]
    public void CompletedPlaySettlesExactlyAndCallsCompletionOnce()
    {
        var face = cardObject.AddComponent<SpriteRenderer>();
        face.sprite = sprite;
        face.sortingOrder = 11;
        var controller = controllerObject.AddComponent<CardAnimationController>();
        int completed = 0;
        var target = new Vector3(2, 3, 0);
        var sequence = controller.PlayCardToTable(cardObject.transform, face, target, 7, .5f, () => completed++);
        sequence.Complete(true);
        Assert.Less(Vector3.Distance(target, cardObject.transform.position), .0001f);
        Assert.Less(Vector3.Distance(Vector3.one * .5f, cardObject.transform.localScale), .0001f);
        Assert.Less(Quaternion.Angle(Quaternion.Euler(0, 0, 7), cardObject.transform.rotation), .01f);
        Assert.AreEqual(11, face.sortingOrder);
        Assert.AreEqual(1, completed);
    }
}
