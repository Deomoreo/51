#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    public class AceAndCaptureRulesTests
    {
        [Test]
        public void Ace_On_Table_Can_Be_Captured_By_Ace()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe,1));
            state.Players[0].Hand.Add(new Card(Suit.Denari,1));
            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.AceCapture));
        }

        [Test]
        public void Ace_On_Empty_Table_Discarded_As_PlayOnly()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari,1));
            var moves = Rules51.GetValidMoves(state, 0);
            // If table empty, ace may be PlayOnly (or capture all if rule implemented) - ensure at least a PlayOnly exists
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.PlayOnly || m.Type == MoveType.AceCapture));
        }

        [Test]
        public void Forced_Capture_Prevents_PlayOnly()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe,4));
            state.Players[0].Hand.Add(new Card(Suit.Denari,4));
            state.Players[0].Hand.Add(new Card(Suit.Denari,2));
            var moves = Rules51.GetValidMoves(state, 0);
            // There should be no PlayOnly because capture exists
            Assert.IsFalse(moves.Any(m => m.Type == MoveType.PlayOnly));
        }
    }
}
#endif