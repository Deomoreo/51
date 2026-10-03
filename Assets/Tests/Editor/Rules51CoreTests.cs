using NUnit.Framework;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Tests
{
    /// <summary>
    /// Comprehensive tests for Rules51 core game logic.
    /// </summary>
    public class Rules51CoreTests
    {
        #region Game Creation Tests

        [Test]
        public void CreateNewGame_Creates_4_Players()
        {
            var state = Rules51.CreateNewGame();
            Assert.AreEqual(4, state.NumPlayers);
            Assert.AreEqual(4, state.Players.Count);
        }

        [Test]
        public void CreateNewGame_Deals_3_Cards_Per_Player()
        {
            var state = Rules51.CreateNewGame(4);
            foreach (var player in state.Players)
            {
                Assert.AreEqual(3, player.Hand.Count, "Each player should have 3 cards");
            }
        }

        [Test]
        public void CreateNewGame_Deals_4_Cards_To_Table()
        {
            var state = Rules51.CreateNewGame(4);
            Assert.AreEqual(4, state.Table.Count, "Table should have 4 cards");
        }

        [Test]
        public void CreateNewGame_Never_Has_Two_Aces_On_Table()
        {
            // Run multiple times to ensure redeal logic works
            for (int i = 0; i < 50; i++)
            {
                var state = Rules51.CreateNewGame(4);
                int aceCount = state.Table.Count(c => c.IsAce);
                Assert.Less(aceCount, 2, "Table should never have 2 or more aces");
            }
        }

        [Test]
        public void CreateNewGame_Deck_Has_Remaining_Cards()
        {
            var state = Rules51.CreateNewGame(4);
            // 40 total - 12 (3 per player) - 4 (table) = 24
            Assert.AreEqual(24, state.Deck.Count);
        }

        #endregion

        #region Valid Moves Tests

        [Test]
        public void GetValidMoves_Returns_PlayOnly_When_No_Captures()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Table.Add(new Card(Suit.Coppe, 3));
            state.Table.Add(new Card(Suit.Bastoni, 4));

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(MoveType.PlayOnly, moves[0].Type);
        }

        [Test]
        public void GetValidMoves_Returns_CaptureEqual_For_Matching_Cards()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Table.Add(new Card(Suit.Coppe, 5)); // Same value

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.CaptureEqual));
        }

        [Test]
        public void GetValidMoves_Returns_CaptureSum_For_Multiple_Cards()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 7));
            state.Table.Add(new Card(Suit.Coppe, 3));
            state.Table.Add(new Card(Suit.Bastoni, 4)); // 3 + 4 = 7

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.CaptureSum && m.CapturedCards.Count == 2));
        }

        [Test]
        public void GetValidMoves_Returns_Capture15_When_Sum_Equals_15()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Table.Add(new Card(Suit.Coppe, 10)); // 5 + 10 = 15

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.Capture15));
        }

        [Test]
        public void GetValidMoves_Forced_Capture_Rule()
        {
            // When a capture is possible, PlayOnly should NOT be an option
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 5));
            state.Table.Add(new Card(Suit.Coppe, 5)); // Can capture

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsFalse(moves.Any(m => m.Type == MoveType.PlayOnly), "PlayOnly should not be available when capture exists");
        }

        #endregion

        #region Ace Capture Tests

        [Test]
        public void Ace_Captures_Single_Ace_From_Table()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 1)); // Ace
            state.Table.Add(new Card(Suit.Coppe, 1)); // Another ace
            state.Table.Add(new Card(Suit.Bastoni, 5));

            var moves = Rules51.GetValidMoves(state, 0);
            var aceCapture = moves.FirstOrDefault(m => m.Type == MoveType.AceCapture);
            
            Assert.IsNotNull(aceCapture);
            Assert.AreEqual(1, aceCapture.CapturedCards.Count);
            Assert.IsTrue(aceCapture.CapturedCards[0].IsAce);
        }

        [Test]
        public void Ace_Captures_All_Cards_When_No_Ace_On_Table()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 1)); // Ace
            state.Table.Add(new Card(Suit.Coppe, 3));
            state.Table.Add(new Card(Suit.Bastoni, 5));
            state.Table.Add(new Card(Suit.Spade, 7));

            var moves = Rules51.GetValidMoves(state, 0);
            var aceCapture = moves.FirstOrDefault(m => m.Type == MoveType.AceCapture);
            
            Assert.IsNotNull(aceCapture);
            Assert.AreEqual(3, aceCapture.CapturedCards.Count, "Ace should capture all table cards when no ace is present");
        }

        [Test]
        public void Ace_On_Empty_Table_Is_PlayOnly()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Denari, 1));
            // Table is empty

            var moves = Rules51.GetValidMoves(state, 0);
            Assert.IsNotEmpty(moves);
            Assert.IsTrue(moves.All(m => m.Type == MoveType.PlayOnly));
        }

        #endregion

        #region Matta (7 of Coppe) Tests

        [Test]
        public void Matta_Plays_As_Normal_Seven_Not_Wildcard()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 7)); // Matta
            state.Table.Add(new Card(Suit.Denari, 3));
            state.Table.Add(new Card(Suit.Bastoni, 8));

            var moves = Rules51.GetValidMoves(state, 0);

            // Outside of an accuso context, the Matta plays as a plain 7: it must NOT
            // capture unrelated table cards (3 and 8) as a wildcard would.
            Assert.IsFalse(moves.Any(m => m.Type == MoveType.CaptureEqual),
                "Matta should not act as a wildcard capturing any single card during play");

            // As a normal 7, it can still capture via sum-to-15 (7 + 8 = 15).
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.Capture15 && m.CapturedCards.Count == 1 && m.CapturedCards[0].Value == 8));
        }

        [Test]
        public void Matta_Does_Not_Act_As_Ace_For_Capture()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Add(new Card(Suit.Coppe, 7)); // Matta
            state.Table.Add(new Card(Suit.Denari, 1)); // Ace on table

            var moves = Rules51.GetValidMoves(state, 0);

            // Outside of an accuso context, the Matta is a plain 7: it cannot capture
            // the Ace (7 != 1, and no subset sums to 15-7=8 with only a lone Ace).
            Assert.IsFalse(moves.Any(m => m.Type == MoveType.AceCapture));
            Assert.IsTrue(moves.Any(m => m.Type == MoveType.PlayOnly && m.PlayedCard.IsMatta));
        }

        [Test]
        public void Matta_Is_Not_Wild_For_Normal_Captures()
        {
            var state = new GameState(2);
            state.Players[0].Hand.Clear();
            state.Players[1].Hand.Clear();
            state.Table.Clear();

            var matta = new Card(Suit.Coppe, 7);
            state.Table.Add(matta);

            state.Players[0].Hand.Add(new Card(Suit.Spade, 3));

            var moves = Rules51.GetValidMoves(state, 0);
            var movesFor3 = moves.Where(m => m.PlayedCard.Value == 3).ToList();
            Assert.IsTrue(movesFor3.All(m => m.Type == MoveType.PlayOnly));

            state.Players[0].Hand.Clear();
            state.Players[0].Hand.Add(new Card(Suit.Denari, 7));
            moves = Rules51.GetValidMoves(state, 0);
            var movesFor7 = moves.Where(m => m.PlayedCard.Value == 7).ToList();
            Assert.IsTrue(movesFor7.Any(m => m.Type == MoveType.CaptureEqual && m.CapturedCards.Any(c => c.IsMatta)));
        }

        #endregion

        #region ApplyMove Tests

        [Test]
        public void ApplyMove_Removes_Card_From_Hand()
        {
            var state = new GameState(2);
            var card = new Card(Suit.Denari, 5);
            state.Players[0].Hand.Add(card);
            state.Table.Add(new Card(Suit.Coppe, 3));

            var move = new Move(0, card, MoveType.PlayOnly);
            Rules51.ApplyMove(state, move);

            Assert.AreEqual(0, state.Players[0].Hand.Count);
        }

        [Test]
        public void ApplyMove_PlayOnly_Adds_Card_To_Table()
        {
            var state = new GameState(2);
            var card = new Card(Suit.Denari, 5);
            state.Players[0].Hand.Add(card);

            var move = new Move(0, card, MoveType.PlayOnly);
            Rules51.ApplyMove(state, move);

            Assert.Contains(card, state.Table);
        }

        [Test]
        public void ApplyMove_Capture_Adds_To_CapturedCards()
        {
            var state = new GameState(2);
            var playedCard = new Card(Suit.Denari, 5);
            var tableCard = new Card(Suit.Coppe, 5);
            state.Players[0].Hand.Add(playedCard);
            state.Table.Add(tableCard);

            var move = new Move(0, playedCard, MoveType.CaptureEqual, new List<Card> { tableCard });
            Rules51.ApplyMove(state, move);

            Assert.AreEqual(2, state.Players[0].CapturedCards.Count);
            Assert.Contains(playedCard, state.Players[0].CapturedCards);
            Assert.Contains(tableCard, state.Players[0].CapturedCards);
        }

        [Test]
        public void ApplyMove_Capture_Removes_Cards_From_Table()
        {
            var state = new GameState(2);
            var playedCard = new Card(Suit.Denari, 5);
            var tableCard = new Card(Suit.Coppe, 5);
            state.Players[0].Hand.Add(playedCard);
            state.Table.Add(tableCard);

            var move = new Move(0, playedCard, MoveType.CaptureEqual, new List<Card> { tableCard });
            Rules51.ApplyMove(state, move);

            Assert.AreEqual(0, state.Table.Count);
        }

        [Test]
        public void ApplyMove_Scopa_Increments_ScopaCount()
        {
            var state = new GameState(2);
            state.Deck.Add(new Card(Suit.Spade, 2)); // Ensure deck not empty
            var playedCard = new Card(Suit.Denari, 5);
            var tableCard = new Card(Suit.Coppe, 5);
            state.Players[0].Hand.Add(playedCard);
            state.Players[0].Hand.Add(new Card(Suit.Bastoni, 3)); // Extra card so not last play
            state.Table.Add(tableCard);

            var move = new Move(0, playedCard, MoveType.CaptureEqual, new List<Card> { tableCard });
            Rules51.ApplyMove(state, move);

            Assert.AreEqual(1, state.Players[0].ScopaCount);
        }

        [Test]
        public void ApplyMove_Last_Play_No_Scopa()
        {
            // Scopa should NOT count on the very last play of the round
            var state = new GameState(2);
            // Deck is empty
            var playedCard = new Card(Suit.Denari, 5);
            var tableCard = new Card(Suit.Coppe, 5);
            state.Players[0].Hand.Add(playedCard); // Only card in hand
            state.Table.Add(tableCard);

            var move = new Move(0, playedCard, MoveType.CaptureEqual, new List<Card> { tableCard });
            Rules51.ApplyMove(state, move);

            Assert.AreEqual(0, state.Players[0].ScopaCount, "Last capture should not count as Scopa");
        }

        [Test]
        public void ApplyMove_Advances_To_Next_Player()
        {
            var state = new GameState(4);
            state.CurrentPlayerIndex = 0;
            var card = new Card(Suit.Denari, 5);
            state.Players[0].Hand.Add(card);

            var move = new Move(0, card, MoveType.PlayOnly);
            Rules51.ApplyMove(state, move);

            // Clockwise = -1 with modulo (3 after 0)
            Assert.AreEqual(3, state.CurrentPlayerIndex);
        }

        #endregion

        #region Tutorial

        /// <summary>
        /// La partita guidata (UI51TutorialView) racconta queste carte: stessa sequenza di TurnController.StartNewGame
        /// (CreateNewGame(2), poi DealInitialCards in StartSmazzata). Se cambia la distribuzione va cercato un seme nuovo.
        /// </summary>
        [Test]
        public void TutorialSeed_DealsTheCardsTheTutorialTalksAbout()
        {
            GameState state;
            try
            {
                Rules51.Reseed(Project51.Unity.GameSceneInitializer.TutorialSeed);
                state = Rules51.CreateNewGame(2);
                Rules51.DealInitialCards(state);
            }
            finally { Rules51.Reseed(System.Environment.TickCount); }

            Assert.AreEqual(1, state.DealerIndex, "mazziere il bot");
            Assert.AreEqual(0, state.CurrentPlayerIndex, "di mano tu");
            CollectionAssert.AreEqual(new[] { new Card(Suit.Denari, 1), new Card(Suit.Bastoni, 7), new Card(Suit.Bastoni, 2) }, state.Players[0].Hand);
            CollectionAssert.AreEquivalent(new[] { new Card(Suit.Bastoni, 1), new Card(Suit.Coppe, 10), new Card(Suit.Spade, 4), new Card(Suit.Coppe, 3) }, state.Table);
            Assert.IsNull(RoundManager.DealerAccusoFor(state.Table), "niente 15/30 del mazziere");
            foreach (var p in state.Players)
                Assert.IsFalse(AccusiChecker.IsCirulla(p.Hand) || AccusiChecker.IsDecino(p.Hand), "niente accusi");
        }

        #endregion
    }
}
