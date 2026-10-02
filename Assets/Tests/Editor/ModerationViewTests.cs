#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Auth;
using Project51.Unity.UI;

namespace Project51.Tests
{
    /// <summary>UI51 Fase 11 (2.54): testi di Sospensione ed Esito segnalazione. I conteggi veri li fa il CloudScript (Server/CloudScript/test.js).</summary>
    public class ModerationViewTests
    {
        [TestCase(0, "00:00")]
        [TestCase(-5, "00:00")]
        [TestCase(2538, "42:18")]
        [TestCase(3600, "1h 0m")]
        [TestCase(86399, "23h 59m")]
        [TestCase(86400, "1g 0h 0m")]
        [TestCase(172799, "1g 23h 59m")]
        public void Countdown(int seconds, string expected) => Assert.AreEqual(expected, UI51SuspensionView.Countdown(seconds));

        [TestCase("abbandoni", "Abbandoni ripetuti")]
        [TestCase("segnalazioni", "Segnalazioni dei giocatori")]
        [TestCase("", "Comportamento scorretto")]
        [TestCase(null, "Comportamento scorretto")]
        [TestCase("  Linguaggio offensivo ", "Linguaggio offensivo")]
        public void ReasonLabel(string reason, string expected) => Assert.AreEqual(expected, UI51SuspensionView.ReasonLabel(reason));

        [Test]
        public void Body_AlwaysPointsToBots()
        {
            foreach (var r in new[] { "abbandoni", "segnalazioni", "altro" })
                StringAssert.EndsWith("contro i bot.", UI51SuspensionView.Body(r));
            StringAssert.StartsWith("Hai lasciato a metà", UI51SuspensionView.Body("abbandoni"));
        }

        [TestCase("2026-09-28", "1 vs 1", "Segnalazione del 28 settembre · partita 1 vs 1. Per privacy non mostriamo il nome.")]
        [TestCase("2026-01-03", "", "Segnalazione del 3 gennaio. Per privacy non mostriamo il nome.")]
        [TestCase("ieri", "2 vs 2", "Segnalazione · partita 2 vs 2. Per privacy non mostriamo il nome.")]
        [TestCase(null, null, "Segnalazione. Per privacy non mostriamo il nome.")]
        public void OutcomeInfo(string date, string mode, string expected) => Assert.AreEqual(expected, UI51ReportOutcomeView.Info(date, mode));

        [Test]
        public void OutcomeLines_OneLinePerOutcome_ThenPrivacy()
        {
            var two = new[] { new ServerReportOutcome { data = "2026-09-28", modo = "1 vs 1" }, new ServerReportOutcome { data = "2026-09-30", modo = "2 vs 2" } };
            Assert.AreEqual("Segnalazione del 28 settembre · partita 1 vs 1.\nSegnalazione del 30 settembre · partita 2 vs 2.\n" +
                            "Per privacy non mostriamo i nomi.", UI51ReportOutcomeView.Lines(two));
            var five = new ServerReportOutcome[5];
            for (int i = 0; i < 5; i++) five[i] = new ServerReportOutcome { data = "2026-09-2" + i, modo = "" };
            StringAssert.Contains("E altre 2.", UI51ReportOutcomeView.Lines(five));
            Assert.AreEqual(UI51ReportOutcomeView.Info("2026-09-28", "1 vs 1"), UI51ReportOutcomeView.Lines(new[] { two[0] }));
        }

        [Test]
        public void ForfeitXp_ThreePerDay_OncePerOpponent_ResetsNextDay()
        {
            string saved = null;
            Assert.IsTrue(Project51.UIV2.Core.MatchResultsV2.ForfeitRewarded("A", "2026-10-01", ref saved));
            Assert.IsFalse(Project51.UIV2.Core.MatchResultsV2.ForfeitRewarded("A", "2026-10-01", ref saved), "stesso avversario");
            Assert.IsTrue(Project51.UIV2.Core.MatchResultsV2.ForfeitRewarded("", "2026-10-01", ref saved), "sconosciuto");
            Assert.IsTrue(Project51.UIV2.Core.MatchResultsV2.ForfeitRewarded(null, "2026-10-01", ref saved), "sconosciuto, mai doppio");
            Assert.IsFalse(Project51.UIV2.Core.MatchResultsV2.ForfeitRewarded("B", "2026-10-01", ref saved), "quarta del giorno");
            Assert.AreEqual("2026-10-01|A,?,?", saved);
            Assert.IsTrue(Project51.UIV2.Core.MatchResultsV2.ForfeitRewarded("A", "2026-10-02", ref saved), "giorno dopo");
            Assert.AreEqual("2026-10-02|A", saved);
        }

        [Test]
        public void Rejoin_OnlyOwnRegisteredMatchFromAnotherRun_WithinTheSeatTime()
        {
            long now = 638000000000000000L, sec = System.TimeSpan.TicksPerSecond;
            string[] marker = "P1|oldrun|1 vs 1|room-7".Split(new[] { '|' }, 4);
            int left;
            Assert.AreEqual("room-7", ModerationService.RejoinRoom(marker, "P1", true, "newrun", (now - 15 * sec).ToString(), now, out left));
            Assert.AreEqual(45, left);
            Assert.AreEqual("room-7", ModerationService.RejoinRoom(marker, "P1", true, "newrun", "", now, out left), "crash: si prova");
            Assert.AreEqual(60, left);
            Assert.IsNull(ModerationService.RejoinRoom(marker, "P1", true, "newrun", (now - 61 * sec).ToString(), now, out left), "posto scaduto");
            Assert.IsNull(ModerationService.RejoinRoom(marker, "P1", false, "newrun", "", now, out left), "ospite");
            Assert.IsNull(ModerationService.RejoinRoom(marker, "P2", true, "newrun", "", now, out left), "altro account");
            Assert.IsNull(ModerationService.RejoinRoom(marker, "P1", true, "oldrun", "", now, out left), "stessa esecuzione");
            Assert.IsNull(ModerationService.RejoinRoom("P1|oldrun|1 vs 1".Split(new[] { '|' }, 4), "P1", true, "newrun", "", now, out left), "vecchio formato");
        }

        [Test]
        public void TurnTimer_KeyChangesWithEveryMove()
        {
            // Il tempo del turno e la "una sola mossa per turno" ripartono quando la chiave cambia: ogni mossa deve cambiarla.
            var state = Project51.Core.Rules51.CreateNewGame(2);
            var seen = new System.Collections.Generic.HashSet<long> { Project51.Unity.TurnController.TurnKey(state) };
            for (int i = 0; i < 6; i++)
            {
                var moves = Project51.Core.Rules51.GetValidMoves(state, state.CurrentPlayerIndex);
                Assert.IsNotEmpty(moves);
                Project51.Core.Rules51.ApplyMove(state, moves[0]);
                Assert.IsTrue(seen.Add(Project51.Unity.TurnController.TurnKey(state)), "mossa " + i);
            }
        }

        [Test]
        public void TurnId_GrowsWithEveryMove_WholeSmazzata()
        {
            // Di due mosse per lo stesso turno vale la prima: quella in ritardo ha un numero gia' passato e si scarta senza resync.
            var state = Project51.Core.Rules51.CreateNewGame(2);
            var round = new Project51.Core.RoundManager(state);
            int played = 0, id = Project51.Unity.TurnController.TurnId(state);
            while (!state.RoundEnded)
            {
                var moves = Project51.Core.Rules51.GetValidMoves(state, state.CurrentPlayerIndex);
                if (moves.Count == 0) break;
                round.ApplyMove(moves[0]);
                played++;
                int next = Project51.Unity.TurnController.TurnId(state);
                Assert.Greater(next, id, "mossa " + played);
                id = next;
            }
            Assert.GreaterOrEqual(played, 6);
        }

        // --- Ospiti: sanzioni sul dispositivo, stesse regole del server (Server/CloudScript/test.js).
        const long T0 = 1790000000L, H = 3600L, D = 86400L;

        [Test]
        public void Device_FiveAbandonsInAWeek_EscalateThenResetAfter30CleanDays()
        {
            var d = new DeviceModeration();
            for (int i = 0; i < 4; i++) d.AddAbandon(T0 + i);
            Assert.IsNull(d.Suspension(T0 + 10), "4 non bastano");
            d.AddAbandon(T0 + 10);
            Assert.AreEqual(24 * H, d.Suspension(T0 + 10).secondi);
            Assert.AreEqual("abbandoni", d.Suspension(T0 + 10).motivo);
            d.AddAbandon(T0 + 20);
            Assert.AreEqual(1, d.volte, "niente seconda sospensione mentre e' in corso");

            long now = T0;
            foreach (long hours in new[] { 72L, 72L, 168L, 168L })
            {
                now += 8 * D;
                for (int i = 0; i < 5; i++) d.AddAbandon(now + i);
                Assert.AreEqual(hours * H, d.Suspension(now + 4).secondi);
            }
            now += 7 * D + 31 * D;
            for (int i = 0; i < 5; i++) d.AddAbandon(now);
            Assert.AreEqual(1, d.volte, "30 giorni puliti");
            Assert.AreEqual(24 * H, d.Suspension(now).secondi);

            var old = new DeviceModeration();
            for (int i = 0; i < 4; i++) old.AddAbandon(T0);
            old.AddAbandon(T0 + 7 * D);
            Assert.IsNull(old.Suspension(T0 + 7 * D), "abbandoni vecchi di 7 giorni non contano");
        }

        [Test]
        public void Device_ClockFollowsTheServer()
        {
            long phone = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            DeviceModeration.SyncClock((phone + 3 * D) * 1000);
            Assert.That(DeviceModeration.Now - phone, Is.InRange(3 * D - 2, 3 * D + 2), "telefono indietro di 3 giorni: vale il server");
            DeviceModeration.SyncClock(0);
            Assert.That(DeviceModeration.Now - phone, Is.InRange(3 * D - 2, 3 * D + 2), "senza risposta resta l'ultima ora nota");
            DeviceModeration.SyncClock(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Assert.That(DeviceModeration.Now - System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Is.InRange(-1L, 1L));
        }

        [Test]
        public void Device_MirrorsServerSanctionsOnce_WithDeviceEscalation()
        {
            var d = new DeviceModeration();
            var s = new ServerSuspension { secondi = 24 * 3600, motivo = "segnalazioni", volte = 1, fine = "2026-10-02T10:00:00Z" };
            var m = new ServerSuspension { secondi = 24 * 3600, motivo = "emoticon", volte = 1, fine = "2026-10-02T11:00:00Z" };
            d.Mirror(s, m, T0);
            Assert.AreEqual(24 * H, d.Suspension(T0).secondi);
            Assert.AreEqual("segnalazioni", d.Suspension(T0).motivo);
            Assert.AreEqual(24 * H, d.Mute(T0).secondi);
            d.Mirror(s, m, T0 + 2 * D);
            Assert.IsNull(d.Suspension(T0 + 2 * D), "la stessa sanzione del server si porta una volta sola");
            var again = new ServerSuspension { secondi = 3600, motivo = "emoticon", volte = 1, fine = "2026-10-05T11:00:00Z" };
            d.Mirror(null, again, T0 + 3 * D);
            Assert.AreEqual(72 * H, d.Mute(T0 + 3 * D).secondi, "seconda volta sul dispositivo: 3 giorni");
            Assert.IsNull(new DeviceModeration().Mute(T0));
        }
    }
}
#endif
