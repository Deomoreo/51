using NUnit.Framework;
using Project51.Core;
using System.Linq;
using System.Reflection;

namespace Project51.Tests
{
    public class Rules51ExtraTests
    {
        [Test]
        public void Forced_Capture_Prevents_PlayOnly_Moves()
        {
            var state = new GameState(2);
            // Player has a card that can capture and another that would be a normal play
            state.Table.Add(new Card(Suit.Coppe, 5));
            var captureCard = new Card(Suit.Denari, 5); // can capture
            var freeCard = new Card(Suit.Spade, 3); // cannot capture anything
            state.Players[0].Hand.Add(captureCard);
            state.Players[0].Hand.Add(freeCard);

            var moves = Rules51.GetValidMoves(state, 0);
            // The 5 of Denari can capture, so it must NOT have a PlayOnly move
            Assert.IsFalse(moves.Any(m => m.Type == MoveType.PlayOnly && m.PlayedCard.Equals(captureCard)));
            // The 3 of Spade cannot capture anything, so it remains discardable
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.PlayOnly && m.PlayedCard.Equals(freeCard)));
        }

        [Test]
        public void Dealer_Initial_Accuso_With_Matta_Assigned_Awards_Points_And_Takes_Table()
        {
            var state = new GameState(2);
            state.DealerIndex = 0;
            // Table: matta + 5 + 3 -> matta can be assigned 7 to make 15
            state.Table.Add(new Card(Suit.Coppe, 7)); // matta
            state.Table.Add(new Card(Suit.Denari, 5));
            state.Table.Add(new Card(Suit.Spade, 3));

            var rm = new RoundManager(state);

            // Invoke private ProcessDealerInitialAccuso via reflection
            var mi = typeof(RoundManager).GetMethod("ProcessDealerInitialAccuso", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "Could not find ProcessDealerInitialAccuso method via reflection");
            mi.Invoke(rm, null);

            // Dealer should have received 1 accuso point and taken the table
            Assert.AreEqual(1, state.Players[state.DealerIndex].RoundAccusiPoints);
            Assert.AreEqual(0, state.Players[state.DealerIndex].AccusiPoints, "Dealer 15/30 is not a hand accuso");
            Assert.AreEqual(0, state.Table.Count);
            Assert.IsTrue(state.Players[state.DealerIndex].CapturedCards.Count >= 3);
        }
    }
}
