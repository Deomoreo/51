#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;

namespace Project51.Tests
{
    public class PlayerSelectionTests
    {
        [Test]
        public void Valid_Manual_Selection_For_CaptureSum_Is_Accepted()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe, 4));
            state.Table.Add(new Card(Suit.Bastoni, 5));
            state.Players[0].Hand.Add(new Card(Suit.Denari, 6));

            var selection = new List<Card> { state.Table[0], state.Table[1] };
            bool ok = Rules51.TryGetMoveFromSelection(state, 0, state.Players[0].Hand[0], selection, out var move);
            Assert.IsTrue(ok);
            Assert.IsNotNull(move);
            Assert.AreEqual(MoveType.Capture15, move.Type);
        }

        [Test]
        public void Invalid_Manual_Selection_Is_Rejected()
        {
            var state = new GameState(2);
            state.Table.Add(new Card(Suit.Coppe, 4));
            state.Players[0].Hand.Add(new Card(Suit.Denari, 6));

            // Selecting a non-matching card (that doesn't exist on table)
            var fake = new Card(Suit.Denari, 2);
            var selection = new List<Card> { fake };
            bool ok = Rules51.TryGetMoveFromSelection(state, 0, state.Players[0].Hand[0], selection, out var move);
            Assert.IsFalse(ok);
            Assert.IsNull(move);
        }
    }
}
#endif
