using System.Reflection;
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;
using UnityEngine;

namespace Project51.Tests
{
    /// <summary>
    /// Build 3, B3: un tocco conta solo quando TurnController.AcceptsLocalInput e' vero (suo turno E tavolo fermo).
    /// Prima un tocco fatto durante distribuzione o finestra accuso veniva tenuto e giocato dopo da solo.
    /// </summary>
    public class TurnControllerInputGateTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void LocalInputWaitsForTheTableToBeStill()
        {
            // busyUntil e' in tempo del Play: dopo una sessione in Play qui risulterebbe ancora "occupato".
            typeof(GamePresentation).GetField("busyUntil", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 0f);
            go = new GameObject("TurnControllerInputGateTest");
            var turn = go.AddComponent<TurnController>();
            var state = Rules51.CreateNewGame(2);
            state.CurrentPlayerIndex = GameModeService.Current.LocalPlayerIndex;
            typeof(TurnController).GetField("gameState", Private).SetValue(turn, state);
            Assume.That(turn.IsHumanPlayerTurn, "Il giocatore locale deve essere umano in allenamento");

            Assert.IsTrue(turn.AcceptsLocalInput, "turno mio, tavolo fermo");

            var dealing = typeof(TurnController).GetField("isRedealPendingVisual", Private);
            dealing.SetValue(turn, true);
            Assert.IsFalse(turn.AcceptsLocalInput, "distribuzione o finestra accuso in corso");
            dealing.SetValue(turn, false);

            var flying = typeof(TurnController).GetField("isMoveAnimationInProgress", Private);
            flying.SetValue(turn, true);
            Assert.IsFalse(turn.AcceptsLocalInput, "una carta sta volando");
            flying.SetValue(turn, false);

            state.CurrentPlayerIndex = 1 - state.CurrentPlayerIndex;
            Assert.IsFalse(turn.AcceptsLocalInput, "turno dell'altro");
        }
    }
}
