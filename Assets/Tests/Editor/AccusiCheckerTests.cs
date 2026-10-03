using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Project51.Core;

namespace Project51.Tests
{
    /// <summary>
    /// Every Cirulla / Decino hand once spread over five test files, as one table.
    /// Hand notation: suit letter (D Denari, C Coppe, B Bastoni, S Spade) + rank; C7 is the Matta.
    /// Cirulla: sum &lt;= 9 with the Matta counted as 1. Decino: a tris, the Matta completing a pair.
    /// </summary>
    public class AccusiCheckerTests
    {
        static List<Card> Hand(string cards) => cards?.Split(' ').Select(c => new Card(
            c[0] == 'D' ? Suit.Denari : c[0] == 'C' ? Suit.Coppe : c[0] == 'B' ? Suit.Bastoni : Suit.Spade,
            int.Parse(c.Substring(1)))).ToList();

        [TestCase("C7 D1 B1", true)]
        [TestCase("C7 D1 B2", true)]
        [TestCase("C7 D1 S3", true)]
        [TestCase("C7 D1 B4", true)]
        [TestCase("C7 D1 S5", true)]
        [TestCase("C7 D1 B6", true)]
        [TestCase("C7 D2 S2", true)]
        [TestCase("C7 D2 B2", true)]
        [TestCase("C7 D2 B3", true)]
        [TestCase("D2 C3 C7", true)]
        [TestCase("C7 D2 S4", true)]
        [TestCase("C7 D2 B5", true)]
        [TestCase("C7 D2 S6", true)]
        [TestCase("C7 D3 B3", true)]
        [TestCase("C7 D3 S4", true)]
        [TestCase("C7 D3 B5", true)]
        [TestCase("C7 D4 S4", true)]
        [TestCase("C7 D4 B4", true)]
        [TestCase("D3 C3 B3", true)]
        [TestCase("D1 C1 B1", true)]
        [TestCase("C7 D5 B5", false)]
        [TestCase("C7 D2 B7", false)]
        [TestCase("C7 D6 S6", false)]
        [TestCase("C7 D8 B10", false)]
        [TestCase("D6 C4 C7", false)]
        [TestCase("D4 C4 B4", false)]
        [TestCase("D4 C5 S6", false)]
        [TestCase("D8 C1 B1", false)]
        [TestCase("D1 C1", false)]
        [TestCase(null, false)]
        public void IsCirulla(string hand, bool expected)
        {
            Assert.AreEqual(expected, AccusiChecker.IsCirulla(Hand(hand)));
        }

        [TestCase("C7 D1 B1", true)]
        [TestCase("C7 D2 B2", true)]
        [TestCase("C7 D3 S3", true)]
        [TestCase("C7 D4 B4", true)]
        [TestCase("C7 D5 S5", true)]
        [TestCase("D5 C5 C7", true)]
        [TestCase("C7 D6 B6", true)]
        [TestCase("C7 D7 B7", true)]
        [TestCase("C7 D8 S8", true)]
        [TestCase("C7 D9 B9", true)]
        [TestCase("C7 D10 S10", true)]
        [TestCase("C5 D5 B5", true)]
        [TestCase("D1 C1 B1", true)]
        [TestCase("D10 C10 B10", true)]
        [TestCase("C10 D10 S10", true)]
        [TestCase("C7 D4 B5", false)]
        [TestCase("D4 C5 C7", false)]
        [TestCase("C7 D5 B6", false)]
        [TestCase("C7 D2 B10", false)]
        [TestCase("D5 C6 B7", false)]
        [TestCase("D4 C5 S6", false)]
        [TestCase("D5 C5", false)]
        [TestCase(null, false)]
        public void IsDecino(string hand, bool expected)
        {
            Assert.AreEqual(expected, AccusiChecker.IsDecino(Hand(hand)));
        }
    }
}
