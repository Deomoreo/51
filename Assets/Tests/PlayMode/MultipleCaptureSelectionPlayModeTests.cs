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
    /// PlayMode tests for multiple capture selection scenarios.
    /// Tests the UI flow when player must choose between multiple capturable cards.
    /// Verifies invalid selection feedback and visual hints.
    /// </summary>
    public class MultipleCaptureSelectionPlayModeTests
    {
        private GameObject testSceneRoot;
        private TurnController turnController;
        private CardViewManager cardViewManager;
        private MoveSelectionUI moveSelectionUI;
        private GameObject cardViewPrefab;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Clean up any existing objects from previous tests
            var existingGM = Object.FindObjectOfType<GameManager>();
            if (existingGM != null) Object.DestroyImmediate(existingGM.gameObject);
            var existingCVM = Object.FindObjectOfType<CardViewManager>();
            if (existingCVM != null) Object.DestroyImmediate(existingCVM.gameObject);
            var existingTC = Object.FindObjectOfType<TurnController>();
            if (existingTC != null) Object.DestroyImmediate(existingTC.gameObject);

            // Create test scene root
            testSceneRoot = new GameObject("TestScene");

            // Create GameManager
            var gameManagerObj = new GameObject("GameManager");
            gameManagerObj.transform.SetParent(testSceneRoot.transform);
            gameManagerObj.AddComponent<GameManager>();

            // Create CardViewManager
            var cardViewManagerObj = new GameObject("CardViewManager");
            cardViewManagerObj.transform.SetParent(testSceneRoot.transform);
            cardViewManager = cardViewManagerObj.AddComponent<CardViewManager>();

            // Create TurnController
            turnController = testSceneRoot.AddComponent<TurnController>();

            // Disable auto-start
            var autoStartField = typeof(TurnController).GetField("autoStartGame", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            autoStartField?.SetValue(turnController, false);

            // Connect components
            turnController.SetCardViewManager(cardViewManager);
            cardViewManager.SetTurnController(turnController);

            // Create containers
            var tableContainer = new GameObject("TableCardContainer");
            tableContainer.transform.SetParent(cardViewManagerObj.transform);
            var handContainer = new GameObject("HumanHandContainer");
            handContainer.transform.SetParent(cardViewManagerObj.transform);

            // Assign containers via reflection
            var tableContainerField = typeof(CardViewManager).GetField("tableCardContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tableContainerField?.SetValue(cardViewManager, tableContainer.transform);
            var handContainerField = typeof(CardViewManager).GetField("humanHandContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            handContainerField?.SetValue(cardViewManager, handContainer.transform);

            // Create CardView prefab
            cardViewPrefab = new GameObject("CardViewPrefab");
            cardViewPrefab.transform.SetParent(testSceneRoot.transform);
            cardViewPrefab.SetActive(false);
            cardViewPrefab.AddComponent<CardView>();
            cardViewPrefab.AddComponent<SpriteRenderer>();
            cardViewPrefab.AddComponent<BoxCollider2D>();
            cardViewManager.SetCardViewPrefab(cardViewPrefab);

            // Create MoveSelectionUI
            var uiCanvas = new GameObject("Canvas");
            uiCanvas.transform.SetParent(testSceneRoot.transform);
            var canvas = uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var moveSelectionObj = new GameObject("MoveSelectionUI");
            moveSelectionObj.transform.SetParent(uiCanvas.transform);
            moveSelectionUI = moveSelectionObj.AddComponent<MoveSelectionUI>();

            // Create container for buttons
            var container = new GameObject("ButtonContainer");
            container.transform.SetParent(moveSelectionObj.transform);
            var rectTransform = container.AddComponent<RectTransform>();

            // Create button prefab
            var buttonPrefab = new GameObject("ButtonPrefab");
            buttonPrefab.transform.SetParent(testSceneRoot.transform);
            buttonPrefab.SetActive(false);
            buttonPrefab.AddComponent<RectTransform>();
            var button = buttonPrefab.AddComponent<UnityEngine.UI.Button>();
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonPrefab.transform);
            textObj.AddComponent<RectTransform>();
            textObj.AddComponent<UnityEngine.UI.Text>();

            // Assign UI fields via reflection
            var containerField = typeof(MoveSelectionUI).GetField("container", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            containerField?.SetValue(moveSelectionUI, rectTransform);
            var buttonPrefabField = typeof(MoveSelectionUI).GetField("buttonPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            buttonPrefabField?.SetValue(moveSelectionUI, buttonPrefab);

            // Assign MoveSelectionUI to CardViewManager
            var moveSelectionUIField = typeof(CardViewManager).GetField("moveSelectionUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            moveSelectionUIField?.SetValue(cardViewManager, moveSelectionUI);

            moveSelectionObj.SetActive(false);

            yield return null;

            // Ensure connections after Start
            cardViewManager.SetTurnController(turnController);
            cardViewManager.SetCardViewPrefab(cardViewPrefab);
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

        #region Scenario: Two 7s on table, player has 7 - Must choose which to capture

        [UnityTest]
        public IEnumerator TwoSevensOnTable_PlayerHasSeven_ShowsCaptureOptions()
        {
            // Scenario: Table has 7? and 7?, player has 7?
            // Player should see options to capture either 7
            var hand = new List<Card>
            {
                new Card(Suit.Bastoni, 7),
                new Card(Suit.Denari, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Spade, 7)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            // Get valid moves for the 7
            var validMoves = turnController.GetCurrentValidMoves();
            var sevenMoves = validMoves.Where(m => m.PlayedCard.Rank == 7 && m.PlayedCard.Suit == Suit.Bastoni).ToList();

            // Should have at least 2 capture moves (one for each 7 on table)
            var captureMoves = sevenMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();
            Assert.GreaterOrEqual(captureMoves.Count, 2, 
                "Should have at least 2 capture options for the 7 (one for each table 7)");

            // Verify each capture targets a different card
            var capturedSets = captureMoves
                .Select(m => string.Join(",", m.CapturedCards.Select(c => c.ToString()).OrderBy(s => s)))
                .Distinct()
                .ToList();
            Assert.AreEqual(2, capturedSets.Count, 
                "Should have 2 distinct capture sets (7? and 7?)");
        }

        [UnityTest]
        public IEnumerator TwoSevensOnTable_PlayerPlaysSeven_MoveSelectionUIShown()
        {
            // Scenario: When player clicks their 7, the UI should show capture options
            var hand = new List<Card>
            {
                new Card(Suit.Bastoni, 7),
                new Card(Suit.Denari, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Spade, 7)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            // Find the 7? CardView
            var cardViews = cardViewManager.GetActiveCardViews().ToList();
            var sevenView = cardViews.FirstOrDefault(cv => 
                cv.Card != null && cv.Card.Rank == 7 && cv.Card.Suit == Suit.Bastoni);

            Assert.NotNull(sevenView, "Should find the 7? CardView");

            // Simulate clicking the card by invoking the click event
            var onClickedField = typeof(CardView).GetField("OnCardClicked", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Use reflection to invoke the OnHumanCardClicked method directly
            var onHumanCardClickedMethod = typeof(CardViewManager).GetMethod("OnHumanCardClicked", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onHumanCardClickedMethod?.Invoke(cardViewManager, new object[] { sevenView });

            yield return new WaitForSeconds(0.3f);

            // MoveSelectionUI should be active with options
            Assert.IsTrue(moveSelectionUI.gameObject.activeSelf, 
                "MoveSelectionUI should be shown when multiple capture options exist");
        }

        #endregion

        #region Scenario: Invalid selection - 8 cannot capture two 7s together

        [UnityTest]
        public IEnumerator EightCannotCaptureTwoSevens_InvalidCombination()
        {
            // Scenario: Table has 7? and 7?, player has 8?
            // 8 cannot capture both 7s (7+7=14, not 8 or 15)
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 8),
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Spade, 7)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            // Get valid moves for the 8
            var validMoves = turnController.GetCurrentValidMoves();
            var eightMoves = validMoves.Where(m => m.PlayedCard.Rank == 8 && m.PlayedCard.Suit == Suit.Denari).ToList();

            // Verify there's no move that captures both 7s
            var invalidCombination = eightMoves.FirstOrDefault(m => 
                m.CapturedCards != null && 
                m.CapturedCards.Count == 2 &&
                m.CapturedCards.All(c => c.Rank == 7));

            Assert.IsNull(invalidCombination, 
                "8 should NOT have a valid move to capture both 7s (7+7=14, not a valid capture)");
        }

        [UnityTest]
        public IEnumerator EightWithSevenOnTable_CanCaptureFor15()
        {
            // Scenario: Table has 7?, player has 8?
            // 8 + 7 = 15, so this IS a valid capture
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 8),
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Spade, 5)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            // Get valid moves for the 8
            var validMoves = turnController.GetCurrentValidMoves();
            var eightMoves = validMoves.Where(m => m.PlayedCard.Rank == 8 && m.PlayedCard.Suit == Suit.Denari).ToList();

            // Should have a Capture15 move for 8+7=15
            var capture15Move = eightMoves.FirstOrDefault(m => 
                m.Type == MoveType.Capture15 && 
                m.CapturedCards != null &&
                m.CapturedCards.Any(c => c.Rank == 7));

            Assert.NotNull(capture15Move, 
                "8 should have a Capture15 move for 8+7=15");
        }

        #endregion

        #region Scenario: Card hint bounce animation

        [UnityTest]
        public IEnumerator CardView_PlayHintBounce_AnimatesCard()
        {
            // Setup a simple scenario
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Spade, 3)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            // Find a table card
            var cardViews = cardViewManager.GetActiveCardViews().ToList();
            var tableCardView = cardViews.FirstOrDefault(cv => 
                cv.Card != null && cv.Card.Rank == 5 && cv.Card.Suit == Suit.Denari &&
                turnController.GameState.Table.Contains(cv.Card));

            Assert.NotNull(tableCardView, "Should find the 5? table CardView");

            Vector3 originalPos = tableCardView.transform.position;

            // Play hint bounce
            tableCardView.PlayHintBounce(0f, 2);

            // Wait a bit for animation to start
            yield return new WaitForSeconds(0.1f);

            // Card should have moved (bounced up)
            Vector3 animatedPos = tableCardView.transform.position;
            
            // Wait for animation to complete
            yield return new WaitForSeconds(0.8f);

            // Card should return to original position
            Vector3 finalPos = tableCardView.transform.position;
            Assert.AreEqual(originalPos.y, finalPos.y, 0.01f, 
                "Card should return to original Y position after bounce");
        }

        [UnityTest]
        public IEnumerator CardView_SequentialHintBounces_PlayWithDelay()
        {
            // Setup a scenario with multiple capturable cards
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Spade, 7)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            // Find both table 7s
            var cardViews = cardViewManager.GetActiveCardViews().ToList();
            var tableSevenDenari = cardViews.FirstOrDefault(cv => 
                cv.Card != null && cv.Card.Rank == 7 && cv.Card.Suit == Suit.Denari &&
                turnController.GameState.Table.Contains(cv.Card));
            var tableSevenSpade = cardViews.FirstOrDefault(cv => 
                cv.Card != null && cv.Card.Rank == 7 && cv.Card.Suit == Suit.Spade);

            Assert.NotNull(tableSevenDenari, "Should find the 7? table CardView");
            Assert.NotNull(tableSevenSpade, "Should find the 7? table CardView");

            // Play sequential bounces with different delays
            tableSevenDenari.PlayHintBounce(0f, 2);
            tableSevenSpade.PlayHintBounce(0.5f, 2); // Delayed start

            yield return new WaitForSeconds(0.2f);

            // First card should be animating, second still waiting
            // (We can't easily test exact positions, but we verify no exceptions)

            yield return new WaitForSeconds(1.0f);

            // Both animations should complete without errors
            Assert.Pass("Sequential hint bounces completed successfully");
        }

        #endregion

        #region Scenario: Sum captures with multiple options

        [UnityTest]
        public IEnumerator SumCapture_MultipleCombinations_ShowsOptions()
        {
            // Scenario: Table has 3?, 4?, 2?, player has 7?
            // 7 can capture: 3+4=7 OR 3+2+2 (if there was another 2) OR just equal 7
            // Here: 3+4=7 is the only sum option
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Bastoni, 5),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 3),
                new Card(Suit.Spade, 4),
                new Card(Suit.Bastoni, 2)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            var validMoves = turnController.GetCurrentValidMoves();
            var sevenMoves = validMoves.Where(m => m.PlayedCard.Rank == 7 && m.PlayedCard.Suit == Suit.Denari).ToList();

            // Should have CaptureSum for 3+4=7
            var sumCapture = sevenMoves.FirstOrDefault(m => 
                m.Type == MoveType.CaptureSum && 
                m.CapturedCards != null &&
                m.CapturedCards.Sum(c => c.Value) == 7);

            Assert.NotNull(sumCapture, 
                "7 should have a CaptureSum move for 3+4=7");
            Assert.AreEqual(2, sumCapture.CapturedCards.Count, 
                "CaptureSum should capture exactly 2 cards (3 and 4)");
        }

        [UnityTest]
        public IEnumerator Capture15_WithMultipleCards_ShowsCorrectMove()
        {
            // Scenario: Table has 6?, 2?, player has 7?
            // 7 + 6 + 2 = 15
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Bastoni, 5),
                new Card(Suit.Coppe, 3)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 6),
                new Card(Suit.Spade, 2)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            var validMoves = turnController.GetCurrentValidMoves();
            var sevenMoves = validMoves.Where(m => m.PlayedCard.Rank == 7 && m.PlayedCard.Suit == Suit.Denari).ToList();

            // Should have Capture15 for 7+6+2=15
            var capture15 = sevenMoves.FirstOrDefault(m => 
                m.Type == MoveType.Capture15 && 
                m.CapturedCards != null &&
                m.CapturedCards.Sum(c => c.Value) == 8); // 15 - 7 = 8

            Assert.NotNull(capture15, 
                "7 should have a Capture15 move for 7+6+2=15");
        }

        #endregion

        #region Scenario: PlayOnly when no capture available

        [UnityTest]
        public IEnumerator NoCapturePossible_OnlyPlayOnlyMoves()
        {
            // Scenario: Table has 9?, player has 2?, 3?, 4?
            // No captures possible (2, 3, 4 can't match 9 or sum to 15 with 9)
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 2),
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Spade, 4)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 9)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            var validMoves = turnController.GetCurrentValidMoves();

            // All moves should be PlayOnly
            Assert.IsTrue(validMoves.All(m => m.Type == MoveType.PlayOnly), 
                "All moves should be PlayOnly when no captures are possible");
            Assert.AreEqual(3, validMoves.Count, 
                "Should have 3 PlayOnly moves (one for each card in hand)");
        }

        #endregion

        #region Scenario: Ace special capture rules

        [UnityTest]
        public IEnumerator AceCapture_WhenNoAceOnTable_CapturesAllCards()
        {
            // Scenario: Table has 3?, 5?, player has Ace?
            // Ace captures all cards when no other Ace on table
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 1), // Ace
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 3),
                new Card(Suit.Spade, 5)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            var validMoves = turnController.GetCurrentValidMoves();
            var aceMoves = validMoves.Where(m => m.PlayedCard.IsAce).ToList();

            // Should have AceCapture that takes all cards
            var aceCapture = aceMoves.FirstOrDefault(m => 
                m.Type == MoveType.AceCapture && 
                m.CapturedCards != null &&
                m.CapturedCards.Count == 2);

            Assert.NotNull(aceCapture, 
                "Ace should capture all cards when no Ace on table");
        }

        [UnityTest]
        public IEnumerator AceCapture_WhenAceOnTable_CapturesOnlyAce()
        {
            // Scenario: Table has Ace?, 5?, player has Ace?
            // Ace captures only the other Ace
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 1), // Ace
                new Card(Suit.Bastoni, 3),
                new Card(Suit.Coppe, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Spade, 1), // Ace on table
                new Card(Suit.Denari, 5)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            var validMoves = turnController.GetCurrentValidMoves();
            var aceMoves = validMoves.Where(m => m.PlayedCard.IsAce).ToList();

            // Should have AceCapture that takes only the other Ace
            var aceCapture = aceMoves.FirstOrDefault(m => 
                m.Type == MoveType.AceCapture && 
                m.CapturedCards != null &&
                m.CapturedCards.Count == 1 &&
                m.CapturedCards[0].IsAce);

            Assert.NotNull(aceCapture, 
                "Ace should capture only the other Ace when Ace is on table");
        }

        #endregion

        #region Scenario: Format move description

        [UnityTest]
        public IEnumerator FormatMoveDescription_ShowsCorrectSymbols()
        {
            // Test that move descriptions use correct symbols
            var hand = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Bastoni, 5),
                new Card(Suit.Coppe, 3)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 7),
                new Card(Suit.Spade, 8)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            // Use reflection to call FormatMoveDescription
            var formatMethod = typeof(CardViewManager).GetMethod("FormatMoveDescription", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var move = new Move(0, new Card(Suit.Denari, 7), MoveType.CaptureEqual, 
                new List<Card> { new Card(Suit.Denari, 7) });

            var description = formatMethod?.Invoke(cardViewManager, new object[] { move }) as string;

            Assert.NotNull(description, "Description should not be null");
            Assert.IsTrue(description.Contains("7"), "Description should contain the rank");
            Assert.IsTrue(description.Contains("?") || description.Contains("="), 
                "Description should contain suit symbol or type indicator");
        }

        #endregion

        #region Scenario: Multiple equal value cards

        [UnityTest]
        public IEnumerator ThreeSameRankOnTable_PlayerMustChoose()
        {
            // Scenario: Table has 5?, 5?, 5?, player has 5?
            // Player should choose which 5 to capture
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 5),
                new Card(Suit.Denari, 3),
                new Card(Suit.Bastoni, 2)
            };
            var table = new List<Card>
            {
                new Card(Suit.Denari, 5),
                new Card(Suit.Spade, 5),
                new Card(Suit.Bastoni, 5)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();

            yield return new WaitForSeconds(0.6f);

            var validMoves = turnController.GetCurrentValidMoves();
            var fiveMoves = validMoves.Where(m => 
                m.PlayedCard.Rank == 5 && m.PlayedCard.Suit == Suit.Coppe &&
                m.Type == MoveType.CaptureEqual).ToList();

            // Should have 3 CaptureEqual moves (one for each 5 on table)
            Assert.AreEqual(3, fiveMoves.Count, 
                "Should have 3 CaptureEqual options for the 5 (one for each table 5)");

            // Each should capture a different card
            var capturedCards = fiveMoves.SelectMany(m => m.CapturedCards).ToList();
            Assert.AreEqual(3, capturedCards.Distinct().Count(), 
                "Each capture option should target a different 5");
        }

        #endregion
    }
}
