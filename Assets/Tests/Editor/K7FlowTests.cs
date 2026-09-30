using System;
using System.Reflection;
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using UnityEngine;

public class K7FlowTests
{
    private static Type CoreType(string name)
    {
        var type = typeof(GamePreferences).Assembly.GetType("Project51.Core." + name);
        Assert.IsNotNull(type, "K7 flow policy must be implemented.");
        return type;
    }
    private static object Countdown() => Activator.CreateInstance(CoreType("RoundAdvanceCountdown"));
    private static void Start(object timer) => timer.GetType().GetMethod("Start").Invoke(timer, null);
    private static bool Advance(object timer, float delta, bool authority = true, bool blocked = false) =>
        (bool)timer.GetType().GetMethod("Advance").Invoke(timer, new object[] { delta, authority, blocked });

    [Test]
    public void RoundAdvancesExactlyOnceAfterEightVisibleSeconds()
    {
        var timer = Countdown(); Start(timer);
        Assert.IsFalse(Advance(timer, 3f));
        Assert.IsFalse(Advance(timer, 4f));
        Assert.IsTrue(Advance(timer, 1f));
        Assert.IsFalse(Advance(timer, 100f));
    }
    [Test]
    public void ModalTimeDoesNotConsumeTheCountdown()
    {
        var timer = Countdown(); Start(timer);
        Assert.IsFalse(Advance(timer, 3f));
        Assert.IsFalse(Advance(timer, 100f, blocked: true));
        Assert.IsFalse(Advance(timer, 4f));
        Assert.IsTrue(Advance(timer, 1f));
    }
    [Test]
    public void NewHostGetsFreshReadingTimeAndCancelPreventsHiddenAdvancement()
    {
        var timer = Countdown(); Start(timer);
        Assert.IsFalse(Advance(timer, 7f));
        Assert.IsFalse(Advance(timer, 100f, authority: false));
        Assert.IsFalse(Advance(timer, 1f));
        Assert.IsFalse(Advance(timer, 6f));
        Assert.IsTrue(Advance(timer, 1f));
        Start(timer);
        timer.GetType().GetMethod("Cancel").Invoke(timer, null);
        Assert.IsFalse(Advance(timer, 100f));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void RouletteSpinIsShortAndStillSelectsTheSuppliedDealer(int winner)
    {
        var root = new GameObject("K7 roulette");
        var controller = root.AddComponent<DealerRouletteController>();
        var panel = new GameObject("Panel"); panel.transform.SetParent(root.transform);
        var slots = new GameObject[4]; var trophies = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            slots[i] = new GameObject("Slot" + i); slots[i].transform.SetParent(panel.transform);
            trophies[i] = new GameObject("Winner" + i); trophies[i].transform.SetParent(slots[i].transform);
        }
        void Set(string name, object data) => typeof(DealerRouletteController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, data);
        Set("panelRoot", panel); Set("slotRoots", slots); Set("slotTrophies", trophies);
        // Existing scenes retain their old serialized values; runtime must cap them.
        Set("startDelaySeconds", .5f); Set("minStepDelay", .16f); Set("maxStepDelay", .5f);
        try
        {
            float spin = 0f;
            var routine = controller.PlayRoulette(new[] { "Tu", "A", "B", "C" }, winner, 4);
            int guard = 0;
            while (routine.MoveNext() && guard++ < 40)
            {
                if (routine.Current is WaitForSeconds wait)
                    spin += (float)typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(wait);
                else if (routine.Current == null) Set("continuePressed", true);
            }
            Assert.Less(guard, 40);
            Assert.Less(spin, 2f, "Cosmetic roulette must not retain the old multi-loop wait.");
            for (int i = 0; i < 4; i++) Assert.AreEqual(i == winner, trophies[i].activeSelf);
            Assert.IsFalse(panel.activeSelf);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // UI51 Fase 5 S10: in 1v1 gira la ruota del mockup al posto del vecchio pannello; offline AL TAVOLO chiude subito.
    [TestCase(0)] [TestCase(2)]
    public void OneVsOneShowsTheWheelAndHandsBackOnContinue(int winner)
    {
        var root = new GameObject("K7 wheel");
        var controller = root.AddComponent<DealerRouletteController>();
        var panel = new GameObject("Panel"); panel.transform.SetParent(root.transform);
        var wheel = new GameObject("UI51"); wheel.transform.SetParent(panel.transform);
        var design = new GameObject("Design"); design.transform.SetParent(panel.transform);
        var slots = new GameObject[4];
        for (int i = 0; i < 4; i++) { slots[i] = new GameObject("Slot" + i); slots[i].transform.SetParent(design.transform); }
        void Set(string name, object data) => typeof(DealerRouletteController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, data);
        Set("panelRoot", panel); Set("slotRoots", slots); Set("ui51Wheel", wheel); Set("legacyOnly", new[] { design });
        try
        {
            var routine = controller.PlayRoulette(new[] { "Tu", "-", "Bot 2", "-" }, winner, 2);
            Assert.IsTrue(routine.MoveNext());
            var wait = (System.Collections.IEnumerator)routine.Current;
            Assert.IsTrue(wait.MoveNext());
            Assert.IsTrue(panel.activeSelf && wheel.activeSelf, "ruota accesa");
            Assert.IsFalse(design.activeSelf, "vecchio pannello spento");
            Assert.AreEqual(winner == 0, controller.WheelLocalDealer);
            controller.Continue();
            Assert.IsFalse(wait.MoveNext());
            Assert.IsFalse(panel.activeSelf);
            Assert.IsFalse(routine.MoveNext());
            Assert.AreEqual(7.7f, DealerRouletteController.Timing(DealerRouletteController.WheelEndAt, true), "online: tempi fissi");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
