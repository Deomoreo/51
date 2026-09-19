#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    /// <summary>
    /// Regole usate dai client in rete per capire, dal solo stato ricevuto dall'host, che la smazzata
    /// e' appena stata distribuita e se il mazziere ha gia' fatto Scopa da 15/30.
    /// </summary>
    public class SmazzataStartTests
    {
        private static GameState FreshState(int players = 2, params Card[] table)
        {
            var state = new GameState(players) { DealerIndex = 0 };
            state.Deck.Clear();
            for (int i = 0; i < 40 - 3 * players - RoundManager.InitialTableCards; i++) state.Deck.Add(new Card(Suit.Bastoni, (i % 10) + 1));
            for (int p = 0; p < players; p++)
            {
                state.Players[p].Hand.Clear();
                for (int c = 0; c < 3; c++) state.Players[p].Hand.Add(new Card(Suit.Spade, c + 1));
            }
            state.Table.Clear();
            state.Table.AddRange(table);
            return state;
        }

        private static Card[] Table(params int[] ranks)
        {
            var cards = new Card[ranks.Length];
            for (int i = 0; i < ranks.Length; i++) cards[i] = new Card(Suit.Denari, ranks[i]);
            return cards;
        }

        [Test]
        public void DealerAccusoIsFifteenOrThirtyOnTheTableCards()
        {
            Assert.AreEqual(AccusoType.Dealer15, RoundManager.DealerAccusoFor(Table(1, 2, 3, 9)));
            Assert.AreEqual(AccusoType.Dealer30, RoundManager.DealerAccusoFor(Table(10, 10, 5, 5)));
            Assert.IsNull(RoundManager.DealerAccusoFor(Table(1, 2, 3, 4)));
            Assert.IsNull(RoundManager.DealerAccusoFor(new List<Card>()));
        }

        [Test]
        public void MattaOnTheTableTakesTheValueThatCompletesFifteen()
        {
            // 7 di coppe (matta) + 2 + 3 + 4: con la matta a 6 la somma fa 15.
            var table = new List<Card> { new Card(Suit.Coppe, 7), new Card(Suit.Denari, 2), new Card(Suit.Denari, 3), new Card(Suit.Denari, 4) };
            Assert.AreEqual(AccusoType.Dealer15, RoundManager.DealerAccusoFor(table));

            // 10 + 10 + 7 di coppe + 3: con la matta a 7 la somma fa 30.
            var thirty = new List<Card> { new Card(Suit.Denari, 10), new Card(Suit.Spade, 10), new Card(Suit.Coppe, 7), new Card(Suit.Denari, 3) };
            Assert.AreEqual(AccusoType.Dealer30, RoundManager.DealerAccusoFor(thirty));

            // Nessun valore da 1 a 10 porta a 15 o 30.
            var none = new List<Card> { new Card(Suit.Coppe, 7), new Card(Suit.Denari, 10), new Card(Suit.Spade, 10), new Card(Suit.Bastoni, 10) };
            Assert.IsNull(RoundManager.DealerAccusoFor(none));
        }

        [Test]
        public void FreshSmazzataIsRecognisedBeforeAnyCardIsPlayed()
        {
            var state = FreshState(2, Table(1, 2, 3, 4));
            Assert.IsTrue(RoundManager.IsFreshSmazzata(state));

            // Una carta giocata: non e' piu' l'inizio.
            state.Players[0].Hand.RemoveAt(0);
            Assert.IsFalse(RoundManager.IsFreshSmazzata(state));

            var ended = FreshState(4, Table(1, 2, 3, 4));
            Assert.IsTrue(RoundManager.IsFreshSmazzata(ended));
            ended.RoundEnded = true;
            Assert.IsFalse(RoundManager.IsFreshSmazzata(ended));
        }

        [Test]
        public void HandCounterIsReadFromTheStateEvenWithoutDealing()
        {
            // Stato ricevuto dall'host (nessuna distribuzione locale): 1v1 = 6 mani, 4 giocatori = 3.
            var oneVsOne = new RoundManager(FreshState(2, Table(1, 2, 3, 4)));
            Assert.AreEqual(6, oneVsOne.TotalHands);
            Assert.AreEqual(1, oneVsOne.CurrentHandNumber);

            var fourPlayers = new RoundManager(FreshState(4, Table(1, 2, 3, 4)));
            Assert.AreEqual(3, fourPlayers.TotalHands);
            Assert.AreEqual(1, fourPlayers.CurrentHandNumber);

            // Mazzo a meta': 1v1 con 12 carte rimaste = quarta mano delle sei.
            var midState = FreshState(2, Table(1, 2, 3, 4));
            midState.Deck.RemoveRange(0, midState.Deck.Count - 12);
            Assert.AreEqual(4, new RoundManager(midState).CurrentHandNumber);

            // Ultima mano: mazzo finito.
            var lastState = FreshState(2, Table(1, 2, 3, 4));
            lastState.Deck.Clear();
            Assert.AreEqual(6, new RoundManager(lastState).CurrentHandNumber);
        }

        [Test]
        public void DealerAccusoAtStartIsFoundFromTheDealerPile()
        {
            var state = FreshState(2, Table(1, 2, 3, 9));
            new RoundManager(state); // non serve, ma documenta che lo stato e' quello di una smazzata
            // Simula cio' che fa l'host: il mazziere prende le carte del tavolo.
            state.Players[state.DealerIndex].CapturedCards.AddRange(state.Table);
            state.Table.Clear();

            Assert.IsTrue(RoundManager.IsFreshSmazzata(state));
            Assert.IsTrue(RoundManager.TryGetDealerAccusoAtStart(state, out var type, out var swept));
            Assert.AreEqual(AccusoType.Dealer15, type);
            Assert.AreEqual(4, swept.Count);

            var normal = FreshState(2, Table(1, 2, 3, 4));
            Assert.IsFalse(RoundManager.TryGetDealerAccusoAtStart(normal, out _, out _));
        }
    }
}
#endif
