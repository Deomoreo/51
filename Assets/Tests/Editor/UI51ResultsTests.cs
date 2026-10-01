#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    /// <summary>UI51 Fase 7 (fine smazzata e fine partita): logica pura dietro alla nuova grafica.</summary>
    public class UI51ResultsTests
    {
        [Test]
        public void RowPointsAddUpToTheHandScoreWithAccusi()
        {
            var s = new SmazzataScore { WonCards = true, HasSetteBello = true, HasPiccola = true, PiccolaExtras = 2, ScopaCount = 3, AccusiPoints = 10 };
            int sum = Enumerable.Range(0, ResultsSheet.Labels.Length).Sum(r => ResultsSheet.Points(s, r));
            Assert.AreEqual(s.Points + s.AccusiPoints, sum);
            Assert.AreEqual(5, ResultsSheet.Points(s, 5), "piccola con 4 e 5 = 3 + 2");
        }

        [Test]
        public void RanksShareTiesLikeTheUserChose()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 2, 4 }, ResultsSheet.Ranks(new[] { 52, 44, 44, 30 }));
            CollectionAssert.AreEqual(new[] { 2, 1, 2, 2 }, ResultsSheet.Ranks(new[] { 10, 20, 10, 10 }));
        }

        [Test]
        public void DetailsFollowTheMockupWording()
        {
            var state = new GameState(2);
            state.Players[1].CapturedCards.Add(new Card(Suit.Denari, 7));
            state.Players[1].RoundAccusiPoints = 3;
            var b = new[] { new SmazzataScore { CardCount = 22, PrimieraScore = 78 }, new SmazzataScore { CardCount = 18, PrimieraScore = -1, HasPiccola = true, PiccolaExtras = 1 } };
            System.Func<int, string> name = p => p == 0 ? "Tu" : "Marco_93";
            Assert.AreEqual("22 – 18", ResultsSheet.Detail(state, b, 0, name, name, 0));
            Assert.AreEqual("preso da Marco_93", ResultsSheet.Detail(state, b, 2, name, name, 0));
            Assert.AreEqual("preso da te", ResultsSheet.Detail(state, b, 2, name, name, 1));
            Assert.AreEqual("78 – -", ResultsSheet.Detail(state, b, 3, name, name, 0));
            Assert.AreEqual("nessuno", ResultsSheet.Detail(state, b, 4, name, name, 0));
            Assert.AreEqual("Asso, 2, 3 e 4 di denari", ResultsSheet.Detail(state, b, 5, name, name, 0));
            Assert.AreEqual("Marco_93", ResultsSheet.Detail(state, b, 7, name, name, 0));
            Assert.AreEqual("14 · 9 · 10 · 7", ResultsSheet.Joined(new[] { "14", "9", "10", "7" }));
        }

        [Test]
        public void ColumnsStartFromMyEntryEvenWhenIAmNotSeatZero()
        {
            var state = new GameState(4);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 0 }, Project51.Unity.UI.UI51ResultsView.Columns(state, 1));
            state.TeamMode = true;
            CollectionAssert.AreEqual(new[] { 1, 0 }, Project51.Unity.UI.UI51ResultsView.Columns(state, 1));
        }

        [Test]
        public void RaceNoteCountsFromMeAtTwoAndFromTheLeaderAtFour()
        {
            Assert.AreEqual("17 punti alla vittoria", ResultsSheet.RaceNote(new[] { 34, 40 }, 0, 51));
            Assert.AreEqual("1 punto alla vittoria", ResultsSheet.RaceNote(new[] { 50, 40 }, 0, 51));
            Assert.AreEqual("in testa: 11 punti alla vittoria", ResultsSheet.RaceNote(new[] { 34, 40, 12, 3 }, 2, 51));
            Assert.AreEqual("pari: si gioca ancora", ResultsSheet.RaceNote(new[] { 55, 55 }, 0, 51));
        }
    }
}
#endif
