#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    public class Rules51RestoredTests
    {
        [Test]
        public void DealInitial_Should_Not_Leave_TwoOrMore_Aces_On_Table()
        {
            var state = new GameState(2);
            for (int i = 0; i < 10; i++)
            {
                Rules51.DealInitialCards(state);
                Assert.AreEqual(4, state.Table.Count, "Initial deal must place 4 cards on table");
                int aceCount = state.Table.Count(c => c.IsAce);
                Assert.Less(aceCount, 2, "DealInitialCards should avoid leaving two or more aces on the table");
            }
        }

        [Test]
        public void AceCapture_Moves_Behave_As_Specified()
        {
            var state = new GameState(2);

            state.Table.Clear();
            state.Players[0].Hand.Clear();
            state.Players[1].Hand.Clear();

            var tableAce = new Card(Suit.Denari, 1);
            state.Table.Add(tableAce);
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 1));

            var moves = Rules51.GetValidMoves(state, 0);
            var aceMoves = moves.Where(m => m.PlayedCard.IsAce).ToList();
            Assert.IsTrue(aceMoves.Any());
            Assert.IsTrue(aceMoves.Any(m => m.Type == MoveType.AceCapture && m.CapturedCards.Count == 1 && m.CapturedCards[0].IsAce));

            state.Table.Clear();
            state.Table.Add(new Card(Suit.Spade, 4));
            state.Table.Add(new Card(Suit.Bastoni, 6));
            state.Players[0].Hand.Clear();
            state.Players[0].Hand.Add(new Card(Suit.Denari, 1));

            moves = Rules51.GetValidMoves(state, 0);
            aceMoves = moves.Where(m => m.PlayedCard.IsAce).ToList();
            Assert.IsTrue(aceMoves.Any(m => m.Type == MoveType.AceCapture && m.CapturedCards.Count == state.Table.Count));

            state.Table.Clear();
            state.Players[0].Hand.Clear();
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 1));

            moves = Rules51.GetValidMoves(state, 0);
            aceMoves = moves.Where(m => m.PlayedCard.IsAce).ToList();
            Assert.IsTrue(aceMoves.Any(m => m.Type == MoveType.PlayOnly));
        }

        [Test]
        public void Matta_Is_Not_Wild_For_Normal_Captures()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Clear();
            state.Players[1].Hand.Clear();
            state.Table.Clear();

            var matta = new Card(Suit.Coppe, 7);
            state.Table.Add(matta);

            state.Players[0].Hand.Add(new Card(Suit.Spade, 3));

            var moves = Rules51.GetValidMoves(state, 0);
            var movesFor3 = moves.Where(m => m.PlayedCard.Value == 3).ToList();
            Assert.IsTrue(movesFor3.All(m => m.Type == MoveType.PlayOnly));

            state.Players[0].Hand.Clear();
            state.Players[0].Hand.Add(new Card(Suit.Denari, 7));
            moves = Rules51.GetValidMoves(state, 0);
            var movesFor7 = moves.Where(m => m.PlayedCard.Value == 7).ToList();
            Assert.IsTrue(movesFor7.Any(m => m.Type == MoveType.CaptureEqual && m.CapturedCards.Any(c => c.IsMatta)));
        }

        [Test]
        public void DealerAccuso_MattaCompletes_DecinoAndCirulla()
        {
            var state = new GameState(2);
            var rm = new RoundManager(state);

            state.Players[0].Hand.AddRange(new[] { new Card(Suit.Coppe, 7), new Card(Suit.Denari, 1), new Card(Suit.Bastoni, 1) });
            bool cirulla = rm.TryPlayerAccuso(0, AccusoType.Cirulla);
            Assert.IsTrue(cirulla);
            Assert.AreEqual(3, state.Players[0].AccusiPoints);

            state.Players[1].Hand.AddRange(new[] { new Card(Suit.Coppe, 7), new Card(Suit.Denari, 5), new Card(Suit.Spade, 5) });
            bool decino = rm.TryPlayerAccuso(1, AccusoType.Decino);
            Assert.IsTrue(decino);
            Assert.AreEqual(10, state.Players[1].AccusiPoints);
        }

        [Test]
        public void Scopa_Not_Assigned_On_Last_Play_With_Empty_Deck()
        {
            var state = new GameState(2);
            state.Deck.Clear();
            state.Table.Add(new Card(Suit.Coppe, 5));
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Players[1].Hand.Clear();

            var move = new Move(0, state.Players[0].Hand[0], MoveType.CaptureEqual, new List<Card> { state.Table[0] });
            Rules51.ApplyMove(state, move);

            Assert.AreEqual(0, state.Players[0].ScopaCount);
        }

        [Test]
        public void Ace_Play_Behavior_With_Deck_Variants()
        {
            var state = new GameState(2);
            state.Deck.Clear();
            state.Deck.Add(new Card(Suit.Denari, 2));
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 1));
            state.Table.Clear();

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.PlayedCard.IsAce && m.Type == MoveType.PlayOnly));

            var state2 = new GameState(2);
            state2.Deck.Clear();
            state2.Table.Clear();
            state2.Players[0].Hand.Add(new Card(Suit.Bastoni, 1));
            var moves2 = Rules51.GetValidMoves(state2, 0);
            Assert.IsTrue(moves2.Any(m => m.PlayedCard.IsAce && m.Type == MoveType.PlayOnly));

            var state3 = new GameState(2);
            state3.Deck.Clear();
            state3.Table.Add(new Card(Suit.Coppe, 4));
            state3.Table.Add(new Card(Suit.Spade, 6));
            state3.Players[0].Hand.Add(new Card(Suit.Denari, 1));
            var moves3 = Rules51.GetValidMoves(state3, 0);
            Assert.IsTrue(moves3.Any(m => m.PlayedCard.IsAce && m.Type == MoveType.AceCapture && m.CapturedCards.Count == 2));
        }
    }
}
#endif
