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
    /// PlayMode tests for Matta (7 di Coppe) special behavior.
    /// Tests UI interactions, sprite swapping, and visual feedback.
    /// </summary>
    public class MattaBehaviorPlayModeTests
    {
        private GameObject testSceneRoot;
        private TurnController turnController;
        private CardViewManager cardViewManager;
        private GameObject cardViewPrefab; // Store as field to ensure it persists across yields

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Ensure no leftover GameManager or CardViewManager from previous tests
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
            
            // Create root GameObject for test scene
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

            // Prevent auto-start (MUST be before first frame)
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
            cardViewPrefab.transform.SetParent(testSceneRoot.transform); // Parent to test root for cleanup
            cardViewPrefab.SetActive(false); // Prefab should be inactive
            cardViewPrefab.AddComponent<CardView>();
            cardViewPrefab.AddComponent<SpriteRenderer>();
            // Add BoxCollider2D so the card can be detected
            cardViewPrefab.AddComponent<BoxCollider2D>();

            // Assign prefab to CardViewManager using public setter
            cardViewManager.SetCardViewPrefab(cardViewPrefab);

            // Let Awake/Start run on all components
            yield return null;

            // After Start(), ensure connections are still valid using public setters
            cardViewManager.SetTurnController(turnController);
            cardViewManager.SetCardViewPrefab(cardViewPrefab);

            // Force an initial refresh so the first StartNewGame has visuals ready
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
        public IEnumerator Matta_WithDecino_CreatesCardViews()
        {
            // Verify setup was successful
            Assert.NotNull(cardViewManager, "CardViewManager should exist");
            Assert.NotNull(turnController, "TurnController should exist");
            Assert.NotNull(cardViewPrefab, "CardViewPrefab should exist");
            
            // Setup: Matta + pair for Decino
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7),  // Matta
                new Card(Suit.Denari, 6),
                new Card(Suit.Bastoni, 6)  // Pair -> Decino with Matta
            };
            
            turnController.StartNewGame();
            
            // Verify game started
            Assert.NotNull(turnController.GameState, "GameState should exist after StartNewGame");
            
            turnController.SetupScenarioForCurrentPlayer(hand, new List<Card>());
            
            // Verify scenario was set up
            Assert.AreEqual(3, turnController.GameState.Players[0].Hand.Count, "Player 0 should have 3 cards");
            
            // Force refresh to ensure cards are rendered
            cardViewManager.ForceRefresh();
            
            // Wait for UI to refresh (longer than InvokeRepeating delay)
            yield return new WaitForSeconds(0.6f);
            
            // Use CardViewManager's GetActiveCardViews instead of FindObjectsOfType
            // to avoid issues with Unity's deferred object destruction
            var allCardViews = cardViewManager.GetActiveCardViews()
                .Where(cv => cv != null && cv.gameObject.activeInHierarchy && cv.Card != null)
                .ToArray();
            
            // Debug: log how many we found
            Debug.Log($"Found {allCardViews.Length} active CardViews with non-null Card");
            
            Assert.GreaterOrEqual(allCardViews.Length, 3, 
                "Should have at least 3 CardViews for the hand");
            
            // Find Matta CardView
            var mattaView = allCardViews.FirstOrDefault(cv => cv.Card.IsMatta);
            
            Assert.NotNull(mattaView, "Matta CardView should exist");
            Assert.AreEqual(Suit.Coppe, mattaView.Card.Suit, "Matta should be 7 of Coppe");
            Assert.AreEqual(7, mattaView.Card.Rank, "Matta should be rank 7");
        }

        [UnityTest]
        public IEnumerator Matta_WithCirulla_CreatesCardViews()
        {
            // Setup: Matta + cards summing ?9 for Cirulla
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7),  // Matta (counts as 1)
                new Card(Suit.Denari, 4),
                new Card(Suit.Bastoni, 4)  // 1+4+4=9 -> Cirulla
            };
            
            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, new List<Card>());
            
            // Force refresh to ensure cards are rendered
            cardViewManager.ForceRefresh();
            
            // Wait for UI to refresh (longer than InvokeRepeating delay)
            yield return new WaitForSeconds(0.6f);
            
            // Use CardViewManager's GetActiveCardViews instead of FindObjectsOfType
            var allCardViews = cardViewManager.GetActiveCardViews()
                .Where(cv => cv != null && cv.gameObject.activeInHierarchy && cv.Card != null)
                .ToArray();
            var mattaView = allCardViews.FirstOrDefault(cv => cv.Card.IsMatta);
            
            Assert.NotNull(mattaView, "Matta CardView should exist for Cirulla scenario");
        }

        [UnityTest]
        public IEnumerator Matta_WithoutAccuso_CreatesCardViews()
        {
            // Setup: Matta without triggering any accuso
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7),  // Matta
                new Card(Suit.Denari, 6),
                new Card(Suit.Bastoni, 3)  // 6+3+1=10 -> no accuso
            };
            
            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, new List<Card>());
            
            // Force refresh to ensure cards are rendered
            cardViewManager.ForceRefresh();
            
            // Wait for UI to refresh (longer than InvokeRepeating delay)
            yield return new WaitForSeconds(0.6f);
            
            // Use CardViewManager's GetActiveCardViews instead of FindObjectsOfType
            var allCardViews = cardViewManager.GetActiveCardViews()
                .Where(cv => cv != null && cv.gameObject.activeInHierarchy && cv.Card != null)
                .ToArray();
            var mattaView = allCardViews.FirstOrDefault(cv => cv.Card.IsMatta);
            
            Assert.NotNull(mattaView, "Matta CardView should exist even without accuso");
        }

        [UnityTest]
        public IEnumerator GameState_InitializesCorrectly()
        {
            // Test that TurnController initializes GameState properly
            Assert.IsNull(turnController.GameState, "GameState should be null before StartNewGame");

            // Start a new game
            turnController.StartNewGame();

            yield return new WaitForSeconds(0.5f);

            Assert.NotNull(turnController.GameState, "GameState should exist after StartNewGame");
            Assert.AreEqual(4, turnController.GameState.NumPlayers, "Should have 4 players");
            Assert.GreaterOrEqual(turnController.GameState.CurrentPlayerIndex, 0,
                "Should have a valid current player");
        }

        [UnityTest]
        public IEnumerator ValidMoves_GeneratedForMattaDecino()
        {
            // Setup Decino scenario
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7),
                new Card(Suit.Denari, 6),
                new Card(Suit.Bastoni, 6)
            };
            
            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, new List<Card>());
            yield return new WaitForSeconds(0.3f);
            
            var validMoves = turnController.GetCurrentValidMoves();
            
            Assert.NotNull(validMoves, "Valid moves should not be null");
            Assert.Greater(validMoves.Count, 0, "Should have at least some valid moves");
            
            // Check if Matta can be played
            var mattaMoves = validMoves.Where(m => m.PlayedCard.IsMatta).ToList();
            Assert.Greater(mattaMoves.Count, 0, "Matta should have valid moves");
        }

        [UnityTest]
        public IEnumerator CardViewManager_RefreshesAfterScenarioSetup()
        {
            // Setup scenario
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7),
                new Card(Suit.Denari, 5),
                new Card(Suit.Bastoni, 3)
            };
            
            var table = new List<Card>
            {
                new Card(Suit.Spade, 2),
                new Card(Suit.Denari, 3)
            };
            
            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            
            // Force refresh to ensure cards are rendered
            cardViewManager.ForceRefresh();
            
            // Wait for UI to refresh (longer than InvokeRepeating delay)
            yield return new WaitForSeconds(0.6f);
            
            // Use CardViewManager's GetActiveCardViews instead of FindObjectsOfType
            var allCardViews = cardViewManager.GetActiveCardViews()
                .Where(cv => cv != null && cv.gameObject.activeInHierarchy && cv.Card != null)
                .ToArray();
            
            Assert.GreaterOrEqual(allCardViews.Length, 5, 
                "Should have CardViews for 3 hand cards + 2 table cards");
            
            // Check hand cards
            var handViews = allCardViews.Where(cv => 
                hand.Any(h => h.Suit == cv.Card.Suit && h.Rank == cv.Card.Rank)).ToList();
            Assert.AreEqual(3, handViews.Count, "Should have 3 hand card views");
            
            // Check table cards
            var tableViews = allCardViews.Where(cv => 
                table.Any(t => t.Suit == cv.Card.Suit && t.Rank == cv.Card.Rank)).ToList();
            Assert.AreEqual(2, tableViews.Count, "Should have 2 table card views");
        }
    }
}
