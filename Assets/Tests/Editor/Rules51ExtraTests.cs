#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Linq;
using System.Reflection;

namespace Project51.Tests
{
    public class Rules51ExtraTests
    {
        [Test]
        public void Ace_Play_Captures_Single_Ace_When_Present()
        {
            var state = new GameState(2);
            // Table has an Ace and another card
            state.Table.Add(new Card(Suit.Coppe, 1));
            state.Table.Add(new Card(Suit.Denari, 4));
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 1)); // playing an ace

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.PlayedCard.IsAce && m.Type == MoveType.AceCapture && m.CapturedCards.Count == 1 && m.CapturedCards[0].IsAce));
        }

        [Test]
        public void Ace_Play_Captures_All_When_No_Aces_On_Table()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe, 4));
            state.Table.Add(new Card(Suit.Denari, 6));
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 1)); // playing an ace

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.PlayedCard.IsAce && m.Type == MoveType.AceCapture && m.CapturedCards.Count == 2));
        }

        [Test]
        public void Ace_Play_Is_PlayOnly_When_Table_Empty()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 1)); // playing an ace

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.PlayedCard.IsAce && m.Type == MoveType.PlayOnly));
        }

        [Test]
        public void Forced_Capture_Prevents_PlayOnly_Moves()
        {
            var state = new GameState(2);
            // Player has a card that can capture and another that would be a normal play
            state.Table.Add(new Card(Suit.Coppe, 5));
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5)); // can capture
            state.Players[0].Hand.Add(new Card(Suit.Spade, 3));

            var moves = Rules51.GetValidMoves(state, 0);
            // Since there's at least one capture move, there should be no PlayOnly moves
            Assert.IsFalse(moves.Any(m => m.Type == MoveType.PlayOnly));
        }

        [Test]
        public void Scopa_Not_Counted_On_Last_Play()
        {
            var state = new GameState(2);
            // Prepare end-of-deck, other player has no cards
            state.Deck.Clear();
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5)); // will play to capture
            state.Players[1].Hand.Clear();
            state.Table.Add(new Card(Suit.Coppe, 5));

            var move = new Move(0, new Card(Suit.Denari, 5), MoveType.CaptureEqual, new System.Collections.Generic.List<Card> { state.Table[0] });
            Rules51.ApplyMove(state, move);

            // Because this was the last play (deck empty and all hands empty after play), scopa should not be counted
            Assert.AreEqual(0, state.Players[0].ScopaCount);
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
            Assert.AreEqual(1, state.Players[state.DealerIndex].AccusiPoints);
            Assert.AreEqual(0, state.Table.Count);
            Assert.IsTrue(state.Players[state.DealerIndex].CapturedCards.Count >= 3);
        }
    }
}
#endif
