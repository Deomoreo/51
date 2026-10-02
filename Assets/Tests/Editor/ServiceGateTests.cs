#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Project51.Auth;

namespace Project51.Tests
{
    /// <summary>UI51 Fase 9: aggiornamento obbligatorio e manutenzione letti da Title Data.</summary>
    public class ServiceGateTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void IsOlder_ComparesProjectVersionsAsDecimals()
        {
            Assert.IsTrue(ServiceGate.IsOlder("2.43", "2.5"));   // 2.5 = 2.50
            Assert.IsTrue(ServiceGate.IsOlder("2.09", "2.10"));
            Assert.IsFalse(ServiceGate.IsOlder("2.50", "2.5"));
            Assert.IsFalse(ServiceGate.IsOlder("3.0", "2.99"));
            Assert.IsFalse(ServiceGate.IsOlder("2.43", ""));     // illeggibile: non blocca
            Assert.IsFalse(ServiceGate.IsOlder("2.43", "abc"));
        }

        [Test]
        public void Parse_UpdateWinsOverMaintenance()
        {
            var g = ServiceGate.Parse("{\"minima\":\"2.50\",\"novita\":[\"Uno\",\"Due\"]}", "{\"fine\":\"2026-10-01T13:00:00Z\"}", "2.44", Now);
            Assert.AreEqual(ServiceGate.State.Update, g.state);
            Assert.AreEqual("2.50", g.version);
            CollectionAssert.AreEqual(new[] { "Uno", "Due" }, g.news);
        }

        [Test]
        public void Parse_MaintenanceOnlyUntilItsEnd()
        {
            var g = ServiceGate.Parse(null, "{\"fine\":\"2026-10-01T13:00:00Z\"}", "2.44", Now);
            Assert.AreEqual(ServiceGate.State.Maintenance, g.state);
            Assert.AreEqual(new DateTime(2026, 10, 1, 13, 0, 0, DateTimeKind.Utc), g.endUtc);
            Assert.AreEqual(ServiceGate.State.Open, ServiceGate.Parse(null, "{\"fine\":\"2026-10-01T11:00:00Z\"}", "2.44", Now).state);
            Assert.AreEqual(ServiceGate.State.Open, ServiceGate.Parse("non json", "{}", "2.44", Now).state);
        }

        [Test]
        public void Countdown_MinutesLikeTheMockup_HoursWhenLong()
        {
            Assert.AreEqual("42:18", ServiceGate.Countdown(TimeSpan.FromSeconds(42 * 60 + 18)));
            Assert.AreEqual("1:02:03", ServiceGate.Countdown(TimeSpan.FromSeconds(3723)));
            Assert.AreEqual("00:00", ServiceGate.Countdown(TimeSpan.FromSeconds(-5)));
        }
    }
}
#endif
