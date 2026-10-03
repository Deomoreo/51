using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    public class PlayerXpTests
    {
        [Test]
        public void Curve_Is100Plus20PerLevel()
        {
            Assert.AreEqual(100, PlayerXp.XpToNext(1));
            Assert.AreEqual(120, PlayerXp.XpToNext(2));
            Assert.AreEqual(0, PlayerXp.TotalForLevel(1));
            Assert.AreEqual(100, PlayerXp.TotalForLevel(2));
            Assert.AreEqual(220, PlayerXp.TotalForLevel(3));
            for (int l = 1; l < 50; l++)
                Assert.AreEqual(PlayerXp.XpToNext(l), PlayerXp.TotalForLevel(l + 1) - PlayerXp.TotalForLevel(l));
        }

        [Test]
        public void LevelOf_AndXpInLevel()
        {
            Assert.AreEqual(1, PlayerXp.LevelOf(0));
            Assert.AreEqual(1, PlayerXp.LevelOf(99));
            Assert.AreEqual(2, PlayerXp.LevelOf(100));
            Assert.AreEqual(3, PlayerXp.LevelOf(230));
            Assert.AreEqual(10, PlayerXp.XpInLevel(230));
            Assert.AreEqual(PlayerXp.MaxLevel, PlayerXp.LevelOf(int.MaxValue / 2));
        }

        [Test]
        public void MatchAward_BaseBonusCapAndTraining()
        {
            Assert.AreEqual(40, PlayerXp.MatchAward(true, 0, 0, false));
            Assert.AreEqual(20, PlayerXp.MatchAward(false, 0, 0, false));
            Assert.AreEqual(49, PlayerXp.MatchAward(true, 2, 1, false));
            Assert.AreEqual(60, PlayerXp.MatchAward(true, 10, 3, false));
            Assert.AreEqual(20, PlayerXp.MatchAward(true, 0, 0, true));
            Assert.AreEqual(10, PlayerXp.MatchAward(false, 0, 0, true));
        }
    }
}
