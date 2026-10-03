using NUnit.Framework;
using Project51.Unity;

namespace Project51.Tests
{
    // Posto visivo 0=locale, 1=sinistra, 2=alto, 3=destra: un solo calcolo per banner, mani, emoticon e mazziere.
    public class SeatOfTests
    {
        [Test]
        public void Duel_OpponentSitsOnTop()
        {
            Assert.AreEqual(0, CardViewManager.SeatOf(1, 1, 2));
            Assert.AreEqual(2, CardViewManager.SeatOf(0, 1, 2));
            Assert.AreEqual(2, CardViewManager.SeatOf(1, 0, 2));
        }

        [Test]
        public void FourPlayers_RotateAroundLocal_WithNegativeWrap()
        {
            Assert.AreEqual(new[] { 0, 1, 2, 3 }, new[] { CardViewManager.SeatOf(2, 2, 4), CardViewManager.SeatOf(3, 2, 4), CardViewManager.SeatOf(0, 2, 4), CardViewManager.SeatOf(1, 2, 4) });
            Assert.AreEqual(3, CardViewManager.SeatOf(0, 1, 4));
            Assert.AreEqual(0, CardViewManager.SeatOf(0, 0, 0));
        }
    }
}
