#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    public class MatchScoreTests
    {
        private static IEnumerable<Card> Cards(Suit suit, params int[] ranks) => ranks.Select(r => new Card(suit, r));

        // Tutte le 40 carte: coppia 0 (posti 0+2) con 22 carte e 7 denari ma nessun giocatore
        // sopra le soglie da solo; coppia 1 (posti 1+3) con la Grande.
        private static GameState DealtFourPlayers(bool teamMode)
        {
            var s = new GameState(4) { TeamMode = teamMode };
            s.Players[0].CapturedCards.AddRange(Cards(Suit.Denari, 1, 2, 3, 4).Concat(Cards(Suit.Coppe, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)));
            s.Players[2].CapturedCards.AddRange(Cards(Suit.Denari, 5, 6, 7).Concat(Cards(Suit.Spade, 1, 2, 3, 4, 5)));
            s.Players[1].CapturedCards.AddRange(Cards(Suit.Denari, 8, 9, 10).Concat(Cards(Suit.Spade, 6, 7, 8, 9, 10)));
            s.Players[3].CapturedCards.AddRange(Cards(Suit.Bastoni, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
            return s;
        }

        [Test]
        public void TeamScoringPoolsPartnerCaptures()
        {
            var team = DealtFourPlayers(teamMode: true);
            var breakdown = PunteggioManager.CalculateBreakdown(team);
            Assert.AreEqual(2, breakdown.Length);
            Assert.IsTrue(breakdown[0].WonDenari);
            Assert.IsTrue(breakdown[0].WonCards);
            Assert.IsTrue(breakdown[0].HasSetteBello);
            Assert.AreEqual(3, breakdown[0].PiccolaExtras);
            Assert.IsTrue(breakdown[1].HasGrande);
            // Settebello 1 + denari 1 + carte 1 + piccola 3+3 = 9; Grande = 5. Ogni compagno riceve il punteggio di squadra.
            CollectionAssert.AreEqual(new[] { 9, 5, 9, 5 }, PunteggioManager.CalculateSmazzataScores(team));
        }

        [Test]
        public void FreeForAllScoringIsUnchangedForTheSameCaptures()
        {
            var solo = DealtFourPlayers(teamMode: false);
            Assert.AreEqual(4, PunteggioManager.CalculateBreakdown(solo).Length);
            // Nessuno raggiunge 6 denari o 21 carte; piccola con il 4 = 4, Grande = 5, settebello = 1.
            CollectionAssert.AreEqual(new[] { 4, 5, 1, 0 }, PunteggioManager.CalculateSmazzataScores(solo));
        }

        [Test]
        public void TeamScopeAndAccusiAreSummedAndBothPartnersGetTheTeamTotal()
        {
            var s = DealtFourPlayers(teamMode: true);
            s.Players[0].ScopaCount = 1;
            s.Players[2].ScopaCount = 2;
            s.Players[0].RoundAccusiPoints = 3;
            s.Players[2].RoundAccusiPoints = 10;
            var breakdown = PunteggioManager.CalculateBreakdown(s);
            Assert.AreEqual(3, breakdown[0].ScopaCount);
            Assert.AreEqual(13, breakdown[0].AccusiPoints);

            new RoundManager(s).EndSmazzata();
            Assert.AreEqual(9 + 3 + 13, s.Players[0].TotalScore);
            Assert.AreEqual(s.Players[0].TotalScore, s.Players[2].TotalScore);
            Assert.AreEqual(5, s.Players[1].TotalScore);
            CollectionAssert.AreEqual(new[] { 25, 5 }, MatchScore.RoundScores(s));
        }

        [Test]
        public void RulesTravelWithTheStateAndAreApplied()
        {
            var disabled = new GameState(2) { Rules = new MatchRules { EnableAccusi = false } };
            disabled.Players[0].Hand.AddRange(new[] { new Card(Suit.Coppe, 1), new Card(Suit.Denari, 1), new Card(Suit.Spade, 1) });
            Assert.IsFalse(new RoundManager(disabled).TryPlayerAccuso(0, AccusoType.Decino));

            var halved = new GameState(2) { Rules = new MatchRules { AccusiPointMultiplier = 0.5f } };
            halved.Players[0].Hand.AddRange(new[] { new Card(Suit.Coppe, 1), new Card(Suit.Denari, 1), new Card(Suit.Spade, 1) });
            Assert.IsTrue(new RoundManager(halved).TryPlayerAccuso(0, AccusoType.Decino));
            Assert.AreEqual(5, halved.Players[0].RoundAccusiPoints);
        }

        [Test]
        public void CappottoBonusIsAwardedOnlyOnce()
        {
            var s = new GameState(2) { Rules = new MatchRules { CappottoEndsGameImmediately = false, CappottoBonusPoints = 7 } };
            s.Players[0].CapturedCards.AddRange(Cards(Suit.Denari, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
            s.Players[0].Hand.AddRange(Cards(Suit.Coppe, 2, 3));
            s.Players[1].Hand.AddRange(Cards(Suit.Spade, 4, 6));
            s.Table.Add(new Card(Suit.Bastoni, 9));
            var rm = new RoundManager(s);
            for (int i = 0; i < 4 && !s.RoundEnded; i++)
                rm.ApplyMove(Rules51.GetValidMoves(s, s.CurrentPlayerIndex).First());

            Assert.IsTrue(s.RoundEnded);
            var entry = PunteggioManager.CalculateBreakdown(s)[0];
            Assert.AreEqual(entry.Points + entry.AccusiPoints + 7, s.Players[0].TotalScore);
        }

        [Test]
        public void ImmediateCappottoEndsTheMatchOnTheCompletingCapture()
        {
            var s = new GameState(2);
            s.Players[0].CapturedCards.AddRange(Cards(Suit.Denari, 1, 2, 3, 4, 5, 6, 7, 8, 9));
            s.Players[0].Hand.AddRange(new[] { new Card(Suit.Denari, 10), new Card(Suit.Bastoni, 4) });
            s.Players[1].Hand.AddRange(Cards(Suit.Spade, 4, 5));
            s.Table.Add(new Card(Suit.Coppe, 10));
            s.MatchTotals = new[] { 3, 40 };

            new RoundManager(s).ApplyMove(new Move(0, new Card(Suit.Denari, 10), MoveType.CaptureEqual, new List<Card> { new Card(Suit.Coppe, 10) }));

            Assert.IsTrue(s.RoundEnded);
            Assert.AreEqual(MatchScore.CappottoScore, s.Players[0].TotalScore);
            Assert.IsTrue(MatchScore.IsFinished(s, 51));
            CollectionAssert.AreEqual(new[] { 0 }, MatchScore.Leaders(s));
        }

        [Test]
        public void MatchTotalsCarryOverRotateDealerAndResetAfterAWin()
        {
            var first = new GameState(2) { RoundEnded = true, RoundIndex = 2, DealerIndex = 1, MatchTotals = new[] { 30, 20 } };
            first.Players[0].TotalScore = 10;
            first.Players[1].TotalScore = 25;
            var second = new GameState(2);
            MatchScore.ContinueMatch(first, second, 51);
            CollectionAssert.AreEqual(new[] { 40, 45 }, second.MatchTotals);
            Assert.AreEqual(3, second.RoundIndex);
            Assert.AreEqual(0, second.DealerIndex);

            second.RoundEnded = true;
            second.Players[0].TotalScore = 13;
            second.Players[1].TotalScore = 3;
            Assert.IsTrue(MatchScore.IsFinished(second, 51));
            var rematch = new GameState(2);
            MatchScore.ContinueMatch(second, rematch, 51);
            CollectionAssert.AreEqual(new[] { 0, 0 }, rematch.MatchTotals);
            Assert.AreEqual(1, rematch.RoundIndex);
        }

        [Test]
        public void TiedLeadersAboveTheTargetPlayAnotherSmazzata()
        {
            var s = new GameState(2) { RoundEnded = true, MatchTotals = new[] { 50, 50 } };
            s.Players[0].TotalScore = 2;
            s.Players[1].TotalScore = 2;
            Assert.IsFalse(MatchScore.IsFinished(s, 51));
            var next = new GameState(2);
            MatchScore.ContinueMatch(s, next, 51);
            CollectionAssert.AreEqual(new[] { 52, 52 }, next.MatchTotals);
        }

        [Test]
        public void TeamMatchTotalsUseOneEntryPerTeam()
        {
            var s = new GameState(4) { TeamMode = true, RoundEnded = true, MatchTotals = new[] { 10, 12 } };
            int[] round = { 5, 7, 5, 7 };
            for (int i = 0; i < 4; i++) s.Players[i].TotalScore = round[i];
            Assert.AreEqual(2, MatchScore.EntryCount(s));
            CollectionAssert.AreEqual(new[] { 15, 19 }, MatchScore.Totals(s));
            CollectionAssert.AreEqual(new[] { 1, 3 }, MatchScore.MembersOf(s, 1));

            var soloNext = new GameState(4);
            MatchScore.ContinueMatch(s, soloNext, 51);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, soloNext.MatchTotals, "Changing format starts a new match");
        }

        [Test]
        public void SerializedStateRoundTripsWithRulesTeamsAndTotalsInAnyCulture()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");
                var s = new GameState(4)
                {
                    DealerIndex = 3, CurrentPlayerIndex = 2, LastCapturePlayerIndex = 1, RoundEnded = true, RoundIndex = 4, TeamMode = true,
                    Rules = new MatchRules { EnableAccusi = false, AccusiPointMultiplier = 0.5f, CappottoEndsGameImmediately = false, CappottoBonusPoints = 3 },
                    MatchTotals = new[] { 12, 40 }
                };
                s.Deck.AddRange(Cards(Suit.Bastoni, 1, 2));
                s.Table.Add(new Card(Suit.Coppe, 7));
                s.Players[1].Hand.AddRange(Cards(Suit.Spade, 3, 4));
                s.Players[2].CapturedCards.AddRange(Cards(Suit.Denari, 7, 8));
                s.Players[2].ScopaCards.Add(new Card(Suit.Denari, 8));
                s.Players[2].ScopaCount = 1;
                s.Players[3].AccusiPoints = 3;
                s.Players[3].RoundAccusiPoints = 13;
                s.Players[0].TotalScore = 21;

                var copy = GameStateSerializer.Deserialize(GameStateSerializer.Serialize(s));

                Assert.AreEqual(GameStateSerializer.Serialize(s), GameStateSerializer.Serialize(copy));
                Assert.IsTrue(copy.TeamMode);
                Assert.AreEqual(0.5f, copy.Rules.AccusiPointMultiplier);
                Assert.IsFalse(copy.Rules.EnableAccusi);
                Assert.AreEqual(3, copy.Rules.CappottoBonusPoints);
                CollectionAssert.AreEqual(new[] { 12, 40 }, copy.MatchTotals);
                Assert.AreEqual(4, copy.RoundIndex);
                Assert.AreEqual(13, copy.Players[3].RoundAccusiPoints);
                Assert.AreEqual(new Card(Suit.Denari, 8), copy.Players[2].ScopaCards.Single());
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Test]
        public void TwoVsTwoSeatsFirstTwoPlayersAsPartnersAndGivesTheRestToBots()
        {
            Assert.AreEqual(0, SeatLayout.SeatForJoinOrder(GameFormat.TwoVsTwo, 0));
            Assert.AreEqual(2, SeatLayout.SeatForJoinOrder(GameFormat.TwoVsTwo, 1), "Host and first friend are partners");
            Assert.AreEqual(1, SeatLayout.SeatForJoinOrder(GameFormat.TwoVsTwo, 2));
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, SeatLayout.BotSeats(GameFormat.TwoVsTwo, 4, 2));
            CollectionAssert.AreEquivalent(new[] { 3 }, SeatLayout.BotSeats(GameFormat.TwoVsTwo, 4, 3));

            Assert.AreEqual(1, SeatLayout.SeatForJoinOrder(GameFormat.FourPlayers, 1));
            CollectionAssert.AreEquivalent(new[] { 2, 3 }, SeatLayout.BotSeats(GameFormat.FourPlayers, 4, 2));
            CollectionAssert.IsEmpty(SeatLayout.BotSeats(GameFormat.OneVsOne, 2, 2));
            Assert.AreEqual(-1, SeatLayout.SeatForJoinOrder(GameFormat.TwoVsTwo, -1));
        }

        [Test]
        public void OlderSerializedStatesWithoutTheNewSectionsStillLoad()
        {
            var s = new GameState(2) { DealerIndex = 1, RoundIndex = 2 };
            s.Players[0].Hand.Add(new Card(Suit.Coppe, 3));
            var parts = GameStateSerializer.Serialize(s).Split(new[] { "||" }, System.StringSplitOptions.None);
            var legacy = string.Join("||", parts.Take(parts.Length - 3));

            var copy = GameStateSerializer.Deserialize(legacy);
            Assert.IsFalse(copy.TeamMode);
            Assert.IsNull(copy.Rules);
            Assert.AreEqual(2, copy.RoundIndex);
            Assert.AreEqual(1, copy.Players[0].Hand.Count);
        }
    }
}
#endif
