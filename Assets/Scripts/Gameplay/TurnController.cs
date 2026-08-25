using UnityEngine;
using Project51.Core;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

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
        [SerializeField] private CardAnimationController cardAnimationController;
        
        /// <summary>
        /// Sets the CardViewManager reference at runtime. Useful for tests.
        /// </summary>
        public void SetCardViewManager(CardViewManager manager)
        {
            cardViewManager = manager;
        }

        /// <summary>
        /// Imposta lo stato di gioco ricevuto dalla rete e inizializza RoundManager per il client.
        /// Sostituisce il precedente accesso via reflection da NetworkGameController.
        /// </summary>
        public void SetNetworkGameState(GameState newGameState)
        {
            gameState = newGameState;
            roundManager = new RoundManager(gameState);
            RefreshValidMoves();
            cardViewManager?.ForceRefresh();
            OnMoveExecuted?.Invoke(null);
        }

        [Header("Game Settings")]
        [SerializeField] private bool autoStartGame = false;
        
        [Header("AI Settings")]
        [SerializeField] private float aiMoveDelay = 2.0f;
        [SerializeField] private AIDifficulty aiDifficulty = AIDifficulty.Medium;

        [Header("Redeal Animation")]
        [SerializeField] private float redealStartDelay = 0.2f;

        [Header("Played Card Feedback")]
        [SerializeField] private float playedCardCrossfadeDuration = 0.08f;

        private GameState gameState;
        private List<Move> currentValidMoves;
        private RoundManager roundManager;
        private CirullaAI cirullaAI;
        private bool isMoveAnimationInProgress;
        private bool isRedealPendingVisual;
        private bool isRedealAnimationInProgress;
        private readonly List<Transform> pendingRedealVisualCopies = new List<Transform>();
        
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

            if (cardAnimationController == null)
            {
                cardAnimationController = FindObjectOfType<CardAnimationController>();
                if (cardAnimationController == null)
                {
                    cardAnimationController = gameObject.AddComponent<CardAnimationController>();
                }
            }
            
            // Initialize AI
            if (cirullaAI == null)
            {
                cirullaAI = new CirullaAI(aiDifficulty);
            }

            // Check if GameSceneInitializer exists OR if ActiveConfig is already set
            // If GameSceneInitializer exists, it will call StartNewGame() when ready
            var gameSceneInitializer = FindObjectOfType<GameSceneInitializer>();
            if (gameSceneInitializer != null)
            {
                Debug.Log("[TurnController] GameSceneInitializer found - deferring game start to it.");
                return;
            }

            // Also check if ActiveConfig is set (means GameSceneInitializer.Awake already ran)
            if (GameSceneInitializer.ActiveConfig != null)
            {
                Debug.Log("[TurnController] ActiveConfig already set by GameSceneInitializer - deferring to it.");
                return;
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
            
            // Single-player or no GameManager found - only auto-start if explicitly enabled
            // AND we're not expecting a GameSceneInitializer
            if (autoStartGame)
            {
                Debug.Log("[TurnController] No GameSceneInitializer and autoStartGame=true - starting with defaults.");
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

        // Determine player count from MatchConfig (loaded by GameSceneInitializer)
        int numPlayers = 4;
        var cfg = GameSceneInitializer.ActiveConfig;
        if (cfg != null)
        {
            numPlayers = cfg.PlayerCount;
            Debug.Log($"[TurnController] Starting game with {numPlayers} players (from config: {cfg.Format})");
        }
        else
        {
            Debug.LogWarning("[TurnController] GameSceneInitializer.ActiveConfig is null! Defaulting to 4 players.");
        }

        // starting new game
        gameState = Rules51.CreateNewGame(numPlayers);
        Debug.Log($"[TurnController] GameState created: {gameState.NumPlayers} players, dealer={gameState.DealerIndex}, current={gameState.CurrentPlayerIndex}");
        
        roundManager = new RoundManager(gameState);
        
        // Subscribe to events
        roundManager.OnNewHandsDealt += HandleNewHandsDealt; // For mid-game redeals with staged reveal
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

            CheckAndDeclareAccusiForAllPlayers(refreshVisuals: true);
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
            if (move == null || gameState == null || gameState.RoundEnded || isMoveAnimationInProgress || isRedealPendingVisual || isRedealAnimationInProgress)
            {
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
                    OnLocalPlayerMoveRequested?.Invoke(move);
                    return;
                }

                if (isBot)
                {
                    if (!provider.IsMasterClient)
                    {
                        Debug.LogWarning("Non-master client tried to execute bot move - ignoring.");
                        return;
                    }
                    
                    OnLocalPlayerMoveRequested?.Invoke(move);
                    return;
                }
            }

            // Ensure valid moves list is initialized
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
                    return;
                }
            }

            StartCoroutine(ExecuteMoveWithAnimation(move, fromNetwork));
        }

        /// <summary>
        /// Animates the played card to the table, optionally previews the capture, then commits the move once.
        /// </summary>
        private System.Collections.IEnumerator ExecuteMoveWithAnimation(Move move, bool fromNetwork)
        {
            isMoveAnimationInProgress = true;

            var hiddenRenderers = new List<SpriteRenderer>();
            var visualCopies = new List<Transform>();
            Transform playedVisualForCrossfade = null;
            SpriteRenderer playedVisualRendererForCrossfade = null;

            try
            {
                if (cardAnimationController != null && cardViewManager != null &&
                    cardViewManager.TryGetCardView(move.PlayedCard, out var playedCardView) &&
                    cardAnimationController.TryCreateVisualCopy(
                        playedCardView.transform,
                        playedCardView.CardRenderer,
                        $"PlayedCardAnimation_{move.PlayedCard.Suit}_{move.PlayedCard.Rank}",
                        out var playedVisual,
                        out var playedVisualRenderer))
                {
                    visualCopies.Add(playedVisual);
                    Sprite faceSprite = cardViewManager.GetSpriteForCard(move.PlayedCard);
                    if (faceSprite != null)
                    {
                        playedVisualRenderer.sprite = faceSprite;
                    }

                    playedCardView.CardRenderer.enabled = false;
                    hiddenRenderers.Add(playedCardView.CardRenderer);

                    Vector3 tableTarget = cardViewManager.GetNextTableCardPosition(gameState.Table.Count);
                    float tableScale = cardViewManager.GetTableCardScale();
                    Sequence playSequence = cardAnimationController.PlayCardToTable(
                        playedVisual,
                        playedVisualRenderer,
                        tableTarget,
                        0f,
                        tableScale);
                    yield return playSequence.WaitForCompletion();

                    bool isCapture = move.Type != MoveType.PlayOnly && move.CapturedCards != null && move.CapturedCards.Count > 0;
                    if (isCapture)
                    {
                        var capturedTransforms = new List<Transform>();
                        var capturedRenderers = new List<SpriteRenderer>();
                        bool canAnimateCapture = true;

                        foreach (var capturedCard in move.CapturedCards)
                        {
                            if (!cardViewManager.TryGetCardView(capturedCard, out var capturedCardView) ||
                                !cardAnimationController.TryCreateVisualCopy(
                                    capturedCardView.transform,
                                    capturedCardView.CardRenderer,
                                    $"CapturedCardAnimation_{capturedCard.Suit}_{capturedCard.Rank}",
                                    out var capturedVisual,
                                    out var capturedVisualRenderer))
                            {
                                canAnimateCapture = false;
                                break;
                            }

                            visualCopies.Add(capturedVisual);
                            capturedCardView.CardRenderer.enabled = false;
                            hiddenRenderers.Add(capturedCardView.CardRenderer);
                            capturedTransforms.Add(capturedVisual);
                            capturedRenderers.Add(capturedVisualRenderer);
                        }

                        if (canAnimateCapture)
                        {
                            yield return cardAnimationController.CreateCapturePreview().WaitForCompletion();

                            int playerCount = gameState.NumPlayers;
                            int localPlayerIndex = GameModeService.Current.LocalPlayerIndex;
                            int relativePlayerIndex = (move.PlayerIndex - localPlayerIndex + playerCount) % playerCount;
                            Vector3 pileTarget = cardViewManager.GetPlayerHandAnchor(relativePlayerIndex, playerCount)
                                + cardViewManager.GetPlayerRightDirection(relativePlayerIndex, playerCount) * cardViewManager.GetCapturedPileDistance()
                                + cardViewManager.GetCapturedPileScreenDownOffset();

                            Sequence captureSequence = cardAnimationController.CaptureSequence(
                                playedVisual,
                                playedVisualRenderer,
                                capturedTransforms,
                                capturedRenderers,
                                pileTarget);
                            yield return captureSequence.WaitForCompletion();
                        }
                    }
                    else
                    {
                        playedVisualForCrossfade = playedVisual;
                        playedVisualRendererForCrossfade = playedVisualRenderer;
                    }
                }

                ApplyMoveInternal(move, fromNetwork);

                if (playedVisualRendererForCrossfade != null && !isRedealPendingVisual)
                {
                    yield return DOTween.ToAlpha(
                        () => playedVisualRendererForCrossfade.color,
                        c => playedVisualRendererForCrossfade.color = c,
                        0f,
                        playedCardCrossfadeDuration).WaitForCompletion();
                }
            }
            finally
            {
                bool shouldRestoreHiddenRenderers = !isRedealPendingVisual;
                if (shouldRestoreHiddenRenderers)
                {
                    foreach (var renderer in hiddenRenderers)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = true;
                        }
                    }
                }

                if (isRedealPendingVisual)
                {
                    pendingRedealVisualCopies.AddRange(visualCopies);
                }
                else
                {
                    foreach (var visualCopy in visualCopies)
                    {
                        cardAnimationController?.DestroyVisualCopy(visualCopy);
                    }
                }

                isMoveAnimationInProgress = false;
            }
        }

        /// <summary>
        /// Internal method that applies a move to the game state.
        /// Called either directly (for PlayOnly) or after visual delay (for captures).
        /// </summary>
        private void ApplyMoveInternal(Move move, bool fromNetwork)
        {
            // Use RoundManager to apply move (handles scopa, dealing, end of smazzata)
            if (roundManager == null && gameState != null)
            {
                roundManager = new RoundManager(gameState);
            }
            roundManager.ApplyMove(move);

            if (!isRedealPendingVisual && cardViewManager != null)
            {
                cardViewManager.ForceRefresh();
            }
            
            // Refresh captured piles visuals after move
            if (capturedPileManager != null)
            {
                capturedPileManager.ForceRefresh();
            }
            
            // Notify listeners that a move was executed so they can animate
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
            var provider = GameModeService.Current;
            if (!IsHumanPlayerTurn && !isRedealPendingVisual && !isRedealAnimationInProgress)
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
        /// Legacy helper retained for QA scenarios.
        /// </summary>
        private void DealNewHands()
        {
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

            HandleNewHandsDealt();
        }

        private void HandleNewHandsDealt()
        {
            if (isRedealAnimationInProgress)
            {
                return;
            }

            isRedealPendingVisual = true;
            StartCoroutine(HandleNewHandsRevealSequence());
        }

        private System.Collections.IEnumerator HandleNewHandsRevealSequence()
        {
            isRedealAnimationInProgress = true;

            try
            {
                while (isMoveAnimationInProgress)
                {
                    yield return null;
                }

                if (redealStartDelay > 0f)
                {
                    yield return new WaitForSeconds(redealStartDelay);
                }

                CheckAndDeclareAccusiForAllPlayers(refreshVisuals: false);

                if (cardViewManager != null)
                {
                    cardViewManager.ForceRefresh();
                }

                if (cardAnimationController != null && pendingRedealVisualCopies.Count > 0)
                {
                    foreach (var visualCopy in pendingRedealVisualCopies)
                    {
                        cardAnimationController.DestroyVisualCopy(visualCopy);
                    }
                    pendingRedealVisualCopies.Clear();
                }

                if (cardAnimationController != null && cardViewManager != null && gameState != null)
                {
                    var handViews = new List<CardView>();
                    for (int i = 0; i < gameState.NumPlayers; i++)
                    {
                        foreach (var card in gameState.Players[i].Hand)
                        {
                            if (cardViewManager.TryGetCardView(card, out var cardView) && cardView != null)
                            {
                                handViews.Add(cardView);
                            }
                        }
                    }

                    if (handViews.Count > 0)
                    {
                        yield return cardAnimationController.PlayDealtCardsReveal(handViews).WaitForCompletion();
                    }
                }

                if (capturedPileManager != null)
                {
                    capturedPileManager.ForceRefresh();
                }

                if (cardViewManager != null)
                {
                    cardViewManager.ForceRefresh();
                }
            }
            finally
            {
                isRedealPendingVisual = false;
                isRedealAnimationInProgress = false;
            }

            RefreshValidMoves();

            if (!IsHumanPlayerTurn)
            {
                var provider = GameModeService.Current;
                bool shouldExecuteAI = !provider.IsMultiplayer || provider.IsMasterClient;
                if (shouldExecuteAI)
                {
                    CancelInvoke(nameof(ExecuteAITurn));
                    Invoke(nameof(ExecuteAITurn), aiMoveDelay);
                }
            }
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
        /// Animation is handled locally by ExecuteMoveWithAnimation/CardAnimationController;
        /// in multiplayer the move is broadcast via OnLocalPlayerMoveRequested and replayed
        /// on each client through ExecuteMove(move, fromNetwork: true).
        /// </summary>
        private void ExecuteAITurn()
        {
            if (currentValidMoves == null || currentValidMoves.Count == 0)
            {
                return;
            }

            Move chosenMove = cirullaAI?.ChooseMove(gameState, gameState.CurrentPlayerIndex, currentValidMoves);
            
            if (chosenMove == null)
            {
                var captureMoves = currentValidMoves.Where(m => m.Type != MoveType.PlayOnly).ToList();
                chosenMove = captureMoves.Count > 0 
                    ? captureMoves[Random.Range(0, captureMoves.Count)]
                    : currentValidMoves[Random.Range(0, currentValidMoves.Count)];
            }

            // Execute move: in singleplayer this runs ExecuteMoveWithAnimation directly;
            // in multiplayer it raises OnLocalPlayerMoveRequested so NetworkGameController
            // can broadcast the move via RPC, which each client then applies locally.
            ExecuteMove(chosenMove);
        }

        /// <summary>
        /// Checks all players' hands and automatically declares accusi (Decino/Cirulla) if possible.
        /// This is called at the start of each hand distribution.
        /// </summary>
        private void CheckAndDeclareAccusiForAllPlayers(bool refreshVisuals)
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
            if (!refreshVisuals)
            {
                return;
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
            
            // Use AppFlowManager for centralized navigation
                    AppFlowManager.GoToMainMenu();
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

