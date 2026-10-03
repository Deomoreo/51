using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    public class MattaAndAccusiRulesTests
    {
        [Test]
        public void RoundManager_Declares_Cirulla_With_Matta_As_Ace_And_Awards_Points()
        {
            var state = new GameState(2);
            state.Table.Clear();
            foreach (var p in state.Players) p.Hand.Clear();

            // 2 + 3 + matta(=1) => 6 <= 9 => Cirulla
            state.Players[0].Hand.Add(new Card(Suit.Denari, 2));
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 3));
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 7)); // Matta

            var roundManager = new RoundManager(state);
            bool declared = roundManager.TryPlayerAccuso(0, AccusoType.Cirulla);

            Assert.IsTrue(declared, "Hand qualifies for Cirulla with Matta valued as Ace");
            Assert.AreEqual(3, state.Players[0].AccusiPoints,
                "Cirulla is worth 3 points; RoundManager does not (yet) execute any card capture for it");
        }

        [Test]
        public void RoundManager_Declares_Decino_With_Matta_Completing_Pair_And_Awards_Points()
        {
            var state = new GameState(2);
            state.Table.Clear();
            foreach (var p in state.Players) p.Hand.Clear();

            // 5, 5, matta(=5) => Decino (matta completes the pair into a tris)
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 5));
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 7)); // Matta

            var roundManager = new RoundManager(state);
            bool declared = roundManager.TryPlayerAccuso(0, AccusoType.Decino);

            Assert.IsTrue(declared, "Hand qualifies for Decino with Matta completing the pair of 5s");
            Assert.AreEqual(10, state.Players[0].AccusiPoints,
                "Decino is worth 10 points; RoundManager does not (yet) execute any card capture for it");
        }
    }
}
