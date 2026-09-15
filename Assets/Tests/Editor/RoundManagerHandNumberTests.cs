#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    public class RoundManagerHandNumberTests
    {
        [Test]
        public void TotalHands_Is_Three_For_Standard_FourPlayer_Smazzata()
        {
            var state = new GameState(4);
            var rm = new RoundManager(state);
            rm.StartSmazzata();

            Assert.AreEqual(3, rm.TotalHands);
        }

        [Test]
        public void CurrentHandNumber_Increments_On_Each_Redeal()
        {
            var state = new GameState(4);
            var rm = new RoundManager(state);
            rm.StartSmazzata();

            Assert.AreEqual(1, rm.CurrentHandNumber);
            Assert.Greater(state.Deck.Count, 0); // serve mazzo per la ridistribuzione

            // Svuota le mani di 3 giocatori su 4, lascia una sola carta al giocatore 0
            for (int i = 1; i < state.NumPlayers; i++)
                state.Players[i].Hand.Clear();

            var lastCard = new Card(Suit.Denari, 4);
            state.Players[0].Hand.Clear();
            state.Players[0].Hand.Add(lastCard);
            state.Table.Add(new Card(Suit.Spade, 9)); // niente cattura possibile, solo scarto

            var move = new Move(0, lastCard, MoveType.PlayOnly);
            rm.ApplyMove(move);

            Assert.AreEqual(2, rm.CurrentHandNumber);
        }
    }
}
#endif
