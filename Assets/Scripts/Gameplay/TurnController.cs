using UnityEngine;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;

namespace Project51.Unity
{
    /// <summary>
    /// Main controller for a Cirulla/51 game session in Unity.
    /// Manages the core game state and turn flow.
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private MoveSelectionUI moveSelectionUI;
        [SerializeField] private RoundEndPanel roundEndPanel;
        
        [Header("Managers (Optional - Auto-Find)")]
        [SerializeField] private CardViewManager cardViewManager;
        [SerializeField] private CapturedPileManager capturedPileManager;
        
        /// <summary>
        /// Sets the CardViewManager reference at runtime. Useful for tests.
        /// </summary>
        public void SetCardViewManager(CardViewManager manager)
        {
            cardViewManager = manager;
        }
        
        [Header("Game Settings")]
        [SerializeField] private bool autoStartGame = false;
        
        [Header("AI Settings")]
        [SerializeField] private float aiMoveDelay = 2.0f;
        [SerializeField] private AIDifficulty aiDifficulty = AIDifficulty.Medium;

        private GameState gameState;
        private List<Move> currentValidMoves;
        private RoundManager roundManager;
        private CirullaAI cirullaAI;
        
        /// <summary>
        /// Event fired quando un player esegue una mossa.
        /// NetworkGameController si subscribe a questo evento per sincronizzare via RPC.
        /// </summary>
        public event System.Action<Move> OnMoveExecuted;
        
        /// <summary>
        /// Event fired quando un player (umano locale) vuole eseguire una mossa.
        /// NetworkGameController si subscribe per inviare la mossa via RPC.
        /// </summary>
        public event System.Action<Move> OnLocalPlayerMoveRequested;

        public GameState GameState => gameState;
        public int CurrentPlayerIndex => gameState?.CurrentPlayerIndex ?? -1;
        
        /// <summary>
        /// Check if current player is a human player using GameModeService.
        /// </summary>
        public bool IsHumanPlayerTurn
        {
            get
            {
                if (CurrentPlayerIndex < 0) return false;
                return GameModeService.Current.IsHumanPlayer(CurrentPlayerIndex);
            }
        }

        private void Start()
        {
            // Cache manager references if not assigned
            if (cardViewManager == null)
            {
                cardViewManager = FindObjectOfType<CardViewManager>();
            }
            
            if (capturedPileManager == null)
            {
                capturedPileManager = FindObjectOfType<CapturedPileManager>();
            }
            
            // Initialize AI
            if (cirullaAI == null)
            {
                cirullaAI = new CirullaAI(aiDifficulty);
            }
            
            var provider = GameModeService.Current;
            
            if (provider.IsMultiplayer)
            {
                if (provider.IsMasterClient)
                {
                    Debug.Log("<color=cyan>[MP] Master Client starting game...</color>");
                    StartNewGame();
                }
                else
                {
                    Debug.Log("<color=cyan>[MP] Waiting for Master Client to start game...</color>");
                }
                return;
            }
            
            // Single-player or no GameManager found
            if (autoStartGame)
            {
                StartNewGame();
            }
        }

        // ==== QA Helpers (Context Menu) ====
        [ContextMenu("Setup Matta: Decino in mano (coppia)")]
        private void SetupMattaDecino_Context()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Bastoni, 6),
                new Card(Suit.Denari, 6), // coppia -> decino
            };
            var table = new List<Card>();
            SetupScenarioForCurrentPlayer(hand, table);
        }

        [ContextMenu("Setup Matta: Accuso in mano (somma<=9)")]
        private void SetupMattaAceCapture_Context()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Bastoni, 5),
                new Card(Suit.Denari, 3), // 5+3+1=9 -> accuso
            };
            var table = new List<Card>();
            SetupScenarioForCurrentPlayer(hand, table);
        }

        [ContextMenu("Setup Matta: Nessun hint (no decino/accuso)")]
        private void SetupMattaCapture15_Context()
        {
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7), // Matta
                new Card(Suit.Bastoni, 6),
                new Card(Suit.Denari, 3), // 6+3+1=10 -> nessun accuso e non è coppia
            };
            var table = new List<Card>();
            SetupScenarioForCurrentPlayer(hand, table);
        }

    /// <summary>
    /// Starts a new game of Cirulla/51.
    /// </summary>
    public void StartNewGame()
    {
        // Reset GameState to ensure consistent initialization
        gameState = null;

        // Initialize AI if not already done
        if (cirullaAI == null)
        {
            cirullaAI = new CirullaAI(aiDifficulty);
        }
        
        // starting new game
        gameState = Rules51.CreateNewGame();
        roundManager = new RoundManager(gameState);
        
        // Subscribe to events
        roundManager.OnNewHandsDealt += CheckAndDeclareAccusiForAllPlayers; // For mid-game redeals
        // Do NOT declare on initial hands immediately; we'll handle it after a short visual delay
        
        roundManager.StartSmazzata();
        
        // IMPORTANT: Force immediate visual refresh AFTER StartSmazzata
        // This ensures dealer 15/30 accuso (which removes table cards) happens BEFORE rendering
        // Otherwise cards might be visible for 0.5 seconds before disappearing
        if (cardViewManager != null)
        {
            cardViewManager.ForceRefresh();
        }

        // Delay the initial accusi declaration slightly so all clients see stable visuals first
        // In multiplayer, only Master Client will perform declaration and sync via RPC
        StartCoroutine(DeclareInitialAccusiWithDelay());

        // Compute valid moves for the current player
        RefreshValidMoves();

        // MULTIPLAYER: If we're Master Client, send GameState to all clients
        var provider = GameModeService.Current;
        if (provider.IsMultiplayer && provider.IsMasterClient)
        {
            // Find NetworkGameController and send GameState via reflection (to avoid circular dependency)
            var netControllerType = System.Type.GetType("Project51.Networking.NetworkGameController, Project51.Networking");
            if (netControllerType != null)
            {
                var netInstanceProp = netControllerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var netController = netInstanceProp?.GetValue(null);
                if (netController != null)
                {
                    var sendMethod = netControllerType.GetMethod("SendInitialGameState");
                    sendMethod?.Invoke(netController, new object[] { gameState });
                    Debug.Log("<color=cyan>[MP] Master sent initial GameState to all clients</color>");
                }
            }
        }

        // If it's an AI player's turn, have them play automatically
        if (!IsHumanPlayerTurn)
        {
            Invoke(nameof(ExecuteAITurn), aiMoveDelay); // Delay for visibility
        }
    }

        /// <summary>
        /// Performs initial accusi declaration after a short delay to avoid early visual pop.
        /// In multiplayer, only Master Client will execute and sync.
        /// </summary>
        private System.Collections.IEnumerator DeclareInitialAccusiWithDelay()
        {
            // Small delay for UI stabilization
            yield return new UnityEngine.WaitForSeconds(0.25f);

            var provider = GameModeService.Current;
            
            // In multiplayer, ensure only Master executes initial accusi
            if (provider.IsMultiplayer && !provider.IsMasterClient)
            {
                yield break;
            }

            CheckAndDeclareAccusiForAllPlayers();
        }

        /// <summary>
        /// Refreshes the list of valid moves for the current player.
        /// </summary>
        private void RefreshValidMoves()
        {
            if (gameState == null || gameState.RoundEnded)
            {
                currentValidMoves = new List<Move>();
                return;
            }

            currentValidMoves = Rules51.GetValidMoves(gameState, gameState.CurrentPlayerIndex);
            // player valid moves count
        }

        /// <summary>
        /// Executes a move. Called by human player UI or AI logic.
        /// In multiplayer, local player moves trigger OnLocalPlayerMoveRequested event.
        /// </summary>
        /// <param name="move">The move to execute</param>
        /// <param name="fromNetwork">True if this call originated from a network RPC (prevents re-broadcasting)</param>
        public void ExecuteMove(Move move, bool fromNetwork = false)
        {
            if (gameState == null || gameState.RoundEnded)
            {
                // cannot execute move: game not active
                return;
            }

            var provider = GameModeService.Current;

            // Multiplayer: check if this move needs to be sent via network
            // SKIP if this call is already from network to prevent infinite loop!
            if (!fromNetwork && provider.IsMultiplayer)
            {
                bool isLocalPlayer = provider.IsLocalPlayer(move.PlayerIndex);
                bool isHuman = provider.IsHumanPlayer(move.PlayerIndex);
                bool isBot = provider.IsBotPlayer(move.PlayerIndex);

                if (isLocalPlayer && isHuman)
                {
                    // Local human player - trigger event for NetworkGameController
                    OnLocalPlayerMoveRequested?.Invoke(move);
                    return; // Network controller will call ExecuteMove again via RPC
                }

                if (isBot)
                {
                    if (!provider.IsMasterClient)
                    {
                        Debug.LogWarning("Non-master client tried to execute bot move - ignoring.");
                        return;
                    }
                    
                    // Master Client executes bot move and triggers event
                    OnLocalPlayerMoveRequested?.Invoke(move);
                    return; // Network controller will call ExecuteMove again via RPC
                }
            }

            // Ensure valid moves list is initialized (clients joining mid-game via RPC can hit null)
            if (currentValidMoves == null)
            {
                RefreshValidMoves();
                if (currentValidMoves == null)
                {
                    currentValidMoves = new List<Move>();
                }
            }

            // If move is not in precomputed valid moves, allow a human PlayOnly to be forced
            if (!currentValidMoves.Contains(move))
            {
                bool allowForcedHumanPlayOnly = move.Type == MoveType.PlayOnly
                    && move.PlayerIndex == provider.LocalPlayerIndex
                    && gameState.Players[move.PlayerIndex].Hand.Contains(move.PlayedCard);

                if (!allowForcedHumanPlayOnly)
                {
                    // invalid move
                    return;
                }

                // forced PlayOnly accepted
            }

            // executing move
            // Use RoundManager to apply move (handles scopa, dealing, end of smazzata)
            if (roundManager == null && gameState != null)
            {
                // Client may receive RPC before local roundManager is set up
                roundManager = new RoundManager(gameState);
            }
            roundManager.ApplyMove(move);
            // Refresh captured piles visuals after move
            if (capturedPileManager != null)
            {
                capturedPileManager.ForceRefresh();
            }
            // Notify listeners (e.g. UI) that a move was executed so they can animate
            OnMoveExecuted?.Invoke(move);

            // If RoundManager set RoundEnded, handle end
            if (gameState.RoundEnded) 
            { 
                ShowRoundEndPanel();
                return; 
            }

            // Refresh valid moves for the next player
            RefreshValidMoves();

            // If next player is AI, execute their turn
            // In multiplayer, only Master Client executes AI turns
            if (!IsHumanPlayerTurn)
            {
                bool shouldExecuteAI = !provider.IsMultiplayer || provider.IsMasterClient;

                if (shouldExecuteAI)
                {
                    Invoke(nameof(ExecuteAITurn), aiMoveDelay);
                }
            }
        }

        /// <summary>
        /// Deals 3 new cards to each player from the deck (no new table cards).
        /// </summary>
        private void DealNewHands()
        {
            // dealing new hands

            // Deal clockwise: start from player to the left of dealer
            int firstPlayerIndex = (gameState.DealerIndex - 1 + gameState.NumPlayers) % gameState.NumPlayers;
            for (int round = 0; round < 3; round++)
            {
                for (int offset = 0; offset < gameState.NumPlayers; offset++)
                {
                    int playerIndex = (firstPlayerIndex + offset) % gameState.NumPlayers;
                    if (gameState.Deck.Count > 0)
                    {
                        var card = gameState.Deck[0];
                        gameState.Deck.RemoveAt(0);
                        gameState.Players[playerIndex].Hand.Add(card);
                    }
                }
            }

            // new hands dealt
            
            // Check for automatic accusi declaration after dealing new hands
            CheckAndDeclareAccusiForAllPlayers();
        }

        /// <summary>
        /// Ends the current round, assigns remaining table cards, and computes scores.
        /// </summary>
        private void EndRound()
        {
            // round ended, computing scores

            // Assign remaining table cards to last capture player
            if (gameState.LastCapturePlayerIndex >= 0 && gameState.Table.Count > 0)
            {
                var lastCapturePlayer = gameState.Players[gameState.LastCapturePlayerIndex];
                foreach (var card in gameState.Table)
                {
                    lastCapturePlayer.CapturedCards.Add(card);
                }
                gameState.Table.Clear();

                // remaining table cards assigned
            }

            // TODO: Implement full scoring (Scopa, Sette Bello, Primiera, Denari, Cards, Grande, Piccola, Cappotto)
            // For now, just log captured cards and Scopa counts
            for (int i = 0; i < gameState.NumPlayers; i++)
            {
                var player = gameState.Players[i];
                // player capture/scopa summary
            }

            gameState.RoundEnded = true;
        }

        /// <summary>
        /// AI uses CirullaAI for strategic move selection.
        /// Executes the move with a delay to show the played card before capture.
        /// </summary>
        private void ExecuteAITurn()
        {
            if (currentValidMoves == null || currentValidMoves.Count == 0)
            {
                // AI no valid moves
                return;
            }

            // Use strategic AI to choose move
            Move chosenMove = cirullaAI?.ChooseMove(gameState, gameState.CurrentPlayerIndex, currentValidMoves);
            
            // Fallback if AI returns null
            if (chosenMove == null)
            {
                var captureMoves = currentValidMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();
                chosenMove = captureMoves.Count > 0 
                    ? captureMoves[Random.Range(0, captureMoves.Count)]
                    : currentValidMoves[Random.Range(0, currentValidMoves.Count)];
            }

            // If the move involves a capture, execute in two phases:
            // 1. Play the card to the table (so it's visible)
            // 2. Wait, then complete the capture
            if (chosenMove.Type != MoveType.PlayOnly && chosenMove.CapturedCards != null && chosenMove.CapturedCards.Count > 0)
            {
                StartCoroutine(ExecuteAITurnWithDelay(chosenMove));
            }
            else
            {
                // No capture, execute immediately
                ExecuteMove(chosenMove);
            }
        }

        /// <summary>
        /// Executes an AI capture move with a delay to show the played card.
        /// </summary>
        private System.Collections.IEnumerator ExecuteAITurnWithDelay(Move move)
        {
            // Phase 1: Temporarily add the played card to the table so it's visible
            var playedCard = move.PlayedCard;
            var botPlayer = gameState.Players[move.PlayerIndex];
            
            // Remove from bot hand and add to table temporarily
            botPlayer.Hand.Remove(playedCard);
            gameState.Table.Add(playedCard);
            
            // Wait for player to see the card (1.5 seconds)
            // The card will be flipped to face-up automatically by RefreshCardViews
            yield return new UnityEngine.WaitForSeconds(1.5f);
            
            // Phase 2: Remove from table and add back to hand, then execute real move
            gameState.Table.Remove(playedCard);
            botPlayer.Hand.Add(playedCard);
            
            // Now execute the actual capture move
            ExecuteMove(move);
        }

        /// <summary>
        /// Checks all players' hands and automatically declares accusi (Decino/Cirulla) if possible.
        /// This is called at the start of each hand distribution.
        /// </summary>
        private void CheckAndDeclareAccusiForAllPlayers()
        {
            if (roundManager == null || gameState == null) return;

            for (int i = 0; i < gameState.NumPlayers; i++)
            {
                var hand = gameState.Players[i].Hand;
                
                // Check for Decino first (higher priority - 10 points)
                if (AccusiChecker.IsDecino(hand))
                {
                    bool declared = roundManager.TryPlayerAccuso(i, AccusoType.Decino);
                    if (declared)
                    {
                        Debug.Log($"Player {i} declared DECINO!");
                        // Sync accuso to clients in multiplayer
                        TrySendAccusoSync(i, AccusoType.Decino);
                    }
                    continue; // Don't check for Cirulla if Decino was declared
                }

                // Check for Cirulla (3 points)
                if (AccusiChecker.IsCirulla(hand))
                {
                    bool declared = roundManager.TryPlayerAccuso(i, AccusoType.Cirulla);
                    if (declared)
                    {
                        Debug.Log($"Player {i} declared CIRULLA!");
                        // Sync accuso to clients in multiplayer
                        TrySendAccusoSync(i, AccusoType.Cirulla);
                    }
                }
            }
            // Refresh piles to show accusi badges/updates
            if (capturedPileManager != null)
            {
                capturedPileManager.ForceRefresh();
            }
            // Force card view refresh so hands with accuso are shown face-up
            if (cardViewManager != null)
            {
                cardViewManager.ForceRefresh();
            }
        }

        private void TrySendAccusoSync(int playerIndex, AccusoType type)
        {
            var provider = GameModeService.Current;
            if (!provider.IsMultiplayer) return;

            // Use reflection to call NetworkGameController.SendAccuso (to avoid circular dependency)
            var netControllerType = System.Type.GetType("Project51.Networking.NetworkGameController, Project51.Networking");
            if (netControllerType != null)
            {
                var netInstanceProp = netControllerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var netController = netInstanceProp?.GetValue(null);
                if (netController != null)
                {
                    var sendMethod = netControllerType.GetMethod("SendAccuso", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    sendMethod?.Invoke(netController, new object[] { playerIndex, (int)type });
                }
            }
        }

        /// <summary>
        /// Returns valid moves for the current player (used by UI to show options).
        /// </summary>
        public List<Move> GetCurrentValidMoves()
        {
            return currentValidMoves ?? new List<Move>();
        }

        /// <summary>
        /// Shows the round end panel with scores.
        /// </summary>
        private void ShowRoundEndPanel()
        {
            if (roundEndPanel != null)
            {
                roundEndPanel.OnContinueClicked += OnRoundEndContinue;
                roundEndPanel.OnMainMenuClicked += OnRoundEndMainMenu;
                roundEndPanel.Show(gameState);
            }
            else
            {
                // Fallback: log scores to console
                var scores = PunteggioManager.CalculateSmazzataScores(gameState);
                for (int i = 0; i < gameState.NumPlayers; i++)
                {
                    Debug.Log($"Player {i}: {scores[i]} points (Scope: {gameState.Players[i].ScopaCount})");
                }
            }
        }

        private void OnRoundEndContinue()
        {
            if (roundEndPanel != null)
            {
                roundEndPanel.OnContinueClicked -= OnRoundEndContinue;
                roundEndPanel.OnMainMenuClicked -= OnRoundEndMainMenu;
                roundEndPanel.Hide();
            }
            
            // Start a new round (new smazzata)
            StartNewGame();
        }

        private void OnRoundEndMainMenu()
        {
            if (roundEndPanel != null)
            {
                roundEndPanel.OnContinueClicked -= OnRoundEndContinue;
                roundEndPanel.OnMainMenuClicked -= OnRoundEndMainMenu;
                roundEndPanel.Hide();
            }
            
            // Load main menu scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Returns the valid moves for the current player that use the specified card.
        /// Useful for UI to present options when the player selects a card.
        /// </summary>
        public List<Move> GetMovesForCard(Card card)
        {
            if (currentValidMoves == null) return new List<Move>();
            return currentValidMoves.Where(m => m.PlayedCard.Equals(card)).ToList();
        }

        /// <summary>
        /// Debug/QA helper: set a deterministic scenario for the current player.
        /// Use this to verify Matta behavior without restarting the game.
        /// Provide exact cards for the current player's hand and the table.
        /// </summary>
        public void SetupScenarioForCurrentPlayer(List<Card> handCards, List<Card> tableCards)
        {
            if (gameState == null) return;
            // Force human player's turn for testing scenarios
            gameState.CurrentPlayerIndex = 0;
            var player = gameState.Players[0];
            player.Hand.Clear();
            if (handCards != null) player.Hand.AddRange(handCards);

            gameState.Table.Clear();
            if (tableCards != null) gameState.Table.AddRange(tableCards);

            // Clear moves and recompute
            currentValidMoves = Rules51.GetValidMoves(gameState, 0);

            // Cancel any pending AI invokes
            CancelInvoke();
            // Ensure normal time scale
            Time.timeScale = 1f;

            // Notify UI to refresh views immediately
            if (cardViewManager != null)
            {
                cardViewManager.ForceRefresh();
            }
            else
            {
                OnMoveExecuted?.Invoke(null);
            }
        }

        /// <summary>
        /// Called by UI when the player double-clicks a card: if no capture is possible for this card,
        /// perform a forced PlayOnly (discard). If captures exist, ignores the double-click.
        /// </summary>
        public void OnPlayerDoubleClick(Card card)
        {
            var moves = GetMovesForCard(card);
            // If any capture move exists, ignore double-click (must choose capture)
            if (moves.Any(m => m.Type != MoveType.PlayOnly))
            {
                // double-click ignored: capture available
                return;
            }

            // Otherwise perform a forced PlayOnly
            var playOnly = new Move(0, card, MoveType.PlayOnly);
            ExecuteMove(playOnly);
        }

        /// <summary>
        /// Called by UI when the player confirms a specific move for a selected card (index into moves list).
        /// </summary>
        public void OnPlayerConfirmMove(Card card, int moveIndex)
        {
            var moves = GetMovesForCard(card);
            if (moveIndex < 0 || moveIndex >= moves.Count) { return; }
            ExecuteMove(moves[moveIndex]);
        }

        /// <summary>
        /// Called by UI when the player drags a card onto table and releases over a set of table cards.
        /// The UI should provide the list of table cards targeted for capture (may be empty).
        /// </summary>
        public void OnPlayerDragPlay(Card playedCard, List<Card> targetTableCards)
        {
            var moves = GetMovesForCard(playedCard);
            if (moves == null || moves.Count == 0) { return; }
            // Let the rules engine validate the manual selection (handles matta, forced-capture rules, ace rules, etc.)
            var matches = Rules51.GetMatchingMovesFromSelection(gameState, gameState.CurrentPlayerIndex, playedCard, targetTableCards);
            if (matches.Count == 1)
            {
                ExecuteMove(matches[0]);
                return;
            }
            else if (matches.Count > 1)
            {
                // Multiple equivalent moves (e.g., matta assignments). Let the player choose.
                if (moveSelectionUI != null)
                {
                    var desc = matches.Select(m => m.ToString()).ToList();
                    moveSelectionUI.ShowMoves(desc, idx =>
                    {
                        if (idx >= 0 && idx < matches.Count)
                        {
                            ExecuteMove(matches[idx]);
                        }
                    });
                    return;
                }
                else
                {
                    // fallback: choose first
                    ExecuteMove(matches[0]);
                    return;
                }
            }

            // No matches -> show invalid feedback
            if (moveSelectionUI != null)
            {
                moveSelectionUI.ShowInvalid("Invalid selection");
            }
            else
            {
                // dragged play did not match any valid move
            }
        }
    }
}

