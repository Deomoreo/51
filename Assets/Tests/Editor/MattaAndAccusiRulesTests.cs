#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    public class MattaAndAccusiRulesTests
    {
        [Test]
        public void Cirulla_Matta_As_Ace_When_Checking_Sum()
        {
            // 2 + 3 + matta(=1) => 6 <= 9 => Cirulla
            var hand = new List<Card> { new Card(Suit.Denari, 2), new Card(Suit.Coppe, 3), new Card(Suit.Coppe, 7) };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Cirulla_False_When_Sum_GT_9_With_Matta_As_Ace()
        {
            // 6 + 4 + matta(=1) => 11 > 9 => not Cirulla
            var hand = new List<Card> { new Card(Suit.Denari, 6), new Card(Suit.Coppe, 4), new Card(Suit.Coppe, 7) };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Decino_True_With_Pair_Plus_Matta()
        {
            // 5,5,7C -> matta takes value 5 => Decino
            var hand = new List<Card> { new Card(Suit.Denari, 5), new Card(Suit.Coppe, 5), new Card(Suit.Coppe, 7) };
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Decino_False_With_Matta_And_No_Pair()
        {
            // 4,5,7C -> no pair to complete
            var hand = new List<Card> { new Card(Suit.Denari, 4), new Card(Suit.Coppe, 5), new Card(Suit.Coppe, 7) };
            Assert.IsFalse(AccusiChecker.IsDecino(hand));
        }

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
#endif
