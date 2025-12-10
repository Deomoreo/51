#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    /// <summary>
    /// Comprehensive tests for AccusiChecker - Cirulla and Decino detection.
    /// </summary>
    public class AccusiCheckerComprehensiveTests
    {
        #region Cirulla Tests

        [Test]
        public void Cirulla_Sum_Exactly_9_Returns_True()
        {
            // 3 + 3 + 3 = 9
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 3),
                new Card(Suit.Coppe, 3),
                new Card(Suit.Bastoni, 3)
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Cirulla_Sum_Less_Than_9_Returns_True()
        {
            // 1 + 1 + 1 = 3
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 1),
                new Card(Suit.Coppe, 1),
                new Card(Suit.Bastoni, 1)
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Cirulla_Sum_Greater_Than_9_Returns_False()
        {
            // 4 + 4 + 4 = 12
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 4),
                new Card(Suit.Coppe, 4),
                new Card(Suit.Bastoni, 4)
            };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Cirulla_Matta_Counts_As_1()
        {
            // Matta (7 of Coppe) counts as 1 for Cirulla
            // 1 + 4 + 4 = 9 (Matta as 1)
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Denari, 4),
                new Card(Suit.Bastoni, 4)
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Cirulla_Matta_As_1_Enables_Cirulla()
        {
            // Without Matta: 7 + 1 + 1 = 9 ?
            // With Matta: 1 + 1 + 1 = 3 ?
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta counts as 1
                new Card(Suit.Denari, 1),
                new Card(Suit.Bastoni, 1)
            };
            Assert.IsTrue(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Cirulla_Null_Hand_Returns_False()
        {
            Assert.IsFalse(AccusiChecker.IsCirulla(null));
        }

        [Test]
        public void Cirulla_Wrong_Hand_Size_Returns_False()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 1),
                new Card(Suit.Coppe, 1)
            };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand));
        }

        [Test]
        public void Cirulla_With_Face_Cards_Returns_False()
        {
            // 8 + 1 + 1 = 10 > 9
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 8), // Jack = 8
                new Card(Suit.Coppe, 1),
                new Card(Suit.Bastoni, 1)
            };
            Assert.IsFalse(AccusiChecker.IsCirulla(hand));
        }

        #endregion

        #region Decino Tests

        [Test]
        public void Decino_Three_Same_Rank_Returns_True()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Coppe, 5),
                new Card(Suit.Bastoni, 5)
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Decino_Three_Aces_Returns_True()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 1),
                new Card(Suit.Coppe, 1),
                new Card(Suit.Bastoni, 1)
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Decino_Three_Kings_Returns_True()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 10),
                new Card(Suit.Coppe, 10),
                new Card(Suit.Bastoni, 10)
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Decino_Pair_Plus_Matta_Returns_True()
        {
            // Matta completes a pair to make a trio
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Denari, 6),
                new Card(Suit.Bastoni, 6)
            };
            Assert.IsTrue(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Decino_Different_Ranks_Returns_False()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Coppe, 6),
                new Card(Suit.Bastoni, 7)
            };
            Assert.IsFalse(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Decino_Null_Hand_Returns_False()
        {
            Assert.IsFalse(AccusiChecker.IsDecino(null));
        }

        [Test]
        public void Decino_Wrong_Hand_Size_Returns_False()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Coppe, 5)
            };
            Assert.IsFalse(AccusiChecker.IsDecino(hand));
        }

        [Test]
        public void Decino_Matta_With_Non_Pair_Returns_False()
        {
            // Matta can't make a trio from different ranks
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Denari, 5),
                new Card(Suit.Bastoni, 6)
            };
            Assert.IsFalse(AccusiChecker.IsDecino(hand));
        }

        #endregion
    }

    /// <summary>
    /// Comprehensive tests for PunteggioManager - scoring calculations.
    /// </summary>
    public class PunteggioManagerComprehensiveTests
    {
        #region Scopa Tests

        [Test]
        public void Scopa_Points_Added_To_Total()
        {
            var state = new GameState(2);
            state.Players[0].ScopaCount = 3;
            state.Players[1].ScopaCount = 1;

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.GreaterOrEqual(scores[0], 3);
            Assert.GreaterOrEqual(scores[1], 1);
        }

        #endregion

        #region Sette Bello Tests

        [Test]
        public void SetteBello_Awards_1_Point()
        {
            var state = new GameState(2);
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 7)); // Sette Bello

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.GreaterOrEqual(scores[0], 1);
        }

        [Test]
        public void SetteBello_Only_7_Of_Denari_Counts()
        {
            var state = new GameState(2);
            state.Players[0].CapturedCards.Add(new Card(Suit.Coppe, 7)); // NOT Sette Bello

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            // Should not get SetteBello point
            Assert.AreEqual(0, scores[0]);
        }

        #endregion

        #region Denari Majority Tests

        [Test]
        public void Denari_Majority_Awards_1_Point()
        {
            var state = new GameState(2);
            // Player 0 has 7 denari
            for (int i = 1; i <= 7; i++)
                state.Players[0].CapturedCards.Add(new Card(Suit.Denari, i));
            // Player 1 has 3 denari
            for (int i = 8; i <= 10; i++)
                state.Players[1].CapturedCards.Add(new Card(Suit.Denari, i));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.Greater(scores[0], scores[1]);
        }

        [Test]
        public void Denari_Tie_Awards_No_Points()
        {
            var state = new GameState(2);
            // Both have 5 denari each
            for (int i = 1; i <= 5; i++)
                state.Players[0].CapturedCards.Add(new Card(Suit.Denari, i));
            for (int i = 6; i <= 10; i++)
                state.Players[1].CapturedCards.Add(new Card(Suit.Denari, i));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            // Neither should get denari majority point (tie at 5)
            // BUT other bonuses still apply:
            // Player 0: Piccola (1,2,3 = 3pts) + extras (4,5 = +2pts) = 5 total
            // Player 1: Sette Bello (7 of Denari = 1pt) + Grande (8,9,10 = 5pts) = 6 total
            Assert.AreEqual(5, scores[0]); // Piccola with extras
            Assert.AreEqual(6, scores[1]); // Grande + Sette Bello
        }

        [Test]
        public void Denari_Less_Than_6_No_Point()
        {
            var state = new GameState(2);
            // Player 0 has 5 denari (majority but < 6)
            for (int i = 1; i <= 5; i++)
                state.Players[0].CapturedCards.Add(new Card(Suit.Denari, i));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            // No denari majority point because < 6
            // BUT still gets Piccola bonus: 1,2,3 = 3pts + 4,5 = +2pts = 5 total
            Assert.AreEqual(5, scores[0]); // Piccola with extras
        }

        #endregion

        #region Carte Majority Tests

        [Test]
        public void Carte_Majority_Awards_1_Point()
        {
            var state = new GameState(2);
            // Player 0 has 25 cards
            for (int i = 0; i < 25; i++)
                state.Players[0].CapturedCards.Add(new Card(Suit.Coppe, (i % 10) + 1));
            // Player 1 has 15 cards
            for (int i = 0; i < 15; i++)
                state.Players[1].CapturedCards.Add(new Card(Suit.Bastoni, (i % 10) + 1));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.Greater(scores[0], scores[1]);
        }

        [Test]
        public void Carte_Less_Than_21_No_Point()
        {
            var state = new GameState(2);
            // Player 0 has 20 cards
            for (int i = 0; i < 20; i++)
                state.Players[0].CapturedCards.Add(new Card(Suit.Coppe, (i % 10) + 1));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            // No carte point because < 21
            Assert.AreEqual(0, scores[0]);
        }

        #endregion

        #region Primiera Tests

        [Test]
        public void Primiera_Winner_Gets_1_Point()
        {
            var state = new GameState(2);
            // Player 0 has all 7s (best primiera cards)
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 7));
            state.Players[0].CapturedCards.Add(new Card(Suit.Coppe, 7));
            state.Players[0].CapturedCards.Add(new Card(Suit.Bastoni, 7));
            state.Players[0].CapturedCards.Add(new Card(Suit.Spade, 7));

            // Player 1 has lower cards
            state.Players[1].CapturedCards.Add(new Card(Suit.Denari, 2));
            state.Players[1].CapturedCards.Add(new Card(Suit.Coppe, 2));
            state.Players[1].CapturedCards.Add(new Card(Suit.Bastoni, 2));
            state.Players[1].CapturedCards.Add(new Card(Suit.Spade, 2));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.Greater(scores[0], scores[1]);
        }

        #endregion

        #region Grande Tests

        [Test]
        public void Grande_Awards_5_Points()
        {
            var state = new GameState(2);
            // Grande = Re (10), Cavallo (9), Fante (8) of Denari
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 10));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 9));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 8));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.GreaterOrEqual(scores[0], 5);
        }

        [Test]
        public void Grande_Missing_One_Card_No_Points()
        {
            var state = new GameState(2);
            // Missing Fante (8)
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 10));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 9));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.AreEqual(0, scores[0]);
        }

        #endregion

        #region Piccola Tests

        [Test]
        public void Piccola_Awards_3_Points()
        {
            var state = new GameState(2);
            // Piccola = Asso (1), 2, 3 of Denari
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 1));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 2));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 3));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.GreaterOrEqual(scores[0], 3);
        }

        [Test]
        public void Piccola_With_Extras_Awards_Bonus()
        {
            var state = new GameState(2);
            // Piccola + 4, 5, 6 = 3 + 3 extras = 6 points
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 1));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 2));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 3));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 4));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 5));
            state.Players[0].CapturedCards.Add(new Card(Suit.Denari, 6));

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            Assert.GreaterOrEqual(scores[0], 6);
        }

        #endregion

        #region Combined Scoring Tests

        [Test]
        public void All_Bonuses_Combined()
        {
            var state = new GameState(2);
            
            // Player 0 gets everything
            // All denari (also gives Grande, Piccola, Denari majority, Sette Bello)
            for (int i = 1; i <= 10; i++)
                state.Players[0].CapturedCards.Add(new Card(Suit.Denari, i));
            
            // Extra cards for Carte majority and Primiera
            state.Players[0].CapturedCards.Add(new Card(Suit.Coppe, 7));
            state.Players[0].CapturedCards.Add(new Card(Suit.Bastoni, 7));
            state.Players[0].CapturedCards.Add(new Card(Suit.Spade, 7));
            
            // Add more cards to reach 21
            for (int i = 1; i <= 10; i++)
                state.Players[0].CapturedCards.Add(new Card(Suit.Coppe, i));
            
            state.Players[0].ScopaCount = 2;

            var scores = PunteggioManager.CalculateSmazzataScores(state);

            // Should have: 2 (scope) + 1 (sette bello) + 1 (denari) + 1 (carte) + 1 (primiera) + 5 (grande) + 6 (piccola with extras)
            Assert.GreaterOrEqual(scores[0], 15);
        }

        #endregion
    }
}
#endif
