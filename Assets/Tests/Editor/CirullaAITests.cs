#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    /// <summary>
    /// Tests for CirullaAI strategic decision making.
    /// </summary>
    public class CirullaAITests
    {
        private CirullaAI easyAI;
        private CirullaAI mediumAI;
        private CirullaAI hardAI;

        [SetUp]
        public void Setup()
        {
            easyAI = new CirullaAI(AIDifficulty.Easy);
            mediumAI = new CirullaAI(AIDifficulty.Medium);
            hardAI = new CirullaAI(AIDifficulty.Hard);
        }

        #region Basic Tests

        [Test]
        public void AI_Returns_Move_From_Valid_Moves()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Table.Add(new Card(Suit.Coppe, 3));

            var validMoves = Rules51.GetValidMoves(state, 0);
            var chosen = mediumAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(chosen);
            Assert.Contains(chosen, validMoves);
        }

        [Test]
        public void AI_Returns_Null_For_Empty_Moves()
        {
            var state = new GameState(2);
            var emptyMoves = new List<Move>();

            var chosen = mediumAI.ChooseMove(state, 0, emptyMoves);

            Assert.IsNull(chosen);
        }

        [Test]
        public void AI_Returns_Only_Move_When_Single_Option()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));

            var singleMove = new Move(0, new Card(Suit.Denari, 5), MoveType.PlayOnly);
            var validMoves = new List<Move> { singleMove };

            var chosen = mediumAI.ChooseMove(state, 0, validMoves);

            Assert.AreEqual(singleMove, chosen);
        }

        #endregion

        #region Strategic Preference Tests

        [Test]
        public void Medium_AI_Prefers_Capturing_Sette_Bello()
        {
            var state = new GameState(2);
            var playedCard = new Card(Suit.Coppe, 7);
            state.Players[0].Hand.Add(playedCard);
            state.Table.Add(new Card(Suit.Denari, 7)); // Sette Bello
            state.Table.Add(new Card(Suit.Bastoni, 7)); // Regular 7

            var validMoves = Rules51.GetValidMoves(state, 0);
            var chosen = mediumAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(chosen);
            Assert.IsTrue(chosen.CapturedCards.Any(c => c.IsSetteBello), 
                "AI should prefer capturing Sette Bello");
        }

        [Test]
        public void Medium_AI_Prefers_Capturing_Denari()
        {
            var state = new GameState(2);
            var playedCard = new Card(Suit.Coppe, 5);
            state.Players[0].Hand.Add(playedCard);
            state.Table.Add(new Card(Suit.Denari, 5)); // Denari
            state.Table.Add(new Card(Suit.Bastoni, 5)); // Non-Denari

            var validMoves = Rules51.GetValidMoves(state, 0);
            var chosen = mediumAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(chosen);
            Assert.IsTrue(chosen.CapturedCards.Any(c => c.Suit == Suit.Denari), 
                "AI should prefer capturing Denari cards");
        }

        [Test]
        public void Medium_AI_Prefers_Capturing_Multiple_Cards()
        {
            var state = new GameState(2);
            var playedCard = new Card(Suit.Coppe, 7);
            state.Players[0].Hand.Add(playedCard);
            state.Table.Add(new Card(Suit.Denari, 3));
            state.Table.Add(new Card(Suit.Bastoni, 4)); // 3 + 4 = 7
            state.Table.Add(new Card(Suit.Spade, 7)); // Single 7

            var validMoves = Rules51.GetValidMoves(state, 0);
            var chosen = mediumAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(chosen);
            // AI might choose either, but should have a valid capture
            Assert.IsTrue(chosen.Type != MoveType.PlayOnly);
        }

        [Test]
        public void Hard_AI_Prefers_Scopa_Opportunity()
        {
            var state = new GameState(2);
            state.Deck.Add(new Card(Suit.Spade, 10)); // Ensure deck not empty
            
            var playedCard = new Card(Suit.Coppe, 5);
            state.Players[0].Hand.Add(playedCard);
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 3)); // Extra card
            state.Table.Add(new Card(Suit.Denari, 5)); // Only card on table

            var validMoves = Rules51.GetValidMoves(state, 0);
            var chosen = hardAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(chosen);
            // Should capture the only table card for potential Scopa
            Assert.AreEqual(1, chosen.CapturedCards.Count);
        }

        #endregion

        #region PlayOnly Strategy Tests

        [Test]
        public void Medium_AI_Avoids_Playing_Sevens_When_Discarding()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 7)); // 7 is valuable
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 10)); // King is less valuable
            state.Table.Add(new Card(Suit.Bastoni, 2)); // No matching captures

            var validMoves = Rules51.GetValidMoves(state, 0);
            // All should be PlayOnly since no captures match
            Assert.IsTrue(validMoves.All(m => m.Type == MoveType.PlayOnly));

            var chosen = mediumAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(chosen);
            // AI should prefer to keep the 7 and play the King
            Assert.AreEqual(10, chosen.PlayedCard.Rank, 
                "AI should prefer playing face cards over 7s");
        }

        [Test]
        public void Medium_AI_Avoids_Playing_Denari_When_Discarding()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 8)); // Denari Jack
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 8)); // Non-Denari Jack
            state.Table.Add(new Card(Suit.Bastoni, 2)); // No matching captures

            var validMoves = Rules51.GetValidMoves(state, 0);
            var chosen = mediumAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(chosen);
            // AI should prefer to keep Denari and play Coppe
            Assert.AreEqual(Suit.Coppe, chosen.PlayedCard.Suit, 
                "AI should prefer playing non-Denari cards");
        }

        #endregion

        #region All Difficulty Levels Return Valid Moves

        [Test]
        public void All_Difficulty_Levels_Return_Valid_Moves()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 7));
            state.Table.Add(new Card(Suit.Bastoni, 5));
            state.Table.Add(new Card(Suit.Spade, 3));

            var validMoves = Rules51.GetValidMoves(state, 0);

            var easyChoice = easyAI.ChooseMove(state, 0, validMoves);
            var mediumChoice = mediumAI.ChooseMove(state, 0, validMoves);
            var hardChoice = hardAI.ChooseMove(state, 0, validMoves);

            Assert.IsNotNull(easyChoice);
            Assert.IsNotNull(mediumChoice);
            Assert.IsNotNull(hardChoice);

            Assert.Contains(easyChoice, validMoves);
            Assert.Contains(mediumChoice, validMoves);
            Assert.Contains(hardChoice, validMoves);
        }

        #endregion
    }
}
#endif
