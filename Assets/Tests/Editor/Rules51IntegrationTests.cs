#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Core;
using System.Linq;

namespace Project51.Tests
{
    public class Rules51IntegrationTests
    {
        [Test]
        public void EndToEnd_Scores_Computed_Match_PunteggioManager_Plus_Accusi()
        {
            var state = new GameState(2);
            // Setup captured cards for player 0 and 1
            // Player 0: Grande (10,9,8 of Denari), Sette Bello, Primiera strong
            state.Players[0].CapturedCards.AddRange(new[] {
                new Card(Suit.Denari, 10), new Card(Suit.Denari, 9), new Card(Suit.Denari, 8), new Card(Suit.Denari, 7),
                new Card(Suit.Coppe, 6), new Card(Suit.Bastoni, 1), new Card(Suit.Spade, 5)
            });

            // Player 1: weaker set
            state.Players[1].CapturedCards.AddRange(new[] {
                new Card(Suit.Denari, 6), new Card(Suit.Coppe, 5), new Card(Suit.Bastoni, 4), new Card(Suit.Spade, 3)
            });

            // Scopa counts
            state.Players[0].ScopaCount = 2;
            state.Players[1].ScopaCount = 0;

            // Accusi points (player has declared before)
            state.Players[0].AccusiPoints = 3; // e.g., cirulla
            state.Players[1].AccusiPoints = 0;

            var expectedPoints = PunteggioManager.CalculateSmazzataScores(state);

            var rm = new RoundManager(state);
            rm.EndSmazzata();

            // After EndSmazzata RoundManager should add accusi and update TotalScore
            Assert.AreEqual(expectedPoints[0] + 3, state.Players[0].TotalScore);
            Assert.AreEqual(expectedPoints[1] + 0, state.Players[1].TotalScore);
        }

        [Test]
        public void Player_Accuso_Cirulla_And_Decino_Workflow()
        {
            var state = new GameState(2);
            var rm = new RoundManager(state);

            // Player 0: Cirulla using matta as Ace
            state.Players[0].Hand.AddRange(new[] { new Card(Suit.Coppe, 7), new Card(Suit.Denari, 1), new Card(Suit.Bastoni, 1) });
            bool cirulla = rm.TryPlayerAccuso(0, AccusoType.Cirulla);
            Assert.IsTrue(cirulla);
            Assert.AreEqual(3, state.Players[0].AccusiPoints);

            // Player 1: Decino with matta completing trio
            state.Players[1].Hand.AddRange(new[] { new Card(Suit.Coppe, 7), new Card(Suit.Denari, 5), new Card(Suit.Spade, 5) });
            bool decino = rm.TryPlayerAccuso(1, AccusoType.Decino);
            Assert.IsTrue(decino);
            Assert.AreEqual(10, state.Players[1].AccusiPoints);
        }

        [Test]
        public void FullSmazzata_Simulated_Play_Sequence_LastCaptureGets_RemainingTable()
        {
            var state = new GameState(2);
            // Setup dealer and current player (player to left of dealer)
            state.DealerIndex = 0;
            state.CurrentPlayerIndex = (state.DealerIndex - 1 + state.NumPlayers) % state.NumPlayers; // player of hand

            // Table has 4 and 5
            state.Table.Add(new Card(Suit.Coppe, 4));
            state.Table.Add(new Card(Suit.Bastoni, 5));

            // Player 1 (current) will play a 3 as PlayOnly
            state.Players[1].Hand.Add(new Card(Suit.Spade, 3));

            // Player 0 will play a 6 capturing 4+5
            state.Players[0].Hand.Add(new Card(Suit.Denari, 6));

            // First move: player 1 plays 3 (PlayOnly)
            var move1 = new Move(1, state.Players[1].Hand[0], MoveType.PlayOnly);
            Rules51.ApplyMove(state, move1);

            // Verify table now contains 4,5,3
            Assert.AreEqual(3, state.Table.Count);

            // Second move: player 0 captures 4+5 with 6 using Capture15
            var captured = state.Table.Where(c => c.Value == 4 || c.Value == 5).ToList();
            var move2 = new Move(0, state.Players[0].Hand[0], MoveType.Capture15, captured);
            Rules51.ApplyMove(state, move2);

            // After these plays, both hands empty and deck empty -> end of smazzata
            var rm = new RoundManager(state);
            rm.EndSmazzata();

            // Last capture was by player 0, so remaining table card (3) should be assigned to player 0
            Assert.IsTrue(state.Players[0].CapturedCards.Any(c => c.Value == 3));
            // And table should be empty
            Assert.AreEqual(0, state.Table.Count);
        }
    }
}
#endif
