#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Project51.Auth;

namespace Project51.Tests
{
    /// <summary>C3: data relativa ed etichetta NUOVO della pagina Novita'.</summary>
    public class NewsServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

        [TestCase(0.2, "oggi")]
        [TestCase(1.5, "ieri")]
        [TestCase(2.1, "2 giorni fa")]
        [TestCase(6.9, "6 giorni fa")]
        [TestCase(7, "1 settimana fa")]
        [TestCase(15, "2 settimane fa")]
        [TestCase(29, "4 settimane fa")]
        [TestCase(30, "1 mese fa")]
        [TestCase(95, "3 mesi fa")]
        [TestCase(365, "1 anno fa")]
        [TestCase(800, "2 anni fa")]
        [TestCase(-1, "oggi")]
        public void RelativeTime(double daysAgo, string expected)
        {
            Assert.AreEqual(expected, NewsService.RelativeTime(Now.AddDays(-daysAgo), Now));
        }

        [Test]
        public void IsNew_AfterLastSeen()
        {
            var seen = Now.AddDays(-3);
            Assert.IsTrue(NewsService.IsNew(Now.AddDays(-1), seen, Now));
            Assert.IsFalse(NewsService.IsNew(Now.AddDays(-5), seen, Now));
        }

        [Test]
        public void IsNew_FirstVisit_OnlyRecent()
        {
            Assert.IsTrue(NewsService.IsNew(Now.AddDays(-(NewsService.FirstVisitNewDays - 1)), null, Now));
            Assert.IsFalse(NewsService.IsNew(Now.AddDays(-(NewsService.FirstVisitNewDays + 1)), null, Now));
        }

        // --- Pagina UI51: intestazione nel testo della notizia

        private static NewsStory Story(string body) => NewsService.Parse(new NewsEntry("n", "Titolo", body, Now));

        [Test]
        public void Parse_Header()
        {
            var s = Story("Etichetta: torneo\r\nsottotitolo: Sabato e domenica\nEvidenza: sì\npulsante: iscriviti\n\nPrimo paragrafo\nsu due righe.\n\n\nSecondo: con i due punti.");
            Assert.AreEqual("TORNEO", s.Tag);
            Assert.AreEqual("Sabato e domenica", s.Subtitle);
            Assert.IsTrue(s.Featured);
            Assert.AreEqual("ISCRIVITI", s.Cta);
            CollectionAssert.AreEqual(new[] { "Primo paragrafo su due righe.", "Secondo: con i due punti." }, s.Paragraphs);
        }

        [Test]
        public void Parse_PlainBody_Defaults()
        {
            var s = Story("Ora: le partite si salvano.\n\nAltro.");
            Assert.AreEqual("NOVITÀ", s.Tag);
            Assert.AreEqual(NewsService.DefaultCta, s.Cta);
            Assert.IsFalse(s.Featured);
            Assert.AreEqual("Ora: le partite si salvano.", s.Subtitle);
            Assert.AreEqual(2, s.Paragraphs.Length);
            Assert.AreEqual("NOVITÀ", Story("etichetta: Novita\n\nx").Tag);
            Assert.AreEqual("NOVITÀ", Story("etichetta: sconosciuta\n\nx").Tag);
            Assert.AreEqual(0, Story(null).Paragraphs.Length);
        }

        [Test]
        public void Split_FeaturedOrNewest()
        {
            var a = Story("x"); var b = Story("evidenza: si\n\nx"); var c = Story("x");
            var featured = new System.Collections.Generic.List<NewsStory>();
            var rest = new System.Collections.Generic.List<NewsStory>();
            NewsService.Split(new System.Collections.Generic.List<NewsStory> { a, b, c }, featured, rest);
            CollectionAssert.AreEqual(new[] { b }, featured);
            CollectionAssert.AreEqual(new[] { a, c }, rest);
            NewsService.Split(new System.Collections.Generic.List<NewsStory> { a, c }, featured, rest);
            CollectionAssert.AreEqual(new[] { a }, featured);
            CollectionAssert.AreEqual(new[] { c }, rest);
        }

        [Test]
        public void Date_LongAndShort()
        {
            Assert.AreEqual("24 settembre", NewsService.Date(new DateTime(2026, 9, 24), false));
            Assert.AreEqual("3 gen", NewsService.Date(new DateTime(2026, 1, 3), true));
        }
    }
}
#endif
