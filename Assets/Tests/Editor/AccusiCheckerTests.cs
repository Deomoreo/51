#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;

namespace Project51.Tests
{
    public class AccusiCheckerTests
    {
        [Test]
        public void Cirulla_Detected_When_Player_Has_Matta_And_Two_Aces()
        {
            var hand = new List<Card> { new Card(Suit.Coppe, 7), new Card(Suit.Denari, 1), new Card(Suit.Bastoni, 1) };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Decino_Detected_When_Three_Tens_Present()
        {
            var hand = new List<Card> { new Card(Suit.Coppe,10), new Card(Suit.Denari,10), new Card(Suit.Spade,10) };
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void No_Accuso_When_Hand_Does_Not_Match()
        {
            var hand = new List<Card> { new Card(Suit.Coppe,4), new Card(Suit.Denari,5), new Card(Suit.Spade,6) };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand));
            Assert.IsFalse(AccusiChecker.IsDecino(hand));
        }
    }
}
#endif
