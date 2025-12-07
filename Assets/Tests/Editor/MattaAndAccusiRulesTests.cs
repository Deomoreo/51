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
        public void Rules51_Matta_Generates_Only_Plausible_Capture_Moves()
        {
            var state = new GameState(2);
            state.Table.Clear();
            state.Players[0].Hand.Clear();
            state.Players[1].Hand.Clear();

            // Table: 4 and 5; Hand: matta (7C)
            state.Table.Add(new Card(Suit.Bastoni, 4));
            state.Table.Add(new Card(Suit.Spade, 5));
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 7));

            var moves = Rules51.GetValidMoves(state, 0);

            // Expect at least one move that captures both cards (either Capture15 with 4+5 or AceCapture all)
            Assert.IsTrue(
                moves.Any(m => (m.Type == MoveType.Capture15 || m.Type == MoveType.AceCapture) && m.CapturedCards.Count == 2),
                "Matta should allow capturing both 4 and 5 (via 15 or ace-capture behavior)"
            );

            // Expect single-card equal captures for 4 and 5 as well
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.CaptureEqual && m.CapturedCards.Count == 1 && m.CapturedCards[0].Value == 4));
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.CaptureEqual && m.CapturedCards.Count == 1 && m.CapturedCards[0].Value == 5));
        }
    }
}
#endif
