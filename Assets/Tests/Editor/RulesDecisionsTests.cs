using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project51.Core;
using Project51.Unity;

namespace Project51.Tests
{
    /// <summary>Decisioni di regolamento dell'handoff: SPEC 10 (punti 1-4) e 11 (punto 5, Tre assi).</summary>
    public class RulesDecisionsTests
    {
        private static IEnumerable<Card> Cards(Suit suit, params int[] ranks) => ranks.Select(r => new Card(suit, r));

        // 1. Precedenza della carta uguale.
        [Test]
        public void EqualCardMustBeTakenWithoutSumsOrFifteen()
        {
            var s = new GameState(2);
            var five = new Card(Suit.Denari, 5);
            s.Players[0].Hand.Add(five);
            var two = new Card(Suit.Spade, 2);
            var three = new Card(Suit.Bastoni, 3);
            s.Table.AddRange(new[] { new Card(Suit.Coppe, 5), two, three, new Card(Suit.Spade, 10) });

            // 2+3 = 5 e 5+10 = 15, ma c'e' il 5 in tavola: si prende solo quello.
            var moves = Rules51.GetValidMoves(s, 0);
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(MoveType.CaptureEqual, moves[0].Type);

            s.Table.RemoveAt(0); // senza il 5 tornano somma e 15
            moves = Rules51.GetValidMoves(s, 0);
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.CaptureSum));
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.Capture15));
        }

        // 2. Nel 1v3 punto Carte e punto Denari alla semplice maggioranza; soglie 21 e 6 in 1v1 e 2v2.
        [Test]
        public void CardsAndDenariGoToSimpleMajorityOnlyInOneVsThree()
        {
            // 15 carte e 5 denari contro 14 e 4: sotto le soglie.
            GameState Deal(int players, bool team)
            {
                var s = new GameState(players) { TeamMode = team };
                s.Players[0].CapturedCards.AddRange(Cards(Suit.Denari, 1, 2, 3, 4, 5).Concat(Cards(Suit.Coppe, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)));
                s.Players[1].CapturedCards.AddRange(Cards(Suit.Denari, 6, 7, 8, 9).Concat(Cards(Suit.Spade, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)));
                return s;
            }

            var oneVsThree = PunteggioManager.CalculateBreakdown(Deal(4, false))[0];
            Assert.IsTrue(oneVsThree.WonCards && oneVsThree.WonDenari);
            foreach (var withThresholds in new[] { Deal(2, false), Deal(4, true) })
            {
                var first = PunteggioManager.CalculateBreakdown(withThresholds)[0];
                Assert.IsFalse(first.WonCards || first.WonDenari, "1v1 e 2v2 tengono le soglie");
            }

            // Pari merito (15 carte e 5 denari a testa): nessun punto.
            var tie = Deal(4, false);
            tie.Players[1].CapturedCards.Add(new Card(Suit.Denari, 10));
            Assert.IsFalse(PunteggioManager.CalculateBreakdown(tie).Any(x => x.WonCards || x.WonDenari));
        }

        // 3. L'asso che piglia tutto fa scopa solo se svuota il tavolo.
        [Test]
        public void AceIsAScopaOnlyWhenItEmptiesTheTable()
        {
            var all = new GameState(2);
            all.Players[0].Hand.Add(new Card(Suit.Spade, 1));
            all.Players[1].Hand.Add(new Card(Suit.Coppe, 4)); // non e' l'ultima giocata
            all.Table.AddRange(Cards(Suit.Bastoni, 3, 6, 9));
            var takeAll = Rules51.GetValidMoves(all, 0).Single(m => m.Type == MoveType.AceCapture);
            Assert.IsTrue(TurnController.IsScopaCapture(all, takeAll));
            Rules51.ApplyMove(all, takeAll);
            Assert.AreEqual(1, all.Players[0].ScopaCount);

            // In tavola c'e' un asso: l'asso prende solo quello e le altre carte restano.
            var one = new GameState(2);
            one.Players[0].Hand.Add(new Card(Suit.Spade, 1));
            one.Players[1].Hand.Add(new Card(Suit.Coppe, 4));
            one.Table.AddRange(Cards(Suit.Bastoni, 1, 6, 9));
            var takeAce = Rules51.GetValidMoves(one, 0).Single(m => m.Type == MoveType.AceCapture);
            Assert.AreEqual(1, takeAce.CapturedCards.Count);
            Assert.IsFalse(TurnController.IsScopaCapture(one, takeAce));
            Rules51.ApplyMove(one, takeAce);
            Assert.AreEqual(0, one.Players[0].ScopaCount);
            Assert.AreEqual(2, one.Table.Count);
        }

        // 4. Il 15/30 del mazziere e' un accuso da 1/2 punti, non una scopa.
        [Test]
        public void DealerFifteenAndThirtyAreAccusiNotScope()
        {
            Assert.AreEqual("ACCUSO 15 · +1", TurnController.DealerAccusoTitle(AccusoType.Dealer15, null));
            Assert.AreEqual("ACCUSO 30 · +2", TurnController.DealerAccusoTitle(AccusoType.Dealer30, null));
            Assert.AreEqual("ACCUSO 15", TurnController.DealerAccusoTitle(AccusoType.Dealer15, new MatchRules { EnableAccusi = false }));

            // Le carte si mischiano a caso (Rules51 non accetta un mazzo dato): si ridistribuisce
            // finche' il mazziere non fa 15 o 30.
            for (int i = 0; i < 5000; i++)
            {
                var s = new GameState(2);
                var rm = new RoundManager(s);
                AccusoType? type = null;
                rm.OnDealerAccusoDeclared += (dealer, t, cards) => type = t;
                rm.StartSmazzata();
                if (type == null) continue;

                Assert.AreEqual(type == AccusoType.Dealer30 ? 2 : 1, s.Players[s.DealerIndex].RoundAccusiPoints);
                Assert.AreEqual(0, s.Players.Sum(p => p.ScopaCount));
                return;
            }
            Assert.Fail("Nessun 15/30 del mazziere in 5000 distribuzioni");
        }

        // 5. Tre assi.
        [Test]
        public void TreAssiNeedThreeRealAces()
        {
            var s = new GameState(2);
            s.Players[1].Hand.AddRange(new[] { new Card(Suit.Denari, 1), new Card(Suit.Spade, 1), new Card(Suit.Coppe, 7) });
            Assert.AreEqual(-1, RoundManager.TreAssiHolder(s), "la matta non vale come asso");
            s.Players[1].Hand[2] = new Card(Suit.Bastoni, 1);
            Assert.AreEqual(1, RoundManager.TreAssiHolder(s));
        }

        [Test]
        public void TreAssiBannerSaysWhoGotThemAndWhoWins()
        {
            var solo = new GameState(2);
            Assert.AreEqual("Hai ricevuto tre assi: la partita è tua", TurnController.TreAssiDetail(solo, 0, 0, "Tu"));
            Assert.AreEqual("Bot 2 ha ricevuto tre assi: vince la partita", TurnController.TreAssiDetail(solo, 1, 0, "Bot 2"));
            var team = new GameState(4) { TeamMode = true };
            Assert.AreEqual("Giulia ha ricevuto tre assi: la partita è vostra", TurnController.TreAssiDetail(team, 2, 0, "Giulia"));
            Assert.AreEqual("Marco ha ricevuto tre assi: vince la sua coppia", TurnController.TreAssiDetail(team, 3, 0, "Marco"));
        }

        [TestCase(2, false)]
        [TestCase(4, false)]
        [TestCase(4, true)]
        public void TreAssiAtARedealWinTheMatchBeforeAnyAccuso(int players, bool team)
        {
            var s = new GameState(players) { TeamMode = team, DealerIndex = 0 };
            // Mazzo in ordine di distribuzione: il posto 1 riceve i tre assi, gli altri carte basse.
            int first = (s.DealerIndex - 1 + players) % players;
            var aces = new Queue<Card>(Cards(Suit.Spade, 1).Concat(Cards(Suit.Coppe, 1)).Concat(Cards(Suit.Bastoni, 1)));
            var others = new Queue<Card>(Cards(Suit.Denari, 2, 3, 4, 5, 6, 8, 9, 10).Concat(Cards(Suit.Spade, 2)));
            for (int round = 0; round < 3; round++)
                for (int offset = 0; offset < players; offset++)
                    s.Deck.Add((first + offset) % players == 1 ? aces.Dequeue() : others.Dequeue());
            s.Deck.Add(new Card(Suit.Spade, 9)); // resta qualcosa nel mazzo: non e' l'ultima mano

            var last = new Card(Suit.Coppe, 6);
            s.Players[0].Hand.Add(last);
            s.Table.Add(new Card(Suit.Spade, 5)); // nessuna presa possibile col 6
            var rm = new RoundManager(s);
            bool accusoWindow = false;
            rm.OnNewHandsDealt += () => accusoWindow = true;

            rm.ApplyMove(new Move(0, last, MoveType.PlayOnly));

            Assert.AreEqual(1, RoundManager.TreAssiHolder(s));
            Assert.IsTrue(s.RoundEnded);
            Assert.IsFalse(accusoWindow, "niente finestra Accuso");
            Assert.IsFalse(rm.TryPlayerAccuso(1, AccusoType.Decino), "niente Decino");
            var winners = team ? new[] { 1, 3 } : new[] { 1 };
            for (int p = 0; p < players; p++)
                Assert.AreEqual(winners.Contains(p) ? MatchScore.CappottoScore : 0, s.Players[p].TotalScore, "posto " + p);
            Assert.IsTrue(MatchScore.IsFinished(s, 51));
            CollectionAssert.AreEqual(new[] { MatchScore.EntryOf(s, 1) }, MatchScore.Leaders(s));
        }

        [Test]
        public void TreAssiAtTheFirstDealComeBeforeAccusiAndDealerFifteen()
        {
            // Distribuzione a caso: si ridistribuisce finche' qualcuno non riceve tre assi (~1 volta su 600 a 4).
            for (int i = 0; i < 50000; i++)
            {
                var s = new GameState(4);
                var rm = new RoundManager(s);
                bool accusoWindow = false, dealerAccuso = false;
                rm.OnInitialHandsDealt += () => accusoWindow = true;
                rm.OnDealerAccusoDeclared += (dealer, t, cards) => dealerAccuso = true;
                rm.StartSmazzata();

                int holder = RoundManager.TreAssiHolder(s);
                if (holder < 0)
                {
                    Assert.IsFalse(s.RoundEnded);
                    continue;
                }

                Assert.IsTrue(s.RoundEnded);
                Assert.IsFalse(accusoWindow || dealerAccuso, "i Tre assi vengono prima di ogni accuso");
                Assert.AreEqual(RoundManager.InitialTableCards, s.Table.Count);
                for (int p = 0; p < 4; p++)
                    Assert.AreEqual(p == holder ? MatchScore.CappottoScore : 0, s.Players[p].TotalScore);
                Assert.IsTrue(RoundManager.IsFreshSmazzata(s), "i client in rete devono vedere la distribuzione");
                return;
            }
            Assert.Fail("Nessun Tre assi in 50000 distribuzioni");
        }
    }
}
