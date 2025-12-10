using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project51.Core;
using Project51.Unity;

namespace Project51.PlayModeTests
{
    /// <summary>
    /// PlayMode tests for card interactions (click, drag, selection).
    /// Tests user input handling and move execution.
    /// </summary>
    public class CardInteractionPlayModeTests
    {
        private GameObject testSceneRoot;
        private TurnController turnController;
        private CardViewManager cardViewManager;
        private GameObject cardViewPrefab;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Ensure no leftover objects from previous tests
            var existingGM = Object.FindObjectOfType<GameManager>();
            if (existingGM != null)
            {
                Object.DestroyImmediate(existingGM.gameObject);
            }
            var existingCVM = Object.FindObjectOfType<CardViewManager>();
            if (existingCVM != null)
            {
                Object.DestroyImmediate(existingCVM.gameObject);
            }
            var existingTC = Object.FindObjectOfType<TurnController>();
            if (existingTC != null)
            {
                Object.DestroyImmediate(existingTC.gameObject);
            }
            
            testSceneRoot = new GameObject("TestScene");

            // Add GameManager
            var gameManagerObj = new GameObject("GameManager");
            gameManagerObj.transform.SetParent(testSceneRoot.transform);
            gameManagerObj.AddComponent<GameManager>();

            // Add CardViewManager (required by TurnController)
            var cardViewManagerObj = new GameObject("CardViewManager");
            cardViewManagerObj.transform.SetParent(testSceneRoot.transform);
            cardViewManager = cardViewManagerObj.AddComponent<CardViewManager>();

            // Add TurnController
            turnController = testSceneRoot.AddComponent<TurnController>();

            // Prevent auto-start
            var autoStartField = typeof(TurnController).GetField("autoStartGame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            autoStartField?.SetValue(turnController, false);

            // Connect CardViewManager to TurnController using public setter
            turnController.SetCardViewManager(cardViewManager);

            // Connect TurnController to CardViewManager using public setter
            cardViewManager.SetTurnController(turnController);

            // Create required containers for CardViewManager
            var tableContainer = new GameObject("TableCardContainer");
            tableContainer.transform.SetParent(cardViewManagerObj.transform);

            var handContainer = new GameObject("HumanHandContainer");
            handContainer.transform.SetParent(cardViewManagerObj.transform);

            // Assign containers to CardViewManager via reflection
            var tableContainerField = typeof(CardViewManager).GetField("tableCardContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tableContainerField?.SetValue(cardViewManager, tableContainer.transform);

            var handContainerField = typeof(CardViewManager).GetField("humanHandContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            handContainerField?.SetValue(cardViewManager, handContainer.transform);

            // Create a simple CardView prefab for testing
            cardViewPrefab = new GameObject("CardViewPrefab");
            cardViewPrefab.transform.SetParent(testSceneRoot.transform);
            cardViewPrefab.SetActive(false);
            cardViewPrefab.AddComponent<CardView>();
            cardViewPrefab.AddComponent<SpriteRenderer>();
            cardViewPrefab.AddComponent<BoxCollider2D>();

            // Assign prefab to CardViewManager using public setter
            cardViewManager.SetCardViewPrefab(cardViewPrefab);

            // Let Awake/Start run on all components
            yield return null;

            // After Start(), ensure connections are still valid using public setters
            cardViewManager.SetTurnController(turnController);
            cardViewManager.SetCardViewPrefab(cardViewPrefab);

            // Force an initial refresh
            cardViewManager.ForceRefresh();
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (testSceneRoot != null)
            {
                Object.Destroy(testSceneRoot);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DoubleClick_OnPlayOnlyCard_ExecutesMove()
        {
            // Setup: card that can only be played (no captures)
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 10),  // King - likely no captures
                new Card(Suit.Denari, 2),
                new Card(Suit.Bastoni, 3)
            };

            var table = new List<Card>();  // Empty table

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);
            
            // Get current player hand size
            int initialHandSize = turnController.GameState.Players[0].Hand.Count;
            int initialTableSize = turnController.GameState.Table.Count;
            
            // Simulate double-click on King
            var kingCard = new Card(Suit.Coppe, 10);
            turnController.OnPlayerDoubleClick(kingCard);
            
            yield return new WaitForSeconds(0.2f);
            
            // Verify card was played to table
            Assert.AreEqual(initialHandSize - 1, turnController.GameState.Players[0].Hand.Count,
                "Hand should have one less card");
            Assert.AreEqual(initialTableSize + 1, turnController.GameState.Table.Count,
                "Table should have one more card");
        }

        [UnityTest]
        public IEnumerator GetMovesForCard_ReturnValidMoves()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7),  // Matta
                new Card(Suit.Denari, 5),
                new Card(Suit.Bastoni, 6)
            };

            var table = new List<Card>
            {
                new Card(Suit.Spade, 6)  // Can capture with Bastoni 6
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);
            
            // Get moves for Bastoni 6
            var bastoni6 = new Card(Suit.Bastoni, 6);
            var moves = turnController.GetMovesForCard(bastoni6);
            
            Assert.NotNull(moves, "Moves should not be null");
            Assert.Greater(moves.Count, 0, "Should have at least one move for Bastoni 6");
            
            // Check if there's a capture move
            var captureMove = moves.FirstOrDefault(m => m.Type != MoveType.PlayOnly);
            Assert.NotNull(captureMove, "Should have a capture move for matching card");
        }

        [UnityTest]
        public IEnumerator DragPlay_WithValidTarget_ExecutesCapture()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Coppe, 6),
                new Card(Suit.Bastoni, 7)
            };

            var table = new List<Card>
            {
                new Card(Suit.Spade, 5)  // Matches Denari 5
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);
            
            int initialHandSize = turnController.GameState.Players[0].Hand.Count;
            int initialTableSize = turnController.GameState.Table.Count;
            
            // Simulate drag: play Denari 5 onto Spade 5
            var denari5 = new Card(Suit.Denari, 5);
            var spade5 = new Card(Suit.Spade, 5);
            var targetCards = new List<Card> { spade5 };
            
            turnController.OnPlayerDragPlay(denari5, targetCards);
            
            yield return new WaitForSeconds(0.3f);
            
            // Verify capture happened
            Assert.Less(turnController.GameState.Players[0].Hand.Count, initialHandSize,
                "Hand should have fewer cards after capture");
            Assert.Less(turnController.GameState.Table.Count, initialTableSize,
                "Table should have fewer cards after capture");
        }

        [UnityTest]
        public IEnumerator ExecuteMove_UpdatesGameState()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 3),
                new Card(Suit.Denari, 4),
                new Card(Suit.Bastoni, 5)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, new List<Card>());
            yield return new WaitForSeconds(0.3f);
            
            var validMoves = turnController.GetCurrentValidMoves();
            Assert.Greater(validMoves.Count, 0, "Should have valid moves");
            
            var moveToExecute = validMoves[0];
            int initialHandSize = turnController.GameState.Players[0].Hand.Count;
            
            // Execute the move
            turnController.ExecuteMove(moveToExecute);
            
            yield return new WaitForSeconds(0.2f);
            
            // Verify hand size changed
            Assert.AreNotEqual(initialHandSize, 
                turnController.GameState.Players[0].Hand.Count,
                "Hand size should change after executing move");
        }

        [UnityTest]
        public IEnumerator OnPlayerConfirmMove_WithValidIndex_ExecutesMove()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 2),
                new Card(Suit.Denari, 3),
                new Card(Suit.Bastoni, 4)
            };

            var table = new List<Card>
            {
                new Card(Suit.Spade, 2),
                new Card(Suit.Coppe, 3)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);
            
            var coppe2 = new Card(Suit.Coppe, 2);
            var moves = turnController.GetMovesForCard(coppe2);
            
            if (moves.Count > 0)
            {
                int initialHandSize = turnController.GameState.Players[0].Hand.Count;
                
                // Confirm first available move
                turnController.OnPlayerConfirmMove(coppe2, 0);
                
                yield return new WaitForSeconds(0.2f);
                
                Assert.Less(turnController.GameState.Players[0].Hand.Count, initialHandSize,
                    "Card should be removed from hand after confirming move");
            }
        }
    }
}
