using System;
using System.Reflection;
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using UnityEngine;

public class K7FlowTests
{
    // Uscendo dal Play non si ricarica il dominio: una partita online in Editor lascia GameModeService in multigiocatore.
    [SetUp] public void Offline() => GameModeService.Reset();

    private static bool Advance(RoundAdvanceCountdown timer, float delta, bool authority = true, bool blocked = false) =>
        timer.Advance(delta, authority, blocked);

    [Test]
    public void RoundAdvancesExactlyOnceAfterTenVisibleSeconds()
    {
        var timer = new RoundAdvanceCountdown(); timer.Start();
        Assert.IsFalse(Advance(timer, 3f));
        Assert.IsFalse(Advance(timer, 6f));
        Assert.IsTrue(Advance(timer, 1f));
        Assert.IsFalse(Advance(timer, 100f));
    }
    [Test]
    public void ModalTimeDoesNotConsumeTheCountdown()
    {
        var timer = new RoundAdvanceCountdown(); timer.Start();
        Assert.IsFalse(Advance(timer, 3f));
        Assert.IsFalse(Advance(timer, 100f, blocked: true));
        Assert.IsFalse(Advance(timer, 6f));
        Assert.IsTrue(Advance(timer, 1f));
    }
    [Test]
    public void NewHostGetsFreshReadingTimeAndCancelPreventsHiddenAdvancement()
    {
        var timer = new RoundAdvanceCountdown(); timer.Start();
        Assert.IsFalse(Advance(timer, 9f));
        Assert.IsFalse(Advance(timer, 100f, authority: false));
        Assert.IsFalse(Advance(timer, 1f));
        Assert.IsFalse(Advance(timer, 8f));
        Assert.IsTrue(Advance(timer, 1f));
        timer.Start();
        timer.Cancel();
        Assert.IsFalse(Advance(timer, 100f));
    }

    // UI51 Fase 5 S10 e Fase 6: in 1v1 e a 4 gira la ruota del mockup; offline AL TAVOLO
    // chiude subito. Il mazziere arriva come spicchio: in 1v1 i posti 0 e 2 sono gli spicchi 0 e 1.
    [TestCase(2, 0)] [TestCase(2, 2)] [TestCase(4, 1)] [TestCase(4, 3)]
    public void TheWheelShowsAndHandsBackOnContinue(int players, int winner)
    {
        var root = new GameObject("K7 wheel");
        var controller = root.AddComponent<DealerRouletteController>();
        var panel = new GameObject("Panel"); panel.transform.SetParent(root.transform);
        var wheel = new GameObject("UI51"); wheel.transform.SetParent(panel.transform);
        void Set(string name, object data) => typeof(DealerRouletteController).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, data);
        Set("panelRoot", panel); Set("ui51Wheel", wheel);
        try
        {
            var routine = controller.PlayRoulette(new[] { "Tu", "Bot 2", "Bot 3", "Bot 4" }, winner, players, players == 4);
            Assert.IsTrue(routine.MoveNext());
            var wait = (System.Collections.IEnumerator)routine.Current;
            Assert.IsTrue(wait.MoveNext());
            Assert.IsTrue(panel.activeSelf && wheel.activeSelf, "ruota accesa");
            Assert.AreEqual(players, controller.WheelPlayers);
            Assert.AreEqual(players == 2 ? winner / 2 : winner, controller.WheelDealer);
            Assert.AreEqual(winner == 0, controller.WheelLocalDealer);
            Assert.AreEqual(players == 4, controller.WheelTeams);
            controller.Continue();
            Assert.IsFalse(wait.MoveNext());
            Assert.IsFalse(panel.activeSelf);
            Assert.IsFalse(routine.MoveNext());
            Assert.AreEqual(7.7f, DealerRouletteController.Timing(DealerRouletteController.WheelEndAt, true), "online: tempi fissi");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
