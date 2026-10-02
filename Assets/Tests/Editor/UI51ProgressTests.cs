#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Auth;
using Project51.Core;

namespace Project51.Tests
{
    /// <summary>UI51 Fase 15 (Progressione): logica pura dietro alle schermate nuove.</summary>
    public class UI51ProgressTests
    {
        [TearDown]
        public void Reset() => ProfileService.LevelUpFrom = ProfileService.LevelUpTo = 0;

        [Test]
        public void LevelUpSpansEveryMatchSinceTheLastHome()
        {
            Reset();
            var profile = new ProfileService();
            profile.ApplyServerStats(new ServerReward { statistiche = true, livello = 3 });
            Assert.AreEqual(0, ProfileService.LevelUpTo, "statistiche non ancora lette: non si sa da dove si parte");
            profile.ApplyServerStats(new ServerReward { statistiche = true, livello = 3 });
            Assert.AreEqual(0, ProfileService.LevelUpTo, "stesso livello = niente LivelloSu");
            profile.ApplyServerStats(new ServerReward { statistiche = true, livello = 4 });
            profile.ApplyServerStats(new ServerReward { statistiche = true, livello = 5 });
            Assert.AreEqual(3, ProfileService.LevelUpFrom);
            Assert.AreEqual(5, ProfileService.LevelUpTo);
            profile.ApplyServerStats(new ServerReward { statistiche = false, livello = 9 });
            Assert.AreEqual(5, ProfileService.LevelUpTo, "senza statistiche il server non ha contato la partita");
        }

        [Test]
        public void TrophiesMatchTheQuickProfileMedalsAndCountProgress()
        {
            // Le medaglie del profilo rapido (10 vittorie, 100 partite, 100 scope, livello 10) sono trofei con le stesse soglie.
            foreach (var stats in new[] { new[] { 99, 9, 99, 9 }, new[] { 100, 10, 100, 10 }, new[] { 0, 0, 0, 1 } })
            {
                int medals = PlayerXp.Medals(stats[0], stats[1], stats[2], stats[3]);
                foreach (var t in Trophies.All)
                {
                    bool medal = (t.Stat == TrophyStat.Wins && t.Target == 10) || (t.Stat == TrophyStat.Games && t.Target == 100)
                        || (t.Stat == TrophyStat.Scope && t.Target == 100) || (t.Stat == TrophyStat.Level && t.Target == 10);
                    if (medal) Assert.AreEqual((medals & (1 << t.Medal)) != 0, Trophies.Earned(t, stats[0], stats[1], stats[2], stats[3]), t.Name);
                }
            }
            Assert.AreEqual(0, Trophies.CountEarned(0, 0, 0, 1));
            Assert.AreEqual(Trophies.All.Length, Trophies.CountEarned(9999, 9999, 9999, 100));
            var scope500 = System.Array.Find(Trophies.All, t => t.Name == "Re della scopa");
            Assert.AreEqual(312, Trophies.Progress(scope500, 0, 0, 312, 1));
            Assert.AreEqual(500, Trophies.Progress(scope500, 0, 0, 900, 1), "il progresso si ferma alla soglia");
        }

        [Test]
        public void RankingTextsCountDownAndPointToTheTopTen()
        {
            Assert.AreEqual("Si azzera tra pochi minuti", LeaderboardService.ResetText(System.TimeSpan.FromMinutes(40)));
            Assert.AreEqual("Si azzera tra 1 ora", LeaderboardService.ResetText(new System.TimeSpan(1, 20, 0)));
            Assert.AreEqual("Si azzera tra 1 giorno", LeaderboardService.ResetText(new System.TimeSpan(1, 0, 30, 0)));
            Assert.AreEqual("Si azzera tra 2 giorni e 4 ore", LeaderboardService.ResetText(new System.TimeSpan(2, 4, 10, 0)));

            Assert.AreEqual("Gioca una partita per entrare in classifica", LeaderboardService.Note(0, 0, 500));
            Assert.AreEqual("Sei sul podio!", LeaderboardService.Note(3, 900, 500));
            Assert.AreEqual("Sei nella top 10!", LeaderboardService.Note(10, 500, 500));
            Assert.AreEqual("Continua a giocare per salire", LeaderboardService.Note(14, 300, -1));
            Assert.AreEqual("Ti mancano 1.001 XP per la top 10", LeaderboardService.Note(14, 1000, 2000), "uno in piu' del decimo, migliaia col punto");
        }
    }
}
#endif
