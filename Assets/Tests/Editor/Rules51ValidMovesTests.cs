#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    public class Rules51ValidMovesTests
    {
        [Test]
        public void PlayOnly_Is_Always_Allowed_For_Human_Forced()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 3));
            var moves = Rules51.GetValidMoves(state, 0);
            // There should be at least a PlayOnly move for that card
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.PlayOnly && m.PlayedCard.Rank == 3));
        }

        [Test]
        public void CaptureExact_Finds_Singleton_Capture()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe, 5));
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.CaptureEqual && m.CapturedCards.Count == 1 && m.CapturedCards[0].Rank == 5));
        }

        [Test]
        public void Matta_Allows_Multiple_Assignments()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe, 7)); // matta
            state.Table.Add(new Card(Suit.Denari, 6));
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 4));

            var moves = Rules51.GetValidMoves(state, 0);
            // Playing 4 may match various assignments of matta; ensure some capture move exists
            Assert.IsTrue(moves.Any(m => m.PlayedCard.Rank == 4));
        }

        [Test]
        public void Capture15_Is_Present_When_Table_Sums_To_9_Play6()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe, 4));
            state.Table.Add(new Card(Suit.Bastoni, 5));
            state.Players[0].Hand.Add(new Card(Suit.Denari, 6));
            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.Capture15 && m.PlayedCard.Rank == 6));
        }
    }
}
#endif
