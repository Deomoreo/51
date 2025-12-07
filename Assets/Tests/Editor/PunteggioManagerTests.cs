#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    public class PunteggioManagerTests
    {
        [Test]
        public void Denari_Majority_Requires_At_Least_6()
        {
            var state = new GameState(2);
            for (int i = 0; i < 5; i++) state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 4));
            for (int i = 0; i < 6; i++) state.Players[1].CapturedCards.Add(new Card(Suit.Denari, 2));
            var scores = PunteggioManager.CalculateSmazzataScores(state);
            Assert.GreaterOrEqual(scores[1], 1);
        }

        [Test]
        public void Carte_Majority_Requires_At_Least_21()
        {
            var state = new GameState(2);
            for (int i = 0; i < 21; i++) state.Players[0].CapturedCards.Add(new Card(Suit.Denari, (i%10)+1));
            var scores = PunteggioManager.CalculateSmazzataScores(state);
            Assert.GreaterOrEqual(scores[0], 1);
        }

        [Test]
        public void Primiera_Tie_Results_In_No_Point()
        {
            var state = new GameState(2);
            state.Players[0].CapturedCards.AddRange(new[] { new Card(Suit.Denari,7), new Card(Suit.Coppe,6), new Card(Suit.Bastoni,5), new Card(Suit.Spade,4) });
            state.Players[1].CapturedCards.AddRange(new[] { new Card(Suit.Denari,7), new Card(Suit.Coppe,6), new Card(Suit.Bastoni,5), new Card(Suit.Spade,4) });
            var scores = PunteggioManager.CalculateSmazzataScores(state);
            Assert.AreEqual(scores[0], scores[1]);
        }
    }
}
#endif
