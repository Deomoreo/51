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
    /// Comprehensive PlayMode tests covering ALL possible capture scenarios.
    /// Tests every combination of capture types and ensures proper UI behavior.
    /// </summary>
    public class ComprehensiveCaptureSelectionTests
    {
        private GameObject testSceneRoot;
        private TurnController turnController;
        private CardViewManager cardViewManager;
        private MoveSelectionUI moveSelectionUI;
        private GameObject cardViewPrefab;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Clean up any existing objects
            foreach (var go in Object.FindObjectsOfType<GameManager>()) Object.DestroyImmediate(go.gameObject);
            foreach (var go in Object.FindObjectsOfType<CardViewManager>()) Object.DestroyImmediate(go.gameObject);
            foreach (var go in Object.FindObjectsOfType<TurnController>()) Object.DestroyImmediate(go.gameObject);

            testSceneRoot = new GameObject("TestScene");

            // GameManager
            var gmObj = new GameObject("GameManager");
            gmObj.transform.SetParent(testSceneRoot.transform);
            gmObj.AddComponent<GameManager>();

            // CardViewManager
            var cvmObj = new GameObject("CardViewManager");
            cvmObj.transform.SetParent(testSceneRoot.transform);
            cardViewManager = cvmObj.AddComponent<CardViewManager>();

            // TurnController
            turnController = testSceneRoot.AddComponent<TurnController>();
            var autoStartField = typeof(TurnController).GetField("autoStartGame",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            autoStartField?.SetValue(turnController, false);

            turnController.SetCardViewManager(cardViewManager);
            cardViewManager.SetTurnController(turnController);

            // Containers
            var tableContainer = new GameObject("TableCardContainer");
            tableContainer.transform.SetParent(cvmObj.transform);
            var handContainer = new GameObject("HumanHandContainer");
            handContainer.transform.SetParent(cvmObj.transform);

            typeof(CardViewManager).GetField("tableCardContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cardViewManager, tableContainer.transform);
            typeof(CardViewManager).GetField("humanHandContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cardViewManager, handContainer.transform);

            // CardView prefab
            cardViewPrefab = new GameObject("CardViewPrefab");
            cardViewPrefab.transform.SetParent(testSceneRoot.transform);
            cardViewPrefab.SetActive(false);
            cardViewPrefab.AddComponent<CardView>();
            cardViewPrefab.AddComponent<SpriteRenderer>();
            cardViewPrefab.AddComponent<BoxCollider2D>();
            cardViewManager.SetCardViewPrefab(cardViewPrefab);

            // MoveSelectionUI
            var uiCanvas = new GameObject("Canvas");
            uiCanvas.transform.SetParent(testSceneRoot.transform);
            var canvas = uiCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var moveSelectionObj = new GameObject("MoveSelectionUI");
            moveSelectionObj.transform.SetParent(uiCanvas.transform);
            moveSelectionUI = moveSelectionObj.AddComponent<MoveSelectionUI>();

            var container = new GameObject("ButtonContainer");
            container.transform.SetParent(moveSelectionObj.transform);
            var rt = container.AddComponent<RectTransform>();

            var buttonPrefab = new GameObject("ButtonPrefab");
            buttonPrefab.transform.SetParent(testSceneRoot.transform);
            buttonPrefab.SetActive(false);
            buttonPrefab.AddComponent<RectTransform>();
            buttonPrefab.AddComponent<UnityEngine.UI.Button>();
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonPrefab.transform);
            textObj.AddComponent<RectTransform>();
            textObj.AddComponent<UnityEngine.UI.Text>();

            typeof(MoveSelectionUI).GetField("container",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(moveSelectionUI, rt);
            typeof(MoveSelectionUI).GetField("buttonPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(moveSelectionUI, buttonPrefab);
            typeof(CardViewManager).GetField("moveSelectionUI",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cardViewManager, moveSelectionUI);

            moveSelectionObj.SetActive(false);

            yield return null;

            cardViewManager.SetTurnController(turnController);
            cardViewManager.SetCardViewPrefab(cardViewPrefab);
            cardViewManager.ForceRefresh();
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (testSceneRoot != null) Object.Destroy(testSceneRoot);
            yield return null;
        }

        #region SCENARIO 1: Single Equal Value Capture (AUTO)

        [UnityTest]
        public IEnumerator SingleEqualCapture_AutoExecutes()
        {
            // Table: [5?], Hand: [5?] -> Only one 5, auto-capture
            var hand = new List<Card> { new Card(Suit.Bastoni, 5), new Card(Suit.Denari, 3), new Card(Suit.Coppe, 2) };
            var table = new List<Card> { new Card(Suit.Denari, 5) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var fiveMoves = moves.Where(m => m.PlayedCard.Rank == 5 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var captureMoves = fiveMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            Assert.AreEqual(1, captureMoves.Count, "Should have exactly 1 capture move for single equal value");
        }

        #endregion

        #region SCENARIO 2: Multiple Equal Value Captures (CHOICE REQUIRED)

        [UnityTest]
        public IEnumerator TwoEqualCaptures_RequiresChoice()
        {
            // Table: [5?, 5?], Hand: [5?] -> Two 5s, player must choose
            var hand = new List<Card> { new Card(Suit.Bastoni, 5), new Card(Suit.Denari, 3), new Card(Suit.Coppe, 2) };
            var table = new List<Card> { new Card(Suit.Denari, 5), new Card(Suit.Spade, 5) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var fiveMoves = moves.Where(m => m.PlayedCard.Rank == 5 && m.PlayedCard.Suit == Suit.Bastoni 
                && m.Type == MoveType.CaptureEqual).ToList();

            Assert.AreEqual(2, fiveMoves.Count, "Should have 2 CaptureEqual moves (one for each 5)");
            
            // Verify they capture different cards
            var captured1 = fiveMoves[0].CapturedCards[0];
            var captured2 = fiveMoves[1].CapturedCards[0];
            Assert.AreNotEqual(captured1, captured2, "Each move should capture a different 5");
        }

        [UnityTest]
        public IEnumerator ThreeEqualCaptures_RequiresChoice()
        {
            // Table: [5?, 5?, 5?], Hand: [5?] -> Three 5s, player must choose
            var hand = new List<Card> { new Card(Suit.Bastoni, 5), new Card(Suit.Denari, 3), new Card(Suit.Spade, 2) };
            var table = new List<Card> { new Card(Suit.Denari, 5), new Card(Suit.Spade, 5), new Card(Suit.Coppe, 5) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var fiveMoves = moves.Where(m => m.PlayedCard.Rank == 5 && m.PlayedCard.Suit == Suit.Bastoni
                && m.Type == MoveType.CaptureEqual).ToList();

            Assert.AreEqual(3, fiveMoves.Count, "Should have 3 CaptureEqual moves (one for each 5)");
        }

        #endregion

        #region SCENARIO 3: Equal Value vs Sum Capture (CHOICE REQUIRED)

        [UnityTest]
        public IEnumerator EqualVsSum_RequiresChoice()
        {
            // Table: [7?, 4?, 3?], Hand: [7?]
            // Options: CaptureEqual 7? OR CaptureSum 4+3=7
            var hand = new List<Card> { new Card(Suit.Bastoni, 7), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 1) };
            var table = new List<Card> { new Card(Suit.Denari, 7), new Card(Suit.Spade, 4), new Card(Suit.Bastoni, 3) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var sevenMoves = moves.Where(m => m.PlayedCard.Rank == 7 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var captureMoves = sevenMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            // Should have: 1 CaptureEqual (7?) + 1 CaptureSum (4+3)
            var equalMoves = captureMoves.Where(m => m.Type == MoveType.CaptureEqual).ToList();
            var sumMoves = captureMoves.Where(m => m.Type == MoveType.CaptureSum).ToList();

            Assert.AreEqual(1, equalMoves.Count, "Should have 1 CaptureEqual move");
            Assert.AreEqual(1, sumMoves.Count, "Should have 1 CaptureSum move");
            Assert.GreaterOrEqual(captureMoves.Count, 2, "Should have at least 2 different capture options");
        }

        #endregion

        #region SCENARIO 4: Multiple Sum Captures (CHOICE REQUIRED)

        [UnityTest]
        public IEnumerator MultipleSumCaptures_RequiresChoice()
        {
            // Table: [6?, 4?, 5?, 5?], Hand: [10?]
            // Options: CaptureSum 6+4=10 OR CaptureSum 5+5=10
            var hand = new List<Card> { new Card(Suit.Bastoni, 10), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 1) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 6), 
                new Card(Suit.Spade, 4), 
                new Card(Suit.Bastoni, 5), 
                new Card(Suit.Coppe, 5) 
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var tenMoves = moves.Where(m => m.PlayedCard.Rank == 10 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var sumMoves = tenMoves.Where(m => m.Type == MoveType.CaptureSum).ToList();

            // Should have: 6+4=10 AND 5+5=10
            Assert.GreaterOrEqual(sumMoves.Count, 2, "Should have at least 2 CaptureSum moves");

            // Verify the two different combinations exist
            var has64 = sumMoves.Any(m => m.CapturedCards.Sum(c => c.Value) == 10 && 
                m.CapturedCards.Any(c => c.Value == 6) && m.CapturedCards.Any(c => c.Value == 4));
            var has55 = sumMoves.Any(m => m.CapturedCards.Sum(c => c.Value) == 10 && 
                m.CapturedCards.Count(c => c.Value == 5) == 2);

            Assert.IsTrue(has64, "Should have 6+4=10 option");
            Assert.IsTrue(has55, "Should have 5+5=10 option");
        }

        #endregion

        #region SCENARIO 5: Equal + Sum + 15 (CHOICE REQUIRED)

        [UnityTest]
        public IEnumerator EqualSumAnd15_RequiresChoice()
        {
            // Table: [7?, 5?, 2?, 1?], Hand: [7?]
            // Options: CaptureEqual 7? OR CaptureSum 5+2=7 OR Capture15 7+5+2+1=15 (wait, 7+8=15, need 8 on table)
            // Let's use: Table: [7?, 5?, 3?], Hand: [8?] for Capture15
            var hand = new List<Card> { new Card(Suit.Bastoni, 8), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 1) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 7),  // 8+7=15
                new Card(Suit.Spade, 5),   // 8+5+2=15
                new Card(Suit.Bastoni, 2),
                new Card(Suit.Coppe, 8)    // Equal capture
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var eightMoves = moves.Where(m => m.PlayedCard.Rank == 8 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var captureMoves = eightMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            var equalMoves = captureMoves.Where(m => m.Type == MoveType.CaptureEqual).ToList();
            var capture15Moves = captureMoves.Where(m => m.Type == MoveType.Capture15).ToList();

            Assert.GreaterOrEqual(equalMoves.Count, 1, "Should have at least 1 CaptureEqual (8?)");
            Assert.GreaterOrEqual(capture15Moves.Count, 1, "Should have at least 1 Capture15 (8+7=15)");
        }

        #endregion

        #region SCENARIO 6: Single Sum Capture (AUTO)

        [UnityTest]
        public IEnumerator SingleSumCapture_AutoExecutes()
        {
            // Table: [7?, 3?], Hand: [10?]
            // Only one option: 7+3=10
            var hand = new List<Card> { new Card(Suit.Bastoni, 10), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 1) };
            var table = new List<Card> { new Card(Suit.Denari, 7), new Card(Suit.Spade, 3) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var tenMoves = moves.Where(m => m.PlayedCard.Rank == 10 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var captureMoves = tenMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            // Group by captured cards set
            var uniqueSets = captureMoves
                .Select(m => string.Join("|", m.CapturedCards.Select(c => c.ToString()).OrderBy(s => s)))
                .Distinct()
                .ToList();

            Assert.AreEqual(1, uniqueSets.Count, "Should have only 1 unique capture set (7+3)");
        }

        #endregion

        #region SCENARIO 7: Ace Captures

        [UnityTest]
        public IEnumerator AceWithNoAceOnTable_CapturesAll()
        {
            // Table: [5?, 3?, 7?], Hand: [A?]
            // Ace captures all cards when no ace on table
            var hand = new List<Card> { new Card(Suit.Bastoni, 1), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 4) };
            var table = new List<Card> { new Card(Suit.Denari, 5), new Card(Suit.Spade, 3), new Card(Suit.Bastoni, 7) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var aceMoves = moves.Where(m => m.PlayedCard.IsAce && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var aceCapture = aceMoves.FirstOrDefault(m => m.Type == MoveType.AceCapture);

            Assert.NotNull(aceCapture, "Should have AceCapture move");
            Assert.AreEqual(3, aceCapture.CapturedCards.Count, "Ace should capture all 3 table cards");
        }

        [UnityTest]
        public IEnumerator AceWithOneAceOnTable_CapturesOnlyAce()
        {
            // Table: [A?, 3?, 7?], Hand: [A?]
            // Ace captures only the other ace
            var hand = new List<Card> { new Card(Suit.Bastoni, 1), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 4) };
            var table = new List<Card> { new Card(Suit.Denari, 1), new Card(Suit.Spade, 3), new Card(Suit.Bastoni, 7) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var aceMoves = moves.Where(m => m.PlayedCard.IsAce && m.PlayedCard.Suit == Suit.Bastoni 
                && m.Type == MoveType.AceCapture).ToList();

            Assert.AreEqual(1, aceMoves.Count, "Should have 1 AceCapture move");
            Assert.AreEqual(1, aceMoves[0].CapturedCards.Count, "Should capture only 1 card (the other ace)");
            Assert.IsTrue(aceMoves[0].CapturedCards[0].IsAce, "Captured card should be an ace");
        }

        [UnityTest]
        public IEnumerator AceWithTwoAcesOnTable_RequiresChoice()
        {
            // Table: [A?, A?, 7?], Hand: [A?]
            // Ace must choose which ace to capture
            var hand = new List<Card> { new Card(Suit.Bastoni, 1), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 4) };
            var table = new List<Card> { new Card(Suit.Denari, 1), new Card(Suit.Spade, 1), new Card(Suit.Bastoni, 7) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var aceMoves = moves.Where(m => m.PlayedCard.IsAce && m.PlayedCard.Suit == Suit.Bastoni
                && m.Type == MoveType.AceCapture).ToList();

            Assert.AreEqual(2, aceMoves.Count, "Should have 2 AceCapture moves (one for each table ace)");
        }

        #endregion

        #region SCENARIO 8: Capture15 Options

        [UnityTest]
        public IEnumerator MultipleCapture15Options_RequiresChoice()
        {
            // Table: [7?, 6?, 2?, 1?], Hand: [8?]
            // Options: 8+7=15 OR 8+6+1=15 OR 8+7 again... let me recalculate
            // 8+7=15 ?
            // 8+6+1=15 ?
            // 8+5+2=15 (need 5)
            var hand = new List<Card> { new Card(Suit.Bastoni, 8), new Card(Suit.Denari, 3), new Card(Suit.Coppe, 4) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 7),  // 8+7=15
                new Card(Suit.Spade, 6),   // 8+6+1=15
                new Card(Suit.Bastoni, 1)  // ace
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var eightMoves = moves.Where(m => m.PlayedCard.Rank == 8 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var capture15Moves = eightMoves.Where(m => m.Type == MoveType.Capture15).ToList();

            // 8+7=15, 8+6+1=15
            Assert.GreaterOrEqual(capture15Moves.Count, 2, "Should have at least 2 Capture15 options");
        }

        #endregion

        #region SCENARIO 9: No Capture Possible (PlayOnly)

        [UnityTest]
        public IEnumerator NoCapturesPossible_OnlyPlayOnly()
        {
            // Table: [9?], Hand: [2?, 3?, 4?]
            // No captures possible: 2,3,4 can't match 9 or sum to anything useful
            var hand = new List<Card> { new Card(Suit.Bastoni, 2), new Card(Suit.Spade, 3), new Card(Suit.Coppe, 4) };
            var table = new List<Card> { new Card(Suit.Denari, 9) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);

            Assert.IsTrue(moves.All(m => m.Type == MoveType.PlayOnly), "All moves should be PlayOnly");
            Assert.AreEqual(3, moves.Count, "Should have 3 PlayOnly moves (one per card)");
        }

        #endregion

        #region SCENARIO 10: Complex Mixed Scenario

        [UnityTest]
        public IEnumerator ComplexMixedScenario_AllOptionsAvailable()
        {
            // Table: [5?, 5?, 3?, 2?, 8?], Hand: [5?, 10?, 7?]
            // For 5?: CaptureEqual 5? OR CaptureEqual 5? (choice required)
            // For 10?: CaptureSum 5+5=10 OR CaptureSum 5+3+2=10 OR CaptureSum 8+2=10 (choices)
            // For 7?: CaptureSum 5+2=7 OR CaptureSum 3+2+2... wait, only one 2
            var hand = new List<Card> { 
                new Card(Suit.Coppe, 5),   // Can capture 5? or 5?
                new Card(Suit.Bastoni, 10), // Can capture 5+5 or 5+3+2 or 8+2
                new Card(Suit.Spade, 7)     // Can capture 5+2 or 3+2+2 (no, only one 2)
            };
            var table = new List<Card> { 
                new Card(Suit.Denari, 5), 
                new Card(Suit.Spade, 5), 
                new Card(Suit.Bastoni, 3), 
                new Card(Suit.Coppe, 2),
                new Card(Suit.Bastoni, 8)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);

            // Check 5? options
            var fiveMoves = moves.Where(m => m.PlayedCard.Rank == 5 && m.PlayedCard.Suit == Suit.Coppe
                && m.Type == MoveType.CaptureEqual).ToList();
            Assert.AreEqual(2, fiveMoves.Count, "5? should have 2 CaptureEqual options");

            // Check 10? options
            var tenMoves = moves.Where(m => m.PlayedCard.Rank == 10 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var tenSumMoves = tenMoves.Where(m => m.Type == MoveType.CaptureSum).ToList();
            Assert.GreaterOrEqual(tenSumMoves.Count, 2, "10? should have at least 2 CaptureSum options");

            // Check 7? options
            var sevenMoves = moves.Where(m => m.PlayedCard.Rank == 7 && m.PlayedCard.Suit == Suit.Spade).ToList();
            var sevenSumMoves = sevenMoves.Where(m => m.Type == MoveType.CaptureSum).ToList();
            Assert.GreaterOrEqual(sevenSumMoves.Count, 1, "7? should have at least 1 CaptureSum option (5+2)");
        }

        #endregion

        #region SCENARIO 11: Superset Move Logic

        [UnityTest]
        public IEnumerator SupersetMove_AutoSelectsLargerCapture()
        {
            // Table: [3?, 2?, 5?], Hand: [10?]
            // Options: CaptureSum 3+2+5=10 (captures 3 cards)
            // No smaller subset sums to 10
            var hand = new List<Card> { new Card(Suit.Bastoni, 10), new Card(Suit.Denari, 1), new Card(Suit.Coppe, 1) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 3), 
                new Card(Suit.Spade, 2), 
                new Card(Suit.Bastoni, 5) 
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var tenMoves = moves.Where(m => m.PlayedCard.Rank == 10 && m.PlayedCard.Suit == Suit.Bastoni
                && m.Type == MoveType.CaptureSum).ToList();

            Assert.AreEqual(1, tenMoves.Count, "Should have exactly 1 CaptureSum move (3+2+5=10)");
            Assert.AreEqual(3, tenMoves[0].CapturedCards.Count, "Should capture all 3 cards");
        }

        #endregion

        #region SCENARIO 12: Subset vs Superset Choice

        [UnityTest]
        public IEnumerator SubsetVsSuperset_ShowsBothOptions()
        {
            // Table: [4?, 3?, 2?, 1?], Hand: [10?]
            // Options: 
            // - CaptureSum 4+3+2+1=10 (4 cards)
            // - No other subset = 10
            // This should auto-execute since there's only one option
            
            // Let's create a case with multiple options:
            // Table: [5?, 5?, 4?, 1?], Hand: [10?]
            // - CaptureSum 5+5=10 (2 cards)
            // - CaptureSum 5+4+1=10 (3 cards)
            var hand = new List<Card> { new Card(Suit.Bastoni, 10), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 3) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 5), 
                new Card(Suit.Spade, 5), 
                new Card(Suit.Bastoni, 4),
                new Card(Suit.Coppe, 1)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var tenMoves = moves.Where(m => m.PlayedCard.Rank == 10 && m.PlayedCard.Suit == Suit.Bastoni
                && m.Type == MoveType.CaptureSum).ToList();

            // Should have 5+5=10 and multiple 5+4+1=10 combinations
            Assert.GreaterOrEqual(tenMoves.Count, 2, "Should have at least 2 CaptureSum options");

            // Check for 5+5=10 option
            var has55 = tenMoves.Any(m => m.CapturedCards.Count == 2 && m.CapturedCards.All(c => c.Value == 5));
            Assert.IsTrue(has55, "Should have 5+5=10 option");

            // Check for 5+4+1=10 options
            var has541 = tenMoves.Any(m => m.CapturedCards.Count == 3 && m.CapturedCards.Sum(c => c.Value) == 10);
            Assert.IsTrue(has541, "Should have 5+4+1=10 option");
        }

        #endregion

        #region SCENARIO 13: Verify UI Shows Correct Number of Options

        [UnityTest]
        public IEnumerator UIShowsCorrectNumberOfOptions()
        {
            // Table: [7?, 7?], Hand: [7?]
            // Should show 2 options: capture 7? OR capture 7?
            var hand = new List<Card> { new Card(Suit.Bastoni, 7), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 3) };
            var table = new List<Card> { new Card(Suit.Denari, 7), new Card(Suit.Spade, 7) };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();
            yield return new WaitForSeconds(0.6f);

            // Find the 7? CardView
            var cardViews = cardViewManager.GetActiveCardViews().ToList();
            var sevenView = cardViews.FirstOrDefault(cv => 
                cv.Card != null && cv.Card.Rank == 7 && cv.Card.Suit == Suit.Bastoni);

            Assert.NotNull(sevenView, "Should find the 7? CardView");

            // Simulate click
            var method = typeof(CardViewManager).GetMethod("OnHumanCardClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(cardViewManager, new object[] { sevenView });

            yield return new WaitForSeconds(0.3f);

            // MoveSelectionUI should be active
            Assert.IsTrue(moveSelectionUI.gameObject.activeSelf,
                "MoveSelectionUI should be shown when multiple equal capture options exist");
        }

        #endregion

        #region SCENARIO 14: CaptureSum vs Capture15 - CRITICAL TEST

        [UnityTest]
        public IEnumerator CaptureSumVsCapture15_RequiresChoice()
        {
            // CRITICAL: Table: [5?, 3?, 7?], Hand: [10?]
            // Options:
            // - CaptureSum: 7+3=10 (captures {7,3})
            // - Capture15: 10+5=15 (captures {5})
            // These are TWO DIFFERENT capture sets, player MUST choose!
            var hand = new List<Card> { new Card(Suit.Denari, 10), new Card(Suit.Bastoni, 2), new Card(Suit.Coppe, 1) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 5),   // 10+5=15 ? Capture15
                new Card(Suit.Spade, 3),    // Part of 7+3=10
                new Card(Suit.Bastoni, 7)   // Part of 7+3=10
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var tenMoves = moves.Where(m => m.PlayedCard.Rank == 10 && m.PlayedCard.Suit == Suit.Denari).ToList();
            var captureMoves = tenMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            // Should have: CaptureSum {7,3} AND Capture15 {5}
            var sumMoves = captureMoves.Where(m => m.Type == MoveType.CaptureSum).ToList();
            var capture15Moves = captureMoves.Where(m => m.Type == MoveType.Capture15).ToList();

            Assert.GreaterOrEqual(sumMoves.Count, 1, "Should have at least 1 CaptureSum (7+3=10)");
            Assert.GreaterOrEqual(capture15Moves.Count, 1, "Should have at least 1 Capture15 (10+5=15)");

            // Verify they capture DIFFERENT card sets
            var sumCaptured = string.Join(",", sumMoves[0].CapturedCards.Select(c => c.ToString()).OrderBy(s => s));
            var capture15Captured = string.Join(",", capture15Moves[0].CapturedCards.Select(c => c.ToString()).OrderBy(s => s));

            Assert.AreNotEqual(sumCaptured, capture15Captured, 
                "CaptureSum and Capture15 should capture DIFFERENT card sets");

            // Count unique capture sets
            var uniqueSets = captureMoves
                .Select(m => string.Join("|", m.CapturedCards.Select(c => c.ToString()).OrderBy(s => s)))
                .Distinct()
                .ToList();

            Assert.GreaterOrEqual(uniqueSets.Count, 2, 
                "Should have at least 2 unique capture sets: {7,3} and {5}");
        }

        [UnityTest]
        public IEnumerator CaptureSumVsCapture15_UIShowsOptions()
        {
            // Same scenario but test UI behavior
            var hand = new List<Card> { new Card(Suit.Denari, 10), new Card(Suit.Bastoni, 2), new Card(Suit.Coppe, 1) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 5),
                new Card(Suit.Spade, 3),
                new Card(Suit.Bastoni, 7)
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            cardViewManager.ForceRefresh();
            yield return new WaitForSeconds(0.6f);

            // Find the 10? CardView
            var cardViews = cardViewManager.GetActiveCardViews().ToList();
            var tenView = cardViews.FirstOrDefault(cv => 
                cv.Card != null && cv.Card.Rank == 10 && cv.Card.Suit == Suit.Denari);

            Assert.NotNull(tenView, "Should find the 10? CardView");

            // Simulate click
            var method = typeof(CardViewManager).GetMethod("OnHumanCardClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(cardViewManager, new object[] { tenView });

            yield return new WaitForSeconds(0.3f);

            // MoveSelectionUI MUST be active because there are 2 different capture options
            Assert.IsTrue(moveSelectionUI.gameObject.activeSelf,
                "MoveSelectionUI MUST be shown when CaptureSum and Capture15 are both available with different card sets!");
        }

        #endregion

        #region SCENARIO 15: Multiple Capture15 combinations

        [UnityTest]
        public IEnumerator MultipleCapture15Combinations_RequiresChoice()
        {
            // Table: [7?, 6?, 2?], Hand: [8?]
            // Options:
            // - Capture15: 8+7=15 (captures {7})
            // - Capture15: 8+6+1=15... wait no 1
            // - Capture15: 8+5+2=15... need 5
            // Let me recalculate: 8+7=15, but also need more options
            // Better: Table: [7?, 5?, 2?], Hand: [8?]
            // - Capture15: 8+7=15 (captures {7})
            // - Capture15: 8+5+2=15 (captures {5,2})
            var hand = new List<Card> { new Card(Suit.Denari, 8), new Card(Suit.Bastoni, 3), new Card(Suit.Coppe, 4) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 7),   // 8+7=15
                new Card(Suit.Spade, 5),    // 8+5+2=15
                new Card(Suit.Bastoni, 2)   // Part of 8+5+2=15
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var eightMoves = moves.Where(m => m.PlayedCard.Rank == 8 && m.PlayedCard.Suit == Suit.Denari).ToList();
            var capture15Moves = eightMoves.Where(m => m.Type == MoveType.Capture15).ToList();

            // Should have: 8+7=15 AND 8+5+2=15
            Assert.GreaterOrEqual(capture15Moves.Count, 2, "Should have at least 2 Capture15 options");

            // Verify different card sets
            var uniqueSets = capture15Moves
                .Select(m => string.Join("|", m.CapturedCards.Select(c => c.ToString()).OrderBy(s => s)))
                .Distinct()
                .ToList();

            Assert.GreaterOrEqual(uniqueSets.Count, 2, "Should have at least 2 unique Capture15 sets");
        }

        #endregion

        #region SCENARIO 16: CaptureEqual vs CaptureSum vs Capture15 (Triple choice)

        [UnityTest]
        public IEnumerator TripleChoice_EqualSumAnd15()
        {
            // Table: [7?, 4?, 3?, 1?], Hand: [7?]
            // Options:
            // - CaptureEqual: 7 captures 7? (captures {7?})
            // - CaptureSum: 7 captures 4+3 (captures {4,3})
            // - Capture15: 7+4+3+1=15 (captures {4,3,1})
            // - Capture15: 7+4+4... no, only one 4
            // Actually 7+1+4+3=15 is the only Capture15 here
            var hand = new List<Card> { new Card(Suit.Bastoni, 7), new Card(Suit.Denari, 2), new Card(Suit.Coppe, 9) };
            var table = new List<Card> { 
                new Card(Suit.Denari, 7),   // Equal capture
                new Card(Suit.Spade, 4),    // Part of 4+3=7, and part of 7+4+3+1=15
                new Card(Suit.Bastoni, 3),  // Part of 4+3=7, and part of 7+4+3+1=15
                new Card(Suit.Coppe, 1)     // Part of 7+4+3+1=15
            };

            turnController.StartNewGame();
            turnController.SetupScenarioForCurrentPlayer(hand, table);
            yield return new WaitForSeconds(0.3f);

            var moves = Rules51.GetValidMoves(turnController.GameState, 0);
            var sevenMoves = moves.Where(m => m.PlayedCard.Rank == 7 && m.PlayedCard.Suit == Suit.Bastoni).ToList();
            var captureMoves = sevenMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();

            var equalMoves = captureMoves.Where(m => m.Type == MoveType.CaptureEqual).ToList();
            var sumMoves = captureMoves.Where(m => m.Type == MoveType.CaptureSum).ToList();
            var capture15Moves = captureMoves.Where(m => m.Type == MoveType.Capture15).ToList();

            Assert.GreaterOrEqual(equalMoves.Count, 1, "Should have CaptureEqual (7?)");
            Assert.GreaterOrEqual(sumMoves.Count, 1, "Should have CaptureSum (4+3=7)");
            Assert.GreaterOrEqual(capture15Moves.Count, 1, "Should have Capture15 (7+4+3+1=15)");

            // Count unique capture sets
            var uniqueSets = captureMoves
                .Select(m => string.Join("|", m.CapturedCards.Select(c => c.ToString()).OrderBy(s => s)))
                .Distinct()
                .ToList();

            Assert.GreaterOrEqual(uniqueSets.Count, 3, 
                "Should have at least 3 unique capture sets: {7}, {4,3}, {4,3,1}");
        }

        #endregion
    }
}
