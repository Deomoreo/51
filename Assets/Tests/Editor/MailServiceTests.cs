#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Project51.Auth;

namespace Project51.Tests
{
    /// <summary>UI51 Fase 8: JSON della Posta (dati in sola lettura "Posta" su PlayFab) e scadenze.</summary>
    public class MailServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Parse_ArrayOrWrapper_NewestFirst_DefaultsFilled()
        {
            const string json = "[{\"titolo\":\"Vecchio\",\"data\":\"2026-09-25\"}," +
                                "{\"id\":\"b\",\"tipo\":\"stagione\",\"titolo\":\"Nuovo\",\"data\":\"2026-09-30T08:00:00Z\",\"allegati\":[{\"tipo\":\"monete\",\"quantita\":200}]}]";
            var list = MailService.Parse(json, Now, Now);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("b", list[0].id);
            Assert.AreEqual("Vecchio", list[1].id, "senza id vale il titolo");
            Assert.AreEqual("Team 51", list[1].da);
            Assert.IsTrue(list[0].CanClaim);
            Assert.AreEqual(200, list[0].allegati[0].quantita);
            Assert.AreEqual(1, MailService.Parse("{\"messaggi\":[{\"titolo\":\"x\"}]}", Now, Now).Count, "data assente = fallback");
        }

        [Test]
        public void Parse_DropsOldExpiredAndBroken()
        {
            const string json = "[{\"titolo\":\"Vecchio\",\"data\":\"2026-08-15\"}," +
                                "{\"titolo\":\"Scaduto\",\"data\":\"2026-09-28\",\"scade\":\"2026-09-30\",\"allegati\":[{\"tipo\":\"gemme\",\"quantita\":5}]}," +
                                "{\"titolo\":\"Scaduto ma riscattato\",\"data\":\"2026-09-28\",\"scade\":\"2026-09-30\",\"riscattato\":true,\"allegati\":[{\"tipo\":\"gemme\",\"quantita\":5}]}," +
                                "{\"titolo\":\"\"}]";
            var list = MailService.Parse(json, Now, Now);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Scaduto ma riscattato", list[0].titolo);
            Assert.AreEqual(0, MailService.Parse("non e' json", Now, Now).Count);
            Assert.AreEqual(0, MailService.Parse(null, Now, Now).Count);
        }

        [TestCase(5.5, "Scade tra 5 giorni")]
        [TestCase(1.2, "Scade domani")]
        [TestCase(0.3, "Scade oggi")]
        public void ExpiryLabel(double days, string expected)
        {
            var m = new MailMessage { allegati = new[] { new MailGift { tipo = "forziere", quantita = 1 } }, ExpiresUtc = Now.AddDays(days) };
            Assert.AreEqual(expected, MailService.ExpiryLabel(m, Now));
            m.riscattato = true;
            Assert.AreEqual(string.Empty, MailService.ExpiryLabel(m, Now));
        }
    }
}
#endif
