#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    public class MattaAccusoValueTests
    {
        private static readonly Card Matta = new Card(Suit.Coppe, 7);

        private static List<Card> Hand(params Card[] cards) => new List<Card>(cards);

        [Test]
        public void DecinoTurnsMattaIntoPairRank()
        {
            var hand = Hand(Matta, new Card(Suit.Denari, 5), new Card(Suit.Spade, 5));
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
            Assert.AreEqual(5, AccusiChecker.MattaValueForAccuso(hand));
        }

        [Test]
        public void CirullaThatNeedsTheAceTurnsMattaIntoAce()
        {
            var hand = Hand(Matta, new Card(Suit.Denari, 3), new Card(Suit.Spade, 4));
            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
            Assert.AreEqual(1, AccusiChecker.MattaValueForAccuso(hand));
        }

        [Test]
        public void PairOfAcesIsDecinoWithMattaAsAce()
        {
            // 1 + 1 + matta: la coppia vince sulla Cirulla, la matta diventa il terzo asso.
            var hand = Hand(Matta, new Card(Suit.Denari, 1), new Card(Suit.Spade, 1));
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
            Assert.AreEqual(1, AccusiChecker.MattaValueForAccuso(hand));
        }

        [Test]
        public void NoAccusoOrNoMattaMeansNoTransformation()
        {
            Assert.AreEqual(-1, AccusiChecker.MattaValueForAccuso(Hand(Matta, new Card(Suit.Denari, 6), new Card(Suit.Spade, 9))));
            Assert.AreEqual(-1, AccusiChecker.MattaValueForAccuso(Hand(new Card(Suit.Denari, 2), new Card(Suit.Spade, 2), new Card(Suit.Bastoni, 3))));
            Assert.AreEqual(-1, AccusiChecker.MattaValueForAccuso(Hand(Matta, new Card(Suit.Denari, 7), new Card(Suit.Spade, 7))));
        }
    }
}
#endif
