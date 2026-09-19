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
    }
}
#endif
