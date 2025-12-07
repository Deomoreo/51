#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    public class RoundManagerEdgeTests
    {
        [Test]
        public void Scopa_Not_Counted_On_Last_Play()
        {
            var state = new GameState(2);
            state.Deck.Clear();
            state.Players[0].Hand.Add(new Card(Suit.Denari, 7));
            state.Players[1].Hand.Clear();
            state.Table.Add(new Card(Suit.Coppe, 7));

            var move = new Move(0, new Card(Suit.Denari, 7), MoveType.CaptureSum, new List<Card> { state.Table[0] });
            var rm = new RoundManager(state);
            rm.ApplyMove(move);
            Assert.AreEqual(0, state.Players[0].ScopaCount);
        }

        [Test]
        public void Cappotto_Triggers_Immediate_Win()
        {
            var state = new GameState(2);
            for (int r = 1; r <= 10; r++) state.Players[0].CapturedCards.Add(new Card(Suit.Denari, r));
            var rm = new RoundManager(state);
            rm.EndSmazzata();
            Assert.IsTrue(state.RoundEnded);
            Assert.GreaterOrEqual(state.Players[0].TotalScore, 1000);
        }
    }
}
#endif
